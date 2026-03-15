using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerIdentity : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private RectTransform nameplateRoot;

    public NetworkVariable<FixedString64Bytes> DisplayName =
        new NetworkVariable<FixedString64Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    private static readonly Dictionary<ulong, string> NameByClientId = new();

    public static string GetName(ulong clientId)
    {
        if (NameByClientId.TryGetValue(clientId, out var n) && !string.IsNullOrWhiteSpace(n))
            return n;

        return $"User {clientId}";
    }

    public override void OnNetworkSpawn()
    {
        if (nameText == null)
            nameText = GetComponentInChildren<TMP_Text>(true);

        if (nameplateRoot == null && nameText != null)
            nameplateRoot = nameText.GetComponentInParent<RectTransform>();

        DisplayName.OnValueChanged += OnNameChanged;

        RefreshName();

        if (IsOwner)
        {
            string n = GetLocalDisplayName();
            SubmitIdentityServerRpc(n);
        }
    }

    public override void OnNetworkDespawn()
    {
        DisplayName.OnValueChanged -= OnNameChanged;
        NameByClientId.Remove(OwnerClientId);
    }

    private void OnNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        RefreshName();
    }

    private string GetLocalDisplayName()
    {
        string n = (GameSessionData.Instance != null) ? GameSessionData.Instance.DisplayName : "";

        if (string.IsNullOrWhiteSpace(n))
            n = PlayerPrefs.GetString("PLAYER_NAME", "");

        n = (n ?? "").Trim();
        if (n.Length > 16) n = n.Substring(0, 16);
        if (n.Length < 2) n = "Player";

        return n;
    }

    private void RefreshName()
    {
        string value = DisplayName.Value.ToString();
        if (string.IsNullOrWhiteSpace(value)) value = "Player";

        NameByClientId[OwnerClientId] = value;

        if (nameText != null)
            nameText.text = value;

        RefreshLayout();
    }

    private void RefreshLayout()
    {
        Canvas.ForceUpdateCanvases();

        if (nameText != null)
        {
            var textRect = nameText.rectTransform;
            if (textRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);
        }

        if (nameplateRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(nameplateRoot);
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