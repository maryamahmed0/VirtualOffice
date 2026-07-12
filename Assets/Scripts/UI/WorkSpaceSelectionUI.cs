using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class WorkspaceSelectorUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform workspaceListContent;
    [SerializeField] private GameObject workspaceButtonPrefab;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statusText;

    private AuthService _auth;

    private void Start()
    {
        _auth = gameObject.AddComponent<AuthService>();

        if (titleText != null)
            titleText.text = $"Hello {AuthBridge.DisplayName}،Choose Your Workspace";

        if (AuthBridge.Workspaces != null && AuthBridge.Workspaces.Length > 0)
            BuildList(AuthBridge.Workspaces);
        else
            StartCoroutine(_auth.GetMyWorkspaces(
                AuthBridge.Token, OnWorkspacesLoaded, OnFail));
    }

    void OnWorkspacesLoaded(AuthService.WorkspaceItem[] workspaces)
    {
        AuthBridge.Workspaces = workspaces;
        BuildList(workspaces);
    }

    void BuildList(AuthService.WorkspaceItem[] workspaces)
    {
        foreach (Transform child in workspaceListContent)
            Destroy(child.gameObject);

        foreach (var ws in workspaces)
        {
            var btnObj = Instantiate(workspaceButtonPrefab, workspaceListContent);

     
            var nameText = btnObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null) nameText.text = ws.name;

     
            var codeText = btnObj.transform.Find("CodeText")?.GetComponent<TextMeshProUGUI>();
            if (codeText != null) codeText.text = $"Code: {ws.orgCode}";

            var btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                string orgCode = ws.orgCode;
                string wsName = ws.name;
                btn.onClick.AddListener(() => OnWorkspaceSelected(orgCode, wsName));
            }
        }
    }

    void OnWorkspaceSelected(string orgCode, string wsName)
    {
        if (string.IsNullOrEmpty(orgCode)) return;

        AuthBridge.OrgCode = orgCode;

        if (AuthBridge.IsManager)
        {
            Debug.Log($"[WorkspaceSelector] Manager selected: {wsName} — loading teams");
            StartCoroutine(GetMyTeamsAfterWorkspace());
        }
        else
        {
            AuthBridge.IsReady = true;
            SceneManager.LoadScene("LobbyScene");
        }
    }
    private IEnumerator GetMyTeamsAfterWorkspace()
    {
        var teamService = gameObject.AddComponent<TeamService>();
        bool done = false;
        TeamService.TeamItem[] teams = null;

        yield return StartCoroutine(teamService.GetMyTeams(
            AuthBridge.Token,
            t => { teams = t; done = true; },
            e => { Debug.LogWarning(e); done = true; }
        ));

        AuthBridge.Teams = teams;

        if (teams != null && teams.Length > 1)
            SceneManager.LoadScene("TeamSelectorScene");
        else
        {
            if (teams != null && teams.Length == 1)
            {
                AuthBridge.TeamId = teams[0].id;
                AuthBridge.TeamSize = teams[0].membersCount > 0 ? teams[0].membersCount : 8;
            }
            AuthBridge.IsReady = true;
            SceneManager.LoadScene("LobbyScene");
        }
    }
    void OnFail(string error)
    {
        Debug.LogError("[WorkspaceSelector] " + error);
        if (statusText != null) statusText.text = "Error Loading Workspaces";
    }
}