using System.Collections.Generic;
using UnityEngine;

public class TasksPanelUI : MonoBehaviour
{
    [Header("List")]
    [SerializeField] private Transform contentRoot;   // Viewport/Content
    [SerializeField] private TaskRowUI rowPrefab;

    private readonly List<TaskRowUI> rows = new();

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (contentRoot == null || rowPrefab == null) return;

        // clear
        for (int i = 0; i < rows.Count; i++)
            if (rows[i] != null) Destroy(rows[i].gameObject);
        rows.Clear();

        //  Mock tasks (later: backend)
        var tasks = BuildMockTasks();

        foreach (var t in tasks)
        {
            var row = Instantiate(rowPrefab);
            row.transform.SetParent(contentRoot, false);
            row.Bind(t.title, t.status);
            rows.Add(row);
        }
    }

    private List<(string title, string status)> BuildMockTasks()
    {
        return new List<(string, string)>
        {
            ("Finish UI People Panel", "Doing"),
            ("Fix TeamRoom Visibility", "Todo"),
            ("Test Vivox on Android", "Done"),
            ("Prepare meeting summary flow", "Todo"),
            ("Test Scrolling in task View","Doing"), 
            ("Prepare meeting summary flow and validate mobile scrolling behavior in tasks panel","Doing"),

        };
    }
}