using UnityEngine;
using Unity.Netcode;

public class PlayerRoomState : NetworkBehaviour
{
    public static PlayerRoomState LocalInstance { get; private set; }

    [Header("Debug")]
    [SerializeField] private string currentRoomId;
    [SerializeField] private RoomType currentRoomType = RoomType.None;
    [SerializeField] private string currentTeamId;

    [Header("Fallback")]
    [SerializeField] private RoomMarker defaultFallbackRoom;

    [Header("Anti-spam")]
    [SerializeField] private float roomSwitchCooldown = 0.35f;

    [Header("Auto Find Fallback")]
    [SerializeField] private bool autoFindFallback = true;
    [SerializeField] private string fallbackTag = "LobbyRoom";

    [Header("Spawn Ignore Triggers")]
    [SerializeField] private float ignoreTriggersFor = 1.5f; 
    private float _ignoreUntil;

    public string CurrentRoomId => currentRoomId;
    public RoomType CurrentRoomType => currentRoomType;
    public string CurrentTeamId => currentTeamId;
    public RoomMarker CurrentContext => _currentContext;

    public System.Action<RoomMarker, RoomMarker> OnLocalRoomChanged;

    private RoomMarker _currentContext;
    private float _nextAllowedSwitchTime;

    private void Awake()
    {
        _ignoreUntil = Time.time + ignoreTriggersFor;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        LocalInstance = this;
        Debug.Log("[ROOM] Local PlayerRoomState ready ✅");

        _ignoreUntil = Time.time + ignoreTriggersFor;


        if (autoFindFallback && defaultFallbackRoom == null)
        {
            var go = GameObject.FindWithTag(fallbackTag);
            if (go != null) defaultFallbackRoom = go.GetComponent<RoomMarker>();
            Debug.Log("[ROOM] Fallback found = " + (defaultFallbackRoom ? defaultFallbackRoom.GetDebugName() : "NULL"));
        }

        if (_currentContext == null && defaultFallbackRoom != null)
        {
            ForceSetRoom(defaultFallbackRoom);
        }
        else
        {
           
            ApplyRoomData(_currentContext);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && LocalInstance == this)
            LocalInstance = null;
    }

    public bool CanProcessRoomTriggers() => Time.time >= _ignoreUntil;

    public void EnterRoom(RoomMarker ctx)
    {
        if (!IsOwner || ctx == null) return;

        if (!CanProcessRoomTriggers()) return;

        if (Time.time < _nextAllowedSwitchTime) return;
        if (_currentContext == ctx) return;

        var old = _currentContext;
        _currentContext = ctx;
        ApplyRoomData(_currentContext);

        _nextAllowedSwitchTime = Time.time + roomSwitchCooldown;

        Debug.Log($"[ROOM] RoomChanged {old?.GetDebugName() ?? "None"} -> {_currentContext.GetDebugName()}");
        OnLocalRoomChanged?.Invoke(old, _currentContext);
    }

    public void ExitRoom(RoomMarker ctx)
    {
        if (!IsOwner || ctx == null) return;
        if (!CanProcessRoomTriggers()) return;
        if (_currentContext != ctx) return;
        if (Time.time < _nextAllowedSwitchTime) return;

        var old = _currentContext;

        if (defaultFallbackRoom == null && autoFindFallback)
        {
            var go = GameObject.FindWithTag(fallbackTag);
            if (go != null) defaultFallbackRoom = go.GetComponent<RoomMarker>();
            Debug.Log("[ROOM] Fallback re-check = " + (defaultFallbackRoom ? defaultFallbackRoom.GetDebugName() : "NULL"));
        }

        if (defaultFallbackRoom != null)
        {
            _currentContext = defaultFallbackRoom;
            ApplyRoomData(_currentContext);
            _nextAllowedSwitchTime = Time.time + roomSwitchCooldown;

            Debug.Log($"[ROOM] RoomChanged {old.GetDebugName()} -> {_currentContext.GetDebugName()}");
            OnLocalRoomChanged?.Invoke(old, _currentContext);
            return;
        }

        Debug.LogWarning("[ROOM] No fallback room found. Keeping current room to avoid None.");
        _currentContext = old;
        ApplyRoomData(_currentContext);
    }

    private void ForceSetRoom(RoomMarker ctx)
    {
        var old = _currentContext;
        _currentContext = ctx;
        ApplyRoomData(_currentContext);

        Debug.Log($"[ROOM] ForceRoom {old?.GetDebugName() ?? "None"} -> {_currentContext.GetDebugName()}");
        OnLocalRoomChanged?.Invoke(old, _currentContext);
    }

    private void ApplyRoomData(RoomMarker ctx)
    {
        if (ctx == null)
        {
            currentRoomId = "";
            currentRoomType = RoomType.None;
            currentTeamId = "";
            return;
        }

        currentRoomId = ctx.roomId;
        currentRoomType = ctx.roomType;
        currentTeamId = ctx.teamId;
    }
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
        var localPlayer = NetworkManager.Singleton?.LocalClient?.PlayerObject;
        if (localPlayer == null) return;

        var call = localPlayer.GetComponent<CallController>();
        if (call == null) return;

        call.ReceiveDeclinedFromDispatcher(calleeClientId);
    }
}