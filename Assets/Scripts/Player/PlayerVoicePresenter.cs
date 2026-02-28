using UnityEngine;
using Unity.Netcode;

public class PlayerVoicePresenter : NetworkBehaviour
{
    private PlayerVoiceState voiceState;

    [Header("UI")]
    [SerializeField] private GameObject muteIconObject;
    [SerializeField] private GameObject UnmuteIconObject;

    private bool _subscribed;
    private bool _appliedOnceAfterLogin;

    private void Awake()
    {
        voiceState = GetComponent<PlayerVoiceState>();
    }

    public override void OnNetworkSpawn()
    {
        if (voiceState == null) voiceState = GetComponent<PlayerVoiceState>();
        if (voiceState == null) return;

        if (!_subscribed)
        {
            voiceState.IsMicMuted.OnValueChanged += OnMuteChanged;
            _subscribed = true;
        }

        UpdateMuteUI(voiceState.IsMicMuted.Value);

        if (IsOwner)
            InvokeRepeating(nameof(TryApplyAfterLogin), 0.2f, 0.2f);
    }

    public override void OnNetworkDespawn()
    {
        if (_subscribed && voiceState != null)
        {
            voiceState.IsMicMuted.OnValueChanged -= OnMuteChanged;
            _subscribed = false;
        }

        CancelInvoke(nameof(TryApplyAfterLogin));
    }

    private void TryApplyAfterLogin()
    {
        if (_appliedOnceAfterLogin) { CancelInvoke(nameof(TryApplyAfterLogin)); return; }

        if (VoiceManager.Instance == null) return;
        if (!VoiceManager.Instance.IsLoggedIn) return;

        ApplyVivoxMute(voiceState.IsMicMuted.Value);
        _appliedOnceAfterLogin = true;

        CancelInvoke(nameof(TryApplyAfterLogin));
    }

    public void ToggleMute()
    {
        if (!IsOwner) return;
        if (voiceState == null) return;

        bool newMuted = !voiceState.IsMicMuted.Value;

        UpdateMuteUI(newMuted);

        voiceState.SetMutedServerRpc(newMuted);

        ApplyVivoxMute(newMuted);
    }

    private void OnMuteChanged(bool oldValue, bool newValue)
    {
        UpdateMuteUI(newValue);

        if (IsOwner)
            ApplyVivoxMute(newValue);
    }

    private void ApplyVivoxMute(bool muted)
    {
        if (!IsOwner) return;

        if (VoiceManager.Instance == null) return;
        if (!VoiceManager.Instance.IsLoggedIn) return;

        VoiceManager.Instance.SetMute(muted);
    }

    private void UpdateMuteUI(bool muted)
    {
        if (muteIconObject != null) muteIconObject.SetActive(muted);
        if (UnmuteIconObject != null) UnmuteIconObject.SetActive(!muted);
    }
}