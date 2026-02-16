using UnityEngine;

public class GameSessionData : MonoBehaviour
{
    public static GameSessionData Instance { get; private set; }

    public string DisplayName { get; private set; }
    public string OrgCode { get; private set; }

    // ✅ جديد
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
    }

    public void SetUser(string displayName, string orgCode)
    {
        DisplayName = displayName;
        OrgCode = orgCode;
    }

    // ✅ جديد
    public void SetConnectionInfo(bool isHost, string joinCode)
    {
        IsHost = isHost;
        LastJoinCode = joinCode;
    }
}