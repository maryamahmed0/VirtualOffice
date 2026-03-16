using Unity.Netcode;
using UnityEngine;

public class PlayerSeatInteractor : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerSeatingState seatingState;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private PlayerDoorInteractor doorInteractor;

    private SeatPoint currentSeatInRange;

    public bool HasSeatInRange => currentSeatInRange != null;

    public bool HasUsableSeatInRange =>
        currentSeatInRange != null &&
        NetworkManager.Singleton != null &&
        currentSeatInRange.CanBeUsedBy(NetworkManager.Singleton.LocalClientId);

    public bool IsSitting => seatingState != null && seatingState.IsSitting;
    public SeatPoint CurrentSeat => seatingState != null ? seatingState.CurrentSeat : null;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (seatingState == null) seatingState = GetComponent<PlayerSeatingState>();
        if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
        if (doorInteractor == null) doorInteractor = GetComponent<PlayerDoorInteractor>();
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (UIInputBlocker.BlockGameplayInput) return;

        if (IsSitting)
        {
            if (CurrentSeat != null && CurrentSeat.SitPoint != null)
            {
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsOwner) return;

        if (other.TryGetComponent(out SeatPoint seat))
            currentSeatInRange = seat;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsOwner) return;

        if (other.TryGetComponent(out SeatPoint seat) && seat == currentSeatInRange)
            currentSeatInRange = null;
    }

    public void UseSeat()
    {
        if (!IsOwner) return;
        if (seatingState == null) return;
        if (UIInputBlocker.BlockGameplayInput) return;

        // لو قاعد بالفعل -> قوم
        if (seatingState.IsSitting)
        {
            RequestStandServerRpc();
            return;
        }

        // ممنوع قعدة لو واقف عند باب
        if (doorInteractor != null && doorInteractor.HasDoorInRange)
            return;

        if (currentSeatInRange == null)
            return;

        if (NetworkManager.Singleton == null)
            return;

        if (!currentSeatInRange.CanBeUsedBy(NetworkManager.Singleton.LocalClientId))
            return;

        RequestSitServerRpc(currentSeatInRange.NetworkObjectId);
    }

    [ServerRpc]
    private void RequestSitServerRpc(ulong seatObjectId, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(seatObjectId, out var seatObj))
            return;

        var seat = seatObj.GetComponent<SeatPoint>();
        if (seat == null)
            return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var cc) || cc.PlayerObject == null)
            return;

        var playerObj = cc.PlayerObject;
        var playerSeatState = playerObj.GetComponent<PlayerSeatingState>();
        var playerRb = playerObj.GetComponent<Rigidbody2D>();

        if (playerSeatState == null)
            return;

        if (playerSeatState.IsSitting)
            return;

        if (!seat.ServerTryOccupy(clientId))
            return;

        playerSeatState.ServerSitOnSeat(seat);

        Vector3 pos = seat.SitPoint.position;

        if (playerRb != null)
        {
            playerRb.velocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
            playerRb.position = (Vector2)pos;
        }
        else
        {
            playerObj.transform.position = pos;
        }

        Physics2D.SyncTransforms();
    }

    [ServerRpc]
    private void RequestStandServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (NetworkManager.Singleton == null)
            return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var cc) || cc.PlayerObject == null)
            return;

        var playerObj = cc.PlayerObject;
        var playerSeatState = playerObj.GetComponent<PlayerSeatingState>();
        var playerRb = playerObj.GetComponent<Rigidbody2D>();

        if (playerSeatState == null)
            return;

        if (!playerSeatState.IsSitting)
            return;

        playerSeatState.ServerStandFromCurrentSeat();

        if (playerRb != null)
        {
            playerRb.velocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
        }
    }
}