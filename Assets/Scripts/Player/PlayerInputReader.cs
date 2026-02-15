using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerInputReader : NetworkBehaviour
{
    [SerializeField] private InputActionReference moveAction;

    public Vector2 Move { get; private set; }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        Debug.Log($"[InputReader] IsOwner. moveAction null? {moveAction == null} | action null? {moveAction?.action == null}");

        if (moveAction == null || moveAction.action == null) return;

        moveAction.action.Enable();
        Debug.Log($"[InputReader] Enabled? {moveAction.action.enabled}");

        moveAction.action.performed += OnMove;
        moveAction.action.canceled += OnMove;
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        Move = ctx.ReadValue<Vector2>();
        Debug.Log($"[InputReader] OnMove: {Move}");
    }


    public override void OnNetworkDespawn()
    {
        if (!IsOwner || moveAction?.action == null) return;

        moveAction.action.performed -= OnMove;
        moveAction.action.canceled -= OnMove;
        moveAction.action.Disable();
    }
}