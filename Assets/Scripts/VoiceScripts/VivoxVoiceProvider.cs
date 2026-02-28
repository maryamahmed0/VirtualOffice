using System.Threading.Tasks;
using UnityEngine;

public class VivoxVoiceProvider : MonoBehaviour, IVoiceProvider
{
    public bool IsReady => VoiceManager.Instance != null;
    public bool IsLoggedIn => VoiceManager.Instance != null && VoiceManager.Instance.IsLoggedIn;

    public Task EnsureReadyAsync(string displayName)
        => VoiceManager.Instance.LoginAsync(displayName);

    public Task JoinAsync(string channel)
        => VoiceManager.Instance.JoinRoomVoiceAsync(channel);

    public Task LeaveAsync(string channel)
        => VoiceManager.Instance.LeaveChannelAsync(channel);

    public Task LeaveCurrentAsync()
        => VoiceManager.Instance.LeaveCurrentAsync();

    public void SetMute(bool mute)
        => VoiceManager.Instance.SetMute(mute);
}