using UnityEngine;
using Unity.Netcode;

public class OwnerTag : NetworkBehaviour
{
    private void Update()
    {
        if (IsOwner)
        {
            Debug.Log($"I AM OWNER on {gameObject.name}");
            Debug.Log($"[{name}] OnNetworkSpawn | IsOwner={IsOwner} | IsLocalPlayer={IsLocalPlayer} | OwnerClientId={OwnerClientId} | LocalClientId={NetworkManager.Singleton.LocalClientId}");
        }
         



    }
}