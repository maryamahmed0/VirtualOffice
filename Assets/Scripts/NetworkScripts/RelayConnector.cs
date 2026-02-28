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

    private static void SetConnectionPayload(string playerName, string org)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        string payload = $"{playerName}|{org}";
        nm.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(payload);

        Debug.Log($"[PAYLOAD] set >>>{payload}<<< bytes={nm.NetworkConfig.ConnectionData.Length}");
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

        SetConnectionPayload(playerName, org);

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

        SetConnectionPayload(playerName, org);

        bool ok = NetworkManager.Singleton.StartClient();
        Debug.Log($"[Relay][CLIENT] StartClient() = {ok}");
    }
}