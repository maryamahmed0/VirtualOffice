using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class ClientSceneLogger : MonoBehaviour
{
    private IEnumerator Start()
    {
        var nm = NetworkManager.Singleton;

        while (nm == null)
        {
            Debug.Log("[ClientSceneLogger] Waiting for NetworkManager...");
            yield return null;
            nm = NetworkManager.Singleton;
        }

        Debug.Log($"[ClientSceneLogger] NM name={nm.gameObject.name} scene={nm.gameObject.scene.name} EnableSceneManagement={nm.NetworkConfig.EnableSceneManagement}");

        // ✅ استني لحد ما الـ SceneManager يبقى جاهز
        while (nm.SceneManager == null)
        {
            Debug.Log("[ClientSceneLogger] Waiting for SceneManager...");
            yield return null;
        }

        Debug.Log($"[ClientSceneLogger] SceneManager READY. IsClient={nm.IsClient} IsServer={nm.IsServer}");

        nm.SceneManager.OnSceneEvent += (sceneEvent) =>
        {
            Debug.Log($"[ClientSceneLogger] SceneEvent: {sceneEvent.SceneEventType} scene={sceneEvent.SceneName} clientId={sceneEvent.ClientId}");
        };
    }
}