using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OfficeUIBinder : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject outgoingCard;
    [SerializeField] private GameObject incomingCard;
    [SerializeField] private GameObject inCallBar;
    [SerializeField] private GameObject meetingMicPanel;

    [Header("Outgoing")]
    [SerializeField] private TMP_Text outgoingNameText;
    [SerializeField] private Button cancelOutgoingButton;

    [Header("Incoming")]
    [SerializeField] private TMP_Text incomingNameText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;

    [Header("In Call")]
    [SerializeField] private TMP_Text inCallNameText;
    [SerializeField] private TMP_Text inCallStatusText;
    [SerializeField] private Button endButton;
    [SerializeField] private Button inCallMuteButton;

    [Header("InCall Status Colors")]
    [SerializeField] private Color connectingColor = Color.yellow;
    [SerializeField] private Color connectedColor = Color.green;
    [SerializeField] private Color failedColor = Color.red;          
    [SerializeField] private Color defaultStatusColor = Color.white;

    [Header("InCall Mic Icons (GameObjects)")]
    [SerializeField] private GameObject inCallMicOnIcon;
    [SerializeField] private GameObject inCallMicOffIcon;

    private PlayerRoomState room;
    private CallController call;
    private PlayerVoicePresenter voicePresenter;
    private NetRoomState netRoom;

    private void Awake()
    {
        Debug.Log("[UIBINDER] Awake -> hide panels");
        SetAllHidden();

        if (acceptButton) acceptButton.onClick.AddListener(() => call?.AcceptCall());
        if (declineButton) declineButton.onClick.AddListener(() => call?.DeclineCall());
        if (endButton) endButton.onClick.AddListener(() => call?.EndCall());

        if (cancelOutgoingButton)
        {
            cancelOutgoingButton.onClick.AddListener(() =>
            {
                if (call == null) return;

                if (call.State == CallController.CallState.RingingOut)
                    call.CancelOutgoing();
                else
                    call.EndCall();
            });
        }

        if (inCallMuteButton)
            inCallMuteButton.onClick.AddListener(ToggleMute);

        ApplyMuteUI();
    }

    private void Update()
    {
        if (room == null && PlayerRoomState.LocalInstance != null)
        {
            room = PlayerRoomState.LocalInstance;
            call = room.GetComponent<CallController>();
            netRoom = room.GetComponentInParent<NetRoomState>();
            voicePresenter = room.GetComponent<PlayerVoicePresenter>();

            Debug.Log("[UIBINDER] Hooked. roomType=" + room.CurrentRoomType + " hasCall=" + (call != null));

            if (call != null)
            {
                call.OnIncomingCall += HandleIncoming;
                call.OnCallStarted += HandleCallStarted;
                call.OnCallEnded += HandleCallEnded;
                call.OnVoiceStatusChanged += HandleVoiceStatus;
            }
        }

        bool inMeeting =
     netRoom != null &&
     netRoom.GetZone() == NetRoomState.Zone.Meeting;

        if (meetingMicPanel) meetingMicPanel.SetActive(inMeeting);

        if (call != null)
        {
            if (outgoingCard) outgoingCard.SetActive(call.State == CallController.CallState.RingingOut);
            if (incomingCard) incomingCard.SetActive(call.State == CallController.CallState.RingingIn);

            if (inCallBar) inCallBar.SetActive(
                call.State == CallController.CallState.Connecting ||
                call.State == CallController.CallState.InCall
            );

            if (outgoingNameText && call.State == CallController.CallState.RingingOut)
            {
                outgoingNameText.text = string.IsNullOrWhiteSpace(call.OtherDisplayName)
                    ? $"User {call.OtherClientId}"
                    : call.OtherDisplayName;
            }

            if (inCallNameText &&
                (call.State == CallController.CallState.Connecting || call.State == CallController.CallState.InCall))
            {
                inCallNameText.text = string.IsNullOrWhiteSpace(call.OtherDisplayName)
                    ? "In Call"
                    : call.OtherDisplayName;
            }
        }
        ApplyMuteUI();
    }

    private void OnDestroy()
    {
        if (call != null)
        {
            call.OnIncomingCall -= HandleIncoming;
            call.OnCallStarted -= HandleCallStarted;
            call.OnCallEnded -= HandleCallEnded;
            call.OnVoiceStatusChanged -= HandleVoiceStatus;
        }
    }

    private void HandleIncoming(string callerName, ulong callerId)
    {
        Debug.Log("[UIBINDER] Incoming UI show for: " + callerName);

        if (incomingNameText) incomingNameText.text = callerName;
        if (outgoingCard) outgoingCard.SetActive(false);
    }

    private void HandleCallStarted(string channel)
    {
        Debug.Log("[UIBINDER] InCall UI show (Connecting). channel=" + channel);

        if (incomingCard) incomingCard.SetActive(false);
        if (outgoingCard) outgoingCard.SetActive(false);

        if (inCallNameText)
            inCallNameText.text = string.IsNullOrWhiteSpace(call?.OtherDisplayName) ? "In Call" : call.OtherDisplayName;

        if (inCallStatusText)
        {
            inCallStatusText.text = "Connecting...";
            inCallStatusText.color = connectingColor;
        }

        if (inCallBar) inCallBar.SetActive(true);
    }

    private void HandleVoiceStatus(string status)
    {
        if (!inCallStatusText) return;

        inCallStatusText.text = status;

        if (status == "Connected")
        {
            inCallStatusText.color = connectedColor;
            return;
        }

        if (!string.IsNullOrEmpty(status) &&
            (status.Contains("failed") || status.Contains("Failed") || status.Contains("error") || status.Contains("Error")))
        {
            inCallStatusText.color = failedColor;
            return;
        }

        if (!string.IsNullOrEmpty(status) && status.StartsWith("Connecting"))
        {
            inCallStatusText.color = connectingColor;
            return;
        }

        inCallStatusText.color = defaultStatusColor;
    }

    private void HandleCallEnded()
    {
        Debug.Log("[UIBINDER] Call ended UI hide");

        if (incomingCard) incomingCard.SetActive(false);
        if (outgoingCard) outgoingCard.SetActive(false);
        if (inCallBar) inCallBar.SetActive(false);

        if (inCallStatusText)
        {
            inCallStatusText.text = "";
            inCallStatusText.color = defaultStatusColor;
        }
    }

    private void ToggleMute()
    {
        if (voicePresenter == null) return;

        voicePresenter.ToggleMute();
        ApplyMuteUI();
    }

    private void ApplyMuteUI()
    {
        bool mutedNow = voicePresenter != null && voicePresenter.IsMuted;

        if (inCallMicOnIcon) inCallMicOnIcon.SetActive(!mutedNow);
        if (inCallMicOffIcon) inCallMicOffIcon.SetActive(mutedNow);
    }
    private void SetAllHidden()
    {
        if (outgoingCard) outgoingCard.SetActive(false);
        if (incomingCard) incomingCard.SetActive(false);
        if (inCallBar) inCallBar.SetActive(false);
        if (meetingMicPanel) meetingMicPanel.SetActive(false);

        if (inCallStatusText) inCallStatusText.color = defaultStatusColor;
    }
}