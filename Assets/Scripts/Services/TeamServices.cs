using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class TeamService : MonoBehaviour
{
    private const string BaseUrl = "https://localhost:7080";

    public IEnumerator GetMyTeams(string token,
        Action<TeamItem[]> onSuccess, Action<string> onFail)
    {
        using var req = UnityWebRequest.Get(BaseUrl + "/api/teams/my-teams");
        req.SetRequestHeader("Authorization", "Bearer " + token);

        yield return req.SendWebRequest();

        Debug.Log("[Teams] Raw: " + req.downloadHandler.text);

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<TeamsApiResponse>(req.downloadHandler.text);
            if (resp != null && resp.isSuccess)
                onSuccess?.Invoke(resp.data ?? new TeamItem[0]);
            else
                onFail?.Invoke(resp?.message ?? "Failed");
        }
        else
            onFail?.Invoke($"{req.responseCode}: {req.downloadHandler.text}");
    }

    [Serializable]
    public class TeamsApiResponse
    {
        public TeamItem[] data;
        public bool isSuccess;
        public string message;
    }

    [Serializable]
    public class TeamItem
    {
        public string id;
        public string name;
        public string department;
        public string managerName;
        public int membersCount;
    }
}