using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;

    public Vector2 Move { get; private set; }

    private void OnEnable()
    {
        if (moveAction == null || moveAction.action == null)
        {
            Debug.LogError($"{nameof(PlayerInputReader)}: Move action reference is missing.");
            enabled = false;
            return;
        }

        moveAction.action.Enable();
        moveAction.action.performed += OnMove;
        moveAction.action.canceled += OnMove;
    }

    private void OnDisable()
    {
        if (moveAction?.action == null) return;

        moveAction.action.performed -= OnMove;
        moveAction.action.canceled -= OnMove;
        moveAction.action.Disable();
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        Move = ctx.ReadValue<Vector2>();
    }
}