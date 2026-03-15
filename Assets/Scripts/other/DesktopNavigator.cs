using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DesktopNavigator : MonoBehaviour
{
    public enum ViewId { None, People, Tasks }

    [Header("Root")]
    [SerializeField] private GameObject desktopRoot;

    [Header("Views")]
    [SerializeField] private GameObject peopleView;
    [SerializeField] private GameObject tasksView;

    [Header("Buttons")]
    [SerializeField] private Button peopleBtn;
    [SerializeField] private Button tasksBtn;
    [SerializeField] private Button backBtn;
    [SerializeField] private Button closeBtn;

    private readonly Stack<ViewId> history = new();
    private ViewId current = ViewId.None;

    private void Awake()
    {
        if (desktopRoot == null)
            desktopRoot = gameObject;

        if (peopleBtn) peopleBtn.onClick.AddListener(() => NavigateTo(ViewId.People));
        if (tasksBtn) tasksBtn.onClick.AddListener(() => NavigateTo(ViewId.Tasks));
        if (backBtn) backBtn.onClick.AddListener(Back);

        Show(ViewId.None, pushHistory: false);
    }

    private void OnEnable()
    {
        UIInputBlocker.Acquire(this);
        history.Clear();
        Show(ViewId.None, pushHistory: false);
    }

    private void OnDisable()
    {
        UIInputBlocker.Release(this);
    }

    private void NavigateTo(ViewId v)
    {
        if (v == current) return;
        Show(v, pushHistory: true);
    }

    private void Back()
    {
        if (history.Count == 0)
        {
            Show(ViewId.None, pushHistory: false);
            return;
        }

        var v = history.Pop();
        Show(v, pushHistory: false);
    }

    private void Show(ViewId v, bool pushHistory)
    {
        if (pushHistory)
            history.Push(current);

        current = v;

        if (peopleView) peopleView.SetActive(v == ViewId.People);
        if (tasksView) tasksView.SetActive(v == ViewId.Tasks);

        if (backBtn) backBtn.gameObject.SetActive(v != ViewId.None || history.Count > 0);
    }

    public void CloseFromButton()
    {
        Debug.Log("[DESKTOP NAV] CloseFromButton");

        UIInputBlocker.Release(this);

        history.Clear();
        Show(ViewId.None, pushHistory: false);

        if (desktopRoot != null)
            desktopRoot.SetActive(false);
    }
}