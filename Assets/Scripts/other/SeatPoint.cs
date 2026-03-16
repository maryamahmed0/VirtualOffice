using Unity.Netcode;
using UnityEngine;

public class SeatPoint : NetworkBehaviour
{
    public enum SeatFacing
    {
        None,
        Left,
        Right,
        Up,
        Down
    }

    public enum SeatVisualType
    {
        Normal,
        SideWrap,
        BackCover
    }

    [Header("Seat")]
    [SerializeField] private Transform sitPoint;
    [SerializeField] private bool allowStandToggle = true;

    [Header("Facing")]
    [SerializeField] private SeatFacing forcedFacing = SeatFacing.None;

    [Header("Visual")]
    [SerializeField] private SeatVisualType visualType = SeatVisualType.Normal;

    private readonly NetworkVariable<ulong> occupantClientId =
        new NetworkVariable<ulong>(
            ulong.MaxValue,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public Transform SitPoint => sitPoint != null ? sitPoint : transform;
    public bool AllowStandToggle => allowStandToggle;
    public SeatFacing ForcedFacing => forcedFacing;
    public SeatVisualType VisualType => visualType;

    public bool IsOccupied => occupantClientId.Value != ulong.MaxValue;
    public ulong OccupantClientId => occupantClientId.Value;

    private void Reset()
    {
        sitPoint = transform;
    }

    public bool CanBeUsedBy(ulong clientId)
    {
        return !IsOccupied || occupantClientId.Value == clientId;
    }

    public bool ServerTryOccupy(ulong clientId)
    {
        if (!IsServer)
            return false;

        if (occupantClientId.Value != ulong.MaxValue && occupantClientId.Value != clientId)
            return false;

        occupantClientId.Value = clientId;
        return true;
    }

    public void ServerRelease(ulong clientId)
    {
        if (!IsServer)
            return;

        if (occupantClientId.Value == clientId)
            occupantClientId.Value = ulong.MaxValue;
    }
}