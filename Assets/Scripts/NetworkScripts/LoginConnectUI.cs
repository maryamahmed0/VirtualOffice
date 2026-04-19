using TMPro;
using UnityEngine;
using Unity.Netcode;
using System.Text;
using System.Threading.Tasks;

public class LobbyConnectUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField orgInput;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private TMP_Text joinCodeText;

    [Header("Team (NEW)")]
    [SerializeField] private TMP_InputField teamIdInput;
    [SerializeField] private TMP_InputField teamSizeInput;

    [Header("Avatar (NEW)")]
    [SerializeField] private TMP_Dropdown genderDropdown; 

    private RelayConnector relay;
    private bool busy;
    private static string lastPayload;

    private void Awake()
    {
        relay = FindFirstObjectByType<RelayConnector>();

        if (teamIdInput != null) teamIdInput.text = PlayerPrefs.GetString("TEAM_ID", "TECH");
        if (teamSizeInput != null) teamSizeInput.text = PlayerPrefs.GetInt("TEAM_SIZE", 8).ToString();
        if (genderDropdown != null) genderDropdown.value = PlayerPrefs.GetInt("PLAYER_GENDER", 0);
    }

    private void ApplyConnectionPayload(string playerName, string org, string teamId, int teamSize, bool isGirl)
    {
        var nm = NetworkManager.Singleton;
        string genderFlag = isGirl ? "F" : "M";
        var payload = $"{playerName}|{org}|{teamId}|{teamSize}|{genderFlag}";
        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        nm.NetworkConfig.ConnectionData = bytes;
        lastPayload = payload;
    }

    private static string SanitizeJoinCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "";
        StringBuilder sb = new StringBuilder(code.Length);
        foreach (char c in code)
            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToUpperInvariant(c));
        string cleaned = sb.ToString();
        if (cleaned.Length > 6) cleaned = cleaned.Substring(cleaned.Length - 6, 6);
        return cleaned;
    }

    private static string SanitizeTeamId(string team)
    {
        if (string.IsNullOrWhiteSpace(team)) return "TECH";
        team = team.Trim().ToUpperInvariant();
        var sb = new StringBuilder(team.Length);
        foreach (var c in team)
            if (char.IsLetterOrDigit(c) || c == '_')
                sb.Append(c);
        return sb.Length == 0 ? "TECH" : sb.ToString();
    }

    private static int SanitizeTeamSize(string s)
    {
        if (int.TryParse(s, out int v)) return Mathf.Clamp(v, 1, 50);
        return 8;
    }

    public async void OnEnterClicked()
    {
        if (busy) return;
        busy = true;

        if (Application.platform == RuntimePlatform.WebGLPlayer)
            WebVoiceGate.MarkUserGesture();

        if (Application.platform == RuntimePlatform.Android)
        {
            if (!AndroidMicPermissionGate.HasMicPermission())
                AndroidMicPermissionGate.RequestMicPermission();
        }

        errorText.text = "";
        statusText.text = "Connecting...";

        string displayName = nameInput != null ? nameInput.text.Trim() : "";
        string org = orgInput != null ? orgInput.text.Trim().ToUpperInvariant() : "";
        string joinCode = SanitizeJoinCode(joinCodeInput != null ? joinCodeInput.text : "");
        string teamId = SanitizeTeamId(teamIdInput != null ? teamIdInput.text : "");
        int teamSize = SanitizeTeamSize(teamSizeInput != null ? teamSizeInput.text : "8");
        bool isGirl = genderDropdown != null && genderDropdown.value == 1;

        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(org))
        {
            errorText.text = "Name and Org ID are required.";
            statusText.text = "";
            busy = false;
            return;
        }

        PlayerPrefs.SetString("PLAYER_NAME", displayName);
        PlayerPrefs.SetString("ORG_ID", org);
        PlayerPrefs.SetString("TEAM_ID", teamId);
        PlayerPrefs.SetInt("TEAM_SIZE", teamSize);
        if (genderDropdown != null) PlayerPrefs.SetInt("PLAYER_GENDER", genderDropdown.value);
        PlayerPrefs.Save();

        if (GameSessionData.Instance != null)
        {
            GameSessionData.Instance.SetUser(displayName, org);
            GameSessionData.Instance.SetTeam(teamId, teamSize);
        }

        try
        {
#pragma warning disable CS4014
            VoiceManager.Instance.LoginAsync(displayName);
#pragma warning restore CS4014

            statusText.text = "Connecting to Network...";

            if (relay == null) relay = FindFirstObjectByType<RelayConnector>();
            var nm = NetworkManager.Singleton;

            if (relay == null || nm == null || nm.IsListening)
            {
                errorText.text = "Network Error. Retry.";
                return;
            }

            ApplyConnectionPayload(displayName, org, teamId, teamSize, isGirl);

            if (string.IsNullOrEmpty(joinCode))
            {
                statusText.text = "Creating room...";
                int teamIdHash = Animator.StringToHash(teamId);
                int layoutIndex = teamSize <= 8 ? 0 : (teamSize <= 12 ? 1 : 2);
                GlobalRoomContext.Instance.SetLobbyData(displayName, org, joinCode, teamIdHash, teamSize, layoutIndex);

                string code = await relay.CreateRoomAndHost(displayName, org);

                PlayerPrefs.SetString("JOIN_CODE", code);
                PlayerPrefs.Save();
                GameSessionData.Instance?.SetConnectionInfo(true, code);

                if (joinCodeText != null) joinCodeText.text = $"Join Code: {code}";
                if (joinCodeInput != null) joinCodeInput.text = code;
                GUIUtility.systemCopyBuffer = code;
                statusText.text = "Room created!";
            }
            else
            {
                if (joinCode.Length != 6)
                {
                    errorText.text = "Join code must be 6 chars.";
                    return;
                }
                statusText.text = "Joining room...";
                GameSessionData.Instance?.SetConnectionInfo(false, joinCode);

                int teamIdHash = Animator.StringToHash(teamId);
                int layoutIndex = teamSize <= 8 ? 0 : (teamSize <= 12 ? 1 : 2);
                GlobalRoomContext.Instance.SetLobbyData(displayName, org, joinCode, teamIdHash, teamSize, layoutIndex);

                await relay.JoinRoomAndClient(joinCode, displayName, org);

                PlayerPrefs.SetString("JOIN_CODE", joinCode);
                PlayerPrefs.Save();
                statusText.text = "Joined!";
            }
        }
        catch (System.Exception e)
        {
            errorText.text = "Failed to connect.";
        }
        finally
        {
            busy = false;
        }
    }
}