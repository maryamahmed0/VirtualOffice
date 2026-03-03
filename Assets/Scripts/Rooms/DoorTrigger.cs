using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DoorTrigger : MonoBehaviour
{
    [Header("Door Id (must be unique)")]
    public string doorId = "LobbyToTeam";

    [Header("Destination Zone")]
    public NetRoomState.Zone destinationZone = NetRoomState.Zone.TeamRoom;

    [Header("Teleport Target (Transform in scene)")]
    public Transform targetSpawnPoint;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void OnEnable() => DoorRegistry.Register(this);
    private void OnDisable() => DoorRegistry.Unregister(this);
}