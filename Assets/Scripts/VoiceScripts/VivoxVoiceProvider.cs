using System.Threading.Tasks;
using UnityEngine;

public class VivoxVoiceProvider : MonoBehaviour, IVoiceProvider
{
    public bool IsReady => VoiceManager.Instance != null;
    public bool IsLoggedIn => VoiceManager.Instance != null && VoiceManager.Instance.IsLoggedIn;

    public async Task EnsureReadyAsync(string displayName)
    {
        if (IsLoggedIn) return;

     
#pragma warning disable CS4014
        VoiceManager.Instance.LoginAsync(displayName);
#pragma warning restore CS4014

        int waitSteps = 0;
        while (!IsLoggedIn && waitSteps < 40)
        {
            await Task.Delay(200);
            waitSteps++;
        }

        if (!IsLoggedIn)
        {
            Debug.LogWarning("[VOICE PROVIDER] EnsureReadyAsync timed out, but continuing...");
        }
    }

    public Task JoinAsync(string channel)
        => VoiceManager.Instance.JoinRoomVoiceAsync(channel);

    public Task LeaveAsync(string channel)
        => VoiceManager.Instance.LeaveChannelAsync(channel);

    public Task LeaveCurrentAsync()
        => VoiceManager.Instance.LeaveCurrentAsync();

    public void SetMute(bool mute)
        => VoiceManager.Instance.SetMute(mute);
}