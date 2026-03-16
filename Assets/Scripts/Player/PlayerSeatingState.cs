using Unity.Netcode;
using UnityEngine;

public class PlayerSeatingState : NetworkBehaviour
{
    private readonly NetworkVariable<bool> isSitting =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> currentFacing =
        new NetworkVariable<int>((int)SeatPoint.SeatFacing.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> currentVisualType =
        new NetworkVariable<int>((int)SeatPoint.SeatVisualType.Normal, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<ulong> currentSeatObjectId =
        new NetworkVariable<ulong>(ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsSitting => isSitting.Value;
    public SeatPoint.SeatFacing CurrentSeatFacing => (SeatPoint.SeatFacing)currentFacing.Value;
    public SeatPoint.SeatVisualType CurrentSeatVisualType => (SeatPoint.SeatVisualType)currentVisualType.Value;
    public ulong CurrentSeatObjectId => currentSeatObjectId.Value;

    public SeatPoint CurrentSeat
    {
        get
        {
            if (currentSeatObjectId.Value == ulong.MaxValue || NetworkManager.Singleton == null)
                return null;

            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(currentSeatObjectId.Value, out var no))
                return no.GetComponent<SeatPoint>();

            return null;
        }
    }

    public void ServerSitOnSeat(SeatPoint seat)
    {
        if (!IsServer || seat == null)
            return;

        isSitting.Value = true;
        currentFacing.Value = (int)seat.ForcedFacing;
        currentVisualType.Value = (int)seat.VisualType;
        currentSeatObjectId.Value = seat.NetworkObjectId;
    }

    public void ServerStandFromCurrentSeat()
    {
        if (!IsServer)
            return;

        var seat = CurrentSeat;
        if (seat != null)
            seat.ServerRelease(OwnerClientId);

        isSitting.Value = false;
        currentFacing.Value = (int)SeatPoint.SeatFacing.None;
        currentVisualType.Value = (int)SeatPoint.SeatVisualType.Normal;
        currentSeatObjectId.Value = ulong.MaxValue;
    }
}