using Unity.Netcode;

public class LocalPlayerBinder : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        GlobalRoomContext.Instance.BindLocalPlayer(NetworkObject);
    }
}