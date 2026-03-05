using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MobileActionController : MonoBehaviour
{
    public enum ActionType { None, Door, Call }

    [Header("UI")]
    [SerializeField] private Button actionBtn;
    [SerializeField] private TMP_Text label;

    [Header("Scan")]
    [SerializeField] private float refreshInterval = 0.2f;

    private float nextTime;
    private ActionType currentAction = ActionType.None;

    private ProximityCallScanner callScanner;
    private PlayerDoorInteractor doorInteractor;
    private CallController callController;
    private NetRoomState netRoom;

    private void Awake()
    {
        if (actionBtn) actionBtn.onClick.AddListener(DoAction);
        SetState(ActionType.None);
    }

    private void Update()
    {
        if (Time.time >= nextTime)
        {
            nextTime = Time.time + refreshInterval;
            RefreshContext();
        }
    }

    private void HookLocal()
    {
        var lp = NetworkManager.Singleton?.LocalClient?.PlayerObject;
        if (!lp) return;

        if (callScanner == null) callScanner = lp.GetComponent<ProximityCallScanner>();
        if (doorInteractor == null) doorInteractor = lp.GetComponent<PlayerDoorInteractor>();
        if (callController == null) callController = lp.GetComponent<CallController>();
        if (netRoom == null) netRoom = lp.GetComponentInParent<NetRoomState>();
    }

    private void RefreshContext()
    {
        HookLocal();

        bool inMeeting = (netRoom != null && netRoom.GetZone() == NetRoomState.Zone.Meeting);

        // لو في مكالمة شغالة، خلي الزر disabled (اختياري)
        if (callController != null && callController.State != CallController.CallState.Idle)
        {
            SetState(ActionType.None, "In Call");
            return;
        }

        // ✅ Door أولاً
        if (doorInteractor != null && doorInteractor.HasDoorInRange)
        {
            SetState(ActionType.Door, "Enter");
            return;
        }

        // ✅ Call ثانيًا (ممنوع في meeting)
        if (!inMeeting && callScanner != null && callScanner.CanCall)
        {
            SetState(ActionType.Call, "Call");
            return;
        }

        SetState(ActionType.None, "...");
    }

    private void SetState(ActionType a, string txt = "...")
    {
        currentAction = a;
        if (label) label.text = txt;
        if (actionBtn) actionBtn.interactable = (a != ActionType.None);
    }

    private void DoAction()
    {
        HookLocal();

        switch (currentAction)
        {
            case ActionType.Door:
                doorInteractor?.UseDoor();
                break;

            case ActionType.Call:
                callScanner?.TryRequestCall();
                break;
        }
    }
}