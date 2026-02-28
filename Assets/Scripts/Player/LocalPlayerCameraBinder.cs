using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Cinemachine;

public class LocalPlayerCameraBinder : NetworkBehaviour
{
    private bool _subscribed;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        Debug.Log("[CAM] Local player spawned, trying initial bind...");
        TryBindCamera();

        if (!_subscribed)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            _subscribed = true;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (_subscribed)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _subscribed = false;
        }
    }

    private void OnDestroy()
    {
        if (_subscribed)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _subscribed = false;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsOwner) return;

        Debug.Log($"[CAM] Scene loaded: {scene.name}, rebinding camera...");
        StartCoroutine(BindNextFrame());
    }

    private IEnumerator BindNextFrame()
    {
        // استني فريم/فريمين عشان كل حاجة في المشهد تبقى اتجهزت
        yield return null;
        yield return null;
        TryBindCamera();
    }

    private void TryBindCamera()
    {
        var vcam = FindFirstObjectByType<CinemachineVirtualCamera>(FindObjectsInactive.Exclude);
        if (vcam == null)
        {
            Debug.LogWarning("[CAM] No CinemachineVirtualCamera found in loaded scene.");
            return;
        }

        vcam.Follow = transform;
        // في 2D غالبًا مش محتاجة LookAt، لكن ينفع تسيبيه
        vcam.LookAt = transform;

        Debug.Log($"[CAM] Bound vcam '{vcam.name}' to local player.");
    }
}