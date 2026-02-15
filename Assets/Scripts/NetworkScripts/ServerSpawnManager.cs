using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ServerSpawnManager : MonoBehaviour
{
    private SeatManager seatManager;

    private void Start()
    {
        if (NetworkManager.Singleton == null) return;

        seatManager = FindFirstObjectByType<SeatManager>();
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        StartCoroutine(AssignSeatNextFrame(clientId));
    }

    private IEnumerator AssignSeatNextFrame(ulong clientId)
    {
        // مهم: نستنى فريم عشان PlayerObject يبقى اتعمله spawn
        yield return null;

        if (seatManager == null)
            seatManager = FindFirstObjectByType<SeatManager>();

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            yield break;

        var playerObj = client.PlayerObject;
        if (playerObj == null) yield break;

        if (seatManager != null && seatManager.TryAssignRandomSeat(clientId, out Vector3 pos))
        {
            var rb = playerObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.position = (Vector2)pos;
            }
            else
            {
                playerObj.transform.position = pos;
            }

            Debug.Log($"[SERVER] client {clientId} spawned at {pos}");
        }
        else
        {
            Debug.LogWarning($"[SERVER] No seat available for client {clientId}");
        }
    }
}
