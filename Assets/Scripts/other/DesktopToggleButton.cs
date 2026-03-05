using UnityEngine;
using UnityEngine.UI;

public class DesktopToggleButton : MonoBehaviour
{
    [SerializeField] private GameObject desktopUIRoot;
    [SerializeField] private Button button;

    private void Awake()
    {
        if (!button) button = GetComponent<Button>();
        if (button) button.onClick.AddListener(Toggle);
    }

    private void Toggle()
    {
        if (!desktopUIRoot) return;
        desktopUIRoot.SetActive(!desktopUIRoot.activeSelf);
    }
}