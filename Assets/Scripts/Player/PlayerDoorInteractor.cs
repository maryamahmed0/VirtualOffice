using Unity.Netcode;
using UnityEngine;

public class PlayerDoorInteractor : NetworkBehaviour
{
    private DoorTrigger currentDoor;

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

    private void Update()
    {
        if (!IsOwner) return;
        if (currentDoor == null) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log($"[DOOR] F pressed -> {currentDoor.doorId}");
            UseDoorServerRpc(currentDoor.doorId);
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

        // set zone on server
        var room = playerObj.GetComponent<NetRoomState>();
        room?.ServerSetZone(door.destinationZone);

        // teleport server-authoritative
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