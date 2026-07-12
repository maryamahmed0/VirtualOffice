using System.Collections.Generic;
using UnityEngine;

public class TasksPanelUI : MonoBehaviour
{
    [Header("List")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private TaskRowUI rowPrefab;

    private readonly List<TaskRowUI> _rows = new();
    private TasksService _service;

    private void Awake()
    {
        _service = gameObject.AddComponent<TasksService>();
    }

    private void OnEnable() => Refresh();

    public void Refresh()
    {
        if (contentRoot == null || rowPrefab == null) return;

        if (!AuthBridge.IsReady) { LoadMock(); return; }

        if (AuthBridge.IsManager)
        {
            Debug.Log("[Tasks] Manager view — loading all tasks");
            StartCoroutine(_service.GetManagerTasks(OnTasksLoaded, OnFail));
        }
        else
        {
            Debug.Log("[Tasks] Employee view — loading my tasks");
            StartCoroutine(_service.GetMyTasks(OnTasksLoaded, OnFail));
        }
    }

    void OnTasksLoaded(TasksService.TaskItem[] tasks)
    {
        ClearRows();
        if (tasks.Length == 0)
        {
            Debug.Log("[Tasks] No tasks found");
            return;
        }
        foreach (var t in tasks)
            SpawnRow(t);

        Debug.Log($"[Tasks] Loaded {tasks.Length} tasks");
    }

    void OnFail(string error)
    {
        Debug.LogError("[Tasks] " + error);
        LoadMock();
    }

    void LoadMock()
    {
        ClearRows();

        var mock = new[]
        {
        new TasksService.TaskItem
        {
            title = "Finish UI People Panel",
            description = "Finish the whole people list UI and connect it to API.",
            status = "InProgress",
            assignedToName = "Maryam"
        },

        new TasksService.TaskItem
        {
            title = "Fix TeamRoom Visibility",
            description = "Players should only see teammates inside the team room.",
            status = "Pending",
            assignedToName = "Maryam"
        },

        new TasksService.TaskItem
        {
            title = "Test Vivox on Android",
            description = "Verify microphone permissions and voice communication.",
            status = "Completed",
            assignedToName = "Ahmed"
        }
    };

        foreach (var task in mock)
            SpawnRow(task);
    }

    void SpawnRow(TasksService.TaskItem task)
    {
        var row = Instantiate(rowPrefab, contentRoot);

        row.Bind(task);

        _rows.Add(row);
    }

    void ClearRows()
    {
        foreach (var r in _rows)
            if (r != null) Destroy(r.gameObject);
        _rows.Clear();
    }
}