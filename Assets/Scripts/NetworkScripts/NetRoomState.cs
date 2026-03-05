using Unity.Netcode;
using UnityEngine;

public class NetRoomState : NetworkBehaviour
{
    public enum Zone
    {
        Lobby = 0,
        TeamRoom = 1,
        Meeting = 2
    }

    public NetworkVariable<int> CurrentZone =
        new((int)Zone.Lobby, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public Zone GetZone() => (Zone)CurrentZone.Value;

    public void ServerSetZone(Zone z)
    {
        if (!IsServer) return;
        if (CurrentZone.Value == (int)z) return;

        Debug.Log($"[ZONE][SERVER] client={OwnerClientId} zone {GetZone()} -> {z}");
        CurrentZone.Value = (int)z;

        // ✅ تحديث Presence عشان People UI
        PresenceService.Instance?.ServerUpdateZoneForClient(OwnerClientId, (int)z);

        // ✅ لو عندك LocalVisibilityFilter refresh event (اختياري)
        // LocalVisibilityEvents.RaiseRefresh();
    }
}