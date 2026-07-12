using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class TasksService : MonoBehaviour
{
    private const string BaseUrl = "https://localhost:7080";

    public IEnumerator GetMyTasks(Action<TaskItem[]> onSuccess, Action<string> onFail)
    {
        string url = BaseUrl + "/api/employee/tasks/my?PageSize=50";

        using var req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Authorization", "Bearer " + AuthBridge.Token);

        yield return req.SendWebRequest();

        Debug.Log("[Tasks] Raw: " + req.downloadHandler.text);

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<TasksApiResponse>(req.downloadHandler.text);
            if (resp != null && resp.isSuccess)
                onSuccess?.Invoke(resp.data?.items ?? new TaskItem[0]);
            else
                onFail?.Invoke(resp?.message ?? "Failed");
        }
        else
        {
            onFail?.Invoke($"{req.responseCode}: {req.downloadHandler.text}");
        }
    }

    public IEnumerator GetAllTasks(Action<TaskItem[]> onSuccess, Action<string> onFail)
    {
        string url = BaseUrl + "/api/tasks?orgId=" + AuthBridge.Workspaces;
        if (!string.IsNullOrEmpty(AuthBridge.TeamId))
            url += "&teamId=" + AuthBridge.TeamId;

        using var req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Authorization", "Bearer " + AuthBridge.Token);

        yield return req.SendWebRequest();

        Debug.Log("[Tasks] All Raw: " + req.downloadHandler.text);

        if (req.result != UnityWebRequest.Result.Success)
        {
            onFail?.Invoke($"{req.responseCode}: {req.downloadHandler.text}");
            yield break;
        }

        string raw = req.downloadHandler.text.Trim();

        if (raw.StartsWith("["))
        {
            var wrapper = JsonUtility.FromJson<TasksApiResponse>(
                $"{{\"items\":{raw},\"isSuccess\":true}}");
            onSuccess?.Invoke(wrapper?.items ?? new TaskItem[0]);
            yield break;
        }

        var resp = JsonUtility.FromJson<TasksApiResponse>(raw);
        if (resp != null && resp.isSuccess)
            onSuccess?.Invoke(resp.data?.items ?? new TaskItem[0]);
        else
            onFail?.Invoke(resp?.message ?? "Failed");
    }


    public IEnumerator GetManagerTasks(Action<TaskItem[]> onSuccess, Action<string> onFail, int page = 1, int pageSize = 50)
    {
        string url = $"{BaseUrl}/api/tasks/manager/tasks?Page={page}&PageSize={pageSize}";

        using var req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Authorization", "Bearer " + AuthBridge.Token);

        yield return req.SendWebRequest();

        Debug.Log("[Tasks] Manager Tasks Raw: " + req.downloadHandler.text);

        if (req.result != UnityWebRequest.Result.Success)
        {
            onFail?.Invoke($"{req.responseCode}: {req.downloadHandler.text}");
            yield break;
        }

        string raw = req.downloadHandler.text.Trim();


        if (raw.StartsWith("["))
        {
            var wrapper = JsonUtility.FromJson<TasksApiResponse>(
                $"{{\"items\":{raw},\"isSuccess\":true}}");
            onSuccess?.Invoke(wrapper?.items ?? new TaskItem[0]);
            yield break;
        }


        var resp = JsonUtility.FromJson<TasksApiResponse>(raw);
        if (resp != null && resp.isSuccess)
            onSuccess?.Invoke(resp.data?.items ?? new TaskItem[0]);
        else
            onFail?.Invoke(resp?.message ?? "Failed to fetch manager tasks");
    }

    public IEnumerator UpdateTaskStatus(string taskId, string status,
        Action onSuccess, Action<string> onFail)
    {
        string url = BaseUrl + "/api/tasks/" + taskId + "/status";
        string json = $"{{\"status\":\"{status}\"}}";

        using var req = new UnityWebRequest(url, "PUT");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", "Bearer " + AuthBridge.Token);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            onSuccess?.Invoke();
        else
            onFail?.Invoke($"{req.responseCode}: {req.downloadHandler.text}");
    }

    //Models 
    [Serializable]
    public class TasksApiResponse
    {
        public TasksData data;
        public TaskItem[] items;
        public bool isSuccess;
        public string message;
    }

    [Serializable]
    public class TasksData
    {
        public TaskItem[] items;
        public int totalCount;
    }

    [Serializable]
    public class TaskItem
    {
        public string id;
        public string title;
        public string description;
        public string status;
        public string priority;
        public string assignedToName;
    }
}