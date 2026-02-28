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

    public NetworkObject NearestTarget => _nearest;
    public bool CanCall => canCall;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { enabled = false; return; }
        enabled = true;
        Debug.Log("[CALLSCAN] Local scanner started ✅ owner=" + OwnerClientId);
    }

    private void Update()
    {
        if (Time.time >= _nextScanTime)
        {
            _nextScanTime = Time.time + scanInterval;
            Scan();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log($"[CALLSCAN] E pressed. canCall={CanCall} target={(NearestTarget ? NearestTarget.OwnerClientId.ToString() : "null")} dist={nearestDist:F2}");
        }

        if (CanCall && Input.GetKeyDown(KeyCode.E))
        {
            var call = GetComponent<CallController>();
            if (call == null || NearestTarget == null) return;

            if (call.State != CallController.CallState.Idle) return;

            if (NearestTarget.OwnerClientId == NetworkManager.Singleton.LocalClientId) return;

            Debug.Log("[CALLSCAN] RequestCall -> " + NearestTarget.OwnerClientId);
            call.RequestCall(NearestTarget.OwnerClientId);
        }
    }

    private void Scan()
    {
        canCall = false;
        _nearest = null;
        nearestDist = float.MaxValue;

        if (PlayerRoomState.LocalInstance != null &&
            PlayerRoomState.LocalInstance.CurrentContext != null &&
            PlayerRoomState.LocalInstance.CurrentContext.roomType == RoomType.Meeting)
            return;

        if (NetworkManager.Singleton == null) return;

        var me = NetworkObject;
        var myPos = (Vector2)transform.position;

        foreach (var no in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (no == null || no == me) continue;

            if (!no.IsPlayerObject) continue;

            if (no.GetComponent<PlayerRoomState>() == null) continue;

            if (no.OwnerClientId == NetworkManager.Singleton.LocalClientId) continue;

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
                Debug.Log($"[CALLSCAN] Target(Player) = {nearestClientId} name={_nearest.name} dist={nearestDist:F2}");
                _hadTarget = true;
                _lastTarget = nearestClientId;
            }
        }
        else if (_hadTarget)
        {
            Debug.Log("[CALLSCAN] No target (out of range)");
            _hadTarget = false;
            _lastTarget = ulong.MaxValue;
        }
    }
}