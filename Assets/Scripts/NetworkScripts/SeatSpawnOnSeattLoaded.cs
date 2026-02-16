using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SeatSpawnOnSceneLoaded : MonoBehaviour
{
    [SerializeField] private string meetingSceneName = "MeetingRoom";
    [SerializeField] private NetworkObject playerPrefab;
    private bool subscribed;

    private void OnEnable()
    {
        StartCoroutine(TrySubscribe());
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private IEnumerator TrySubscribe()
    {
        // استنى لحد ما NetworkManager.Singleton يبقى موجود
        while (NetworkManager.Singleton == null)
            yield return null;

        // لازم Scene Management يكون مفعّل عشان SceneManager يبقى شغال
        if (!NetworkManager.Singleton.NetworkConfig.EnableSceneManagement)
        {
            Debug.LogError("[SeatSpawn] Enable Scene Management is OFF in NetworkManager!");
            yield break;
        }

        // SceneManager أحيانًا يتأخر فريم
        while (NetworkManager.Singleton.SceneManager == null)
            yield return null;

        if (subscribed) yield break;

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
        subscribed = true;

        Debug.Log("[SeatSpawn] Subscribed to OnLoadEventCompleted");
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;

        subscribed = false;
    }

    private void OnLoadEventCompleted(string sceneName, LoadSceneMode mode,
        List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        Debug.Log($"[SeatSpawn] OnLoadEventCompleted fired! sceneName={sceneName} IsServer={NetworkManager.Singleton.IsServer}");
        if (!NetworkManager.Singleton.IsServer) return;
        if (sceneName != meetingSceneName) return;

        StartCoroutine(AssignSeatsRoutine());
    }

    private IEnumerator AssignSeatsRoutine()
    {
        yield return null;
        yield return null;

        var seatManager = FindFirstObjectByType<SeatManager>();
        if (seatManager == null)
        {
            Debug.LogError("[SeatSpawn] SeatManager not found in MeetingScene!");
            yield break;
        }

        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = kv.Key;
            var client = kv.Value;

            // خد مكان كرسي لأول عميل
            if (!seatManager.TryAssignRandomSeat(clientId, out Vector3 pos))
                pos = Vector3.zero;

            var playerObj = client.PlayerObject;

            // ✅ لو مفيش PlayerObject.. اعملي Spawn
            if (playerObj == null)
            {
                if (playerPrefab == null)
                {
                    Debug.LogError("[SeatSpawn] playerPrefab is NULL! Assign it in Inspector.");
                    continue;
                }

                var newPlayer = Instantiate(playerPrefab, pos, Quaternion.identity);
                newPlayer.SpawnAsPlayerObject(clientId, true);
                playerObj = newPlayer;
            }

            // ✅ تأكيد وضعه على الكرسي
            var rb = playerObj.GetComponent<Rigidbody2D>();
            if (rb != null) rb.position = (Vector2)pos;
            else playerObj.transform.position = pos;
        }
    }
}