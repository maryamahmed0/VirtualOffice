using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class MeetingReactionsPanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject reactionsPanelRoot;
    [SerializeField] private Button reactionsToggleButton;

    [Header("Reaction Buttons")]
    [SerializeField] private Button heartButton;
    [SerializeField] private Button likeButton;
    [SerializeField] private Button laughButton;
    [SerializeField] private Button sadButton;
    [SerializeField] private Button clapButton;
    [SerializeField] private Button partyButton;

    [Header("Meeting Check")]
    [SerializeField] private NetRoomState localRoomState;

    private bool _isOpen;

    private void OnEnable()
    {
        if (reactionsToggleButton != null)
            reactionsToggleButton.onClick.AddListener(TogglePanel);

        if (heartButton != null)
            heartButton.onClick.AddListener(() => SendReaction(MeetingReactionType.Heart));

        if (likeButton != null)
            likeButton.onClick.AddListener(() => SendReaction(MeetingReactionType.Like));

        if (laughButton != null)
            laughButton.onClick.AddListener(() => SendReaction(MeetingReactionType.Laugh));

        if (sadButton != null)
            sadButton.onClick.AddListener(() => SendReaction(MeetingReactionType.Sad));

        if (clapButton != null)
            clapButton.onClick.AddListener(() => SendReaction(MeetingReactionType.Clap));

        if (partyButton != null)
            partyButton.onClick.AddListener(() => SendReaction(MeetingReactionType.Party));
    }

    private void OnDisable()
    {
        if (reactionsToggleButton != null)
            reactionsToggleButton.onClick.RemoveListener(TogglePanel);

        if (heartButton != null)
            heartButton.onClick.RemoveAllListeners();

        if (likeButton != null)
            likeButton.onClick.RemoveAllListeners();

        if (laughButton != null)
            laughButton.onClick.RemoveAllListeners();

        if (sadButton != null)
            sadButton.onClick.RemoveAllListeners();

        if (clapButton != null)
            clapButton.onClick.RemoveAllListeners();

        if (partyButton != null)
            partyButton.onClick.RemoveAllListeners();
    }

    private void Start()
    {
        SetPanelOpen(false, true);
    }

    private void Update()
    {
        if (localRoomState == null)
            ResolveLocalRoomState();

        bool inMeeting = IsLocalPlayerInMeeting();

        if (!inMeeting && _isOpen)
            SetPanelOpen(false, true);
    }

    private void ResolveLocalRoomState()
    {
        NetRoomState[] roomStates = FindObjectsOfType<NetRoomState>(true);

        foreach (var roomState in roomStates)
        {
            NetworkObject netObj = roomState.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                localRoomState = roomState;
                return;
            }
        }
    }

    private bool IsLocalPlayerInMeeting()
    {
        return localRoomState != null && localRoomState.GetZone() == NetRoomState.Zone.Meeting;
    }

    private void TogglePanel()
    {
        if (!IsLocalPlayerInMeeting())
            return;

        SetPanelOpen(!_isOpen);
    }

    private void SetPanelOpen(bool open, bool force = false)
    {
        if (!force && _isOpen == open)
            return;

        _isOpen = open;

        if (reactionsPanelRoot != null)
            reactionsPanelRoot.SetActive(open);
    }

    private void SendReaction(MeetingReactionType reactionType)
    {
        if (!IsLocalPlayerInMeeting()) return;
        if (MeetingReactionService.Instance == null) return;

        string senderName = ResolveLocalPlayerName();
        MeetingReactionService.Instance.SendReactionToMeeting(senderName, reactionType);

        SetPanelOpen(false);
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