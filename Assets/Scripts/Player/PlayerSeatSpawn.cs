//using UnityEngine;
//using Unity.Netcode;

//public class PlayerSeatSpawn : NetworkBehaviour
//{
//    private SeatManager seatManager;

//    private void Start()
//    {
//        seatManager = FindFirstObjectByType<SeatManager>();
//    }

//    public override void OnNetworkSpawn()
//    {
//        Debug.Log($"Player {OwnerClientId} IsServer={IsServer} before set pos={transform.position}");

//        if (!IsServer) return;

//        var seatManager = FindFirstObjectByType<SeatManager>();
//        if (seatManager != null && seatManager.TryAssignRandomSeat(OwnerClientId, out Vector3 pos))
//        {
//            var rb = GetComponent<Rigidbody2D>();
//            if (rb != null)
//            {
//                rb.velocity = Vector2.zero;
//                rb.angularVelocity = 0f;
//                rb.position = (Vector2)pos;
//            }
//            else
//            {
//                transform.position = pos;
//            }
//        }
//    }
//}
