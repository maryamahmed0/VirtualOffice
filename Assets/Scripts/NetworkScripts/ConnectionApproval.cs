using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic; // ضفنا دي عشان الـ Dictionary
using System.Text;

public class ConnectionApprovalHandler : MonoBehaviour
{
    private bool installed;

    // قاموس مؤقت نحفظ فيه الجندر لحد ما اللاعب يحصله Spawn
    private Dictionary<ulong, bool> pendingGenders = new Dictionary<ulong, bool>();

    private IEnumerator Start()
    {
        while (NetworkManager.Singleton == null)
            yield return null;

        var nm = NetworkManager.Singleton;

        nm.NetworkConfig.ConnectionApproval = true;

        if (!installed)
        {
            nm.ConnectionApprovalCallback -= Approval;
            nm.ConnectionApprovalCallback += Approval;

            // هنشترك في الإيفينت ده عشان نعرف إمتى اللاعب ظهر فعلاً
            nm.OnClientConnectedCallback -= OnClientConnected;
            nm.OnClientConnectedCallback += OnClientConnected;

            installed = true;
            Debug.Log("[APPROVAL] Callback installed ✅");
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback -= Approval;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void Approval(NetworkManager.ConnectionApprovalRequest req, NetworkManager.ConnectionApprovalResponse res)
    {
        string payload = "";
        try { payload = req.Payload != null ? Encoding.UTF8.GetString(req.Payload) : ""; }
        catch { payload = ""; }
        Debug.Log($"[APPROVAL] RAW payload='{payload}' len={payload.Length}");

        // payload variables
        string name = "";
        string org = "";
        string team = "TECH";
        int teamSize = 8;
        bool isGirl = false; // ضفنا الجندر هنا

        if (!string.IsNullOrEmpty(payload))
        {
            var parts = payload.Split('|');

            if (parts.Length > 0) name = parts[0];
            if (parts.Length > 1) org = parts[1];
            if (parts.Length > 2) team = parts[2];
            if (parts.Length > 3) int.TryParse(parts[3], out teamSize);
            if (parts.Length > 4) isGirl = (parts[4] == "F"); // بنقرا الجندر من الـ Payload
        }

        bool ok = !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(org);

        if (!ok)
        {
            res.Approved = false;
            res.Reason = "Invalid payload";
            res.Pending = false;
            Debug.LogWarning($"[APPROVAL] Rejected client {req.ClientNetworkId}. payload='{payload}'");
            return;
        }

        // sanitize team
        team = string.IsNullOrWhiteSpace(team) ? "TECH" : team.Trim().ToUpperInvariant();
        teamSize = Mathf.Clamp(teamSize, 1, 50);

        res.Approved = true;
        res.CreatePlayerObject = true;
        res.Pending = false;

        int teamHash = Animator.StringToHash(team);
        PresenceService.EnqueueApproval(req.ClientNetworkId, name, org, teamHash, teamSize);

        // نحفظ الجندر بتاع الكلاينت ده عشان نطبقه أول ما يظهر
        pendingGenders[req.ClientNetworkId] = isGirl;

        Debug.Log($"[APPROVAL] Approved client {req.ClientNetworkId} name='{name}' org='{org}' team='{team}' size={teamSize} isGirl={isGirl}");
    }

    // الدالة دي بتشتغل أول ما اللاعب يدخل بنجاح والـ PlayerObject بتاعه يظهر في المشهد
    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        // بنسأل: هل الكلاينت ده ليه جندر محفوظ عندنا؟
        if (pendingGenders.TryGetValue(clientId, out bool isGirl))
        {
            // بنجيب اللاعب نفسه
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
            {
                var avatarSync = client.PlayerObject.GetComponent<PlayerAvatarSync>();
                if (avatarSync != null)
                {
                    avatarSync.ServerSetGenderAndRandomize(isGirl); // بنطبق الشكل
                    Debug.Log($"[APPROVAL] Applied Gender to client {clientId}: isGirl={isGirl}");
                }
            }
            pendingGenders.Remove(clientId); // بننضف القاموس عشان منستهلكش ميموري عالفاضي
        }
    }
}