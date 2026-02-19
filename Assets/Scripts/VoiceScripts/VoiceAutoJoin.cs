using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VoiceAutoJoin : MonoBehaviour
{
    [SerializeField] private string meetingSceneName = "MeetingRoom";
    private bool joined;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private async void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (joined) return;
        if (scene.name != meetingSceneName) return;

        // اقرأي البيانات من GameSessionData أو fallback من PlayerPrefs
        string name = (GameSessionData.Instance != null && !string.IsNullOrWhiteSpace(GameSessionData.Instance.DisplayName))
            ? GameSessionData.Instance.DisplayName
            : PlayerPrefs.GetString("PLAYER_NAME", "Player");

        string org = (GameSessionData.Instance != null && !string.IsNullOrWhiteSpace(GameSessionData.Instance.OrgCode))
            ? GameSessionData.Instance.OrgCode
            : PlayerPrefs.GetString("ORG_ID", "ORG");

        string joinCode = (GameSessionData.Instance != null && !string.IsNullOrWhiteSpace(GameSessionData.Instance.LastJoinCode))
            ? GameSessionData.Instance.LastJoinCode
            : "";

        // لو joinCode فاضي لأي سبب، خليها روم على org بس (اختياري)
        string channel = string.IsNullOrEmpty(joinCode)
            ? $"ORG{org}"
            : VoiceChannelUtil.Build(org, joinCode);

        Debug.Log($"[VOICE] AutoJoin scene={scene.name} channel={channel}");

        try
        {
            await EnsureVoiceReady(name, channel);
            joined = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[VOICE] AutoJoin failed: " + e);
        }
    }

    private static async Task EnsureVoiceReady(string name, string channel)
    {
        
        await VoiceManager.Instance.LoginAsync(name);
        await VoiceManager.Instance.JoinRoomVoiceAsync(channel);
    }
}