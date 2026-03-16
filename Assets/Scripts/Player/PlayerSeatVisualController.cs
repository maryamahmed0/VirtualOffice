using Unity.Netcode;
using UnityEngine;

public class PlayerSeatVisualController : NetworkBehaviour
{
    [SerializeField] private PlayerSeatingState seatingState;

    [Header("Seat Overlays")]
    [SerializeField] private GameObject sideWrapLeft;
    [SerializeField] private GameObject sideWrapRight;
    [SerializeField] private GameObject backSeatCover;

    private void Awake()
    {
        if (seatingState == null)
            seatingState = GetComponent<PlayerSeatingState>();

        HideAll();
    }

    private void Update()
    {
        ApplyCurrentVisuals();
    }

    private void ApplyCurrentVisuals()
    {
        HideAll();

        if (seatingState == null || !seatingState.IsSitting)
            return;

        switch (seatingState.CurrentSeatVisualType)
        {
            case SeatPoint.SeatVisualType.SideWrap:
                if (seatingState.CurrentSeatFacing == SeatPoint.SeatFacing.Left && sideWrapLeft != null)
                    sideWrapLeft.SetActive(true);
                else if (seatingState.CurrentSeatFacing == SeatPoint.SeatFacing.Right && sideWrapRight != null)
                    sideWrapRight.SetActive(true);
                break;

            case SeatPoint.SeatVisualType.BackCover:
                if (backSeatCover != null)
                    backSeatCover.SetActive(true);
                break;
        }
    }

    private void HideAll()
    {
        if (sideWrapLeft != null) sideWrapLeft.SetActive(false);
        if (sideWrapRight != null) sideWrapRight.SetActive(false);
        if (backSeatCover != null) backSeatCover.SetActive(false);
    }
}