using UnityEngine;

public class MobileControlsEnabler : MonoBehaviour
{
    [SerializeField] private GameObject mobileControlsRoot;

    private void Awake()
    {
        if (mobileControlsRoot == null)
            mobileControlsRoot = gameObject;

        mobileControlsRoot.SetActive(Application.isMobilePlatform);
    }
}