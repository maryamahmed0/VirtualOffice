using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CallRpcDispatcher : NetworkBehaviour
{
    public static CallRpcDispatcher Instance { get; private set; }

    [SerializeField] private float privateVoiceReadyTimeout = 20f;

    private class PrivateVoiceSession
    {
        public ulong CallerId;
        public ulong CalleeId;
        public string Channel;
        public bool CallerReady;
        public bool CalleeReady;
        public Coroutine TimeoutRoutine;
    }

    private readonly Dictionary<string, PrivateVoiceSession> _privateVoiceSessions = new();

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

        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(callerClientId, out var callerClient) ||
            callerClient.PlayerObject == null)
            return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(targetClientId, out var targetClient) ||
            targetClient.PlayerObject == null)
            return;

        var callerRoom = callerClient.PlayerObject.GetComponent<NetRoomState>();
        var targetRoom = targetClient.PlayerObject.GetComponent<NetRoomState>();

        if (callerRoom != null && callerRoom.GetZone() == NetRoomState.Zone.Meeting)
        {
            Debug.Log("[CALLRPC][SERVER] Blocked request: caller is in Meeting");
            return;
        }

        if (targetRoom != null && targetRoom.GetZone() == NetRoomState.Zone.Meeting)
        {
            Debug.Log("[CALLRPC][SERVER] Blocked request: target is in Meeting");
            return;
        }

        var sendTarget = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { targetClientId } }
        };
        IncomingCallClientRpc(callerName, callerClientId, sendTarget);

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

        string channel = $"P-{Mathf.Min((int)callerClientId, (int)calleeClientId)}-{Mathf.Max((int)callerClientId, (int)calleeClientId)}-{UnityEngine.Random.Range(1000, 9999)}";

        if (_privateVoiceSessions.TryGetValue(channel, out var existing))
        {
            if (existing.TimeoutRoutine != null)
                StopCoroutine(existing.TimeoutRoutine);

            _privateVoiceSessions.Remove(channel);
        }

        var session = new PrivateVoiceSession
        {
            CallerId = callerClientId,
            CalleeId = calleeClientId,
            Channel = channel,
            CallerReady = false,
            CalleeReady = false
        };

        session.TimeoutRoutine = StartCoroutine(PrivateVoiceReadyTimeoutRoutine(session));
        _privateVoiceSessions[channel] = session;

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
    // Voice Ready / Fail handshake
    // ======================

    [ServerRpc(RequireOwnership = false)]
    public void PrivateVoiceReadyServerRpc(string channel, ulong whoReady, ulong otherClientId)
    {
        if (string.IsNullOrWhiteSpace(channel)) return;
        if (!_privateVoiceSessions.TryGetValue(channel, out var session)) return;

        if (whoReady == session.CallerId)
            session.CallerReady = true;
        else if (whoReady == session.CalleeId)
            session.CalleeReady = true;
        else
            return;

        Debug.Log($"[CALLRPC][SERVER] PrivateVoiceReady who={whoReady} channel={channel} callerReady={session.CallerReady} calleeReady={session.CalleeReady}");

        if (session.CallerReady && session.CalleeReady)
        {
            NotifyPrivateVoiceConnected(session);
            CleanupPrivateVoiceSession(channel);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void PrivateVoiceFailedServerRpc(string channel, ulong whoFailed, ulong otherClientId, string reason)
    {
        if (string.IsNullOrWhiteSpace(channel)) return;
        if (!_privateVoiceSessions.TryGetValue(channel, out var session)) return;

        Debug.Log($"[CALLRPC][SERVER] PrivateVoiceFailed who={whoFailed} channel={channel} reason={reason}");

        NotifyPrivateVoiceFailed(session, reason);
        CleanupPrivateVoiceSession(channel);
    }

    private IEnumerator PrivateVoiceReadyTimeoutRoutine(PrivateVoiceSession session)
    {
        yield return new WaitForSeconds(privateVoiceReadyTimeout);

        if (session == null) yield break;
        if (!_privateVoiceSessions.ContainsKey(session.Channel)) yield break;

        if (!(session.CallerReady && session.CalleeReady))
        {
            Debug.Log($"[CALLRPC][SERVER] Private voice timeout channel={session.Channel}");

            NotifyPrivateVoiceFailed(session, "Connection failed");
            CleanupPrivateVoiceSession(session.Channel);
        }
    }

    private void NotifyPrivateVoiceConnected(PrivateVoiceSession session)
    {
        var sendBoth = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { session.CallerId, session.CalleeId }
            }
        };

        PrivateVoiceConnectedClientRpc(session.Channel, sendBoth);
    }

    private void NotifyPrivateVoiceFailed(PrivateVoiceSession session, string reason)
    {
        var sendBoth = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { session.CallerId, session.CalleeId }
            }
        };

        PrivateVoiceFailedClientRpc(session.Channel, reason, sendBoth);
    }

    private void CleanupPrivateVoiceSession(string channel)
    {
        if (string.IsNullOrWhiteSpace(channel)) return;

        if (_privateVoiceSessions.TryGetValue(channel, out var session))
        {
            if (session.TimeoutRoutine != null)
                StopCoroutine(session.TimeoutRoutine);
        }

        _privateVoiceSessions.Remove(channel);
    }

    [ClientRpc]
    private void PrivateVoiceConnectedClientRpc(string channel, ClientRpcParams rpcParams = default)
    {
        var call = FindLocalCallController();
        if (call == null) return;

        call.ReceivePrivateConnectedFromDispatcher(channel);
    }

    [ClientRpc]
    private void PrivateVoiceFailedClientRpc(string channel, string reason, ClientRpcParams rpcParams = default)
    {
        var call = FindLocalCallController();
        if (call == null) return;

        call.ReceivePrivateFailedFromDispatcher(channel, reason);
    }

    // ======================
    // End (Server -> Both)
    // ======================

    [ServerRpc(RequireOwnership = false)]
    public void EndCallServerRpc(ulong otherClientId, ulong whoEnded, string channel)
    {
        Debug.Log($"[CALLRPC][SERVER] End who={whoEnded} -> other={otherClientId} channel={channel}");

        CleanupPrivateVoiceSession(channel);

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