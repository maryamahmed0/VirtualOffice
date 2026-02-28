using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class OfficeSpawnManager : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;

    private readonly Dictionary<ulong, int> assigned = new();
    private readonly List<int> free = new();
    private readonly HashSet<ulong> spawnedOnce = new();

    private void Awake()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            // كل الـ children spawn points
            spawnPoints = GetComponentsInChildren<Transform>(true)
                .Where(t => t != transform)
                .ToArray();
        }

        free.Clear();
        for (int i = 0; i < spawnPoints.Length; i++)
            free.Add(i);
    }

    private void OnEnable()
    {
        StartCoroutine(Hook());
    }

    private IEnumerator Hook()
    {
        while (NetworkManager.Singleton == null) yield return null;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        // ✅ لو الهوست موجود بالفعل (start host)، خليه ياخد spawn مرة واحدة
        if (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.IsHost)
        {
            OnClientConnected(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        StartCoroutine(AssignSpawnWhenReady(clientId));
    }

    private IEnumerator AssignSpawnWhenReady(ulong clientId)
    {
        // استني شوية لحد ما PlayerObject يبقى اتعمله spawn
        yield return null;
        yield return null;

        if (spawnedOnce.Contains(clientId))
            yield break;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
            yield break;

        var playerObj = client.PlayerObject;
        if (playerObj == null)
            yield break;

        Vector3 pos = PickSpawnPos(clientId);

        // ✅ حرّكيه على السيرفر بس (ده اللي هيعمل sync للكل)
        var rb = playerObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = (Vector2)pos;
        }
        else
        {
            playerObj.transform.position = pos;
        }

        spawnedOnce.Add(clientId);
        Debug.Log($"[OFFICE SPAWN] client {clientId} -> {pos}");
    }

    private Vector3 PickSpawnPos(ulong clientId)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return Vector3.zero;

        if (assigned.TryGetValue(clientId, out int idx) &&
            idx >= 0 && idx < spawnPoints.Length)
        {
            return spawnPoints[idx].position;
        }

        int pickIndex;
        if (free.Count > 0)
        {
            int pick = Random.Range(0, free.Count);
            pickIndex = free[pick];
            free.RemoveAt(pick);
        }
        else
        {
            pickIndex = Random.Range(0, spawnPoints.Length);
        }

        assigned[clientId] = pickIndex;
        return spawnPoints[pickIndex].position;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        spawnedOnce.Remove(clientId);

        if (assigned.TryGetValue(clientId, out int idx))
        {
            assigned.Remove(clientId);
            if (!free.Contains(idx)) free.Add(idx);
        }
    }
}