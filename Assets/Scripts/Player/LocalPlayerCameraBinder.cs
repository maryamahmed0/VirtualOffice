using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Cinemachine;

public class LocalPlayerCameraBinder : NetworkBehaviour
{
    private bool _subscribed;
    private NetRoomState _room;

    private Vector3 _lastPos;
    private bool _watchingTeleport;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        Debug.Log($"[CAM] Local owner spawned. NOID={NetworkObjectId} OwnerClientId={OwnerClientId}");

        _lastPos = transform.position;

        StartCoroutine(BindAfterDelay());

        if (!_watchingTeleport)
            StartCoroutine(WatchTeleportThenRebind());

        if (!_subscribed)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            _subscribed = true;
        }

        _room = GetComponent<NetRoomState>();
        if (_room != null)
            _room.CurrentZone.OnValueChanged += OnZoneChanged;
    }

    public override void OnNetworkDespawn()
    {
        Cleanup();
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        if (_room != null)
            _room.CurrentZone.OnValueChanged -= OnZoneChanged;

        if (_subscribed)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _subscribed = false;
        }

        _watchingTeleport = false;
    }

    private void OnZoneChanged(int oldV, int newV)
    {
        if (!IsOwner) return;
        Debug.Log($"[CAM] Zone changed {oldV}->{newV}, rebinding...");
        StartCoroutine(BindAfterDelay());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsOwner) return;
        Debug.Log($"[CAM] Scene loaded: {scene.name}, rebinding...");
        StartCoroutine(BindAfterDelay());

     
        if (!_watchingTeleport)
            StartCoroutine(WatchTeleportThenRebind());
    }

    private IEnumerator BindAfterDelay()
    {
        yield return null;
        yield return null;
        yield return null;
        TryBindToThisOwner();
    }

  
    private IEnumerator WatchTeleportThenRebind()
    {
        _watchingTeleport = true;

        int frames = 120;
        float threshold = 0.5f; 

        while (frames-- > 0 && IsOwner)
        {
            yield return null;

            var p = transform.position;
            if (Vector3.Distance(p, _lastPos) > threshold)
            {
                Debug.Log($"[CAM] Detected teleport/move burst -> Rebind. pos={p}");
                TryBindToThisOwner();
                _lastPos = p;
                break;
            }

            _lastPos = p;
        }

        _watchingTeleport = false;
    }

    private void TryBindToThisOwner()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogWarning("[CAM] NetworkManager.Singleton is NULL");
            return;
        }

        var localPlayerObj = nm.SpawnManager.GetLocalPlayerObject();
        if (localPlayerObj == null)
        {
            Debug.LogWarning("[CAM] GetLocalPlayerObject() is NULL (not spawned yet?)");
            return;
        }

        Transform followTarget = localPlayerObj.transform;

        var mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("[CAM] Camera.main is NULL");
            return;
        }

        var brain = mainCam.GetComponent<CinemachineBrain>();
        if (brain == null)
        {
            Debug.LogWarning("[CAM] CinemachineBrain not found on Main Camera");
            return;
        }


        CinemachineVirtualCamera vcam = null;
        var vcamGO = GameObject.Find("Virtual Camera");
        if (vcamGO != null) vcam = vcamGO.GetComponent<CinemachineVirtualCamera>();

        if (vcam == null)
            vcam = FindFirstObjectByType<CinemachineVirtualCamera>(FindObjectsInactive.Exclude);

        if (vcam == null)
        {
            Debug.LogWarning("[CAM] No CinemachineVirtualCamera found.");
            return;
        }

        vcam.Follow = followTarget;
        vcam.LookAt = followTarget;
        vcam.PreviousStateIsValid = false; 

        Debug.Log($"[CAM] Bound vcam to LOCAL PLAYER. LocalNOID={localPlayerObj.NetworkObjectId} pos={followTarget.position}");
    }

    public void ForceRebind()
    {
        StartCoroutine(BindAfterDelay());
    }
}