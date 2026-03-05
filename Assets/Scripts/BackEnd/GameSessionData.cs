using UnityEngine;

public class GameSessionData : MonoBehaviour
{
    public static GameSessionData Instance { get; private set; }

    public string DisplayName { get; private set; }
    public string OrgCode { get; private set; }

    public string TeamId { get; private set; } = "TECH";
    public int TeamSize { get; private set; } = 8;

    public string LastJoinCode { get; private set; } = "";
    public bool IsHost { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        
        TeamId = PlayerPrefs.GetString("TEAM_ID", "TECH").ToUpperInvariant();
        TeamSize = PlayerPrefs.GetInt("TEAM_SIZE", 8);
    }

    public void SetUser(string displayName, string orgCode)
    {
        DisplayName = displayName;
        OrgCode = orgCode;
    }

    public void SetTeam(string teamId, int teamSize)
    {
        TeamId = string.IsNullOrWhiteSpace(teamId) ? "TECH" : teamId.Trim().ToUpperInvariant();
        TeamSize = Mathf.Clamp(teamSize, 1, 50);
    }

    public void SetConnectionInfo(bool isHost, string joinCode)
    {
        IsHost = isHost;
        LastJoinCode = joinCode;
    }
}