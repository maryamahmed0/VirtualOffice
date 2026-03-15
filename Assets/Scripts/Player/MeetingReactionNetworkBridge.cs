using Unity.Netcode;
using UnityEngine;

public class MeetingReactionNetworkBridge : NetworkBehaviour
{
    public static MeetingReactionNetworkBridge LocalInstance { get; private set; }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            LocalInstance = this;
            Debug.Log("[REACTION BRIDGE] LocalInstance assigned");
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (LocalInstance == this)
            LocalInstance = null;
    }

    public void SendReactionToMeeting(string senderName, MeetingReactionType reactionType)
    {
        if (!IsOwner) return;

        Debug.Log($"[REACTION BRIDGE] SendReactionToMeeting sender={senderName}, reaction={reactionType}");
        SendReactionToMeetingServerRpc(senderName, (int)reactionType);
    }

    [ServerRpc]
    private void SendReactionToMeetingServerRpc(string senderName, int reactionTypeInt, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (MeetingReactionService.Instance == null)
            return;

        var senderRoomState = MeetingReactionService.Instance.FindRoomStatePublic(senderClientId);
        if (senderRoomState == null) return;
        if (senderRoomState.GetZone() != NetRoomState.Zone.Meeting) return;

        var targets = MeetingReactionService.Instance.GetMeetingClientIdsPublic();
        if (targets == null || targets.Count == 0) return;

        BroadcastReactionClientRpc(
            senderClientId,
            senderName,
            reactionTypeInt,
            new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = targets
                }
            });
    }

    [ClientRpc]
    private void BroadcastReactionClientRpc(
        ulong senderClientId,
        string senderName,
        int reactionTypeInt,
        ClientRpcParams clientRpcParams = default)
    {
        if (MeetingReactionService.Instance == null)
            return;

        MeetingReactionService.Instance.ReceiveNetworkReaction(
            senderClientId,
            senderName,
            (MeetingReactionType)reactionTypeInt);
    }
}