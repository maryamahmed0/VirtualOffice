using Unity.Netcode;
using UnityEngine;

public class MeetingChatNetworkBridge : NetworkBehaviour
{
    public static MeetingChatNetworkBridge LocalInstance { get; private set; }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            LocalInstance = this;
            Debug.Log("[CHAT BRIDGE] LocalInstance assigned");
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (LocalInstance == this)
            LocalInstance = null;
    }

    public void SendMessageToMeeting(string senderName, string text)
    {
        if (!IsOwner) return;
        SendMessageToMeetingServerRpc(senderName, text);
    }

    [ServerRpc]
    private void SendMessageToMeetingServerRpc(string senderName, string text, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        var senderRoomState = MeetingChatService.Instance != null
            ? MeetingChatService.Instance.FindRoomStatePublic(senderClientId)
            : null;

        if (senderRoomState == null) return;
        if (senderRoomState.GetZone() != NetRoomState.Zone.Meeting) return;

        var targets = MeetingChatService.Instance != null
            ? MeetingChatService.Instance.GetMeetingClientIdsPublic()
            : null;

        if (targets == null || targets.Count == 0) return;

        BroadcastMessageClientRpc(
            senderClientId,
            senderName,
            text,
            new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = targets
                }
            });
    }

    [ClientRpc]
    private void BroadcastMessageClientRpc(
        ulong senderClientId,
        string senderName,
        string text,
        ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"[CHAT BRIDGE] BroadcastMessageClientRpc {senderName}: {text}");
        MeetingChatService.Instance?.ReceiveNetworkMessage(senderClientId, senderName, text);
    }
}