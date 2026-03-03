using System;
using Unity.Netcode;
using UnityEngine;

public class ProximityCallScanner : NetworkBehaviour
{
    [Header("Range")]
    [SerializeField] private float callRange = 2.5f;
    [SerializeField] private float scanInterval = 0.25f;

    [Header("Debug")]
    [SerializeField] private bool canCall;
    [SerializeField] private ulong nearestClientId;
    [SerializeField] private float nearestDist;

    private float _nextScanTime;
    private NetworkObject _nearest;
    private ulong _lastTarget = ulong.MaxValue;
    private bool _hadTarget;

    private LocalVisibilityFilter vis;

    public NetworkObject NearestTarget => _nearest;
    public bool CanCall => canCall;
    public ulong NearestClientId => nearestClientId;
    public float NearestDist => nearestDist;

    public event Action<ulong, NetworkObject> OnTargetChanged;
    public event Action OnTargetCleared;

    public string NearestName =>
        (nearestClientId == 0) ? "" : PlayerIdentity.GetName(nearestClientId);

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { enabled = false; return; }
        enabled = true;

        // ✅ امسك الـ visibility filter مرة واحدة
        vis = FindFirstObjectByType<LocalVisibilityFilter>(FindObjectsInactive.Exclude);

        Debug.Log("[CALLSCAN] Local scanner started ✅ owner=" + OwnerClientId);
    }

    private void Update()
    {
        if (Time.time >= _nextScanTime)
        {
            _nextScanTime = Time.time + scanInterval;
            Scan();
        }

        // للـ PC فقط (اختياري)
        if (CanCall && Input.GetKeyDown(KeyCode.E))
        {
            TryRequestCall();
        }
    }

    public void TryRequestCall()
    {
        var call = GetComponent<CallController>();
        if (call == null || NearestTarget == null) return;

        if (call.State != CallController.CallState.Idle) return;

        if (NearestTarget.OwnerClientId == NetworkManager.Singleton.LocalClientId) return;

        Debug.Log("[CALLSCAN] RequestCall -> " + NearestTarget.OwnerClientId);
        call.RequestCall(NearestTarget.OwnerClientId);
    }

    public void ForceScanNow()
    {
        _nextScanTime = 0f;
        _hadTarget = false;
        _lastTarget = ulong.MaxValue;
        Scan();
    }

    private void Scan()
    {
        canCall = false;
        _nearest = null;
        nearestDist = float.MaxValue;

        // ✅ لو في Meeting، ممنوع private calls
        if (PlayerRoomState.LocalInstance != null &&
            PlayerRoomState.LocalInstance.CurrentContext != null &&
            PlayerRoomState.LocalInstance.CurrentContext.roomType == RoomType.Meeting)
        {
            ClearIfHadTarget();
            return;
        }

        if (NetworkManager.Singleton == null) { ClearIfHadTarget(); return; }

        // لو vis اتعمل Destroy لأي سبب، رجّعه
        if (vis == null)
            vis = FindFirstObjectByType<LocalVisibilityFilter>(FindObjectsInactive.Exclude);

        var me = NetworkObject;
        var myPos = (Vector2)transform.position;

        foreach (var no in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (no == null || no == me) continue;
            if (!no.IsPlayerObject) continue;
            if (no.GetComponent<PlayerRoomState>() == null) continue;
            if (no.OwnerClientId == NetworkManager.Singleton.LocalClientId) continue;

            // ✅ فلترة: ما تسكانش على حد "مش ظاهر" حسب TeamRoom rules
            if (vis != null && !vis.IsVisibleToLocal(no.gameObject))
                continue;

            float d = Vector2.Distance(myPos, (Vector2)no.transform.position);
            if (d <= callRange && d < nearestDist)
            {
                nearestDist = d;
                _nearest = no;
            }
        }

        if (_nearest != null)
        {
            canCall = true;
            nearestClientId = _nearest.OwnerClientId;

            if (!_hadTarget || _lastTarget != nearestClientId)
            {
                Debug.Log($"[CALLSCAN] Target(Player) = {nearestClientId} name={NearestName} dist={nearestDist:F2}");
                _hadTarget = true;
                _lastTarget = nearestClientId;

                OnTargetChanged?.Invoke(nearestClientId, _nearest);
            }
        }
        else
        {
            ClearIfHadTarget();
        }
    }

    private void ClearIfHadTarget()
    {
        canCall = false;
        _nearest = null;
        nearestClientId = 0;
        nearestDist = float.MaxValue;

        if (_hadTarget)
        {
            Debug.Log("[CALLSCAN] No target (out of range)");
            _hadTarget = false;
            _lastTarget = ulong.MaxValue;

            OnTargetCleared?.Invoke();
        }
    }
}