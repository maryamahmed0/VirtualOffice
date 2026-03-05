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
        if (provider == null)
            Debug.LogError("[VOICECOORD] ProviderBehaviour لازم يبقى Component بيطبق IVoiceProvider");
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
        // Local player = PlayerRoomState.LocalInstance موجود عندك وبيمثل اللاعب المحلي
        if (PlayerRoomState.LocalInstance == null) return;

        localRoomState = PlayerRoomState.LocalInstance.GetComponentInParent<NetRoomState>();
        if (localRoomState == null) return;

        localRoomState.CurrentZone.OnValueChanged -= OnZoneChanged;
        localRoomState.CurrentZone.OnValueChanged += OnZoneChanged;

        CancelInvoke(nameof(TryHookLocalZone));
        Debug.Log("[VOICECOORD] Hooked ZoneChanged ✅");

        // طبّق الحالة الحالية فورًا
        OnZoneChanged(localRoomState.CurrentZone.Value, localRoomState.CurrentZone.Value);
    }

    private void OnZoneChanged(int oldZ, int newZ)
    {
        var zone = localRoomState.GetZone();
        inMeetingRoom = (zone == NetRoomState.Zone.Meeting);

        voiceOpVersion++;

        if (inMeetingRoom && autoJoinMeeting)
        {
            _ = EnsureMeetingVoiceAsync(voiceOpVersion);
        }
        else
        {
            // لو مش في meeting، اقفل meeting voice (إلا لو في private call)
            if (string.IsNullOrEmpty(activePrivateChannel))
                _ = LeaveMeetingAsync(voiceOpVersion);
        }
    }

    private async Task EnsureMeetingVoiceAsync(int v)
    {
        if (provider == null) return;
        if (!string.IsNullOrEmpty(activePrivateChannel)) return;

        string name = ResolveName();
        string channel = ResolveMeetingChannel();

        if (activeMeetingChannel == channel) return;

        if (Application.platform == RuntimePlatform.Android)
        {
            if (!AndroidMicPermissionGate.HasMicPermission())
            {
                AndroidMicPermissionGate.RequestMicPermission();
                Debug.LogWarning("[VOICECOORD] Requested mic permission. Waiting for user...");
                return; // نوقف المحاولة دلوقتي
            }
        }

        try
        {
            await provider.EnsureReadyAsync(name);
            if (v != voiceOpVersion || !inMeetingRoom) return;

            await provider.LeaveCurrentAsync();
            if (v != voiceOpVersion || !inMeetingRoom) return;

            await provider.JoinAsync(channel);
            if (v != voiceOpVersion || !inMeetingRoom) return;

            provider.SetMute(false);

            activeMeetingChannel = channel;
            Debug.Log("[VOICECOORD] Meeting voice ON ✅ " + channel);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VOICECOORD] EnsureMeetingVoice FAILED ❌ " + e);
        }
    }

    private async Task LeaveMeetingAsync(int v)
    {
        if (provider == null) return;
        if (string.IsNullOrEmpty(activeMeetingChannel)) { await provider.LeaveCurrentAsync(); return; }

        string old = activeMeetingChannel;

        try
        {
            await provider.LeaveAsync(old);
            if (v != voiceOpVersion) return;

            Debug.Log("[VOICECOORD] Meeting voice OFF ✅ " + old);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VOICECOORD] LeaveMeeting FAILED ❌ " + e);
        }
        finally
        {
            if (activeMeetingChannel == old) activeMeetingChannel = null;
        }
    }

    // ====== Private Call (زي ما كان عندك) ======

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
            await provider.EnsureReadyAsync(name);

            if (!string.IsNullOrEmpty(activeMeetingChannel))
                await provider.LeaveAsync(activeMeetingChannel);

            await provider.LeaveCurrentAsync();
            await provider.JoinAsync(privateChannel);

            provider.SetMute(false);

            Debug.Log("[VOICECOORD] Private voice ON ✅ " + privateChannel);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VOICECOORD] StartPrivateCall FAILED ❌ " + e);
            activePrivateChannel = null;

            // لو رجعنا meeting
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
            await provider.LeaveAsync(old);
            Debug.Log("[VOICECOORD] Private voice OFF ✅ " + old);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VOICECOORD] EndPrivateCall FAILED ❌ " + e);
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