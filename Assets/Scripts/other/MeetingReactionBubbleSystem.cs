using Unity.Netcode;
using UnityEngine;

public class MeetingReactionBubbleSystem : MonoBehaviour
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
        if (_subscribed && MeetingReactionService.Instance != null)
        {
            MeetingReactionService.Instance.OnReactionReceived -= HandleReactionReceived;
            _subscribed = false;
        }
    }

    private void TrySubscribe()
    {
        if (_subscribed) return;
        if (MeetingReactionService.Instance == null) return;

        MeetingReactionService.Instance.OnReactionReceived += HandleReactionReceived;
        _subscribed = true;
    }

    private void HandleReactionReceived(MeetingReactionService.ReactionMessage msg)
    {
        try
        {
            if (MeetingReactionService.Instance == null)
                return;

            NetworkObject playerObject = MeetingReactionService.Instance.FindPlayerObject(msg.SenderClientId);
            if (playerObject == null)
            {
                Debug.LogWarning($"[REACTION SYS] No player object found for clientId={msg.SenderClientId}");
                return;
            }

            PlayerReactionBubble bubble = playerObject.GetComponentInChildren<PlayerReactionBubble>(true);
            if (bubble == null)
            {
                Debug.LogWarning($"[REACTION SYS] PlayerReactionBubble missing for clientId={msg.SenderClientId}");
                return;
            }

            bubble.ShowReaction(msg.ReactionType);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[REACTION SYS] Exception: {ex}");
        }
    }
}