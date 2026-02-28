using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class AutoLoadScene : MonoBehaviour
{
    [SerializeField] private string SceneName = "TheOffice";

    [Header("Timing")]
    [SerializeField] private float hostFallbackDelay = 1.0f;

    private bool networkLoadIssued;

    private void OnEnable()
    {
        StartCoroutine(WaitAndSubscribe());
    }

    private IEnumerator WaitAndSubscribe()
    {
        while (NetworkManager.Singleton == null) yield return null;

        NetworkManager.Singleton.OnServerStarted += OnServerStarted;

        Debug.Log("[AutoLoad] Subscribed events");
        Debug.Log($"[AutoLoad] SceneManagementEnabled? {NetworkManager.Singleton.NetworkConfig.EnableSceneManagement}");
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
    }

    private void OnServerStarted()
    {
        if (!NetworkManager.Singleton.IsServer) return;

        Debug.Log($"[AutoLoad] OnServerStarted IsServer={NetworkManager.Singleton.IsServer} IsHost={NetworkManager.Singleton.IsHost}");

        CancelInvoke(nameof(HostFallbackLoad));
        Invoke(nameof(HostFallbackLoad), hostFallbackDelay);
    }

    private void HostFallbackLoad()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (networkLoadIssued) return;

        // ✅ لو احنا أصلاً في MeetingRoom متعملش أي حاجة
        if (SceneManager.GetActiveScene().name == SceneName)
        {
            networkLoadIssued = true;
            Debug.Log("[AutoLoad] Already in MeetingRoom. No reload.");
            return;
        }

        IssueNetworkLoad();
    }

    private void IssueNetworkLoad()
    {
        if (NetworkManager.Singleton.SceneManager == null)
        {
            Debug.LogError("[AutoLoad] SceneManager NULL. Ensure 'Enable Scene Management' on NetworkManager is checked.");
            return;
        }

        if (networkLoadIssued) return;
        networkLoadIssued = true;

        Debug.Log($"[AutoLoad][SERVER] NetworkSceneManager.LoadScene -> {SceneName}");
        NetworkManager.Singleton.SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
    }
}