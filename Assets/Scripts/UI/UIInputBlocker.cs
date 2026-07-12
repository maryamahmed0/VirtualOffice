using System.Collections.Generic;
using UnityEngine;

public static class UIInputBlocker
{
    private static readonly HashSet<object> _blockers = new();

    public static bool BlockGameplayInput => _blockers.Count > 0;

    public static void Acquire(object owner)
    {
        if (owner == null) return;
        _blockers.Add(owner);
        Debug.Log($"[UIBLOCK] Acquire {owner.GetType().Name} count={_blockers.Count}");
    }

    public static void Release(object owner)
    {
        if (owner == null) return;
        _blockers.Remove(owner);
        Debug.Log($"[UIBLOCK] Release {owner.GetType().Name} count={_blockers.Count}");
    }

    public static void ClearAll()
    {
        _blockers.Clear();
        Debug.Log("[UIBLOCK] ClearAll");
    }
}