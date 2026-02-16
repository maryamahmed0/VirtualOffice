using UnityEngine;
using Unity.Netcode;
using System.Text;

public class ConnectionApproval : MonoBehaviour
{
    // في نسخة التطوير: نخلي الـHost هو اللي يحدد org المسموح بها من GameSession
    private string allowedOrg;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // لو هوست: هنعتبر org بتاع الغرفة = org اللي كتبه في اللوجين
        // لو كلاينت: مش هنحتاج allowedOrg هنا لأنه مش بيعمل approval
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request,
                               NetworkManager.ConnectionApprovalResponse response)
    {
        // الـCallback ده بيتنفذ على السيرفر بس
        string payload = Encoding.UTF8.GetString(request.Payload);
        string[] parts = payload.Split('|');

        if (parts.Length < 2)
        {
            response.Approved = false;
            response.Reason = "Invalid payload";
            return;
        }

        string clientOrg = parts[1].Trim().ToUpperInvariant();

        // أول مرة سيرفر يشتغل: ثبّت org بتاع الغرفة من بيانات الهوست
        if (string.IsNullOrEmpty(allowedOrg))
        {
            allowedOrg = GameSessionData.Instance != null
                ? (GameSessionData.Instance.OrgCode ?? "").Trim().ToUpperInvariant()
                : "";
        }

        bool ok = !string.IsNullOrEmpty(clientOrg) &&
                  !string.IsNullOrEmpty(allowedOrg) &&
                  clientOrg == allowedOrg;

        response.Approved = ok;
        response.CreatePlayerObject = ok;
        response.Reason = ok ? "" : "Organization mismatch";
    }
}