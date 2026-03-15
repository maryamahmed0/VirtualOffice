using TMPro;
using UnityEngine;
using Unity.Netcode;
using System.Text;

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

    private RelayConnector relay;
    private bool busy;
    private static string lastPayload;

    private void Awake()
    {
        relay = FindFirstObjectByType<RelayConnector>();

        // prefill
        if (teamIdInput != null) teamIdInput.text = PlayerPrefs.GetString("TEAM_ID", "TECH");
        if (teamSizeInput != null) teamSizeInput.text = PlayerPrefs.GetInt("TEAM_SIZE", 8).ToString();
    }

    // payload: name|org|teamId|teamSize
    private void ApplyConnectionPayload(string playerName, string org, string teamId, int teamSize)
    {
        var nm = NetworkManager.Singleton;
        var payload = $"{playerName}|{org}|{teamId}|{teamSize}";
        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        nm.NetworkConfig.ConnectionData = bytes;

        lastPayload = payload;
        Debug.Log($"[PAYLOAD] set >>>{payload}<<< bytes={bytes.Length}");
    }

    private static string SanitizeJoinCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "";

        StringBuilder sb = new StringBuilder(code.Length);
        foreach (char c in code)
            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToUpperInvariant(c));

        string cleaned = sb.ToString();
        if (cleaned.Length > 6)
            cleaned = cleaned.Substring(cleaned.Length - 6, 6);

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
        if (int.TryParse(s, out int v))
            return Mathf.Clamp(v, 1, 50);
        return 8;
    }

    public async void OnEnterClicked()
    {
        if (busy) return;
        busy = true;

        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            WebVoiceGate.MarkUserGesture();
            Debug.Log("[WEB] Enter clicked - gesture sent");
        }
        // Android mic permission (popup)
        if (Application.platform == RuntimePlatform.Android)
        {
            if (!AndroidMicPermissionGate.HasMicPermission())
                AndroidMicPermissionGate.RequestMicPermission();
        }
        errorText.text = "";
        statusText.text = "Connecting...";

        string displayName = nameInput != null ? nameInput.text.Trim() : "";
        string org = orgInput != null ? orgInput.text.Trim().ToUpperInvariant() : "";

        string joinCodeRaw = joinCodeInput != null ? joinCodeInput.text : "";
        string joinCode = SanitizeJoinCode(joinCodeRaw);

        // TEAM
        string teamIdRaw = teamIdInput != null ? teamIdInput.text : "";
        string teamSizeRaw = teamSizeInput != null ? teamSizeInput.text : "8";

        string teamId = SanitizeTeamId(teamIdRaw);
        int teamSize = SanitizeTeamSize(teamSizeRaw);

        // Validate basic fields
        if (string.IsNullOrWhiteSpace(displayName))
        {
            errorText.text = "Please enter a name.";
            statusText.text = "";
            busy = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(org))
        {
            errorText.text = "Please enter Org ID.";
            statusText.text = "";
            busy = false;
            return;
        }

        // Save locally
        PlayerPrefs.SetString("PLAYER_NAME", displayName);
        PlayerPrefs.SetString("ORG_ID", org);
        PlayerPrefs.SetString("TEAM_ID", teamId);
        PlayerPrefs.SetInt("TEAM_SIZE", teamSize);
        PlayerPrefs.Save();

        // Save to session data
        if (GameSessionData.Instance != null)
        {
            GameSessionData.Instance.SetUser(displayName, org);
            GameSessionData.Instance.SetTeam(teamId, teamSize);
        }

        try
        {
            if (relay == null)
                relay = FindFirstObjectByType<RelayConnector>();

            if (relay == null)
            {
                errorText.text = "RelayConnector not found in scene.";
                statusText.text = "";
                return;
            }

            var nm = NetworkManager.Singleton;
            if (nm == null)
            {
                errorText.text = "NetworkManager.Singleton is NULL.";
                statusText.text = "";
                return;
            }

            if (nm.IsListening)
            {
                statusText.text = "Already running (Host/Client). Stop and retry.";
                return;
            }

            // apply payload BEFORE starting host/client
            ApplyConnectionPayload(displayName, org, teamId, teamSize);
            Debug.Log("[PAYLOAD] FINAL >>>" + lastPayload + "<<<");

            if (string.IsNullOrEmpty(joinCode))
            {
                statusText.text = "Creating room...";

                int teamIdHash = Animator.StringToHash(teamId);
                int layoutIndex = teamSize <= 8 ? 0 : (teamSize <= 12 ? 1 : 2);

                GlobalRoomContext.Instance.SetLobbyData(displayName, org, joinCode, teamIdHash, teamSize, layoutIndex);
                // Host
                string code = await relay.CreateRoomAndHost(displayName, org);

                PlayerPrefs.SetString("JOIN_CODE", code);
                PlayerPrefs.Save();

                GameSessionData.Instance?.SetConnectionInfo(true, code);

                statusText.text = "Room created. Share join code:";
                if (joinCodeText != null) joinCodeText.text = $"Join Code: {code}";
                if (joinCodeInput != null) joinCodeInput.text = code;

                GUIUtility.systemCopyBuffer = code;
                Debug.Log($"[UI] Copied JoinCode to clipboard: >>>{code}<<<");
            }
            else
            {
                if (joinCode.Length != 6)
                {
                    errorText.text = $"Join code should be 6 chars. You entered: '{joinCode}' (len={joinCode.Length})";
                    statusText.text = "";
                    return;
                }

                statusText.text = "Joining room...";
                GameSessionData.Instance?.SetConnectionInfo(false, joinCode);

                Debug.Log($"[UI] Joining with EXACT >>>{joinCode}<<< len={joinCode.Length}");

                int teamIdHash = Animator.StringToHash(teamId);
                int layoutIndex = teamSize <= 8 ? 0 : (teamSize <= 12 ? 1 : 2);

                GlobalRoomContext.Instance.SetLobbyData(displayName, org, joinCode, teamIdHash, teamSize, layoutIndex);
                await relay.JoinRoomAndClient(joinCode, displayName, org);

                PlayerPrefs.SetString("JOIN_CODE", joinCode);
                PlayerPrefs.Save();

                statusText.text = "Joined!";
            }
        }
        catch (Unity.Services.Core.RequestFailedException e)
        {
            Debug.LogError($"[UGS] RequestFailed: Status={e.ErrorCode} Msg={e.Message}");
            statusText.text = "";
            errorText.text = $"Failed ({e.ErrorCode}). {e.Message}";
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            statusText.text = "";
            errorText.text = "Failed. Check console.";
        }
        finally
        {
            busy = false;
        }
    }
}