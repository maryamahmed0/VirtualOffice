public interface IVoiceProvider
{
    bool IsReady { get; }
    bool IsLoggedIn { get; }

    System.Threading.Tasks.Task EnsureReadyAsync(string displayName);
    System.Threading.Tasks.Task JoinAsync(string channel);
    System.Threading.Tasks.Task LeaveAsync(string channel);
    System.Threading.Tasks.Task LeaveCurrentAsync();

    void SetMute(bool mute);
}