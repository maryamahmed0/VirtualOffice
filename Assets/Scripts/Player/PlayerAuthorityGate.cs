using UnityEngine;
using Unity.Netcode;

public class PlayerAuthorityGate : NetworkBehaviour
{
    [SerializeField] private Behaviour[] disableIfNotOwner;

    public override void OnNetworkSpawn()
    {
        bool enable = IsOwner;

        foreach (var b in disableIfNotOwner)
        {
            if (b != null) b.enabled = enable;
        }
    }
}