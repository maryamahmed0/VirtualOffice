using Unity.Netcode;
using UnityEngine;

public class SeatPoint : NetworkBehaviour
{
    public enum SeatFacing { None, Left, Right, Up, Down }
    public enum SeatVisualType { Normal, SideWrap, BackCover }

    [Header("Seat")]
    [SerializeField] private Transform sitPoint;
    [SerializeField] private bool allowStandToggle = true;

    [Header("Facing")]
    [SerializeField] private SeatFacing forcedFacing = SeatFacing.None;

    [Header("Visual")]
    [SerializeField] private SeatVisualType visualType = SeatVisualType.Normal;

    public Transform SitPoint => sitPoint != null ? sitPoint : transform;
    public bool AllowStandToggle => allowStandToggle;
    public SeatFacing ForcedFacing => forcedFacing;
    public SeatVisualType VisualType => visualType;

    private void Reset()
    {
        sitPoint = transform;
    }

    public bool CanBeUsedBy(ulong clientId)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return false;

        NetworkObject myPlayerObj = null;

        if (IsServer)
        {
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
                myPlayerObj = client.PlayerObject;
        }
        else
        {
            if (NetworkManager.Singleton.LocalClientId == clientId && NetworkManager.Singleton.LocalClient != null)
                myPlayerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
        }

        if (myPlayerObj == null) return false;

        var myRoomState = myPlayerObj.GetComponent<NetRoomState>();
        var myZone = myRoomState != null ? myRoomState.GetZone() : NetRoomState.Zone.Lobby;

        var myTeamIdentity = myPlayerObj.GetComponent<PlayerTeamIdentity>();
        int myTeamHash = myTeamIdentity != null ? myTeamIdentity.TeamIdHash.Value : 0;

        foreach (var networkObj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {

            if (!networkObj.IsPlayerObject || networkObj.OwnerClientId == clientId)
                continue;

            var otherSeatState = networkObj.GetComponent<PlayerSeatingState>();

            if (otherSeatState != null && otherSeatState.IsSitting && otherSeatState.CurrentSeatObjectId == this.NetworkObjectId)
            {
                var otherRoomState = networkObj.GetComponent<NetRoomState>();
                var otherZone = otherRoomState != null ? otherRoomState.GetZone() : NetRoomState.Zone.Lobby;

                if (myZone == NetRoomState.Zone.TeamRoom && otherZone == NetRoomState.Zone.TeamRoom)
                {
                    var otherTeamIdentity = networkObj.GetComponent<PlayerTeamIdentity>();
                    int otherTeamHash = otherTeamIdentity != null ? otherTeamIdentity.TeamIdHash.Value : 0;

                    if (myTeamHash == otherTeamHash)
                    {
                        return false;
                    }
                }
                else
                {
                    return false; 
                }
            }
        }

        return true;
    }

    public bool ServerTryOccupy(ulong clientId)
    {
        if (!IsServer) return false;
        return CanBeUsedBy(clientId);
    }
}