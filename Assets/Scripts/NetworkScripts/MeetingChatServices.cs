using System;
using System.Collections.Generic;
using UnityEngine;

public class MeetingChatService : MonoBehaviour
{
    public static MeetingChatService Instance { get; private set; }

    public struct ChatMessage
    {
        public ulong SenderClientId;
        public string SenderName;
        public string Text;

        public ChatMessage(ulong senderClientId, string senderName, string text)
        {
            SenderClientId = senderClientId;
            SenderName = senderName;
            Text = text;
        }
    }

    public event Action<ChatMessage> OnMessageReceived;

    private readonly List<ChatMessage> _messages = new();
    public IReadOnlyList<ChatMessage> Messages => _messages;

    [Header("Validation")]
    [SerializeField] private int maxMessageLength = 200;
    [SerializeField] private int maxStoredMessages = 100;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log("[CHAT SERVICE] Awake");
    }

    public void SendMessageToMeeting(string senderName, string text)
    {
        text = SanitizeText(text);
        senderName = SanitizeName(senderName);

        if (string.IsNullOrWhiteSpace(text))
            return;

        if (MeetingChatNetworkBridge.LocalInstance == null)
        {
            Debug.LogWarning("[CHAT SERVICE] No local MeetingChatNetworkBridge found.");
            return;
        }

        Debug.Log($"[CHAT SERVICE] Send via bridge sender={senderName}, text={text}");
        MeetingChatNetworkBridge.LocalInstance.SendMessageToMeeting(senderName, text);
    }

    public void ReceiveNetworkMessage(ulong senderClientId, string senderName, string text)
    {
        text = SanitizeText(text);
        senderName = SanitizeName(senderName);

        if (string.IsNullOrWhiteSpace(text))
            return;

        var msg = new ChatMessage(senderClientId, senderName, text);

        _messages.Add(msg);

        if (_messages.Count > maxStoredMessages)
            _messages.RemoveAt(0);

        Debug.Log($"[CHAT SERVICE] ReceiveNetworkMessage {senderName}: {text}");
        OnMessageReceived?.Invoke(msg);
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
            if (netObj == null) continue;

            if (netObj.OwnerClientId == clientId)
                return roomState;
        }

        return null;
    }

    private string SanitizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value.Trim();

        if (value.Length > maxMessageLength)
            value = value.Substring(0, maxMessageLength);

        return value;
    }

    private string SanitizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "Player";

        value = value.Trim();

        if (value.Length > 24)
            value = value.Substring(0, 24);

        return value;
    }
}