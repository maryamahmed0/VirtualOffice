using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class SpawnOnServerStart : MonoBehaviour
{
    private NetworkObject no;
    private bool spawned;

    private void Awake()
    {
        no = GetComponent<NetworkObject>();
    }

    private void Update()
    {
        if (spawned) return;

        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        if (nm.IsServer && nm.IsListening && !no.IsSpawned)
        {
            no.Spawn();
            spawned = true;
            Debug.Log("[NET] RpcDispatcher spawned ");
        }
        else if (no.IsSpawned)
        {
            spawned = true;
        }
    }
}