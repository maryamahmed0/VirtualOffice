using Unity.Netcode;
using UnityEngine;

public class CallRpcDispatcher : NetworkBehaviour
{
    public static CallRpcDispatcher Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        Debug.Log("[CALLRPC] Awake instance set");
        DontDestroyOnLoad(gameObject);
    }

    private static CallController FindLocalCallController()
    {
        if (NetworkManager.Singleton == null) return null;

        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer != null && localPlayer.TryGetComponent(out CallController callFromPlayer))
            return callFromPlayer;

        var all = Object.FindObjectsByType<CallController>(FindObjectsSortMode.None);
        foreach (var c in all)
        {
            if (c != null && c.IsOwner) return c;
        }

        return null;
    }

    private string GetNameOnServer(ulong clientId)
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) &&
            client.PlayerObject != null &&
            client.PlayerObject.TryGetComponent<PlayerIdentity>(out var id))
        {
            var n = id.DisplayName.Value.ToString();
            if (!string.IsNullOrWhiteSpace(n)) return n;
        }

        return PlayerIdentity.GetName(clientId);
    }

    // ======================
    // Request (Server -> Target + Caller)
    // ======================

    [ServerRpc(RequireOwnership = false)]
    public void RequestCallServerRpc(ulong targetClientId, string callerName, ulong callerClientId)
    {
        Debug.Log($"[CALLRPC][SERVER] Request from {callerClientId} -> {targetClientId} name={callerName}");

        // ابعتي للTarget: اسم الـ caller
        var sendTarget = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { targetClientId } }
        };
        IncomingCallClientRpc(callerName, callerClientId, sendTarget);

        // ابعتي للCaller: Ringing + اسم الـ target (عشان outgoing card تبقى صح)
        string targetName = GetNameOnServer(targetClientId);

        var sendCaller = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { callerClientId } }
        };
        OutgoingRingingClientRpc(targetClientId, targetName, sendCaller);
    }

    [ClientRpc]
    private void IncomingCallClientRpc(string callerName, ulong callerClientId, ClientRpcParams rpcParams = default)
    {
        var call = FindLocalCallController();
        if (call == null) return;

        call.ReceiveIncomingFromDispatcher(callerName, callerClientId);
    }

    [ClientRpc]
    private void OutgoingRingingClientRpc(ulong targetClientId, string targetName, ClientRpcParams rpcParams = default)
    {
        var call = FindLocalCallController();
        if (call == null) return;

        call.ReceiveOutgoingRingingFromDispatcher(targetClientId, targetName);
    }

    // ======================
    // Accept (Server -> Both)
    // ======================

    [ServerRpc(RequireOwnership = false)]
    public void AcceptCallServerRpc(ulong callerClientId, ulong calleeClientId)
    {
        Debug.Log($"[CALLRPC][SERVER] Accept caller={callerClientId} callee={calleeClientId}");

        string callerName = GetNameOnServer(callerClientId);
        string calleeName = GetNameOnServer(calleeClientId);

        string channel =
            $"P_{Mathf.Min((int)callerClientId, (int)calleeClientId)}_{Mathf.Max((int)callerClientId, (int)calleeClientId)}_{UnityEngine.Random.Range(1000, 9999)}";

        var sendBoth = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { callerClientId, calleeClientId } }
        };

        StartPrivateCallClientRpc(channel, callerClientId, calleeClientId, callerName, calleeName, sendBoth);
    }

    [ClientRpc]
    private void StartPrivateCallClientRpc(string channel, ulong callerId, ulong calleeId, string callerName, string calleeName, ClientRpcParams rpcParams = default)
    {
        var call = FindLocalCallController();
        if (call == null) return;

        call.ReceiveStartPrivateFromDispatcher(channel, callerId, calleeId, callerName, calleeName);
    }

    // ======================
    // End (Server -> Both)
    // ======================

    [ServerRpc(RequireOwnership = false)]
    public void EndCallServerRpc(ulong otherClientId, ulong whoEnded, string channel)
    {
        Debug.Log($"[CALLRPC][SERVER] End who={whoEnded} -> other={otherClientId} channel={channel}");

        var sendBoth = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { otherClientId, whoEnded } }
        };

        RemoteEndedClientRpc(whoEnded, channel, sendBoth);
    }

    [ClientRpc]
    private void RemoteEndedClientRpc(ulong whoEnded, string channel, ClientRpcParams rpcParams = default)
    {
        var call = FindLocalCallController();
        if (call == null) return;

        call.ReceiveRemoteEndedFromDispatcher(whoEnded, channel);
    }

    // ======================
    // Cancel Outgoing (Server -> Target)
    // ======================

    [ServerRpc(RequireOwnership = false)]
    public void CancelOutgoingServerRpc(ulong targetClientId, ulong whoCanceled)
    {
        Debug.Log($"[CALLRPC][SERVER] CancelOutgoing who={whoCanceled} -> target={targetClientId}");

        var sendTarget = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { targetClientId } }
        };

        IncomingCanceledClientRpc(whoCanceled, sendTarget);
    }

    [ClientRpc]
    private void IncomingCanceledClientRpc(ulong whoCanceled, ClientRpcParams rpcParams = default)
    {
        var call = FindLocalCallController();
        if (call == null) return;

        call.ReceiveIncomingCanceledFromDispatcher(whoCanceled);
    }

    // ======================
    // Decline (Server -> Caller)
    // ======================

    [ServerRpc(RequireOwnership = false)]
    public void DeclineCallServerRpc(ulong callerClientId, ulong calleeClientId)
    {
        Debug.Log($"[CALLRPC][SERVER] Decline caller={callerClientId} callee={calleeClientId}");

        var sendCaller = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { callerClientId } }
        };

        CallDeclinedClientRpc(calleeClientId, sendCaller);
    }

    [ClientRpc]
    private void CallDeclinedClientRpc(ulong calleeClientId, ClientRpcParams rpcParams = default)
    {
        var call = FindLocalCallController();
        if (call == null) return;

        call.ReceiveDeclinedFromDispatcher(calleeClientId);
    }
}