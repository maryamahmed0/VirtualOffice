using System.Threading.Tasks;
using UnityEngine;

public class VoiceCoordinator : MonoBehaviour
{
    [SerializeField] private MonoBehaviour providerBehaviour;
    private IVoiceProvider provider;

    [Header("Meeting Auto Join")]
    [SerializeField] private bool autoJoinMeeting = true;

    [Header("State (debug)")]
    [SerializeField] private string activeMeetingChannel;
    [SerializeField] private string activePrivateChannel;

   
    private PlayerVoicePresenter _localPresenter;
    private static VoiceCoordinator _instance;

    private NetRoomState localRoomState;
    private bool inMeetingRoom;
    private int voiceOpVersion = 0;

    private void Awake()
    {
        Debug.Log("[VOICECOORD] Awake. provider=" + (providerBehaviour ? providerBehaviour.name : "NULL"));
        if (_instance != null) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        provider = providerBehaviour as IVoiceProvider;
    }

    private void OnEnable()
    {
        InvokeRepeating(nameof(TryHookLocalZone), 0.2f, 0.2f);
    }

    private void OnDisable()
    {
        if (localRoomState != null)
            localRoomState.CurrentZone.OnValueChanged -= OnZoneChanged;

        CancelInvoke(nameof(TryHookLocalZone));
    }

    private void TryHookLocalZone()
    {
        if (PlayerRoomState.LocalInstance == null) return;

        localRoomState = PlayerRoomState.LocalInstance.GetComponentInParent<NetRoomState>();
        if (localRoomState == null) return;

        localRoomState.CurrentZone.OnValueChanged -= OnZoneChanged;
        localRoomState.CurrentZone.OnValueChanged += OnZoneChanged;

        CancelInvoke(nameof(TryHookLocalZone));
        Debug.Log("[VOICECOORD] Hooked ZoneChanged");

        OnZoneChanged(localRoomState.CurrentZone.Value, localRoomState.CurrentZone.Value);
    }

    private void OnZoneChanged(int oldZ, int newZ)
    {
        var zone = localRoomState.GetZone();
        inMeetingRoom = (zone == NetRoomState.Zone.Meeting);

        voiceOpVersion++;

        Debug.Log($"[VOICECOORD] OnZoneChanged triggered. Zone={zone}, inMeetingRoom={inMeetingRoom}");

        if (inMeetingRoom && autoJoinMeeting)
        {
            Debug.Log("[VOICECOORD] Triggering EnsureMeetingVoiceAsync...");
            _ = EnsureMeetingVoiceAsync(voiceOpVersion);
        }
        else
        {
            Debug.Log("[VOICECOORD] Triggering LeaveMeetingAsync...");
            if (string.IsNullOrEmpty(activePrivateChannel))
                _ = LeaveMeetingAsync(voiceOpVersion);
            else
                Debug.Log("[VOICECOORD] Skipped LeaveMeetingAsync because a private call is active.");
        }
    }

    private async Task EnsureMeetingVoiceAsync(int v)
    {
        Debug.Log($"[VOICECOORD] EnsureMeetingVoiceAsync Started. Version={v}");

        if (provider == null)
        {
            Debug.LogWarning("[VOICECOORD] Aborted: Provider is NULL!");
            return;
        }

        if (!string.IsNullOrEmpty(activePrivateChannel))
        {
            Debug.LogWarning($"[VOICECOORD] Aborted: Currently in a private call ({activePrivateChannel}). Cannot join meeting voice.");
            return;
        }

        string name = ResolveName();
        string channel = ResolveMeetingChannel();

        Debug.Log($"[VOICECOORD] Target Meeting Channel: {channel}");

        if (activeMeetingChannel == channel)
        {
            Debug.Log("[VOICECOORD] Aborted: Already in the target meeting channel.");
            return;
        }

        if (Application.platform == RuntimePlatform.Android)
        {
            if (!AndroidMicPermissionGate.HasMicPermission())
            {
                AndroidMicPermissionGate.RequestMicPermission();
                Debug.LogWarning("[VOICECOORD] Requested mic permission. Waiting for user...");
                return;
            }
        }

        try
        {
            Debug.Log("[VOICECOORD] Awaiting EnsureReadyAsync (Login)...");
            await provider.EnsureReadyAsync(name);
            Debug.Log($"[VOICECOORD] EnsureReadyAsync done. Provider IsLoggedIn = {provider.IsLoggedIn}");

            if (v != voiceOpVersion || !inMeetingRoom)
            {
                Debug.LogWarning("[VOICECOORD] Aborted: Zone or version changed while logging in.");
                return;
            }

            Debug.Log("[VOICECOORD] Awaiting LeaveCurrentAsync...");
            await provider.LeaveCurrentAsync();
            if (v != voiceOpVersion || !inMeetingRoom) return;

            Debug.Log($"[VOICECOORD] Awaiting JoinAsync for channel: {channel}...");
            await provider.JoinAsync(channel);
            if (v != voiceOpVersion || !inMeetingRoom) return;

            if(VoiceManager.Instance != null)
            {
                await VoiceManager.Instance.RefreshAudioStreamAsync();
            }
            ApplyCurrentMuteState();

            activeMeetingChannel = channel;
            Debug.Log("[VOICECOORD] Meeting voice ON " + channel);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VOICECOORD] EnsureMeetingVoice FAILED " + e);
        }
    }

    private async Task LeaveMeetingAsync(int v)
    {
        if (provider == null) return;
        if (string.IsNullOrEmpty(activeMeetingChannel))
        {
            await provider.LeaveCurrentAsync();
            return;
        }

        string old = activeMeetingChannel;

        try
        {
            Debug.Log($"[VOICECOORD] Leaving meeting channel: {old}");
            await provider.LeaveAsync(old);
            if (v != voiceOpVersion) return;

            Debug.Log("[VOICECOORD] Meeting voice OFF " + old);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VOICECOORD] LeaveMeeting FAILED " + e);
        }
        finally
        {
            if (activeMeetingChannel == old) activeMeetingChannel = null;
        }
    }

    private void ApplyCurrentMuteState()
    {
        if (_localPresenter == null)
        {
            var nm = Unity.Netcode.NetworkManager.Singleton;
            if (nm?.LocalClient?.PlayerObject != null)
                _localPresenter = nm.LocalClient.PlayerObject.GetComponent<PlayerVoicePresenter>();
        }

        bool shouldBeMuted = _localPresenter != null && _localPresenter.IsMuted;
        provider?.SetMute(shouldBeMuted);
    }

    // ====== Private Call ======

    public async Task<bool> StartPrivateCallAsync(string privateChannel)
    {
        if (provider == null) return false;

        if (Application.platform == RuntimePlatform.Android)
        {
            if (!AndroidMicPermissionGate.HasMicPermission())
            {
                AndroidMicPermissionGate.RequestMicPermission();
                Debug.LogWarning("[VOICECOORD] Requested mic permission for private call. Waiting for user...");
                return false;
            }
        }

        string name = ResolveName();
        activePrivateChannel = privateChannel;

        try
        {
            Debug.Log($"[VOICECOORD] Starting Private Call: {privateChannel}");
            await provider.EnsureReadyAsync(name);

            if (!string.IsNullOrEmpty(activeMeetingChannel))
                await provider.LeaveAsync(activeMeetingChannel);

            await provider.LeaveCurrentAsync();
            await provider.JoinAsync(privateChannel);

            if (VoiceManager.Instance != null)
            {
                await VoiceManager.Instance.RefreshAudioStreamAsync();
            }

            ApplyCurrentMuteState();

            Debug.Log("[VOICECOORD] Private voice ON " + privateChannel);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VOICECOORD] StartPrivateCall FAILED " + e);
            activePrivateChannel = null; 

            if (inMeetingRoom && autoJoinMeeting)
                await EnsureMeetingVoiceAsync(voiceOpVersion);

            return false;
        }
    }

    public async Task EndPrivateCallAsync()
    {
        if (provider == null) return;
        if (string.IsNullOrEmpty(activePrivateChannel)) return;

        string old = activePrivateChannel;
        activePrivateChannel = null;

        try
        {
            Debug.Log($"[VOICECOORD] Ending Private Call: {old}");
            await provider.LeaveAsync(old);
            Debug.Log("[VOICECOORD] Private voice OFF  " + old);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VOICECOORD] EndPrivateCall FAILED  " + e);
        }

        if (inMeetingRoom && autoJoinMeeting)
            await EnsureMeetingVoiceAsync(voiceOpVersion);
    }

    private string ResolveName()
    {
        if (GameSessionData.Instance != null && !string.IsNullOrWhiteSpace(GameSessionData.Instance.DisplayName))
            return GameSessionData.Instance.DisplayName;

        return PlayerPrefs.GetString("PLAYER_NAME", "Player");
    }

    private string ResolveMeetingChannel()
    {
        string org = (GameSessionData.Instance != null && !string.IsNullOrWhiteSpace(GameSessionData.Instance.OrgCode))
            ? GameSessionData.Instance.OrgCode
            : PlayerPrefs.GetString("ORG_ID", "ORG");

        string joinCode = (GameSessionData.Instance != null && !string.IsNullOrWhiteSpace(GameSessionData.Instance.LastJoinCode))
            ? GameSessionData.Instance.LastJoinCode
            : PlayerPrefs.GetString("JOIN_CODE", "");

        return string.IsNullOrEmpty(joinCode) ? $"ORG{org}" : VoiceChannelUtil.Build(org, joinCode);
    }
}