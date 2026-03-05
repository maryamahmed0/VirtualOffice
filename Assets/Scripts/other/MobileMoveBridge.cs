using UnityEngine;

public class MobileMoveBridge : MonoBehaviour
{
    [SerializeField] private DynamicJoystickTouch joystick;

    private PlayerInputReader inputReader;

    private void Start()
    {
        if (!joystick) joystick = GetComponentInChildren<DynamicJoystickTouch>(true);
        joystick.OnMoveVector += OnMove;
    }

    private void OnDestroy()
    {
        if (joystick != null) joystick.OnMoveVector -= OnMove;
    }

    private void OnMove(Vector2 v)
    {
        if (inputReader == null)
        {
            var lp = Unity.Netcode.NetworkManager.Singleton?.LocalClient?.PlayerObject;
            if (lp) inputReader = lp.GetComponent<PlayerInputReader>();
        }

        if (inputReader == null) return;

        // لو وقفت اللمسة
        if (v.sqrMagnitude < 0.0001f)
        {
            inputReader.ClearMobileOverride();
            return;
        }

        inputReader.SetMoveFromUI(v);
    }
}