using UnityEngine;

public class TeamRoomLayoutVisuals : MonoBehaviour
{
    [SerializeField] private GameObject layoutSmall;
    [SerializeField] private GameObject layoutMed;
    [SerializeField] private GameObject layoutLarge;

    private PlayerTeamIdentity localTeam;
    private NetRoomState localRoom; 

    private void OnEnable()
    {
        SetVisible(layoutSmall, false);
        SetVisible(layoutMed, false);
        SetVisible(layoutLarge, false);
        InvokeRepeating(nameof(TryBind), 0.2f, 0.2f);
    }

    private void TryBind()
    {
        if (PlayerRoomState.LocalInstance == null) return;

        localTeam = PlayerRoomState.LocalInstance.GetComponentInParent<PlayerTeamIdentity>();
        localRoom = PlayerRoomState.LocalInstance.GetComponentInParent<NetRoomState>();

        if (localTeam == null || localRoom == null) return;

        localTeam.LayoutIndex.OnValueChanged += (_, __) => Apply();
        localRoom.CurrentZone.OnValueChanged += (_, __) => Apply();

        CancelInvoke(nameof(TryBind));
        Apply();
    }

    private void Apply()
    {
        if (localTeam == null || localRoom == null) return;

        bool inTeam = localRoom.GetZone() == NetRoomState.Zone.TeamRoom;
        int idx = localTeam.LayoutIndex.Value;

        SetVisible(layoutSmall, inTeam && idx == 0);
        SetVisible(layoutMed, inTeam && idx == 1);
        SetVisible(layoutLarge, inTeam && idx == 2);
    }

    private static void SetVisible(GameObject root, bool on)
    {
        if (!root) return;

        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            r.enabled = on;

        foreach (var tr in root.GetComponentsInChildren<UnityEngine.Tilemaps.TilemapRenderer>(true))
            tr.enabled = on;
    }
}