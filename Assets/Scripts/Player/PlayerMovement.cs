using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4.5f;

    private Rigidbody2D rb;
    private Vector2 desiredVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    /// <summary>
    /// Set desired move direction (normalized or not). This only stores intent; actual movement happens in FixedUpdate.
    /// </summary>
    public void SetMoveInput(Vector2 moveInput)
    {
        // Optional: normalize to avoid faster diagonal movement
        desiredVelocity = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
    }

    private void FixedUpdate()
    {
        rb.velocity = desiredVelocity * moveSpeed;
    }
}