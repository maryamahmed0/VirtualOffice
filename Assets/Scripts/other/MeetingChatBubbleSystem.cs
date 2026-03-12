using Unity.Netcode;
using UnityEngine;

public class MeetingChatBubbleSystem : MonoBehaviour
{
    private bool _subscribed;

    private void Start()
    {
        TrySubscribe();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (_subscribed && MeetingChatService.Instance != null)
        {
            MeetingChatService.Instance.OnMessageReceived -= HandleMessageReceived;
            _subscribed = false;
        }
    }

    private void TrySubscribe()
    {
        if (_subscribed) return;
        if (MeetingChatService.Instance == null) return;

        MeetingChatService.Instance.OnMessageReceived += HandleMessageReceived;
        _subscribed = true;
    }

    private void HandleMessageReceived(MeetingChatService.ChatMessage msg)
    {
        try
        {
            if (MeetingChatService.Instance == null)
                return;

            NetworkObject playerObject = MeetingChatService.Instance.FindPlayerObject(msg.SenderClientId);
            if (playerObject == null)
            {
                Debug.LogWarning($"[CHAT BUBBLE] No player object found for clientId={msg.SenderClientId}");
                return;
            }

            PlayerChatBubble bubble = playerObject.GetComponentInChildren<PlayerChatBubble>(true);
            if (bubble == null)
            {
                Debug.LogWarning($"[CHAT BUBBLE] PlayerChatBubble missing on player/clientId={msg.SenderClientId}");
                return;
            }

            bubble.ShowMessage(msg.Text);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CHAT BUBBLE] Exception while showing bubble: {ex}");
        }
    }
}