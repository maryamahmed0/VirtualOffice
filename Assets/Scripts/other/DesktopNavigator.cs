using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DesktopNavigator : MonoBehaviour
{
    public enum ViewId { None, People, Tasks }

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
        if (peopleBtn) peopleBtn.onClick.AddListener(() => NavigateTo(ViewId.People));
        if (tasksBtn) tasksBtn.onClick.AddListener(() => NavigateTo(ViewId.Tasks));
        if (backBtn) backBtn.onClick.AddListener(Back);
        if (closeBtn) closeBtn.onClick.AddListener(CloseAll);

        Show(ViewId.None, pushHistory: false);
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

        // Back يظهر لو مش None أو فيه history
        if (backBtn) backBtn.gameObject.SetActive(v != ViewId.None || history.Count > 0);
    }

    private void CloseAll()
    {
        // يقفل الـUI كله
        gameObject.SetActive(false);

        // reset stack
        history.Clear();
        current = ViewId.None;
    }
}