using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class WorkspaceSessionService : MonoBehaviour
{
    private const string BaseUrl = "https://localhost:7080";

    // Check Session 
    public IEnumerator CheckSession(string orgCode,
        Action<string> onJoinCodeFound,
        Action onNoSession)
    {
        string url = BaseUrl + "/api/workspaces/" + orgCode + "/session";

        using var req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Authorization", "Bearer " + AuthBridge.Token);

        yield return req.SendWebRequest();

        Debug.Log("[Session] Check raw: " + req.downloadHandler.text);

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<SessionApiResponse>(req.downloadHandler.text);
            if (resp != null && resp.isSuccess && resp.data != null
                && !string.IsNullOrEmpty(resp.data.joinCode))
            {
                Debug.Log("[Session] Found joinCode: " + resp.data.joinCode);
                onJoinCodeFound?.Invoke(resp.data.joinCode);
            }
            else
            {
                Debug.Log("[Session] No active session");
                onNoSession?.Invoke();
            }
        }
        else
        {
            Debug.Log("[Session] No session or error — will host");
            onNoSession?.Invoke();
        }
    }

    //Save Session
    public IEnumerator SaveSession(string orgCode, string joinCode,
        Action onSuccess, Action<string> onFail)
    {
        string url = BaseUrl + "/api/workspaces/" + orgCode + "/session";
        string json = $"{{\"joinCode\":\"{joinCode}\"}}";

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", "Bearer " + AuthBridge.Token);

        yield return req.SendWebRequest();

        Debug.Log("[Session] Save raw: " + req.downloadHandler.text);

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[Session] Saved joinCode: " + joinCode);
            onSuccess?.Invoke();
        }
        else
            onFail?.Invoke($"{req.responseCode}: {req.downloadHandler.text}");
    }

    //Delete Session 
    public IEnumerator DeleteSession(string orgCode,
        Action onSuccess, Action<string> onFail)
    {
        string url = BaseUrl + "/api/workspaces/" + orgCode + "/session";

        using var req = new UnityWebRequest(url, "DELETE");
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Authorization", "Bearer " + AuthBridge.Token);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[Session] Deleted");
            onSuccess?.Invoke();
        }
        else
            onFail?.Invoke($"{req.responseCode}: {req.downloadHandler.text}");
    }

    //  Models 
    [Serializable]
    class SessionApiResponse
    {
        public SessionData data;
        public bool isSuccess;
        public string message;
    }

    [Serializable]
    class SessionData
    {
        public string orgCode;
        public string joinCode;
    }
}