using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskRowUI : MonoBehaviour
{
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] TMP_Text statusText;
    [SerializeField] TMP_Text assigneeText;
    [SerializeField] Image statusDot;
    [SerializeField] private RectTransform rootRect;

    public void Bind(TasksService.TaskItem task)
    {
        titleText.text = task.title;
        descriptionText.text = task.description;
        statusText.text = task.status;

        var c = ColorForStatus(task.status);
        statusText.color = c;

        if (statusDot != null)
            statusDot.color = c;

        if (assigneeText != null)
        {
            bool showAssignee =
                AuthBridge.IsManager &&
                !string.IsNullOrWhiteSpace(task.assignedToName);

            assigneeText.gameObject.SetActive(showAssignee);

            if (showAssignee)
                assigneeText.text = task.assignedToName;
        }

        RefreshLayout();
    }

    private void RefreshLayout()
    {
        Canvas.ForceUpdateCanvases();
        if (titleText != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(titleText.rectTransform);
        if (rootRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        else if (transform is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    private static Color ColorForStatus(string s)
    {
        return s switch
        {
            "Completed" => new Color(0.35f, 0.85f, 0.55f, 1f), // Green
            "InProgress" => new Color(1.00f, 0.82f, 0.25f, 1f), // Yellow
            "Submitted" => new Color(0.30f, 0.70f, 1.00f, 1f), // Blue
            "Rejected" => new Color(0.95f, 0.35f, 0.35f, 1f), // Red
            _ => new Color(0.75f, 0.75f, 0.75f, 1f), // Gray
        };
    }
}