using UnityEngine;
#if UNITY_NETCODE_GAMEOBJECTS
using Unity.Netcode;
#endif

public class PlayerController2D : MonoBehaviour
{
    private PlayerInputReader input;
    private PlayerMovement motor;

#if UNITY_NETCODE_GAMEOBJECTS
    private NetworkObject netObj;
#endif

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        motor = GetComponent<PlayerMovement>();

#if UNITY_NETCODE_GAMEOBJECTS
        netObj = GetComponent<NetworkObject>();
#endif
    }

    private void Update()
    {
#if UNITY_NETCODE_GAMEOBJECTS
        // لو Multiplayer: بس الـ Owner هو اللي يقرأ input ويحرك نفسه
        if (netObj != null && !netObj.IsOwner) return;
#endif
        motor.SetMoveInput(input.Move);
    }
}