using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class LocalVisibilityFilter : MonoBehaviour
{
    [SerializeField] private float refreshInterval = 0.25f;

    private NetRoomState localRoom;
    private PlayerTeamIdentity localTeam;

    private void OnEnable()
    {
        StartCoroutine(Boot());
    }

    private IEnumerator Boot()
    {

        while (PlayerRoomState.LocalInstance == null)
            yield return null;

        localRoom = PlayerRoomState.LocalInstance.GetComponentInParent<NetRoomState>();
        localTeam = PlayerRoomState.LocalInstance.GetComponentInParent<PlayerTeamIdentity>();

        StartCoroutine(RefreshLoop());
    }

    private IEnumerator RefreshLoop()
    {
        var wait = new WaitForSeconds(refreshInterval);

        while (true)
        {
            ApplyNow();
            yield return wait;
        }
    }

    private void ApplyNow()
    {
        if (localRoom == null || localTeam == null) return;
        if (NetworkManager.Singleton == null) return;

        var localId = NetworkManager.Singleton.LocalClientId;

        foreach (var kvp in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
        {
            var no = kvp.Value;
            if (no == null) continue;

     
            var r = no.GetComponent<NetRoomState>();
            var t = no.GetComponent<PlayerTeamIdentity>();
            if (r == null || t == null) continue;

   
            if (no.OwnerClientId == localId)
            {
                SetVisible(no.gameObject, true);
                continue;
            }

            bool shouldSee = ShouldSee(localRoom, localTeam, r, t);
            SetVisible(no.gameObject, shouldSee);
        }
    }

    private bool ShouldSee(NetRoomState localR, PlayerTeamIdentity localT,
                           NetRoomState otherR, PlayerTeamIdentity otherT)
    {
        bool localInTeam = localR.GetZone() == NetRoomState.Zone.TeamRoom;
        bool otherInTeam = otherR.GetZone() == NetRoomState.Zone.TeamRoom;


        if (!localInTeam && !otherInTeam) return true;


        if (localInTeam != otherInTeam) return false;


        return localT.TeamIdHash.Value == otherT.TeamIdHash.Value;
    }

    private static void SetVisible(GameObject root, bool on)
    {
        if (!root) return;

        // Renderers
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            r.enabled = on;

        foreach (var tr in root.GetComponentsInChildren<UnityEngine.Tilemaps.TilemapRenderer>(true))
            tr.enabled = on;

        foreach (var c in root.GetComponentsInChildren<Canvas>(true))
            c.enabled = on;
    }
    public bool IsVisibleToLocal(GameObject targetPlayerRoot)
    {
        if (targetPlayerRoot == null) return false;

        var localR = localRoom;
        var localT = localTeam;
        if (localR == null || localT == null) return true; // fallback

        var otherR = targetPlayerRoot.GetComponentInParent<NetRoomState>();
        var otherT = targetPlayerRoot.GetComponentInParent<PlayerTeamIdentity>();
        if (otherR == null || otherT == null) return true;

        if (NetworkManager.Singleton != null)
        {
            var no = targetPlayerRoot.GetComponentInParent<NetworkObject>();
            if (no != null && no.OwnerClientId == NetworkManager.Singleton.LocalClientId)
                return true;
        }

        return ShouldSee(localR, localT, otherR, otherT);
    }
}