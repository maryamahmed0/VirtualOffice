using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Text;

public class ConnectionApprovalHandler : MonoBehaviour
{
    private bool installed;

    private IEnumerator Start()
    {
        // استنى لحد ما NetworkManager يبقى موجود
        while (NetworkManager.Singleton == null)
            yield return null;

        var nm = NetworkManager.Singleton;

        // لازم قبل StartHost/StartClient
        nm.NetworkConfig.ConnectionApproval = true;

        if (!installed)
        {
            nm.ConnectionApprovalCallback -= Approval;
            nm.ConnectionApprovalCallback += Approval;
            installed = true;
            Debug.Log("[APPROVAL] Callback installed ✅");
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.ConnectionApprovalCallback -= Approval;
    }

    private void Approval(NetworkManager.ConnectionApprovalRequest req, NetworkManager.ConnectionApprovalResponse res)
    {
        string payload = "";
        try { payload = req.Payload != null ? Encoding.UTF8.GetString(req.Payload) : ""; }
        catch { payload = ""; }
        Debug.Log($"[APPROVAL] RAW payload='{payload}' len={payload.Length}");
        // payload: name|org|teamId|teamSize
        string name = "";
        string org = "";
        string team = "TECH";
        int teamSize = 8;

        if (!string.IsNullOrEmpty(payload))
        {
            var parts = payload.Split('|');

            if (parts.Length > 0) name = parts[0];
            if (parts.Length > 1) org = parts[1];
            if (parts.Length > 2) team = parts[2];
            if (parts.Length > 3) int.TryParse(parts[3], out teamSize);
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

        Debug.Log($"[APPROVAL] Approved client {req.ClientNetworkId} name='{name}' org='{org}' team='{team}' size={teamSize}");
    }
}