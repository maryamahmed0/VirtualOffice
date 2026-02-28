using System.Threading.Tasks;
using UnityEngine;

public class VoiceCoordinator : MonoBehaviour
{
    [Header("Provider (Vivox الآن / WebRTC لاحقاً)")]
    [SerializeField] private MonoBehaviour providerBehaviour;
    private IVoiceProvider provider;

    [Header("Meeting")]
    [SerializeField] private string meetingRoomId = "meeting_main";
    [SerializeField] private bool autoJoinMeeting = true;

    [Header("State (debug)")]
    [SerializeField] private string activeMeetingChannel;
    [SerializeField] private string activePrivateChannel;
    [SerializeField] private bool inMeetingRoom;

    private static VoiceCoordinator _instance;

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
        InvokeRepeating(nameof(TryHookRoomEvents), 0.2f, 0.2f);
    }

    private void OnDisable()
    {
        if (PlayerRoomState.LocalInstance != null)
            PlayerRoomState.LocalInstance.OnLocalRoomChanged -= OnRoomChanged;

        CancelInvoke(nameof(TryHookRoomEvents));
    }

    private void TryHookRoomEvents()
    {
        if (PlayerRoomState.LocalInstance == null) return;

        PlayerRoomState.LocalInstance.OnLocalRoomChanged -= OnRoomChanged;
        PlayerRoomState.LocalInstance.OnLocalRoomChanged += OnRoomChanged;

        CancelInvoke(nameof(TryHookRoomEvents));
        Debug.Log("[VOICECOORD] Hooked RoomChanged ✅");
    }

    private void OnRoomChanged(RoomContext oldRoom, RoomContext newRoom)
    {
        if (newRoom == null) return;

        inMeetingRoom = (newRoom.roomType == RoomType.Meeting);

        if (inMeetingRoom && autoJoinMeeting)
        {
            _ = EnsureMeetingVoiceAsync(newRoom);
        }
        else
        {
            if (string.IsNullOrEmpty(activePrivateChannel))
                _ = LeaveMeetingAsync();
        }
    }

    private async Task EnsureMeetingVoiceAsync(RoomContext meetingCtx)
    {
        if (provider == null) return;
        if (!string.IsNullOrEmpty(activePrivateChannel)) return;

        string name = ResolveName();
        string channel = ResolveMeetingChannel(meetingCtx);

        if (activeMeetingChannel == channel) return;
        activeMeetingChannel = channel;

        try
        {
            await provider.EnsureReadyAsync(name);

            await provider.LeaveCurrentAsync();
            await provider.JoinAsync(channel);

            // ✅ مهم: افتحي المايك بعد Join (عشان مفيش صوت)
            provider.SetMute(false);

            Debug.Log("[VOICECOORD] Meeting voice ON ✅ " + channel);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VOICECOORD] EnsureMeetingVoice FAILED ❌ " + e);
        }
    }

    private async Task LeaveMeetingAsync()
    {
        if (provider == null) return;
        if (string.IsNullOrEmpty(activeMeetingChannel)) return;

        try
        {
            await provider.LeaveAsync(activeMeetingChannel);
            Debug.Log("[VOICECOORD] Meeting voice OFF ✅ " + activeMeetingChannel);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VOICECOORD] LeaveMeeting FAILED ❌ " + e);
        }
        finally
        {
            activeMeetingChannel = null;
        }
    }
    public async Task<bool> StartPrivateCallAsync(string privateChannel)
    {
        if (provider == null) return false;

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

            if (inMeetingRoom && autoJoinMeeting && PlayerRoomState.LocalInstance?.CurrentContext != null)
                await EnsureMeetingVoiceAsync(PlayerRoomState.LocalInstance.CurrentContext);

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

        if (inMeetingRoom && autoJoinMeeting && PlayerRoomState.LocalInstance?.CurrentContext != null)
        {
            await EnsureMeetingVoiceAsync(PlayerRoomState.LocalInstance.CurrentContext);
        }
    }

    private string ResolveName()
    {
        if (GameSessionData.Instance != null && !string.IsNullOrWhiteSpace(GameSessionData.Instance.DisplayName))
            return GameSessionData.Instance.DisplayName;

        return PlayerPrefs.GetString("PLAYER_NAME", "Player");
    }

    private string ResolveMeetingChannel(RoomContext ctx)
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