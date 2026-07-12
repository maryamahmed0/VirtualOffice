using TMPro;
using UnityEngine;
using Unity.Netcode;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.Networking; 

public class LobbyConnectUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField orgInput;
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private TMP_Text joinCodeText;

    [Header("Team")]
    [SerializeField] private TMP_InputField teamIdInput;
    [SerializeField] private TMP_InputField teamSizeInput;

    [Header("Avatar")]
    [SerializeField] private TMP_Dropdown genderDropdown;

    private RelayConnector relay;
    private WorkspaceSessionService sessionService;
    private bool busy;
    private static string lastPayload;

    private void Awake()
    {
        relay = FindFirstObjectByType<RelayConnector>();

        sessionService = GetComponent<WorkspaceSessionService>();
        if (sessionService == null)
            sessionService = gameObject.AddComponent<WorkspaceSessionService>();

        if (teamIdInput != null) teamIdInput.text = PlayerPrefs.GetString("TEAM_ID", "TECH");
        if (teamSizeInput != null) teamSizeInput.text = PlayerPrefs.GetInt("TEAM_SIZE", 8).ToString();
        if (genderDropdown != null) genderDropdown.value = PlayerPrefs.GetInt("PLAYER_GENDER", 0);
    }

    private void ApplyConnectionPayload(string playerName,
        string org, string teamId, int teamSize,
        bool isGirl)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        string genderFlag = isGirl ? "F" : "M";
        string payload = $"{playerName}|{org}|{teamId}|{teamSize}|{genderFlag}";
        nm.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(payload);
        lastPayload = payload;
    }

    private static string SanitizeJoinCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "";
        var sb = new StringBuilder(code.Length);
        foreach (char c in code)
            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToUpperInvariant(c));

        string cleaned = sb.ToString();
        if (cleaned.Length > 6) cleaned = cleaned.Substring(cleaned.Length - 6, 6);
        return cleaned;
    }

    private static string SanitizeTeamId(string team)
    {
        return string.IsNullOrWhiteSpace(team)
            ? ""
            : team.Trim();
    }

    private static int SanitizeTeamSize(string s)
    {
        if (int.TryParse(s, out int v)) return Mathf.Clamp(v, 1, 50);
        return 8;
    }

    public void OnEnterClicked()
    {
        if (busy) return;
        busy = true;
        StartCoroutine(OnEnterFlow());
    }

    private IEnumerator OnEnterFlow()
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            WebVoiceGate.MarkUserGesture();

        if (Application.platform == RuntimePlatform.Android)
        {
            if (!AndroidMicPermissionGate.HasMicPermission())
                AndroidMicPermissionGate.RequestMicPermission();
        }

        errorText.text = "";
        statusText.text = "Connecting...";

        string displayName, org, teamId;
        int teamSize;
        bool isGirl;

        if (AuthBridge.IsReady)
        {
            displayName = AuthBridge.DisplayName;
            org = AuthBridge.OrgCode.ToUpperInvariant();
            teamId = SanitizeTeamId(AuthBridge.TeamId);
            teamSize = AuthBridge.TeamSize > 0 ? AuthBridge.TeamSize : 8;
            isGirl = AuthBridge.Gender == "Female" || AuthBridge.Gender == "F";
        }
        else
        {
            displayName = nameInput != null ? nameInput.text.Trim() : "";
            org = orgInput != null ? orgInput.text.Trim().ToUpperInvariant() : "";
            teamId = SanitizeTeamId(teamIdInput != null ? teamIdInput.text : "");
            teamSize = SanitizeTeamSize(teamSizeInput != null ? teamSizeInput.text : "8");
            isGirl = genderDropdown != null && genderDropdown.value == 1;
        }

        string joinCode = SanitizeJoinCode(joinCodeInput != null ? joinCodeInput.text : "");

        if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(org))
        {
            errorText.text = "Name and Org ID are required.";
            statusText.text = "";
            busy = false;
            yield break;
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

#pragma warning disable CS4014
        VoiceManager.Instance.LoginAsync(displayName);
#pragma warning restore CS4014

        statusText.text = "Connecting to Network...";

        if (relay == null) relay = FindFirstObjectByType<RelayConnector>();
        var nm = NetworkManager.Singleton;

        if (relay == null || nm == null || nm.IsListening)
        {
            errorText.text = "Network Error. Retry.";
            busy = false;
            yield break;
        }

        ApplyConnectionPayload(displayName, org, teamId, teamSize, isGirl);

        int teamIdHash = Animator.StringToHash(teamId);
        int layoutIndex = teamSize <= 8 ? 0 : (teamSize <= 12 ? 1 : 2);

        if (AuthBridge.IsReady && !string.IsNullOrEmpty(AuthBridge.OrgCode))
        {
            statusText.text = "Checking workspace...";

            string foundJoinCode = null;

            yield return StartCoroutine(sessionService.CheckSession(
                AuthBridge.OrgCode,
                code => { foundJoinCode = code; },
                () => { }
            ));

            if (!string.IsNullOrEmpty(foundJoinCode))
            {
      
                statusText.text = "Joining existing session...";
                GameSessionData.Instance?.SetConnectionInfo(false, foundJoinCode);
                GlobalRoomContext.Instance.SetLobbyData(displayName, org, foundJoinCode, teamIdHash, teamSize, layoutIndex);

                var joinTask = relay.JoinRoomAndClient(foundJoinCode, displayName, org);
                yield return new WaitUntil(() => joinTask.IsCompleted);

                if (joinTask.IsFaulted)
                {
                    Debug.LogWarning("[Lobby] Stale JoinCode detected. Deleting from API and creating a new room...");
                    statusText.text = "Fixing session...";

                    string url = "https://localhost:7080/api/workspaces/" + AuthBridge.OrgCode + "/session";
                    using var req = UnityWebRequest.Delete(url);

                    req.SetRequestHeader("Authorization", "Bearer " + AuthBridge.Token);
                    req.certificateHandler = new BypassCertificate();
                    yield return req.SendWebRequest();


                    statusText.text = "Creating new room...";
                    GlobalRoomContext.Instance.SetLobbyData(displayName, org, "", teamIdHash, teamSize, layoutIndex);

                    var hostTask = relay.CreateRoomAndHost(displayName, org, teamSize);
                    yield return new WaitUntil(() => hostTask.IsCompleted);

                    if (hostTask.IsFaulted)
                    {
                        errorText.text = "Failed to create room.";
                        busy = false;
                        yield break;
                    }

                    string newCode = hostTask.Result;

                    yield return StartCoroutine(sessionService.SaveSession(
                        AuthBridge.OrgCode, newCode,
                        () => Debug.Log("[Session] Saved new session "),
                        err => Debug.LogWarning("[Session] Save failed: " + err)
                    ));

                    PlayerPrefs.SetString("JOIN_CODE", newCode);
                    PlayerPrefs.Save();
                    GameSessionData.Instance?.SetConnectionInfo(true, newCode);

                    if (joinCodeText != null) joinCodeText.text = $"Join Code: {newCode}";
                    if (joinCodeInput != null) joinCodeInput.text = newCode;
                    GUIUtility.systemCopyBuffer = newCode;
                    statusText.text = "Room created!";
                }
                else
                {
                  
                    PlayerPrefs.SetString("JOIN_CODE", foundJoinCode);
                    PlayerPrefs.Save();
                    statusText.text = "Joined!";
                }
            }
            else
            {
                statusText.text = "Creating room...";
                GlobalRoomContext.Instance.SetLobbyData(displayName, org, "", teamIdHash, teamSize, layoutIndex);

                var hostTask = relay.CreateRoomAndHost(displayName, org, teamSize);
                yield return new WaitUntil(() => hostTask.IsCompleted);

                if (hostTask.IsFaulted)
                {
                    errorText.text = "Failed to create room.";
                    busy = false;
                    yield break;
                }

                string code = hostTask.Result;

                yield return StartCoroutine(sessionService.SaveSession(
                    AuthBridge.OrgCode, code,
                    () => Debug.Log("[Session] Saved ✓"),
                    err => Debug.LogWarning("[Session] Save failed: " + err)
                ));

                PlayerPrefs.SetString("JOIN_CODE", code);
                PlayerPrefs.Save();
                GameSessionData.Instance?.SetConnectionInfo(true, code);

                if (joinCodeText != null) joinCodeText.text = $"Join Code: {code}";
                if (joinCodeInput != null) joinCodeInput.text = code;
                GUIUtility.systemCopyBuffer = code;
                statusText.text = "Room created!";
            }
        }
        else
        {
            if (string.IsNullOrEmpty(joinCode))
            {
                statusText.text = "Creating room...";
                GlobalRoomContext.Instance.SetLobbyData(displayName, org, "", teamIdHash, teamSize, layoutIndex);

                var hostTask = relay.CreateRoomAndHost(displayName, org, teamSize);
                yield return new WaitUntil(() => hostTask.IsCompleted);

                if (hostTask.IsFaulted)
                {
                    errorText.text = "Failed to create room.";
                    busy = false;
                    yield break;
                }

                string code = hostTask.Result;
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
                    busy = false;
                    yield break;
                }

                statusText.text = "Joining room...";
                GameSessionData.Instance?.SetConnectionInfo(false, joinCode);
                GlobalRoomContext.Instance.SetLobbyData(displayName, org, joinCode, teamIdHash, teamSize, layoutIndex);

                var joinTask = relay.JoinRoomAndClient(joinCode, displayName, org);
                yield return new WaitUntil(() => joinTask.IsCompleted);

                if (joinTask.IsFaulted)
                {
                    errorText.text = "Failed to join.";
                    busy = false;
                    yield break;
                }

                PlayerPrefs.SetString("JOIN_CODE", joinCode);
                PlayerPrefs.Save();
                statusText.text = "Joined!";
            }
        }

        busy = false;
    }
}