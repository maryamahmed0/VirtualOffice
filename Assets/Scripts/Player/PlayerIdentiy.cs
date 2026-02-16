using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Collections;

public class PlayerIdentity : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;

    public NetworkVariable<FixedString64Bytes> DisplayName =
        new NetworkVariable<FixedString64Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (nameText == null)
            nameText = GetComponentInChildren<TMP_Text>(true);

        DisplayName.OnValueChanged += (_, __) => RefreshName();

        // اعرض اللي موجود (حتى لو فاضي) عشان مايبقاش Null
        RefreshName();

        // بس الـOwner هو اللي يبعت الاسم للسيرفر
        if (!IsOwner) return;

        string n = GetLocalDisplayName();
        SubmitIdentityServerRpc(n);
    }

    private string GetLocalDisplayName()
    {
        // 1) من GameSessionData
        string n = (GameSessionData.Instance != null) ? GameSessionData.Instance.DisplayName : "";

        // 2) fallback من PlayerPrefs (لو المشهد اتفتح قبل ما SessionData تتجهز)
        if (string.IsNullOrWhiteSpace(n))
            n = PlayerPrefs.GetString("PLAYER_NAME", "");

        // sanitize
        n = (n ?? "").Trim();
        if (n.Length > 16) n = n.Substring(0, 16);
        if (n.Length < 2) n = "Player";

        return n;
    }

    private void RefreshName()
    {
        if (nameText == null) return;

        string value = DisplayName.Value.ToString();
        nameText.text = string.IsNullOrWhiteSpace(value) ? "Player" : value;
    }

    [ServerRpc(RequireOwnership = true)]
    private void SubmitIdentityServerRpc(string displayName)
    {
        displayName = (displayName ?? "").Trim();
        if (displayName.Length > 16) displayName = displayName.Substring(0, 16);
        if (displayName.Length < 2) displayName = "Player";

        DisplayName.Value = displayName;
    }
}