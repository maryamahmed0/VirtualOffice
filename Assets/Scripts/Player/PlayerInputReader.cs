using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerInputReader : NetworkBehaviour
{
    [SerializeField] private InputActionReference moveAction;

    public Vector2 Move { get; private set; }
    public bool UseMobileOverride { get; private set; }
    private Vector2 mobileMove;
    private PlayerSeatingState seatingState;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        if (seatingState == null)
            seatingState = GetComponent<PlayerSeatingState>();

        if (Application.isMobilePlatform && Application.platform != RuntimePlatform.WebGLPlayer)
            return;

        if (moveAction == null || moveAction.action == null) return;

        moveAction.action.Enable();
        moveAction.action.performed += OnMove;
        moveAction.action.canceled += OnMove;
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        if (UIInputBlocker.BlockGameplayInput || (seatingState != null && seatingState.IsSitting))
        {
            Move = Vector2.zero;
            return;
        }

        if (UseMobileOverride)
            return;

        Move = ctx.ReadValue<Vector2>();
    }

    public void SetMoveFromUI(Vector2 v)
    {
        if (UIInputBlocker.BlockGameplayInput || (seatingState != null && seatingState.IsSitting))
        {
            Move = Vector2.zero;
            return;
        }

        UseMobileOverride = true;
        mobileMove = v;
        Move = v;
    }

    public void ClearMobileOverride()
    {
        UseMobileOverride = false;
        mobileMove = Vector2.zero;
        Move = Vector2.zero;
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (UIInputBlocker.BlockGameplayInput || (seatingState != null && seatingState.IsSitting))
            Move = Vector2.zero;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        if (Application.isMobilePlatform && Application.platform != RuntimePlatform.WebGLPlayer)
            return;

        if (moveAction?.action == null) return;

        moveAction.action.performed -= OnMove;
        moveAction.action.canceled -= OnMove;
        moveAction.action.Disable();
    }
}