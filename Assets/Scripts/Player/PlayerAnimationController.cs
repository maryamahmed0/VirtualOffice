using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody2D targetRb;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private PlayerSeatingState seatingState;

    [Header("Tuning")]
    [SerializeField] private float moveThreshold = 0.05f;
    [SerializeField] private float pressThreshold = 0.5f;
    [SerializeField] private float mobileAxisBias = 0.1f;

    private Animator animator;
    private Vector2 lastMove = Vector2.down;
    private Vector3 lastFramePosition;

    private bool upHeld;
    private bool downHeld;
    private bool leftHeld;
    private bool rightHeld;

    private int upOrder;
    private int downOrder;
    private int leftOrder;
    private int rightOrder;
    private int pressCounter;

    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private static readonly int LastMoveXHash = Animator.StringToHash("LastMoveX");
    private static readonly int LastMoveYHash = Animator.StringToHash("LastMoveY");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsSittingHash = Animator.StringToHash("IsSitting");


    public void UpdateAnimatorReference()
    {
        animator = GetComponentInChildren<Animator>(false);
    }
    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (targetRb == null)
            targetRb = GetComponentInParent<Rigidbody2D>();

        if (inputReader == null)
            inputReader = GetComponentInParent<PlayerInputReader>();

        if (seatingState == null)
            seatingState = GetComponentInParent<PlayerSeatingState>();
    }

    private void Update()
    {
        if (animator == null || targetRb == null)
            return;

        bool isSitting = seatingState != null && seatingState.IsSitting;
        animator.SetBool(IsSittingHash, isSitting);

        if (isSitting)
        {
            Vector2 sitDir = ResolveSitDirection();
            lastMove = sitDir;

            animator.SetBool(IsMovingHash, false);
            animator.SetFloat(MoveXHash, 0f);
            animator.SetFloat(MoveYHash, 0f);
            animator.SetFloat(LastMoveXHash, sitDir.x);
            animator.SetFloat(LastMoveYHash, sitDir.y);

            // مهم: تحديث المكان هنا عشان لما يقوم ميحسبش سرعة وهمية فجأة
            lastFramePosition = transform.position;
            return;
        }

        // 👈 التعديل السحري هنا: حساب السرعة يدوياً بدل targetRb.velocity
        Vector2 velocity = (transform.position - lastFramePosition) / Time.deltaTime;
        lastFramePosition = transform.position; // حفظ المكان للفريم الجاي

        bool isMoving = velocity.sqrMagnitude > (moveThreshold * moveThreshold);

        Vector2 rawMove = inputReader != null ? inputReader.Move : Vector2.zero;
        UpdateHeldStates(rawMove);

        Vector2 animDir = Vector2.zero;

        if (isMoving)
        {
            bool useMobileStyle = inputReader != null && inputReader.UseMobileOverride;

            animDir = useMobileStyle
                ? ResolveMobileAnimationDirection(rawMove, velocity)
                : ResolveDesktopAnimationDirection(rawMove, velocity);

            lastMove = animDir;
        }

        animator.SetBool(IsMovingHash, isMoving);
        animator.SetFloat(MoveXHash, isMoving ? animDir.x : 0f);
        animator.SetFloat(MoveYHash, isMoving ? animDir.y : 0f);
        animator.SetFloat(LastMoveXHash, lastMove.x);
        animator.SetFloat(LastMoveYHash, lastMove.y);
    }
    private void UpdateHeldStates(Vector2 move)
    {
        bool newUp = move.y > pressThreshold;
        bool newDown = move.y < -pressThreshold;
        bool newRight = move.x > pressThreshold;
        bool newLeft = move.x < -pressThreshold;

        if (newUp && !upHeld) upOrder = ++pressCounter;
        if (newDown && !downHeld) downOrder = ++pressCounter;
        if (newRight && !rightHeld) rightOrder = ++pressCounter;
        if (newLeft && !leftHeld) leftOrder = ++pressCounter;

        upHeld = newUp;
        downHeld = newDown;
        rightHeld = newRight;
        leftHeld = newLeft;
    }

    private Vector2 ResolveDesktopAnimationDirection(Vector2 rawMove, Vector2 velocity)
    {
        bool verticalOnly = (upHeld || downHeld) && !leftHeld && !rightHeld;
        bool horizontalOnly = (leftHeld || rightHeld) && !upHeld && !downHeld;

        if (verticalOnly)
        {
            if (upHeld) return Vector2.up;
            if (downHeld) return Vector2.down;
        }

        if (horizontalOnly)
        {
            if (rightHeld) return Vector2.right;
            if (leftHeld) return Vector2.left;
        }

        int bestOrder = -1;
        Vector2 bestDir = lastMove;

        if (upHeld && upOrder > bestOrder)
        {
            bestOrder = upOrder;
            bestDir = Vector2.up;
        }

        if (downHeld && downOrder > bestOrder)
        {
            bestOrder = downOrder;
            bestDir = Vector2.down;
        }

        if (rightHeld && rightOrder > bestOrder)
        {
            bestOrder = rightOrder;
            bestDir = Vector2.right;
        }

        if (leftHeld && leftOrder > bestOrder)
        {
            bestOrder = leftOrder;
            bestDir = Vector2.left;
        }

        if (bestOrder == -1)
            bestDir = ResolveDominantAxis(rawMove, velocity);

        return bestDir;
    }

    private Vector2 ResolveMobileAnimationDirection(Vector2 rawMove, Vector2 velocity)
    {
        return ResolveDominantAxis(rawMove, velocity);
    }

    private Vector2 ResolveDominantAxis(Vector2 rawMove, Vector2 velocity)
    {
        Vector2 fallback = rawMove.sqrMagnitude > 0.0001f ? rawMove : velocity;

        float absX = Mathf.Abs(fallback.x);
        float absY = Mathf.Abs(fallback.y);

        if (absX > absY + mobileAxisBias)
            return fallback.x >= 0f ? Vector2.right : Vector2.left;

        if (absY > absX + mobileAxisBias)
            return fallback.y >= 0f ? Vector2.up : Vector2.down;

        return lastMove;
    }

    private Vector2 ResolveSitDirection()
    {
      
        if (seatingState != null)
        {
            switch (seatingState.CurrentSeatFacing)
            {
                case SeatPoint.SeatFacing.Left: return Vector2.left;
                case SeatPoint.SeatFacing.Right: return Vector2.right;
                case SeatPoint.SeatFacing.Up: return Vector2.up;
                case SeatPoint.SeatFacing.Down: return Vector2.down;
            }
        }

      
        if (Mathf.Abs(lastMove.x) > Mathf.Abs(lastMove.y))
        {
         
            return lastMove.x > 0f ? Vector2.right : Vector2.left;
        }
        else
        {
            
            if (Mathf.Abs(lastMove.y) < 0.01f) return Vector2.down;
            return lastMove.y > 0f ? Vector2.up : Vector2.down;
        }
    }
}