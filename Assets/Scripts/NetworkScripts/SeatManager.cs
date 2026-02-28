using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SeatManager : NetworkBehaviour
{
    [SerializeField] private Seat[] seats;

    private readonly Dictionary<ulong, int> assigned = new();
    private readonly List<int> free = new();

    private void Awake()
    {
        if (seats == null || seats.Length == 0)
            seats = GetComponentsInChildren<Seat>(true);

        free.Clear();
        for (int i = 0; i < seats.Length; i++)
            free.Add(i);
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager != null)
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        ReleaseSeat(clientId);
    }

    public bool TryAssignRandomSeat(ulong clientId, out Vector3 spawnPos)
    {
        spawnPos = Vector3.zero;

        if (!IsServer) return false;
        if (seats == null || seats.Length == 0) return false;

        if (assigned.TryGetValue(clientId, out int existingIndex))
        {
            if (existingIndex < 0 || existingIndex >= seats.Length) return false;
            spawnPos = seats[existingIndex].SpawnPoint.position;
            return true;
        }

        if (free.Count == 0) return false;

        int pick = Random.Range(0, free.Count);
        int seatIndex = free[pick];
        free.RemoveAt(pick);

        assigned[clientId] = seatIndex;
        spawnPos = seats[seatIndex].SpawnPoint.position;

        Debug.Log($"[SERVER] Assign client {clientId} -> seat {seatIndex} pos {spawnPos}");
        return true;
    }

    public void ReleaseSeat(ulong clientId)
    {
        if (!IsServer) return;

        if (assigned.TryGetValue(clientId, out int seatIndex))
        {
            assigned.Remove(clientId);

            if (!free.Contains(seatIndex))
                free.Add(seatIndex);

            Debug.Log($"[SERVER] Release client {clientId} seat {seatIndex}");
        }
    }
}
