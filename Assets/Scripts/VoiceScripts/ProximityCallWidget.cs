using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ProximityCallWidget : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform root;
    [SerializeField] private GameObject visual;
    [SerializeField] private Button callButton;

    [Header("Position")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private Vector2 screenOffset = new Vector2(0, 0);

    private ProximityCallScanner scanner;
    private CallController call;
    private NetworkObject targetNO;
    private bool hooked;

    private void Awake()
    {
        if (root == null) root = (RectTransform)transform;

        if (visual != null) visual.SetActive(false);

        if (callButton)
            callButton.onClick.AddListener(() => scanner?.TryRequestCall());
    }

    private void Update()
    {
        if (!hooked)
            TryHookLocal();

        if (visual == null) return;

        if (call != null && call.State != CallController.CallState.Idle)
        {
            if (visual.activeSelf) visual.SetActive(false);
            return;
        }

        if (!visual.activeSelf) return;
        if (targetNO == null) { visual.SetActive(false); return; }

        var cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = targetNO.transform.position + worldOffset;
        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0)
        {
            visual.SetActive(false);
            return;
        }

        root.position = screenPos + (Vector3)screenOffset;
    }

    private void TryHookLocal()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        var lp = nm.LocalClient?.PlayerObject;
        if (lp == null) return;

        scanner = lp.GetComponent<ProximityCallScanner>();
        call = lp.GetComponent<CallController>();

        if (scanner == null || call == null) return;

        scanner.OnTargetChanged += OnTargetChanged;
        scanner.OnTargetCleared += OnTargetCleared;

        call.OnCallEnded += OnLocalCallEnded;
        call.OnCallStarted += OnLocalCallStarted;

        hooked = true;
        Debug.Log("[ProximityCallWidget] Hooked local scanner+call ✅");
    }
    private void OnLocalCallStarted(string _)
    {
        if (visual != null && visual.activeSelf)
            visual.SetActive(false);
    }

    private void OnLocalCallEnded()
    {

        scanner?.ForceScanNow();
    }
    private void OnDestroy()
    {
        if (scanner != null)
        {
            scanner.OnTargetChanged -= OnTargetChanged;
            scanner.OnTargetCleared -= OnTargetCleared;
        }
        if (call != null)
        {
            call.OnCallEnded -= OnLocalCallEnded;
            call.OnCallStarted -= OnLocalCallStarted;
        }
    }

    private void OnTargetChanged(ulong id, NetworkObject no)
    {

        if (call != null && call.State != CallController.CallState.Idle)
            return;

        targetNO = no;

        if (visual != null && !visual.activeSelf)
            visual.SetActive(true);
    }

    private void OnTargetCleared()
    {
        targetNO = null;

        if (visual != null && visual.activeSelf)
            visual.SetActive(false);
    }
}