using Unity.Netcode;
using UnityEngine;

public class ZoneDebugPrinter : NetworkBehaviour
{
    private NetRoomState room;

    public override void OnNetworkSpawn()
    {
        room = GetComponent<NetRoomState>();
        if (room == null) return;

        room.CurrentZone.OnValueChanged += OnZoneChanged;

        if (IsOwner)
            Debug.Log($"[ZONE][CLIENT] initial = {room.GetZone()}");
    }

    private void OnZoneChanged(int oldV, int newV)
    {
        if (!IsOwner) return;
        Debug.Log($"[ZONE][CLIENT] changed {(NetRoomState.Zone)oldV} -> {(NetRoomState.Zone)newV}");
    }
}