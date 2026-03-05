using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RoomZoneTrigger : MonoBehaviour
{
    [SerializeField] private RoomMarker roomContext;

    private void Reset()
    {
        roomContext = GetComponent<RoomMarker>();

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Awake()
    {
        if (roomContext == null)
            roomContext = GetComponent<RoomMarker>();

        if (roomContext == null)
            Debug.LogError("[ROOM] RoomZoneTrigger missing RoomContext on " + gameObject.name);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (roomContext == null) return;

        var playerRoom = other.GetComponentInParent<PlayerRoomState>();
        if (playerRoom == null) return;

        if (!playerRoom.CanProcessRoomTriggers()) return; 

        playerRoom.EnterRoom(roomContext);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (roomContext == null) return;

        var playerRoom = other.GetComponentInParent<PlayerRoomState>();
        if (playerRoom == null) return;

        if (!playerRoom.CanProcessRoomTriggers()) return; 

        playerRoom.ExitRoom(roomContext);
    }
}