using Unity.Netcode;
using UnityEngine;

public class PlayerTeamCollisionLayer : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private NetRoomState room;
    [SerializeField] private PlayerTeamIdentity team;

    [Header("Only change THESE (movement body)")]
    [SerializeField] private Collider2D bodyCollider;   // جسم اللاعب
    [SerializeField] private Rigidbody2D bodyRigidbody; // ريجيد بودي الحركة

    private int LDefault, LSmall, LMed, LLarge;

    private void Awake()
    {
        LDefault = LayerMask.NameToLayer("Player_Default");
        LSmall = LayerMask.NameToLayer("Player_Small");
        LMed = LayerMask.NameToLayer("Player_Med");
        LLarge = LayerMask.NameToLayer("Player_Large");
    }

    public override void OnNetworkSpawn()
    {
        if (room != null) room.CurrentZone.OnValueChanged += (_, __) => Apply();
        if (team != null) team.LayoutIndex.OnValueChanged += (_, __) => Apply();
        Apply();
    }

    private void Apply()
    {
        if (room == null || team == null) return;

        bool inTeam = room.GetZone() == NetRoomState.Zone.TeamRoom;

        int target = LDefault;
        if (inTeam)
        {
            int idx = team.LayoutIndex.Value;
            target = (idx == 0) ? LSmall : (idx == 1 ? LMed : LLarge);
        }

        ApplyLayerToBodyOnly(target);
    }

    private void ApplyLayerToBodyOnly(int layer)
    {
        if (bodyCollider != null) bodyCollider.gameObject.layer = layer;
        if (bodyRigidbody != null) bodyRigidbody.gameObject.layer = layer;

        // مهم: لو bodyCollider على نفس الجيم اوبجكت بتاع rb، سطر واحد كفاية
    }
}