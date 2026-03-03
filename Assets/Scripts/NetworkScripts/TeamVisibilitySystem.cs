using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TeamVisibilitySystem : MonoBehaviour
{
    public static TeamVisibilitySystem Instance { get; private set; }
    private Coroutine co;

    [Header("Debug")]
    [SerializeField] private bool verbose = false;
    [SerializeField] private int framesDelay = 3;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        StartCoroutine(Hook());
    }

    private IEnumerator Hook()
    {
        while (NetworkManager.Singleton == null) yield return null;

        NetworkManager.Singleton.OnClientConnectedCallback += _ => RequestRebuild("OnClientConnected");
        NetworkManager.Singleton.OnClientDisconnectCallback += _ => RequestRebuild("OnClientDisconnected");

        Debug.Log($"[VIS] Hooked callbacks. IsServer={NetworkManager.Singleton.IsServer} IsHost={NetworkManager.Singleton.IsHost}");

        if (NetworkManager.Singleton.IsServer)
            RequestRebuild("OnEnable/Server");
    }

    public void RequestRebuild(string reason = "Manual")
    {
        var nm = NetworkManager.Singleton;

        if (nm == null) { Debug.LogWarning($"[VIS] RequestRebuild ignored ({reason}) nm=null"); return; }

        if (!nm.IsServer)
        {
            if (verbose)
                Debug.LogWarning($"[VIS] RequestRebuild ignored ({reason}) because NOT server. IsClient={nm.IsClient} IsHost={nm.IsHost}");
            return;
        }

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(RebuildAfterFrames(framesDelay, reason));
    }

    private IEnumerator RebuildAfterFrames(int frames, string reason)
    {
        for (int i = 0; i < frames; i++) yield return null;
        FullRefresh(reason);
        co = null;
    }

    private void FullRefresh(string reason)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;

        // PlayerObjects (targets)
        var targets = new List<NetworkObject>();
        foreach (var kvp in nm.ConnectedClients)
        {
            var p = kvp.Value.PlayerObject;
            if (p != null) targets.Add(p);
        }

        // ✅ observers الحقيقيين = clientIds الرسمية
        var observers = nm.ConnectedClientsIds;

        Debug.Log($"[VIS] FullRefresh START. reason={reason} targets={targets.Count} observers={observers.Count}");

        foreach (var target in targets)
        {
            var tRoom = target.GetComponent<NetRoomState>();
            var tTeam = target.GetComponent<PlayerTeamIdentity>();
            if (tRoom == null || tTeam == null) continue;

            foreach (var observerId in observers)
            {
                // يشوف نفسه
                if (observerId == target.OwnerClientId) { SafeShow(target, observerId); continue; }

                // observer player object
                if (!nm.ConnectedClients.TryGetValue(observerId, out var obsClient) || obsClient.PlayerObject == null)
                    continue;

                var obsObj = obsClient.PlayerObject;
                var oRoom = obsObj.GetComponent<NetRoomState>();
                var oTeam = obsObj.GetComponent<PlayerTeamIdentity>();
                if (oRoom == null || oTeam == null) continue;

                bool shouldSee = ShouldSee(tRoom, tTeam, oRoom, oTeam);

                if (!shouldSee)
                {
                    SafeHide(target, observerId);
                    if (verbose) Debug.Log($"[VIS] HIDE targetOwner={target.OwnerClientId} obsId={observerId}");
                }
                else
                {
                    SafeShow(target, observerId);
                    if (verbose) Debug.Log($"[VIS] SHOW targetOwner={target.OwnerClientId} obsId={observerId}");
                }
            }
        }

        Debug.Log("[VIS] FullRefresh END.");
    }

    private void SafeHide(NetworkObject target, ulong observerId)
    {
        try
        {
            target.NetworkHide(observerId);
        }
        catch (VisibilityChangeException)
        {
            // already hidden -> ignore
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[VIS] Hide failed target={target.OwnerClientId} obs={observerId} err={e.Message}");
        }
    }

    private void SafeShow(NetworkObject target, ulong observerId)
    {
        try
        {
            target.NetworkShow(observerId);
        }
        catch (VisibilityChangeException)
        {
            // already visible -> ignore
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[VIS] Show failed target={target.OwnerClientId} obs={observerId} err={e.Message}");
        }
    }

    private bool ShouldSee(NetRoomState tRoom, PlayerTeamIdentity tTeam,
                           NetRoomState oRoom, PlayerTeamIdentity oTeam)
    {
        bool tInTeam = tRoom.GetZone() == NetRoomState.Zone.TeamRoom;
        bool oInTeam = oRoom.GetZone() == NetRoomState.Zone.TeamRoom;

        if (!tInTeam && !oInTeam) return true;
        if (tInTeam != oInTeam) return false;

        return tTeam.TeamIdHash.Value == oTeam.TeamIdHash.Value;
    }
}