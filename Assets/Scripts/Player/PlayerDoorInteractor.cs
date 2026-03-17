using Unity.Netcode;
using UnityEngine;

public class PlayerDoorInteractor : NetworkBehaviour
{
    private DoorTrigger currentDoor;

    [SerializeField] private PlayerSeatingState seatingState;
    [SerializeField] private PlayerSeatInteractor seatInteractor;

    private void Awake()
    {
        if (seatingState == null)
            seatingState = GetComponent<PlayerSeatingState>();

        if (seatInteractor == null)
            seatInteractor = GetComponent<PlayerSeatInteractor>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsOwner) return;

        if (other.TryGetComponent(out DoorTrigger door))
        {
            currentDoor = door;
            Debug.Log($"[DOOR] In range: {door.doorId} (press F)");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsOwner) return;

        if (other.TryGetComponent(out DoorTrigger door) && door == currentDoor)
        {
            Debug.Log($"[DOOR] Out of range: {door.doorId}");
            currentDoor = null;
        }
    }

    public bool HasDoorInRange => currentDoor != null;

    public void UseDoor()
    {
        if (!IsOwner) return;
        if (UIInputBlocker.BlockGameplayInput) return;
        if (seatingState != null && seatingState.IsSitting) return;
        if (currentDoor == null) return;

        Debug.Log($"[DOOR] UseDoor() -> {currentDoor.doorId}");
        UseDoorServerRpc(currentDoor.doorId);
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (UIInputBlocker.BlockGameplayInput) return;
        if (Application.isMobilePlatform) return;

        if (!Input.GetKeyDown(KeyCode.F))
            return;

        // لو قاعد -> يقوم
        if (seatingState != null && seatingState.IsSitting)
        {
            seatInteractor?.UseSeat();
            return;
        }

        // الباب له أولوية
        if (currentDoor != null)
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                WebVoiceGate.MarkUserGesture();
            }

            Debug.Log($"[DOOR] F pressed -> {currentDoor.doorId}");
            UseDoorServerRpc(currentDoor.doorId);
            return;
        }

        // بعده الكرسي لو متاح فعلاً
        if (seatInteractor != null && seatInteractor.HasUsableSeatInRange)
        {
            seatInteractor.UseSeat();
        }
    }

    [ServerRpc]
    private void UseDoorServerRpc(string doorId, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (!DoorRegistry.TryGet(doorId, out var door))
        {
            Debug.LogWarning($"[DOOR][SERVER] door not found id={doorId}");
            return;
        }

        if (door.targetSpawnPoint == null)
        {
            Debug.LogWarning($"[DOOR][SERVER] targetSpawnPoint NULL id={doorId}");
            return;
        }

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var cc) || cc.PlayerObject == null)
        {
            Debug.LogWarning($"[DOOR][SERVER] player not found clientId={clientId}");
            return;
        }

        var playerObj = cc.PlayerObject;

        var room = playerObj.GetComponent<NetRoomState>();
        room?.ServerSetZone(door.destinationZone);

        var seatState = playerObj.GetComponent<PlayerSeatingState>();
        if (seatState != null && seatState.IsSitting)
            seatState.ServerStandFromCurrentSeat();

        Vector3 pos = door.targetSpawnPoint.position;
        var rb = playerObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = (Vector2)pos;
        }
        else
        {
            playerObj.transform.position = pos;
        }

        Physics2D.SyncTransforms();

        Debug.Log($"[DOOR][SERVER] client={clientId} -> {door.destinationZone} teleportedTo={pos}");
    }
}