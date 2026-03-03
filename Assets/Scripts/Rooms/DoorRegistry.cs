using System.Collections.Generic;
using UnityEngine;

public static class DoorRegistry
{
    static readonly Dictionary<string, DoorTrigger> map = new();

    public static void Register(DoorTrigger door)
    {
        if (door == null) return;
        if (string.IsNullOrWhiteSpace(door.doorId)) return;
        map[door.doorId] = door;
    }

    public static void Unregister(DoorTrigger door)
    {
        if (door == null) return;
        if (string.IsNullOrWhiteSpace(door.doorId)) return;

        if (map.TryGetValue(door.doorId, out var d) && d == door)
            map.Remove(door.doorId);
    }

    public static bool TryGet(string id, out DoorTrigger door) => map.TryGetValue(id, out door);
}