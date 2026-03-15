using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class MuteButtonUI : MonoBehaviour
{
    [SerializeField] private Button muteButton;

    [Header("Button Icons")]
    [SerializeField] private GameObject micIcon;
    [SerializeField] private GameObject micMutedIcon;

    private PlayerVoicePresenter localPresenter;
    private PlayerVoiceState voiceState;

    private void Start()
    {
        if (muteButton == null)
        {
            Debug.LogError("[MuteButtonUI] muteButton missing!");
            return;
        }

        muteButton.onClick.AddListener(OnMuteClicked);

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        TryHookLocalPlayer();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;

        if (voiceState != null)
            voiceState.IsMicMuted.OnValueChanged -= OnMuteChanged;
    }

    private void OnClientConnected(ulong _)
    {
        TryHookLocalPlayer();
    }

    private void TryHookLocalPlayer()
    {
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.LocalClient == null) return;
        if (NetworkManager.Singleton.LocalClient.PlayerObject == null) return;

        var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;

        var newPresenter = playerObj.GetComponent<PlayerVoicePresenter>();
        var newVoiceState = playerObj.GetComponent<PlayerVoiceState>();

        if (voiceState != null)
            voiceState.IsMicMuted.OnValueChanged -= OnMuteChanged;

        localPresenter = newPresenter;
        voiceState = newVoiceState;

        if (voiceState != null)
        {
            voiceState.IsMicMuted.OnValueChanged += OnMuteChanged;
            UpdateButtonUI(voiceState.IsMicMuted.Value);
        }
    }

    private void OnMuteClicked()
    {
        if (localPresenter == null)
            TryHookLocalPlayer();

        if (localPresenter != null)
            localPresenter.ToggleMute();
    }

    private void OnMuteChanged(bool oldValue, bool newValue)
    {
        UpdateButtonUI(newValue);
    }

    private void UpdateButtonUI(bool muted)
    {
        if (micIcon != null)
            micIcon.SetActive(!muted);

        if (micMutedIcon != null)
            micMutedIcon.SetActive(muted);
    }
}