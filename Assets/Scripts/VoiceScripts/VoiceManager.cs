using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Vivox;
using System.Runtime.InteropServices;

public class VoiceManager : MonoBehaviour
{
    public static VoiceManager Instance { get; private set; }

    public bool IsLoggedIn { get; private set; }
    public string CurrentChannel { get; private set; }
    public bool IsMutedLocal { get; private set; }

    private readonly SemaphoreSlim _lock = new(1, 1);

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void ResumeWebAudioContext();

    [DllImport("__Internal")]
    private static extern void HardMuteWebMic(bool mute);

    [DllImport("__Internal")]
    private static extern void InitWebMicInterceptor();

    [DllImport("__Internal")]
    private static extern void StartPeriodicAudioUnlock();

    [DllImport("__Internal")]
    private static extern void StopPeriodicAudioUnlock();

    [DllImport("__Internal")]
    private static extern void CleanupAudioElements();
#endif

    private void Awake()
    {
        Debug.Log("[VOICE] VoiceManager Awake >>> REAL NUCLEAR MUTE <<<");
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_WEBGL && !UNITY_EDITOR
        try { InitWebMicInterceptor(); } catch { }
#endif
    }

    public async Task LoginAsync(string playerDisplayName)
    {
        if (IsLoggedIn) return;

        Debug.Log("[VOICE] Starting Login Flow...");
        _ = LoginInternalBackground(playerDisplayName);

        float timer = 0f;
        while (timer < 3.5f)
        {
            await Task.Yield();
            timer += Time.deltaTime;
            if (IsLoggedIn) break;
        }

        if (!IsLoggedIn)
        {
            IsLoggedIn = true;
        }
    }

    private async Task LoginInternalBackground(string playerDisplayName)
    {
        try
        {
            await UGSBootstrap.EnsureSignedIn();

#if UNITY_WEBGL && !UNITY_EDITOR
            try { ResumeWebAudioContext(); } catch { }
#endif

            await VivoxService.Instance.InitializeAsync();

            try
            {
                if (VivoxService.Instance.IsLoggedIn)
                {
                    Debug.Log("[VOICE] Already logged in, logging out first...");
                    await VivoxService.Instance.LogoutAsync();
                    Debug.Log("[VOICE] Logged out ");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[VOICE] Logout attempt failed (ignored): " + ex.Message);
            }

            await VivoxService.Instance.LoginAsync(new LoginOptions { DisplayName = playerDisplayName });

            IsLoggedIn = true;
            Debug.Log("[VOICE] Vivox official callback: Logged in ");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[VOICE] Background Login Exception: " + e.Message);
        }
    }

    public async Task JoinRoomVoiceAsync(string channelName)
    {
        if (!IsLoggedIn) return;
        if (CurrentChannel == channelName) return;

        await _lock.WaitAsync();
        try
        {
            if (CurrentChannel == channelName) return;

            _ = VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly);

            float timer = 0f;
            while (timer < 1.5f)
            {
                await Task.Yield();
                timer += Time.deltaTime;
            }

            CurrentChannel = channelName;
            Debug.Log("[VOICE] Joined channel  " + channelName);

#if UNITY_WEBGL && !UNITY_EDITOR
            try { StartPeriodicAudioUnlock(); } catch { }
            Debug.Log("[VOICE] Periodic Audio Unlock started ");
#endif

            SetMute(IsMutedLocal);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VOICE] Join FAILED  " + e.Message);
        }
        finally
        {
            _lock.Release();
        }
    }

    public void SetMute(bool mute)
    {
        try
        {
            if (!IsLoggedIn) return;

            IsMutedLocal = mute;

            if (mute)
            {
                VivoxService.Instance.MuteInputDevice();

                if (!string.IsNullOrEmpty(CurrentChannel))
                    _ = VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.None);
            }
            else
            {
                VivoxService.Instance.UnmuteInputDevice();
                if (!string.IsNullOrEmpty(CurrentChannel))
                    _ = VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.All);
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            HardMuteWebMic(mute);
#endif
            Debug.Log("[VOICE] Mute=" + mute + " (Real Hard Mute Applied)");
        }
        catch { }
    }

    public async Task LeaveCurrentAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!IsLoggedIn || string.IsNullOrEmpty(CurrentChannel)) return;

            string toLeave = CurrentChannel;
            CurrentChannel = null;

#if UNITY_WEBGL && !UNITY_EDITOR
            try { CleanupAudioElements(); } catch { }
            try { StopPeriodicAudioUnlock(); } catch { }
            Debug.Log("[VOICE] Periodic Audio Unlock stopped ");
#endif

            _ = VivoxService.Instance.LeaveChannelAsync(toLeave);

            float timer = 0f;
            while (timer < 0.5f) { await Task.Yield(); timer += Time.deltaTime; }

            Debug.Log("[VOICE] Left  " + toLeave);
        }
        finally { _lock.Release(); }
    }

    public async Task LeaveChannelAsync(string channelName)
    {
        if (string.IsNullOrEmpty(channelName)) return;

        await _lock.WaitAsync();
        try
        {
            if (!IsLoggedIn) return;

#if UNITY_WEBGL && !UNITY_EDITOR
            try { CleanupAudioElements(); } catch { }
            try { StopPeriodicAudioUnlock(); } catch { }
            Debug.Log("[VOICE] Periodic Audio Unlock stopped ");
#endif

            _ = VivoxService.Instance.LeaveChannelAsync(channelName);
            if (CurrentChannel == channelName) CurrentChannel = null;

            float timer = 0f;
            while (timer < 0.5f) { await Task.Yield(); timer += Time.deltaTime; }

            Debug.Log("[VOICE] Left  " + channelName);
        }
        finally { _lock.Release(); }
    }

    public async Task RefreshAudioStreamAsync()
    {
        if (!IsLoggedIn) return;
        try
        {
            VivoxService.Instance.MuteInputDevice();
            VivoxService.Instance.MuteOutputDevice();

            float timer = 0f;
            while (timer < 0.1f) { await Task.Yield(); timer += Time.deltaTime; }

            if (!IsMutedLocal)
            {
                VivoxService.Instance.UnmuteInputDevice();
            }
            VivoxService.Instance.UnmuteOutputDevice();

#if UNITY_WEBGL && !UNITY_EDITOR
            try { StartPeriodicAudioUnlock(); } catch { }
            Debug.Log("[VOICE] Periodic Audio Unlock re-started after refresh ");
#endif
        }
        catch { }
    }
}