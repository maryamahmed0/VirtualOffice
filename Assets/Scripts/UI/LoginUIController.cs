using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LoginUIController : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField orgCodeInput;
    public Button loginButton;
    public TextMeshProUGUI statusText;

    private AuthService _auth;
    private string _token;

    void Start()
    {
        _auth = gameObject.AddComponent<AuthService>();
        loginButton.onClick.AddListener(OnLoginClicked);
    }

    void OnLoginClicked()
    {
        string email = emailInput.text.Trim();
        string pass = passwordInput.text;
        string orgCode = orgCodeInput.text.Trim();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(orgCode))
        {
            statusText.text = "Enter Email, Password and Org Code";
            return;
        }

        SetBusy(true, "Logging in...");
        StartCoroutine(_auth.Login(email, pass, orgCode, OnLoginSuccess, OnFail));
    }

    void OnLoginSuccess(AuthService.LoginResponse data)
    {
        _token = data.token;
        AuthBridge.Token = data.token;
        AuthBridge.DisplayName = $"{data.user.firstName} {data.user.lastName}".Trim();
        AuthBridge.Role = data.user.roles != null && data.user.roles.Length > 0
                                 ? data.user.roles[0] : "Employee";
        AuthBridge.OrgCode = data.user.orgCode ?? "";

        Debug.Log($"[Login]  {AuthBridge.DisplayName} | Role: {AuthBridge.Role}");

        SetBusy(true, "Loading profile...");
        StartCoroutine(_auth.GetProfile(_token, OnProfileLoaded, OnProfileFail));
    }

    void OnProfileLoaded(AuthService.ProfileResponse profile)
    {

        AuthBridge.Gender = profile.gender ?? "";


        if (string.IsNullOrEmpty(AuthBridge.OrgCode) && !string.IsNullOrEmpty(profile.orgCode))
            AuthBridge.OrgCode = profile.orgCode;

        if (profile.teams != null && profile.teams.Length > 0)
        {
            AuthBridge.TeamId = profile.teams[0].id;
            AuthBridge.TeamName = profile.teams[0].name;
            AuthBridge.TeamSize = profile.teams[0].membersCount;
        }



        Debug.Log($"[Login] Profile loaded | Gender: {AuthBridge.Gender} | OrgCode: {AuthBridge.OrgCode}");


        if (AuthBridge.Role == "Manager")
        {
            SetBusy(true, "Loading workspaces...");
            StartCoroutine(_auth.GetMyWorkspaces(_token, OnWorkspacesLoaded, OnFail));
        }
        else
        {

            if (!string.IsNullOrEmpty(AuthBridge.OrgCode))
            {
                AuthBridge.IsReady = true;
                Debug.Log($"[Login] Employee ready | OrgCode: {AuthBridge.OrgCode}");
                FindFirstObjectByType<BootstrapLoader>()?.GoToLobby();
            }
            else
            {

                SetBusy(true, "Fetching organization...");
                StartCoroutine(_auth.GetOrgCode(_token, OnOrgCodeLoaded, OnFail));
            }
        }
    }

    void OnProfileFail(string error)
    {
        Debug.LogWarning("[Login] Profile failed: " + error + " — continuing without gender");

        if (AuthBridge.Role == "Manager")
        {
            SetBusy(true, "Loading workspaces...");
            StartCoroutine(_auth.GetMyWorkspaces(_token, OnWorkspacesLoaded, OnFail));
        }
        else
        {
            if (!string.IsNullOrEmpty(AuthBridge.OrgCode))
            {
                AuthBridge.IsReady = true;
                FindFirstObjectByType<BootstrapLoader>()?.GoToLobby();
            }
            else
            {
                StartCoroutine(_auth.GetOrgCode(_token, OnOrgCodeLoaded, OnFail));
            }
        }
    }

    void OnOrgCodeLoaded(string orgCode)
    {
        AuthBridge.OrgCode = orgCode;
        AuthBridge.IsReady = true;
        Debug.Log($"[Login] OrgCode loaded: {orgCode}");
        FindFirstObjectByType<BootstrapLoader>()?.GoToLobby();
    }

    void OnWorkspacesLoaded(AuthService.WorkspaceItem[] workspaces)
    {
        AuthBridge.Workspaces = workspaces;

        if (workspaces == null || workspaces.Length == 0)
        {
            ProceedAfterWorkspace();
            return;
        }

        if (workspaces.Length == 1)
        {
            AuthBridge.OrgCode = workspaces[0].orgCode;
            ProceedAfterWorkspace();
        }
        else
        {
            SceneManager.LoadScene("WorkspaceSelectorScene");
        }
    }

    void ProceedAfterWorkspace()
    {
        if (AuthBridge.IsManager)
        {
            SetBusy(true, "Loading teams...");
            var teamService = gameObject.AddComponent<TeamService>();
            StartCoroutine(teamService.GetMyTeams(_token, OnTeamsLoaded, OnTeamsFail));
        }
        else
        {

            AuthBridge.IsReady = true;
            FindFirstObjectByType<BootstrapLoader>()?.GoToLobby();
        }
    }

    void OnTeamsLoaded(TeamService.TeamItem[] teams)
    {
        AuthBridge.Teams = teams;

        if (teams == null || teams.Length == 0)
        {
            AuthBridge.IsReady = true;
            FindFirstObjectByType<BootstrapLoader>()?.GoToLobby();
            return;
        }

        if (teams.Length == 1)
        {
            AuthBridge.TeamId = teams[0].id;
            AuthBridge.TeamSize = teams[0].membersCount > 0 ? teams[0].membersCount : 8;
            AuthBridge.IsReady = true;
            Debug.Log($"[Login] Single team: {teams[0].name}");
            FindFirstObjectByType<BootstrapLoader>()?.GoToLobby();
        }
        else
        {
            Debug.Log($"[Login] Multiple teams: {teams.Length}");
            SceneManager.LoadScene("TeamSelectorScene");
        }
    }

    void OnTeamsFail(string error)
    {
        Debug.LogWarning("[Login] Teams failed: " + error);
        AuthBridge.IsReady = true;
        FindFirstObjectByType<BootstrapLoader>()?.GoToLobby();
    }

    void OnFail(string error)
    {
        statusText.text = "Error: " + error;
        SetBusy(false, "");
        Debug.LogError("[Login] " + error);
    }

    void SetBusy(bool busy, string msg)
    {
        loginButton.interactable = !busy;
        statusText.text = msg;
    }
}