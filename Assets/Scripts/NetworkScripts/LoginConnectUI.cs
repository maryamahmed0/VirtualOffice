using TMPro;
using UnityEngine;
using Unity.Netcode;
using System.Text;

public class LobbyConnectUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_InputField orgInput;
    [SerializeField] private TMP_InputField joinCodeInput; 
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private TMP_Text joinCodeText; 

    private RelayConnector relay;
    private bool busy;

    private void Awake()
    {
        relay = FindFirstObjectByType<RelayConnector>();
    }


    void ApplyConnectionPayload(string playerName, string org)
    {
        var nm = NetworkManager.Singleton;
        var payload = $"{playerName}|{org}";
        nm.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(payload);
        Debug.Log($"[PAYLOAD] set >>>{payload}<<< bytes={nm.NetworkConfig.ConnectionData.Length}");
    }

    private static string SanitizeJoinCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "";


        System.Text.StringBuilder sb = new System.Text.StringBuilder(code.Length);
        foreach (char c in code)
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToUpperInvariant(c));
        }

        string cleaned = sb.ToString();

        if (cleaned.Length > 6)
            cleaned = cleaned.Substring(cleaned.Length - 6, 6);

        return cleaned;
    }

 
    public async void OnEnterClicked()
    {
        if (busy) return;
        busy = true;

        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            WebVoiceGate.MarkUserGesture();
            Debug.Log("[WEB] Enter clicked - gesture sent");
        }

        errorText.text = "";
        statusText.text = "Connecting...";

        string displayName = nameInput.text.Trim();
        string org = orgInput.text.Trim().ToUpperInvariant();

        string joinCodeRaw = joinCodeInput != null ? joinCodeInput.text : "";
        string joinCode = SanitizeJoinCode(joinCodeRaw);

        if (string.IsNullOrWhiteSpace(displayName))
        {
            errorText.text = "Please enter a name.";
            statusText.text = "";
            busy = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(org))
        {
            errorText.text = "Please enter Org ID.";
            statusText.text = "";
            busy = false;
            return;
        }


        if (GameSessionData.Instance != null)
            GameSessionData.Instance.SetUser(displayName, org);

        PlayerPrefs.SetString("PLAYER_NAME", displayName);
        PlayerPrefs.SetString("ORG_ID", org);

        try
        {
            if (relay == null)
                relay = FindFirstObjectByType<RelayConnector>();

            if (relay == null)
            {
                errorText.text = "RelayConnector not found in scene.";
                statusText.text = "";
                return;
            }

            var nm = NetworkManager.Singleton;
            if (nm == null)
            {
                errorText.text = "NetworkManager.Singleton is NULL.";
                statusText.text = "";
                return;
            }

            if (nm.IsListening)
            {
                statusText.text = "Already running (Host/Client). Stop and retry.";
                return;
            }

            ApplyConnectionPayload(displayName, org);

            if (string.IsNullOrEmpty(joinCode))
            {
                statusText.text = "Creating room...";

                // Host
                string code = await relay.CreateRoomAndHost(displayName, org);
                PlayerPrefs.SetString("JOIN_CODE", code);
                PlayerPrefs.Save();


                GameSessionData.Instance?.SetConnectionInfo(true, code);

                statusText.text = "Room created. Share join code:";
                if (joinCodeText != null) joinCodeText.text = $"Join Code: {code}";

                if (joinCodeInput != null) joinCodeInput.text = code;

                GUIUtility.systemCopyBuffer = code;
                Debug.Log($"[UI] Copied JoinCode to clipboard: >>>{code}<<<");
            }
            else
            {
                if (joinCode.Length != 6)
                {
                    errorText.text = $"Join code should be 6 chars. You entered: '{joinCode}' (len={joinCode.Length})";
                    statusText.text = "";
                    return;
                }

                //Client
                statusText.text = "Joining room...";
                GameSessionData.Instance?.SetConnectionInfo(false, joinCode);

                Debug.Log($"[UI] Joining with EXACT >>>{joinCode}<<< len={joinCode.Length}");

                await relay.JoinRoomAndClient(joinCode, displayName, org);
                PlayerPrefs.SetString("JOIN_CODE", joinCode);
                PlayerPrefs.Save();


                statusText.text = "Joined!";
            }
        }
        catch (Unity.Services.Core.RequestFailedException e)
        {
            Debug.LogError($"[UGS] RequestFailed: Status={e.ErrorCode} Msg={e.Message}");
            statusText.text = "";
            errorText.text = $"Failed ({e.ErrorCode}). {e.Message}";
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            statusText.text = "";
            errorText.text = "Failed. Check console.";
        }
        finally
        {
            busy = false;
        }
    }
}