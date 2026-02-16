using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class AutoLoadScene : MonoBehaviour
{
    [SerializeField] private string meetingSceneName = "MeetingRoom";

    [Header("Timing")]
    [SerializeField] private float hostFallbackDelay = 1.0f;     // هوست يدخل حتى لو محدش دخل
    [SerializeField] private float afterClientDelay = 0.25f;     // استنى شوية بعد دخول الكلاينت قبل load

    private bool networkLoadIssued;

    private void OnEnable()
    {
        StartCoroutine(WaitAndSubscribe());
    }

    private IEnumerator WaitAndSubscribe()
    {
        while (NetworkManager.Singleton == null) yield return null;

        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        Debug.Log("[AutoLoad] Subscribed events");
        Debug.Log($"[AutoLoad] SceneManagementEnabled? {NetworkManager.Singleton.NetworkConfig.EnableSceneManagement}");
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnServerStarted()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        Debug.Log($"[AutoLoad] OnServerStarted IsServer={NetworkManager.Singleton.IsServer} IsHost={NetworkManager.Singleton.IsHost}");

        // ✅ fallback للهوست فقط
        CancelInvoke(nameof(HostFallbackLoad));
        Invoke(nameof(HostFallbackLoad), hostFallbackDelay);
    }

    private void HostFallbackLoad()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        // لو already عملنا network load، خلاص
        if (networkLoadIssued) return;

        Debug.Log("[AutoLoad][SERVER] Fallback: loading MeetingRoom now (host only timing).");
        IssueNetworkLoad();
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        // تجاهل الهوست نفسه
        if (clientId == NetworkManager.Singleton.LocalClientId)
            return;

        Debug.Log($"[AutoLoad][SERVER] Real client connected (id={clientId}). Forcing scene load for sync...");

        // ✅ أول ما يدخل client حقيقي، اوقفي fallback
        CancelInvoke(nameof(HostFallbackLoad));

        // ✅ حتى لو الهوست already في MeetingRoom، هنعمل network load تاني لضمان أن الكلاينت يستلم SceneEvent
        StartCoroutine(LoadAfterShortDelay());
    }

    private IEnumerator LoadAfterShortDelay()
    {
        yield return new WaitForSeconds(afterClientDelay);
        IssueNetworkLoad(forceEvenIfIssued: true);
    }

    private void IssueNetworkLoad(bool forceEvenIfIssued = false)
    {
        if (NetworkManager.Singleton.SceneManager == null)
        {
            Debug.LogError("[AutoLoad] SceneManager NULL. Ensure 'Enable Scene Management' on NetworkManager is checked.");
            return;
        }

        // لو مش forced وسبق أصدرنا load → متكررّيش
        if (networkLoadIssued && !forceEvenIfIssued) return;

        // هنا بنسمح بالـ force لو دخل client متأخر
        networkLoadIssued = true;

        Debug.Log($"[AutoLoad][SERVER] NetworkSceneManager.LoadScene -> {meetingSceneName}");
        NetworkManager.Singleton.SceneManager.LoadScene(meetingSceneName, LoadSceneMode.Single);
    }
}