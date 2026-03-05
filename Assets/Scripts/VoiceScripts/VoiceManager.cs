using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Vivox;

public class VoiceManager : MonoBehaviour
{
    public static VoiceManager Instance { get; private set; }

    public bool IsLoggedIn { get; private set; }
    public string CurrentChannel { get; private set; }

    private readonly SemaphoreSlim _lock = new(1, 1);
    private Task _loginTask;
    private Task _joinTask;
    private string _joiningChannel;

    private void Awake()
    {
        Debug.Log("[VOICE] VoiceManager Awake >>> NEW VERSION <<<");
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task LoginAsync(string playerDisplayName)
    {
        await _lock.WaitAsync();
        try
        {
            if (IsLoggedIn) return;
            _loginTask ??= LoginInternal(playerDisplayName);
        }
        finally { _lock.Release(); }

        await _loginTask;
    }

    private async Task LoginInternal(string playerDisplayName)
    {
        try
        {
            await UGSBootstrap.EnsureSignedIn();
            Debug.Log("[VOICE] UGS signed-in OK, starting Vivox init...");

            Debug.Log("[VOICE] Initialize...");
            await VivoxService.Instance.InitializeAsync();

            const int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    Debug.Log($"[VOICE] Login attempt {attempt} as: {playerDisplayName}");
                    await VivoxService.Instance.LoginAsync(new LoginOptions { DisplayName = playerDisplayName });

                    IsLoggedIn = true;
                    Debug.Log("[VOICE] Logged in ✅");
                    return;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[VOICE] Login attempt {attempt} failed:\n{e}");

                    await TryLogoutReset(); 

                    if (attempt == maxAttempts) throw;

                    await Task.Delay(1000 * attempt);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VOICE] Login FAILED ❌ " + e);
            IsLoggedIn = false;
            throw;
        }
        finally
        {
            _loginTask = null; 
        }
    }

    public async Task JoinRoomVoiceAsync(string channelName)
    {
        Task loginWait = null;

        await _lock.WaitAsync();
        try
        {
            loginWait = _loginTask; 
            if (IsLoggedIn == false && loginWait == null)
                throw new System.InvalidOperationException("Join before login");

            if (CurrentChannel == channelName) return;

            if (_joinTask != null && _joiningChannel == channelName) { }
            else
            {
                _joiningChannel = channelName;
                _joinTask = JoinInternal(channelName);
            }
        }
        finally { _lock.Release(); }

        if (loginWait != null) await loginWait; 
        await _joinTask;
    }

    private async Task JoinInternal(string channelName)
    {
        try
        {
            Debug.Log("[VOICE] Join channel: " + channelName);

            await VivoxService.Instance.JoinGroupChannelAsync(
                channelName,
                ChatCapability.AudioOnly
            );

            CurrentChannel = channelName;
            Debug.Log("[VOICE] Joined channel ✅ " + channelName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VOICE] Join FAILED ❌ " + e);
            throw;
        }
        finally
        {
            _joinTask = null;
            _joiningChannel = null;
        }
    }

    public void SetMute(bool mute)
    {
        try
        {
            if (!IsLoggedIn)
            {
                Debug.LogWarning("[VOICE] SetMute called before login.");
                return;
            }

            if (mute) VivoxService.Instance.MuteInputDevice();
            else VivoxService.Instance.UnmuteInputDevice();

            Debug.Log("[VOICE] Mute=" + mute);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VOICE] SetMute FAILED ❌ " + e);
        }
    }
    public async Task LeaveCurrentAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!IsLoggedIn) return;
            if (string.IsNullOrEmpty(CurrentChannel)) return;

            string toLeave = CurrentChannel;
            CurrentChannel = null;

            Debug.Log("[VOICE] Leave current: " + toLeave);
            await VivoxService.Instance.LeaveChannelAsync(toLeave);
            Debug.Log("[VOICE] Left ✅ " + toLeave);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VOICE] LeaveCurrent FAILED ❌ " + e);
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

            Debug.Log("[VOICE] Leave channel: " + channelName);
            await VivoxService.Instance.LeaveChannelAsync(channelName);

            if (CurrentChannel == channelName) CurrentChannel = null;

            Debug.Log("[VOICE] Left ✅ " + channelName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VOICE] LeaveChannel FAILED ❌ " + e);
        }
        finally { _lock.Release(); }
    }
    private async Task TryLogoutReset()
    {
        try
        {
       
            await VivoxService.Instance.LogoutAsync();
            Debug.Log("[VOICE] Logout reset done");
        }
        catch
        {

        }

        IsLoggedIn = false;
        CurrentChannel = null;
    }
}