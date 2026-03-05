using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PeoplePanelUI : MonoBehaviour
{
    [Header("List")]
    [SerializeField] private Transform contentRoot;   // Viewport/Content
    [SerializeField] private PeopleRowUI rowPrefab;   // Prefab asset

    [Header("Optional Search")]
    [SerializeField] private string search = "";

    private readonly List<PeopleRowUI> rows = new();

    private void OnEnable()
    {
        if (PresenceService.Instance != null)
            PresenceService.Instance.OnRosterChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (PresenceService.Instance != null)
            PresenceService.Instance.OnRosterChanged -= Refresh;
    }

    public void SetSearch(string s)
    {
        search = (s ?? "").Trim();
        Refresh();
    }

    public void Refresh()
    {
        if (PresenceService.Instance == null || contentRoot == null || rowPrefab == null)
            return;

        // clear old
        for (int i = 0; i < rows.Count; i++)
            if (rows[i] != null) Destroy(rows[i].gameObject);
        rows.Clear();

        ulong localId = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.LocalClientId
            : ulong.MaxValue;

        // copy + sort: me first
        var list = new List<PresenceEntry>(PresenceService.Instance.LocalRoster);
        list.Sort((a, b) =>
        {
            bool aMe = a.ClientId == localId;
            bool bMe = b.ClientId == localId;
            if (aMe && !bMe) return -1;
            if (!aMe && bMe) return 1;
            return string.Compare(a.Name.ToString(), b.Name.ToString(), StringComparison.OrdinalIgnoreCase);
        });

        foreach (var p in list)
        {
            string name = p.Name.ToString();

            if (!string.IsNullOrEmpty(search) &&
                name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            var row = Instantiate(rowPrefab);
            row.transform.SetParent(contentRoot, false); // ✅ ضمان parent + UI scale
            row.Bind(p, localId);
            rows.Add(row);
        }
    }
}