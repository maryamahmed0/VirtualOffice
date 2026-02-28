using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : NetworkBehaviour
{
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

    public void SetMoveInput(Vector2 moveInput)
    {
        desiredVelocity = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
    }

    private void FixedUpdate()
    {
        if (!IsServer) return;

        rb.velocity = desiredVelocity * moveSpeed;
    }
}
