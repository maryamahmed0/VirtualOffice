using System;
using System.Collections.Generic;
using UnityEngine;

public class MeetingReactionService : MonoBehaviour
{
    public static MeetingReactionService Instance { get; private set; }

    public struct ReactionMessage
    {
        public ulong SenderClientId;
        public string SenderName;
        public MeetingReactionType ReactionType;

        public ReactionMessage(ulong senderClientId, string senderName, MeetingReactionType reactionType)
        {
            SenderClientId = senderClientId;
            SenderName = senderName;
            ReactionType = reactionType;
        }
    }

    public event Action<ReactionMessage> OnReactionReceived;

    private readonly List<ReactionMessage> _recentReactions = new();
    public IReadOnlyList<ReactionMessage> RecentReactions => _recentReactions;

    [SerializeField] private int maxStoredReactions = 30;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log("[REACTION SERVICE] Awake");
    }

    public void SendReactionToMeeting(string senderName, MeetingReactionType reactionType)
    {
        if (MeetingReactionNetworkBridge.LocalInstance == null)
        {
            Debug.LogWarning("[REACTION SERVICE] No local MeetingReactionNetworkBridge found.");
            return;
        }

        if (string.IsNullOrWhiteSpace(senderName))
            senderName = "Player";

        MeetingReactionNetworkBridge.LocalInstance.SendReactionToMeeting(senderName, reactionType);
    }

    public void ReceiveNetworkReaction(ulong senderClientId, string senderName, MeetingReactionType reactionType)
    {
        if (string.IsNullOrWhiteSpace(senderName))
            senderName = "Player";

        var msg = new ReactionMessage(senderClientId, senderName, reactionType);

        _recentReactions.Add(msg);
        if (_recentReactions.Count > maxStoredReactions)
            _recentReactions.RemoveAt(0);

        Debug.Log($"[REACTION SERVICE] ReceiveNetworkReaction {senderName}: {reactionType}");
        OnReactionReceived?.Invoke(msg);
    }

    public Unity.Netcode.NetworkObject FindPlayerObject(ulong clientId)
    {
        var nm = Unity.Netcode.NetworkManager.Singleton;
        if (nm == null || nm.SpawnManager == null)
            return null;

        foreach (var kvp in nm.SpawnManager.SpawnedObjects)
        {
            var netObj = kvp.Value;
            if (netObj == null)
                continue;

            if (netObj.OwnerClientId == clientId && netObj.IsPlayerObject)
                return netObj;
        }

        return null;
    }

    public List<ulong> GetMeetingClientIdsPublic()
    {
        List<ulong> result = new();

        if (Unity.Netcode.NetworkManager.Singleton == null)
            return result;

        foreach (var kvp in Unity.Netcode.NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = kvp.Key;
            var roomState = FindRoomStatePublic(clientId);

            if (roomState != null && roomState.GetZone() == NetRoomState.Zone.Meeting)
                result.Add(clientId);
        }

        return result;
    }

    public NetRoomState FindRoomStatePublic(ulong clientId)
    {
        var roomStates = FindObjectsOfType<NetRoomState>(true);

        foreach (var roomState in roomStates)
        {
            var netObj = roomState.GetComponent<Unity.Netcode.NetworkObject>();
            if (netObj == null)
                continue;

            if (netObj.OwnerClientId == clientId)
                return roomState;
        }

        return null;
    }
}