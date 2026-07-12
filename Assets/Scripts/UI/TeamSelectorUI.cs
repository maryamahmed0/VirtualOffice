using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TeamSelectorUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform teamListContent;
    [SerializeField] private GameObject teamButtonPrefab;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statusText;

    private TeamService _teamService;

    private void Start()
    {
        _teamService = gameObject.AddComponent<TeamService>();

        if (titleText != null)
            titleText.text = $"Hello {AuthBridge.DisplayName}, Choose Your Team";

        if (AuthBridge.Teams != null && AuthBridge.Teams.Length > 0)
            BuildList(AuthBridge.Teams);
        else
            StartCoroutine(_teamService.GetMyTeams(
                AuthBridge.Token, OnTeamsLoaded, OnFail));
    }

    void OnTeamsLoaded(TeamService.TeamItem[] teams)
    {
        AuthBridge.Teams = teams;
        BuildList(teams);
    }

    void BuildList(TeamService.TeamItem[] teams)
    {
        foreach (Transform child in teamListContent)
            Destroy(child.gameObject);

        foreach (var team in teams)
        {
            var btnObj = Instantiate(teamButtonPrefab, teamListContent);

            var nameText = btnObj.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null) nameText.text = team.name;

            var codeText = btnObj.transform.Find("CodeText")?.GetComponent<TextMeshProUGUI>();
            if (codeText != null) codeText.text = $"Members: {team.membersCount}";

            var btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                string teamId = team.id;
                string teamName = team.name;
                int members = team.membersCount;
                btn.onClick.AddListener(() => OnTeamSelected(teamId, teamName, members));
            }
        }
    }

    void OnTeamSelected(string teamId, string teamName, int membersCount)
    {
        AuthBridge.TeamId = teamId;
        AuthBridge.TeamSize = membersCount > 0 ? membersCount : 8;
        AuthBridge.IsReady = true;

        Debug.Log($"[TeamSelector] Selected: {teamName} | {teamId} | Size: {AuthBridge.TeamSize}");

        SceneManager.LoadScene("LobbyScene");
    }

    void OnFail(string error)
    {
        Debug.LogError("[TeamSelector] " + error);
        if (statusText != null) statusText.text = "Error loading teams";
    }
}