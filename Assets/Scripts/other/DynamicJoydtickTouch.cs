using UnityEngine;
using UnityEngine.EventSystems;

public class DynamicJoystickTouch : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform inputArea;    
    [SerializeField] private RectTransform joystickRoot; 
    [SerializeField] private RectTransform handle;      
    [SerializeField] private Canvas canvas;

    [Header("Config")]
    [SerializeField] private float maxRadius = 80f;
    [SerializeField] private bool hideWhenIdle = true;

    private bool active;
    private int activeFingerId = -1;
    private Vector2 startPosLocalInArea; 
    private Vector2 currentVector;

    public Vector2 Vector => currentVector;
    public System.Action<Vector2> OnMoveVector;

    private Camera UICam =>
        (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera
            : null;

    private void Awake()
    {
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        if (!inputArea) inputArea = GetComponentInParent<RectTransform>();

        SetVisible(false);

        if (handle) handle.anchoredPosition = Vector2.zero;
    }

    private void Update()
    {
        if (UIInputBlocker.BlockGameplayInput)
        {
            if (active)
                End();

            return;
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouse();
#else
    HandleTouch();
#endif
    }

    private void HandleTouch()
    {
        if (Input.touchCount == 0)
        {
            if (active) End();
            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);

            if (!active)
            {
                if (t.phase == TouchPhase.Began)
                {
                    if (IsPointerOverUI(t.fingerId)) continue;
                    Begin(t.fingerId, t.position);
                }
            }
            else
            {
                if (t.fingerId != activeFingerId) continue;

                if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                    Drag(t.position);

                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    End();
            }
        }
    }

#if UNITY_EDITOR || UNITY_STANDALONE
    private void HandleMouse()
    {
        if (!active)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (IsPointerOverUI(-1)) return;
                Begin(-1, Input.mousePosition);
            }
        }
        else
        {
            if (Input.GetMouseButton(0))
                Drag(Input.mousePosition);

            if (Input.GetMouseButtonUp(0))
                End();
        }
    }
#endif

    private void Begin(int fingerId, Vector2 screenPos)
    {
        active = true;
        activeFingerId = fingerId;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            inputArea, screenPos, UICam, out startPosLocalInArea);


        joystickRoot.anchoredPosition = startPosLocalInArea;

        if (handle) handle.anchoredPosition = Vector2.zero;

        SetVisible(true);
        SetVector(Vector2.zero);
    }

    private void Drag(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            inputArea, screenPos, UICam, out var localPosInArea);

        Vector2 delta = localPosInArea - startPosLocalInArea;


        Vector2 clamped = Vector2.ClampMagnitude(delta, maxRadius);

        if (handle) handle.anchoredPosition = clamped;

        SetVector(clamped / maxRadius);
    }

    private void End()
    {
        active = false;
        activeFingerId = -1;

        SetVector(Vector2.zero);

        if (handle) handle.anchoredPosition = Vector2.zero;

        if (hideWhenIdle)
            SetVisible(false);
    }

    private void SetVector(Vector2 v)
    {
        currentVector = v;
        OnMoveVector?.Invoke(currentVector);
    }

    private void SetVisible(bool show)
    {
        if (joystickRoot) joystickRoot.gameObject.SetActive(show);
        if (handle) handle.gameObject.SetActive(show);
    }

    private static bool IsPointerOverUI(int fingerId)
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(fingerId);
    }
}