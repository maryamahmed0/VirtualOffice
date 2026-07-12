using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AuthService : MonoBehaviour
{
    private const string BaseUrl = "https://localhost:7080";

    void Awake()
    {
        System.Net.ServicePointManager.ServerCertificateValidationCallback =
            (sender, cert, chain, errors) => true;
    }

    // Login 
    public IEnumerator Login(string email, string password, string orgCode,
        Action<LoginResponse> onSuccess, Action<string> onFail)
    {
        string json = $"{{\"email\":\"{email}\",\"password\":\"{password}\",\"orgCode\":\"{orgCode}\"}}";

        using var req = new UnityWebRequest(BaseUrl + "/api/Auth/login", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        Debug.Log("[AuthService] Login raw: " + req.downloadHandler.text);

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<LoginApiResponse>(req.downloadHandler.text);
            if (resp != null && resp.isSuccess && resp.data != null)
                onSuccess?.Invoke(resp.data);
            else
                onFail?.Invoke(resp?.message ?? "Login failed");
        }
        else
        {
            onFail?.Invoke($"{req.responseCode}: {req.downloadHandler.text}");
        }
    }
    public IEnumerator GetOrgCode(string token, Action<string> onSuccess, Action<string> onFail)
    {
        using var req = UnityWebRequest.Get(BaseUrl + "/api/Profile/orgcode");
        req.SetRequestHeader("Authorization", "Bearer " + token);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<OrgCodeApiResponse>(req.downloadHandler.text);

            if (resp != null && !string.IsNullOrEmpty(resp.orgCode))
                onSuccess?.Invoke(resp.orgCode);
            else
                onFail?.Invoke("Organization code not found in profile.");
        }
        else
        {
            onFail?.Invoke($"Error {req.responseCode}: {req.downloadHandler.text}");
        }
    }
    public IEnumerator GetMyWorkspaces(string token, Action<WorkspaceItem[]> onSuccess, Action<string> onFail)
    {
        using var req = UnityWebRequest.Get(BaseUrl + "/api/workspaces");
        req.SetRequestHeader("Authorization", "Bearer " + token);

        yield return req.SendWebRequest();


        if (string.IsNullOrEmpty(req.downloadHandler.text))
        {
            onFail?.Invoke("Empty response from server");
            yield break;
        }

        if (req.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var resp = JsonUtility.FromJson<WorkspacesApiResponse>(req.downloadHandler.text);
                if (resp != null && resp.isSuccess)
                {
                    onSuccess?.Invoke(resp.data ?? new WorkspaceItem[0]);
                }
                else
                {
                    onFail?.Invoke(resp?.message ?? "Failed to parse workspaces");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthService] JSON Parsing Error: {ex.Message}");
                onFail?.Invoke("Data format error");
            }
        }
        else
        {
            onFail?.Invoke($"Error {req.responseCode}: {req.downloadHandler.text}");
        }
    }
    public IEnumerator GetProfile(string token,
    Action<ProfileResponse> onSuccess, Action<string> onFail)
    {
        using var req = UnityWebRequest.Get(BaseUrl + "/api/Profile/me");
        req.SetRequestHeader("Authorization", "Bearer " + token);

        yield return req.SendWebRequest();

        Debug.Log("[AuthService] Profile raw: " + req.downloadHandler.text);

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<ProfileApiResponse>(req.downloadHandler.text);
            if (resp != null && resp.isSuccess && resp.data != null)
                onSuccess?.Invoke(resp.data);
            else
                onFail?.Invoke(resp?.message ?? "Profile failed");
        }
        else
            onFail?.Invoke($"{req.responseCode}: {req.downloadHandler.text}");
    }


    // Models
    [Serializable]
    public class LoginApiResponse
    {
        public LoginResponse data;
        public bool isSuccess;
        public string message;
    }

    [Serializable]
    public class LoginResponse
    {
        public string token;
        public string expiration;
        public UserData user;
    }

    [Serializable]
    public class UserData
    {
        public string id;
        public string email;
        public string firstName;
        public string lastName;
        public string orgCode;
        public string workspaceId;
        public string department;
        public int seniorityLevel;
        public string[] roles;
    }

    [Serializable]
    class OrgCodeApiResponse
    {
        public string orgCode;
        public string data;
        public bool isSuccess;
        public string message;
    }

    [Serializable]
    public class WorkspacesApiResponse
    {
        public WorkspaceItem[] data;
        public bool isSuccess;
        public string message;
    }

    [Serializable]
    public class WorkspaceItem
    {
        public string id;
        public string name;
        public string orgCode;
    }
    [Serializable]
    public class ProfileApiResponse
    {
        public ProfileResponse data;
        public bool isSuccess;
        public string message;
    }

    [Serializable]
    public class ProfileResponse
    {
        public string firstName;
        public string lastName;
        public string orgCode;
        public string gender;
        public string[] roles;
        public string department;
        public string seniorityLevel;

        public TeamInfo[] teams;
    }
    [Serializable]
    public class TeamInfo
    {
        public string id;
        public string name;
        public string description;
        public string department;
        public string departmentDisplay;
        public string specialization;
        public int membersCount;
    }

}