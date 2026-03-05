using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PresenceService : NetworkBehaviour
{
    public static PresenceService Instance { get; private set; }

    public event Action OnRosterChanged;

    private readonly Dictionary<ulong, PresenceEntry> serverRoster = new();
    private readonly List<PresenceEntry> localRoster = new();

    public IReadOnlyList<PresenceEntry> LocalRoster => localRoster;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private static readonly Queue<(ulong id, string name, string org, int teamHash, int teamSize)> pending
    = new();

    public static void EnqueueApproval(ulong id, string name, string org, int teamHash, int teamSize)
    {
        pending.Enqueue((id, name, org, teamHash, teamSize));

        // لو Presence جاهزة على السيرفر، نفّذ فورًا
        if (Instance != null && Instance.IsServer)
            Instance.FlushPendingApprovals();
    }

    private void FlushPendingApprovals()
    {
        while (pending.Count > 0)
        {
            var p = pending.Dequeue();
            RegisterFromApproval(p.id, p.name, p.org, p.teamHash, p.teamSize);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            FlushPendingApprovals();
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }

        if (IsClient)
            RequestRosterServerRpc();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        if (serverRoster.Remove(clientId))
            BroadcastRoster();
    }

    public void RegisterFromApproval(ulong clientId, string name, string org, int teamIdHash, int teamSize)
    {
        if (!IsServer) return;

        var e = new PresenceEntry
        {
            ClientId = clientId,
            Name = name,
            Org = org,
            TeamIdHash = teamIdHash,
            Zone = (int)NetRoomState.Zone.Lobby
        };

        serverRoster[clientId] = e;
        BroadcastRoster();
    }

    public void ServerUpdateZoneForClient(ulong clientId, int newZone)
    {
        if (!IsServer) return;
        if (serverRoster.TryGetValue(clientId, out var e))
        {
            e.Zone = newZone;
            serverRoster[clientId] = e;
            BroadcastRoster();
        }
    }

    private void BroadcastRoster()
    {
        var arr = new PresenceEntry[serverRoster.Count];
        int i = 0;
        foreach (var kv in serverRoster) arr[i++] = kv.Value;

        ReceiveRosterClientRpc(arr);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestRosterServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        var target = rpcParams.Receive.SenderClientId;

        var arr = new PresenceEntry[serverRoster.Count];
        int i = 0;
        foreach (var kv in serverRoster) arr[i++] = kv.Value;

        var sendParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { target } }
        };
        ReceiveRosterClientRpc(arr, sendParams);
    }

    [ClientRpc]
    private void ReceiveRosterClientRpc(PresenceEntry[] arr, ClientRpcParams rpcParams = default)
    {
        localRoster.Clear();
        localRoster.AddRange(arr);
        OnRosterChanged?.Invoke();
        Debug.Log($"[PRESENCE] roster size={localRoster.Count}");
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (NetworkManager == null) return;

        if (IsServer)
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }
}