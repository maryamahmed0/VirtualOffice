using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using System.Text;
using System.Collections;
using UnityEngine.Networking;
using System.Net; 

// ---------------------------------------------------
// 1. Data Models
// ---------------------------------------------------
[System.Serializable]
public class MeetingCreateRequest
{
    public string orgCode;
    public string roomId;
    public string vivoxChannelName;
}

public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData) => true;
}

// ---------------------------------------------------
// 2. Relay Connector
// ---------------------------------------------------
public class RelayConnector : MonoBehaviour
{
    [SerializeField] private UnityTransport transport;

    public string CurrentJoinCode { get; private set; }
    private static string RelayProtocol => "wss";
    private bool _isCurrentSessionHost = false;

    private const string BASE_API_URL = "https://localhost:7080/api";

    private void Awake()
    {
        if (transport == null) transport = GetComponent<UnityTransport>();
        if (transport == null) transport = FindFirstObjectByType<UnityTransport>(FindObjectsInactive.Include);
        if (transport == null) Debug.LogError("[RelayConnector] UnityTransport NOT FOUND!");

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnTransportFailure += () => Debug.LogError("[NET] TransportFailure!");
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted +=
                    (sceneName, mode, completed, timedOut) =>
                    Debug.Log($"[NET][Scene] LoadCompleted scene={sceneName}");
            }
        }
    }

    #region Disconnect & Cleanup Logic

    public void DisconnectAndLeave()
    {
        Debug.Log("[RelayConnector] Manual disconnect initiated.");
        if (_isCurrentSessionHost) TriggerDeleteMeetingApi();

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (_isCurrentSessionHost && clientId == NetworkManager.ServerClientId)
        {
            Debug.Log("[RelayConnector] Host disconnected via Event.");
            TriggerDeleteMeetingApi();
        }
    }

    private void OnApplicationQuit()
    {
        if (_isCurrentSessionHost) TriggerDeleteMeetingApi();
    }

    private void OnDestroy()
    {
        if (_isCurrentSessionHost) TriggerDeleteMeetingApi();
    }

    private void TriggerDeleteMeetingApi()
    {
        if (string.IsNullOrEmpty(CurrentJoinCode) || !_isCurrentSessionHost) return;

        _isCurrentSessionHost = false;

        SendDeleteRequestSync(CurrentJoinCode);
    }

    #endregion

    #region API Calls

    private async Task PostMeetingApiAsync(string orgCode, string roomId)
    {
        string url = $"{BASE_API_URL}/meetings";

        var requestData = new MeetingCreateRequest
        {
            orgCode = orgCode,
            roomId = roomId,
            vivoxChannelName = VoiceChannelUtil.Build(orgCode, roomId)
        };

        string jsonBody = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.certificateHandler = new BypassCertificate();

        req.SetRequestHeader("Authorization", "Bearer " + AuthBridge.Token);
        req.SetRequestHeader("Content-Type", "application/json");

        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();

        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log($"[API] Meeting created successfully RoomId: {roomId}");
        else
            Debug.LogError($"[API] Failed to create meeting: {req.error}\n{req.downloadHandler.text}");
    }

    private async Task DeleteMeetingApiAsync(string roomId)
    {
        if (string.IsNullOrEmpty(roomId)) return;

        string url = $"{BASE_API_URL}/meetings/{roomId}";

        using var req = UnityWebRequest.Delete(url);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.certificateHandler = new BypassCertificate();

        req.SetRequestHeader("Authorization", "Bearer " + AuthBridge.Token);
        req.SetRequestHeader("Content-Type", "application/json");

        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();

        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log($"[API] Stuck Meeting ({roomId}) deleted via Async ");
        else
            Debug.LogWarning($"[API] Delete meeting failed: {req.error}");
    }

    private void SendDeleteRequestSync(string roomId)
    {
        try
        {
            string url = $"{BASE_API_URL}/meetings/{roomId}";

          
            ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "DELETE";
            request.Headers.Add("Authorization", "Bearer " + AuthBridge.Token);
            request.ContentType = "application/json";
            request.Timeout = 2000; 

   
            using var response = (HttpWebResponse)request.GetResponse();
            Debug.Log($"[API] Meeting ({roomId}) deleted synchronously  Status: {response.StatusCode}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[API] Sync Delete Failed: {e.Message}");
        }
    }

    #endregion

    #region Netcode & Relay

    private static void AssertPayloadAlreadySet()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        string existing = "";
        try
        {
            if (nm.NetworkConfig.ConnectionData != null && nm.NetworkConfig.ConnectionData.Length > 0)
                existing = Encoding.UTF8.GetString(nm.NetworkConfig.ConnectionData);
        }
        catch { existing = ""; }

        if (string.IsNullOrEmpty(existing))
            Debug.LogWarning("[PAYLOAD] ConnectionData is EMPTY.");
        else
            Debug.Log($"[PAYLOAD] Using >>>{existing}<<<");
    }

    public async Task SmartConnect(string currentJoinCode, string playerName, string org, int maxConnections = 4)
    {
        if (string.IsNullOrEmpty(currentJoinCode))
        {
            Debug.Log("[SmartConnect] No Join Code found. Creating a new Host session...");
            await CreateRoomAndHost(playerName, org, maxConnections);
            return;
        }

        try
        {
            Debug.Log("[SmartConnect] Attempting to join existing session...");
            await JoinRoomAndClient(currentJoinCode, playerName, org);
        }
        catch (RelayServiceException e)
        {
            Debug.LogWarning($"[SmartConnect] Join failed. Error: {e.Reason}");
            Debug.Log("[SmartConnect] Cleaning up old meeting and generating a new one...");

            await DeleteMeetingApiAsync(currentJoinCode);
            await CreateRoomAndHost(playerName, org, maxConnections);
        }
    }

    public async Task<string> CreateRoomAndHost(string playerName, string org, int maxConnections = 4)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            return CurrentJoinCode;

        await UGSBootstrap.EnsureSignedIn();

        Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

        CurrentJoinCode = joinCode;
        Debug.Log($"[Relay][HOST] JoinCode={joinCode} | Region={alloc.Region}");

        transport.SetRelayServerData(new RelayServerData(alloc, RelayProtocol));
        AssertPayloadAlreadySet();

        bool ok = NetworkManager.Singleton.StartHost();

        if (ok)
        {
            _isCurrentSessionHost = true;
            await PostMeetingApiAsync(org, joinCode);
        }

        Debug.Log($"[Relay][HOST] StartHost() = {ok}");
        return joinCode;
    }

    public async Task JoinRoomAndClient(string joinCode, string playerName, string org)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) return;

        await UGSBootstrap.EnsureSignedIn();

        Debug.Log($"[Relay][CLIENT] Joining code={joinCode}");

        var joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
        transport.SetRelayServerData(new RelayServerData(joinAlloc, RelayProtocol));
        AssertPayloadAlreadySet();

        bool ok = NetworkManager.Singleton.StartClient();
        _isCurrentSessionHost = false;

        CurrentJoinCode = joinCode;
        Debug.Log($"[Relay][CLIENT] StartClient() = {ok}");
    }

    #endregion
}