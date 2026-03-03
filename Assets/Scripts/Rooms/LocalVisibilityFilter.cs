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
        // استنى لحد ما Local player يبقى موجود
        while (PlayerRoomState.LocalInstance == null)
            yield return null;

        localRoom = PlayerRoomState.LocalInstance.GetComponentInParent<NetRoomState>();
        localTeam = PlayerRoomState.LocalInstance.GetComponentInParent<PlayerTeamIdentity>();

        // refresher دوري (بسيط ومضمون)
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

            // فلترة على Player objects بس: لازم يبقى عليه المكونين دول
            var r = no.GetComponent<NetRoomState>();
            var t = no.GetComponent<PlayerTeamIdentity>();
            if (r == null || t == null) continue;

            // متخبيش نفسك
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

        // الاتنين برّه TeamRoom
        if (!localInTeam && !otherInTeam) return true;

        // واحد جوّه وواحد برّه
        if (localInTeam != otherInTeam) return false;

        // الاتنين جوّه TeamRoom -> نفس التيم
        return localT.TeamIdHash.Value == otherT.TeamIdHash.Value;
    }

    private static void SetVisible(GameObject root, bool on)
    {
        if (!root) return;

        // Renderers
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            r.enabled = on;

        // Tilemaps لو عندك حاجة متفرعة (غالباً مش هتكون في player)
        foreach (var tr in root.GetComponentsInChildren<UnityEngine.Tilemaps.TilemapRenderer>(true))
            tr.enabled = on;

        // UI فوق الرأس (Canvas)
        foreach (var c in root.GetComponentsInChildren<Canvas>(true))
            c.enabled = on;
    }
}