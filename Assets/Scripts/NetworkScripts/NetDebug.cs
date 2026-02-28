using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class NetDebug : MonoBehaviour
{
    private void Start()
    {
        var nm = NetworkManager.Singleton;
        Debug.Log($"[NetDebug] Scene={SceneManager.GetActiveScene().name}");

        nm.OnClientConnectedCallback += id =>
        {
            Debug.Log($"[NetDebug] OnClientConnected id={id} IsServer={nm.IsServer} IsClient={nm.IsClient} LocalId={nm.LocalClientId}");
        };

        nm.OnClientDisconnectCallback += id =>
        {
            Debug.Log($"[NetDebug] OnClientDisconnect id={id} IsServer={nm.IsServer} IsClient={nm.IsClient}");
        };

        nm.OnTransportFailure += () =>
        {
            Debug.LogError("[NetDebug] TransportFailure");
        };
    }

    private void Update()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        if (Time.frameCount % 120 == 0)
        {
            Debug.Log($"[NetDebug] IsConnectedClient={nm.IsConnectedClient} IsListening={nm.IsListening} ConnectedClients={nm.ConnectedClientsList.Count} ActiveScene={SceneManager.GetActiveScene().name}");
        }
    }
}