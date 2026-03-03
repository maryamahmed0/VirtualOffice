using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using System.Text;

public class RelayConnector : MonoBehaviour
{
    [SerializeField] private UnityTransport transport;

    public string CurrentJoinCode { get; private set; }

    private static string RelayProtocol => "wss";

    private void Awake()
    {
        if (transport == null)
            transport = GetComponent<UnityTransport>();

        if (transport == null)
            transport = FindFirstObjectByType<UnityTransport>(FindObjectsInactive.Include);

        if (transport == null)
            Debug.LogError("[RelayConnector] UnityTransport NOT FOUND!");

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnTransportFailure += () =>
            {
                Debug.LogError("[NET] TransportFailure! Relay connection dropped. Need recreate allocation + restart NetworkManager.");
            };

            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += (sceneName, mode, completed, timedOut) =>
                {
                    Debug.Log($"[NET][Scene] LoadCompleted scene={sceneName} completed={completed.Count} timedOut={timedOut.Count}");
                };
            }
        }
    }

    // ✅ Optional: تأكيد إن payload معمول قبل StartHost/StartClient
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
            Debug.LogWarning("[PAYLOAD] ConnectionData is EMPTY. Expected it to be set by LobbyConnectUI before starting host/client.");
        else
            Debug.Log($"[PAYLOAD] Using existing >>>{existing}<<<");
    }

    public async Task<string> CreateRoomAndHost(string playerName, string org, int maxConnections = 4)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("[Relay][HOST] Already listening. Returning existing join code.");
            return CurrentJoinCode;
        }

        await UGSBootstrap.EnsureSignedIn();

        Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

        CurrentJoinCode = joinCode;

        Debug.Log($"[Relay][HOST] Region={alloc.Region}");
        Debug.Log($"[Relay][HOST] JoinCode EXACT >>>{joinCode}<<< len={joinCode.Length}");
        Debug.Log($"[Relay][HOST] Protocol={RelayProtocol}");

        transport.SetRelayServerData(new RelayServerData(alloc, RelayProtocol));

        // ❌ متكتبش payload هنا
        AssertPayloadAlreadySet();

        bool ok = NetworkManager.Singleton.StartHost();
        Debug.Log($"[Relay][HOST] StartHost() = {ok}");

        return joinCode;
    }

    public async Task JoinRoomAndClient(string joinCode, string playerName, string org)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("[Relay][CLIENT] NetworkManager already listening (Host/Client). Stop first.");
            return;
        }

        await UGSBootstrap.EnsureSignedIn();

        Debug.Log($"[Relay][CLIENT] Joining with EXACT >>>{joinCode}<<< len={joinCode.Length}");
        Debug.Log($"[Relay][CLIENT] Protocol={RelayProtocol}");

        var joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
        Debug.Log($"[Relay][CLIENT] Joined region = {joinAlloc.Region}");

        transport.SetRelayServerData(new RelayServerData(joinAlloc, RelayProtocol));

        // ❌ متكتبش payload هنا
        AssertPayloadAlreadySet();

        bool ok = NetworkManager.Singleton.StartClient();
        Debug.Log($"[Relay][CLIENT] StartClient() = {ok}");
    }
}