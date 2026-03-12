using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MeetingChatUI : MonoBehaviour
{
    [Header("Meeting Controls Root")]
    [SerializeField] private GameObject meetingControlsRoot;

    [Header("Chat Toggle")]
    [SerializeField] private Button chatToggleButton;
    [SerializeField] private GameObject unreadBadge;

    [Header("Chat Window")]
    [SerializeField] private GameObject chatWindowRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;

    [Header("Messages")]
    [SerializeField] private Transform messagesContent;
    [SerializeField] private ScrollRect messagesScrollRect;
    [SerializeField] private MeetingChatMessageRow localRowPrefab;
    [SerializeField] private MeetingChatMessageRow remoteRowPrefab;

    private NetRoomState _localRoomState;
    private bool _isOpen;
    private int _unreadCount;
    private bool _subscribed;

    private void OnEnable()
    {
        if (chatToggleButton != null)
            chatToggleButton.onClick.AddListener(ToggleChat);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseChat);

        if (sendButton != null)
            sendButton.onClick.AddListener(SendCurrentMessage);

        if (inputField != null)
            inputField.onSubmit.AddListener(HandleInputSubmit);

        TrySubscribeToChatService();
    }

    private void OnDisable()
    {
        UIInputBlocker.BlockGameplayInput = false;

        if (chatToggleButton != null)
            chatToggleButton.onClick.RemoveListener(ToggleChat);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseChat);

        if (sendButton != null)
            sendButton.onClick.RemoveListener(SendCurrentMessage);

        if (inputField != null)
            inputField.onSubmit.RemoveListener(HandleInputSubmit);

        if (_subscribed && MeetingChatService.Instance != null)
        {
            MeetingChatService.Instance.OnMessageReceived -= HandleMessageReceived;
            _subscribed = false;
        }
    }

    private void Start()
    {
        ResolveLocalRoomState();
        SetChatOpen(false, true);
        RebuildMessages();
        RefreshMeetingVisibility();
        RefreshUnreadBadge();
        TrySubscribeToChatService();
    }

    private void Update()
    {
        if (_localRoomState == null)
            ResolveLocalRoomState();

        RefreshMeetingVisibility();
    }

    private void HandleInputSubmit(string value)
    {
        if (!_isOpen) return;
        if (!IsLocalPlayerInMeeting()) return;

        SendCurrentMessage();
    }

    private void TrySubscribeToChatService()
    {
        if (_subscribed) return;
        if (MeetingChatService.Instance == null) return;

        MeetingChatService.Instance.OnMessageReceived += HandleMessageReceived;
        _subscribed = true;

        Debug.Log("[CHAT UI] subscribed to MeetingChatService");
    }

    private void ResolveLocalRoomState()
    {
        NetRoomState[] roomStates = FindObjectsOfType<NetRoomState>(true);

        foreach (var roomState in roomStates)
        {
            NetworkObject netObj = roomState.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                _localRoomState = roomState;
                return;
            }
        }
    }

    private bool IsLocalPlayerInMeeting()
    {
        return _localRoomState != null && _localRoomState.GetZone() == NetRoomState.Zone.Meeting;
    }

    private void RefreshMeetingVisibility()
    {
        bool inMeeting = IsLocalPlayerInMeeting();

        if (meetingControlsRoot != null)
            meetingControlsRoot.SetActive(inMeeting);

        if (!inMeeting && _isOpen)
            SetChatOpen(false, true);

        if (!inMeeting)
            ClearUnread();
    }

    private void ToggleChat()
    {
        if (!IsLocalPlayerInMeeting())
            return;

        SetChatOpen(!_isOpen);
    }

    private void CloseChat()
    {
        SetChatOpen(false);
    }

    private void SetChatOpen(bool open, bool force = false)
    {
        if (!force && _isOpen == open)
            return;

        _isOpen = open;
        UIInputBlocker.BlockGameplayInput = open;

        if (chatWindowRoot != null)
            chatWindowRoot.SetActive(open);

        if (open)
        {
            ClearUnread();
            AutoScrollToBottom();

            if (inputField != null)
            {
                inputField.ActivateInputField();
                inputField.Select();
            }
        }
    }

    private void SendCurrentMessage()
    {
        if (!IsLocalPlayerInMeeting()) return;
        if (MeetingChatService.Instance == null) return;
        if (inputField == null) return;

        string text = inputField.text;
        if (string.IsNullOrWhiteSpace(text)) return;

        string senderName = ResolveLocalPlayerName();

        MeetingChatService.Instance.SendMessageToMeeting(senderName, text);

        inputField.text = string.Empty;
        inputField.ActivateInputField();
        inputField.Select();
    }

    private void HandleMessageReceived(MeetingChatService.ChatMessage msg)
    {
        CreateRow(msg.SenderName, msg.Text, msg.SenderClientId);
        AutoScrollToBottom();

        if (!_isOpen && IsLocalPlayerInMeeting())
        {
            _unreadCount++;
            RefreshUnreadBadge();
        }
    }

    private void RebuildMessages()
    {
        if (messagesContent == null || MeetingChatService.Instance == null)
            return;

        for (int i = messagesContent.childCount - 1; i >= 0; i--)
            Destroy(messagesContent.GetChild(i).gameObject);

        foreach (var msg in MeetingChatService.Instance.Messages)
            CreateRow(msg.SenderName, msg.Text, msg.SenderClientId);

        AutoScrollToBottom();
    }

    private void CreateRow(string senderName, string text, ulong senderClientId)
    {
        if (messagesContent == null)
            return;

        bool isLocal = NetworkManager.Singleton != null &&
                       senderClientId == NetworkManager.Singleton.LocalClientId;

        MeetingChatMessageRow prefabToUse = isLocal ? localRowPrefab : remoteRowPrefab;
        if (prefabToUse == null)
            return;

        var row = Instantiate(prefabToUse, messagesContent);
        row.Bind(senderName, text, isLocal);
    }

    private void AutoScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();

        if (messagesScrollRect != null)
        {
            messagesScrollRect.verticalNormalizedPosition = 0f;
            LayoutRebuilder.ForceRebuildLayoutImmediate(messagesScrollRect.content);
            Canvas.ForceUpdateCanvases();
            messagesScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void ClearUnread()
    {
        _unreadCount = 0;
        RefreshUnreadBadge();
    }

    private void RefreshUnreadBadge()
    {
        if (unreadBadge != null)
            unreadBadge.SetActive(_unreadCount > 0);
    }

    private string ResolveLocalPlayerName()
    {
        if (NetworkManager.Singleton == null)
            return "Player";

        var localClient = NetworkManager.Singleton.LocalClient;
        if (localClient == null || localClient.PlayerObject == null)
            return "Player";

        var localPlayer = localClient.PlayerObject;
        var identity = localPlayer.GetComponent<PlayerIdentity>();

        if (identity != null)
        {
            string displayName = identity.DisplayName.Value.ToString();

            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;
        }

        return "Player";
    }
}