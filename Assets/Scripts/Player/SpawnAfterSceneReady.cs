using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class SpawnAfterSceneReady : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (IsServer)
            StartCoroutine(DelayedSeatSpawn());
    }

    private IEnumerator DelayedSeatSpawn()
    {
        // استني عشان MeetingScene وSeatManager يجهزوا
        yield return null;
        yield return null;
        yield return null;

        var seatManager = FindFirstObjectByType<SeatManager>();
        if (seatManager == null)
        {
            Debug.LogError("[SeatSpawn] SeatManager not found (from player)!");
            yield break;
        }

        if (seatManager.TryAssignRandomSeat(OwnerClientId, out var pos))
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.position = (Vector2)pos;
            }
            else
            {
                transform.position = pos;
            }

            Debug.Log($"[SeatSpawn] Player owner {OwnerClientId} spawned at {pos}");
        }
        else
        {
            Debug.LogWarning($"[SeatSpawn] No seat available for {OwnerClientId}");
        }
    }
}