using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(PlayerInputReader))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerController2D : NetworkBehaviour
{
    private PlayerInputReader input;
    private PlayerMovement motor;

    private void Awake()
    {
        input = GetComponent<PlayerInputReader>();
        motor = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (!IsOwner) return;

        SendMoveServerRpc(input.Move);
    }

    [ServerRpc]
    private void SendMoveServerRpc(Vector2 move)
    {
        motor.SetMoveInput(move);
    }
}
