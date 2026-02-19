using UnityEngine;
using Unity.Netcode;

public class PlayerVoicePresenter : NetworkBehaviour
{
    private PlayerVoiceState voiceState;

    [Header("UI")]
    [SerializeField] private GameObject muteIconObject; 
    [SerializeField] private GameObject UnmuteIconObject; 


    private void Awake()
    {
        voiceState = GetComponent<PlayerVoiceState>();
    }

    public override void OnNetworkSpawn()
    {
        if (voiceState == null) voiceState = GetComponent<PlayerVoiceState>();

        // اسمع أي تغيير في المتغير الشبكي (هيحصل لكل الكلاينت)
        voiceState.IsMicMuted.OnValueChanged += OnMuteChanged;

        // اول ما اللاعب يظهر: طبّق الحالة الحالية على الـ UI عند كل الناس
        OnMuteChanged(false, voiceState.IsMicMuted.Value);

        // لو ده الـ Owner (انا)، طبّق كمان على Vivox (Local)
        if (IsOwner)
        {
            ApplyVivoxMute(voiceState.IsMicMuted.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (voiceState != null)
            voiceState.IsMicMuted.OnValueChanged -= OnMuteChanged;
    }

    public void ToggleMute()
    {
        if (!IsOwner) return;

        bool newMuted = !voiceState.IsMicMuted.Value;

        ApplyVivoxMute(newMuted);
        UpdateMuteUI(newMuted); // local instant feedback (اختياري)

        voiceState.SetMutedServerRpc(newMuted);
    }

    private void OnMuteChanged(bool oldValue, bool newValue)
    {
        // (A) حدّث UI عند كل الناس (أيقونة فوق الراس/اسم اللاعب)
        UpdateMuteUI(newValue);

        // (B) لو ده صاحب الشخصية على جهازه، خلي Vivox يطابق الحالة برضو
        if (IsOwner)
            ApplyVivoxMute(newValue);
    }

    private void ApplyVivoxMute(bool muted)
    {
        // مهم: ما تعمليش Login/Join هنا. ده شغل VoiceAutoJoin عندك.
        if (VoiceManager.Instance != null)
            VoiceManager.Instance.SetMute(muted);
    }

    private void UpdateMuteUI(bool muted)
    {
        if (muteIconObject != null)
            muteIconObject.SetActive(muted);

        if (UnmuteIconObject != null)
            UnmuteIconObject.SetActive(!muted);
    }
}