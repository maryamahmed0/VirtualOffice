using UnityEngine;
using Unity.Netcode;
using System.Text;

public class ConnectionApprovalHandler : MonoBehaviour
{
    private void Awake()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("[APPROVAL] No NetworkManager.Singleton in Awake!");
            return;
        }

        nm.NetworkConfig.ConnectionApproval = true;

        // ✅ لازم تتسجل بدري
        nm.ConnectionApprovalCallback -= Approval;
        nm.ConnectionApprovalCallback += Approval;

        Debug.Log("[APPROVAL] Callback installed ✅");
    }

    private void Approval(NetworkManager.ConnectionApprovalRequest req, NetworkManager.ConnectionApprovalResponse res)
    {
        string payload = "";
        try
        {
            payload = req.Payload != null ? Encoding.UTF8.GetString(req.Payload) : "";
        }
        catch { payload = ""; }

        // payload المتوقع: "Name|Org"
        string name = "";
        string org = "";

        if (!string.IsNullOrEmpty(payload))
        {
            var parts = payload.Split('|');
            if (parts.Length >= 2)
            {
                name = parts[0];
                org = parts[1];
            }
        }

        bool ok = !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(org);

        // ✅ هنا انتي تقدري تعملي validation للـ org
        // مثلاً: ok = ok && org == "111";

        if (!ok)
        {
            res.Approved = false;
            res.Reason = "Invalid payload";
            res.Pending = false;
            Debug.LogWarning($"[APPROVAL] Rejected client {req.ClientNetworkId}. payload='{payload}'");
            return;
        }

        res.Approved = true;
        res.CreatePlayerObject = true;
        res.Pending = false;

        Debug.Log($"[APPROVAL] Approved client {req.ClientNetworkId} name='{name}' org='{org}'");
    }
}