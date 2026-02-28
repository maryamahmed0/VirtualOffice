using Unity.Netcode;
using UnityEngine;

public class RpcDispatcherSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject dispatcher;

    private void Start()
    {
        if (!NetworkManager.Singleton) return;

        NetworkManager.Singleton.OnServerStarted += () =>
        {
            if (dispatcher != null && !dispatcher.IsSpawned)
                dispatcher.Spawn(true);
        };
    }
}