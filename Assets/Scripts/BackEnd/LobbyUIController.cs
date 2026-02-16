using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using System.Text;
using System.Collections;

public class LobbyUIController : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField nameInput;
    public TMP_InputField orgInput;

    [Header("UI")]
    public Button enterButton;
    public TMP_Text errorText;
    public TMP_Text statusText;

    [Header("Connection")]
    public float clientTimeoutSeconds = 2.0f;

    private void Start()
    {
        errorText.text = "";
        statusText.text = "";

        nameInput.onValueChanged.AddListener(_ => RefreshUI());
        orgInput.onValueChanged.AddListener(_ => RefreshUI());

        RefreshUI();
    }

    private void RefreshUI()
    {
        errorText.text = "";

        bool valid = IsValidName(nameInput.text) && IsValidOrg(orgInput.text);
        enterButton.interactable = valid;
    }

    public void OnEnterClicked()
    {
        errorText.text = "";

        string displayName = SanitizeName(nameInput.text);
        string orgCode = SanitizeOrg(orgInput.text);

        if (!IsValidName(displayName))
        {
            ShowError("Please enter a valid name (2–16 characters).");
            return;
        }

        if (!IsValidOrg(orgCode))
        {
            ShowError("Please enter a valid organization code (3–24).");
            return;
        }

        // حفظ البيانات (ينفع بعدين تيجي من الباك بدل الحقول)
        GameSessionData.Instance.SetUser(displayName, orgCode);

        // اقفل UI أثناء الاتصال
        enterButton.interactable = false;
        nameInput.interactable = false;
        orgInput.interactable = false;
        statusText.text = "Connecting...";

        // ConnectionData: name|org
        string payload = $"{displayName}|{orgCode}";
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(payload);

        // Strategy حالياً (للتجربة): جرّبي Client، لو مفيش سيرفر يبقى Host
        // لو مش عايزة ده دلوقتي: خليه دايمًا Host أثناء التطوير.
        NetworkManager.Singleton.StartClient();
        StartCoroutine(ClientFallbackToHost());
    }

    private IEnumerator ClientFallbackToHost()
    {
        float t = 0f;

        while (t < clientTimeoutSeconds)
        {
            if (NetworkManager.Singleton.IsConnectedClient)
            {
                statusText.text = "Connected!";
                yield break;
            }

            t += Time.deltaTime;
            yield return null;
        }

        // فشل الاتصال → نعمل Host
        NetworkManager.Singleton.Shutdown();
        yield return null;

        statusText.text = "Starting host...";
        NetworkManager.Singleton.StartHost();
        statusText.text = "Host started!";
    }

    private void ShowError(string msg)
    {
        statusText.text = "";
        errorText.text = msg;

        // رجّعي UI
        nameInput.interactable = true;
        orgInput.interactable = true;
        RefreshUI();
    }

    private bool IsValidName(string s)
    {
        s = SanitizeName(s);
        return s.Length >= 2 && s.Length <= 16;
    }

    private bool IsValidOrg(string s)
    {
        s = SanitizeOrg(s);
        return s.Length >= 3 && s.Length <= 24;
    }

    private string SanitizeName(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Trim();
        if (s.Length > 16) s = s.Substring(0, 16);
        return s;
    }

    private string SanitizeOrg(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Trim().ToUpperInvariant();
        if (s.Length > 24) s = s.Substring(0, 24);
        return s;
    }
}