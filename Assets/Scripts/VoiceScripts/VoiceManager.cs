using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Vivox;

public class VoiceManager : MonoBehaviour
{
    public static VoiceManager Instance { get; private set; }

    public bool IsLoggedIn { get; private set; }
    public string CurrentChannel { get; private set; }

    private void Awake()
    {
        Debug.Log("[VOICE] VoiceManager Awake ✅");
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    public async Task LoginAsync(string playerDisplayName)
    {
        if (IsLoggedIn) return;

        try
        {
            Debug.Log("[VOICE] Initialize...");
            await VivoxService.Instance.InitializeAsync();

            Debug.Log("[VOICE] Login as: " + playerDisplayName);
            await VivoxService.Instance.LoginAsync(new LoginOptions { DisplayName = playerDisplayName });

            IsLoggedIn = true;
            Debug.Log("[VOICE] Logged in ✅");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VOICE] Login FAILED ❌ " + e);
        }
    }

    public async Task JoinRoomVoiceAsync(string channelName)
    {
        try
        {
            Debug.Log("[VOICE] Join channel: " + channelName);

            await VivoxService.Instance.JoinGroupChannelAsync(
                channelName,
                ChatCapability.TextAndAudio // أو AudioOnly لو موجود عندك
            );

            CurrentChannel = channelName;
            Debug.Log("[VOICE] Joined channel ✅ " + channelName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VOICE] Join FAILED ❌ " + e);
        }
    }
    public void SetMute(bool mute)
    {
        if (mute) VivoxService.Instance.MuteInputDevice();
        else VivoxService.Instance.UnmuteInputDevice();

        Debug.Log("[VOICE] Mute=" + mute);
    }
}