//using Unity.Netcode;
//using UnityEngine;

//public class PlayerVisibilityRules : NetworkBehaviour
//{
//    private NetRoomState room;
//    private PlayerTeamIdentity team;

//    public override void OnNetworkSpawn()
//    {
//        room = GetComponent<NetRoomState>();
//        team = GetComponent<PlayerTeamIdentity>();

//        if (!IsServer) return;

//        // ✅ القاعدة الأساسية: مين يشوف اللاعب ده؟
//        NetworkObject.CheckObjectVisibility = ShouldBeVisibleTo;

//        // أول مرة
//        NetworkObject.RebuildObservers(true);
//    }

//    private bool ShouldBeVisibleTo(ulong observerClientId)
//    {
//        // لازم يشوف نفسه
//        if (observerClientId == OwnerClientId) return true;

//        var nm = NetworkManager.Singleton;
//        if (nm == null) return true;

//        // observer player object
//        if (!nm.ConnectedClients.TryGetValue(observerClientId, out var obsClient) || obsClient.PlayerObject == null)
//            return true;

//        var obsRoom = obsClient.PlayerObject.GetComponent<NetRoomState>();
//        var obsTeam = obsClient.PlayerObject.GetComponent<PlayerTeamIdentity>();

//        if (room == null || team == null || obsRoom == null || obsTeam == null)
//            return true;

//        bool tInTeam = room.GetZone() == NetRoomState.Zone.TeamRoom;
//        bool oInTeam = obsRoom.GetZone() == NetRoomState.Zone.TeamRoom;

//        // الاتنين برّه TeamRoom
//        if (!tInTeam && !oInTeam) return true;

//        // واحد جوّه وواحد برّه
//        if (tInTeam != oInTeam) return false;

//        // الاتنين جوّه TeamRoom -> نفس التيم
//        return team.TeamIdHash.Value == obsTeam.TeamIdHash.Value;
//    }
//}