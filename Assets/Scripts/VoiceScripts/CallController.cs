using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class CallController : NetworkBehaviour
{
    public enum CallState { Idle, RingingIn, RingingOut, Connecting, InCall }

    [Header("Debug")]
    [SerializeField] private CallState state = CallState.Idle;
    [SerializeField] private ulong otherClientId;
    [SerializeField] private string activePrivateChannel;
    [SerializeField] private string otherDisplayName;

    public CallState State => state;
    public ulong OtherClientId => otherClientId;
    public string ActivePrivateChannel => activePrivateChannel;
    public string OtherDisplayName => otherDisplayName;

    public event Action<string, ulong> OnIncomingCall;
    public event Action OnCallEnded;
    public event Action<string> OnCallStarted;
    public event Action<string> OnVoiceStatusChanged;

    private VoiceCoordinator _voiceCoord;
    private int _voiceStartVersion;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            Debug.Log($"[CALL] OnNetworkSpawn ownerClient={NetworkManager.Singleton.LocalClientId} IsHost={NetworkManager.Singleton.IsHost}");

        _voiceCoord = FindFirstObjectByType<VoiceCoordinator>();
    }

    private void EnsureCoordinator()
    {
        if (_voiceCoord == null)
            _voiceCoord = FindFirstObjectByType<VoiceCoordinator>();
    }

    public void RequestCall(ulong targetClientId)
    {
        if (!IsOwner) return;
        if (state != CallState.Idle) return;

        if (PlayerRoomState.LocalInstance != null &&
            PlayerRoomState.LocalInstance.CurrentContext != null &&
            PlayerRoomState.LocalInstance.CurrentContext.roomType == RoomType.Meeting)
            return;

        if (NetworkManager.Singleton != null && targetClientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.LogWarning("[CALL] RequestCall blocked (target is myself).");
            return;
        }

        otherClientId = targetClientId;


        otherDisplayName = PlayerIdentity.GetName(targetClientId);

        state = CallState.RingingOut;

        Debug.Log($"[CALL] RequestCall -> target={targetClientId} from={NetworkManager.Singleton.LocalClientId} targetName={otherDisplayName}");
        CallRpcDispatcher.Instance.RequestCallServerRpc(targetClientId, ResolveName(), NetworkManager.Singleton.LocalClientId);
    }

    public void AcceptCall()
    {
        if (!IsOwner) return;
        if (state != CallState.RingingIn) return;

        Debug.Log($"[CALL] AcceptCall by {NetworkManager.Singleton.LocalClientId} other={otherClientId}");
        CallRpcDispatcher.Instance.AcceptCallServerRpc(otherClientId, NetworkManager.Singleton.LocalClientId);
    }

    public void DeclineCall()
    {
        if (!IsOwner) return;
        if (state != CallState.RingingIn) return;

        Debug.Log($"[CALL] DeclineCall by {NetworkManager.Singleton.LocalClientId} other={otherClientId}");

        CallRpcDispatcher.Instance.DeclineCallServerRpc(otherClientId, NetworkManager.Singleton.LocalClientId);
        ResetLocalAndNotify();
    }

    public void CancelOutgoing()
    {
        if (!IsOwner) return;
        if (state != CallState.RingingOut) return;

        Debug.Log($"[CALL] CancelOutgoing by {NetworkManager.Singleton.LocalClientId} other={otherClientId}");

        CallRpcDispatcher.Instance.CancelOutgoingServerRpc(otherClientId, NetworkManager.Singleton.LocalClientId);
        ResetLocalAndNotify();
    }

    public void EndCall()
    {
        if (!IsOwner) return;
        if (state != CallState.InCall && state != CallState.Connecting) return;

        EnsureCoordinator();

        Debug.Log($"[CALL] EndCall by {NetworkManager.Singleton.LocalClientId} other={otherClientId} channel={activePrivateChannel}");

        CallRpcDispatcher.Instance.EndCallServerRpc(otherClientId, NetworkManager.Singleton.LocalClientId, activePrivateChannel);

        if (_voiceCoord != null)
            _ = _voiceCoord.EndPrivateCallAsync();

        ResetLocalAndNotify();
    }

    // ===== Dispatcher Hooks =====

    public void ReceiveIncomingFromDispatcher(string callerName, ulong callerClientId)
    {
        if (!IsOwner) return;
        if (state != CallState.Idle) return;

        state = CallState.RingingIn;
        otherClientId = callerClientId;


        otherDisplayName = callerName;

        OnIncomingCall?.Invoke(callerName, callerClientId);
        Debug.Log($"[CALL] Incoming (dispatcher) from {callerName} ({callerClientId})");
    }


    public void ReceiveOutgoingRingingFromDispatcher(ulong targetClientId, string targetName)
    {
        if (!IsOwner) return;

        otherClientId = targetClientId;
        if (!string.IsNullOrWhiteSpace(targetName))
            otherDisplayName = targetName;

        Debug.Log($"[CALL] Ringing (dispatcher) target {targetClientId} name={otherDisplayName}...");
    }


    public void ReceiveStartPrivateFromDispatcher(string channel, ulong callerId, ulong calleeId, string callerName, string calleeName)
    {
        if (!IsOwner) return;

        state = CallState.Connecting;
        activePrivateChannel = channel;

        ulong localId = NetworkManager.Singleton.LocalClientId;

        otherClientId = (localId == callerId) ? calleeId : callerId;
        otherDisplayName = (localId == callerId) ? calleeName : callerName;

        Debug.Log($"[CALL] Start private (dispatcher) channel={channel} other={otherClientId} otherName={otherDisplayName}");

        OnCallStarted?.Invoke(channel);
        OnVoiceStatusChanged?.Invoke("Connecting...");

        _ = StartPrivateVoiceFlowAsync(channel);
    }

    public void ReceiveRemoteEndedFromDispatcher(ulong whoEnded, string channel)
    {
        if (!IsOwner) return;
        if (state == CallState.Idle) return;

        Debug.Log($"[CALL] Remote ended (dispatcher) who={whoEnded} channel={channel}");

        EnsureCoordinator();
        if (_voiceCoord != null)
            _ = _voiceCoord.EndPrivateCallAsync();

        ResetLocalAndNotify();
    }

    public void ReceiveIncomingCanceledFromDispatcher(ulong whoCanceled)
    {
        if (!IsOwner) return;
        if (state == CallState.Idle) return;

        if (state == CallState.RingingIn)
        {
            Debug.Log($"[CALL] Incoming canceled (dispatcher) by={whoCanceled}");
            ResetLocalAndNotify();
        }
    }

    public void ReceiveDeclinedFromDispatcher(ulong calleeClientId)
    {
        if (!IsOwner) return;
        if (state == CallState.Idle) return;

        if (state == CallState.RingingOut && otherClientId == calleeClientId)
        {
            Debug.Log($"[CALL] Declined (dispatcher) by {calleeClientId}");
            ResetLocalAndNotify();
        }
    }

    // ===== Voice Flow =====

    private async Task StartPrivateVoiceFlowAsync(string channel)
    {
        int myVer = ++_voiceStartVersion;

        EnsureCoordinator();
        if (_voiceCoord == null)
        {
            Debug.LogWarning("[CALL] No VoiceCoordinator found.");
            OnVoiceStatusChanged?.Invoke("Voice unavailable");
            state = CallState.InCall;
            return;
        }

        bool ok = await _voiceCoord.StartPrivateCallAsync(channel);

        if (myVer != _voiceStartVersion) return;
        if (state == CallState.Idle) return;
        if (activePrivateChannel != channel) return;

        if (ok)
        {
            state = CallState.InCall;
            OnVoiceStatusChanged?.Invoke("Connected");
        }
        else
        {
            OnVoiceStatusChanged?.Invoke("Voice failed");

            if (otherClientId != 0 && NetworkManager.Singleton != null)
            {
                CallRpcDispatcher.Instance.EndCallServerRpc(otherClientId, NetworkManager.Singleton.LocalClientId, channel);
            }
            ResetLocalAndNotify();
        }
    }

    // ===== Helpers =====

    private void ResetLocalAndNotify()
    {
        _voiceStartVersion++;

        state = CallState.Idle;
        otherClientId = 0;
        activePrivateChannel = null;
        otherDisplayName = null;

        OnCallEnded?.Invoke();
    }

    private string ResolveName()
    {
        if (GameSessionData.Instance != null && !string.IsNullOrWhiteSpace(GameSessionData.Instance.DisplayName))
            return GameSessionData.Instance.DisplayName;

        return PlayerPrefs.GetString("PLAYER_NAME", "Player");
    }
}