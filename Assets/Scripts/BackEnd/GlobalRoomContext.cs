using System;
using UnityEngine;
using Unity.Netcode;

public class GlobalRoomContext : MonoBehaviour
{
    public static GlobalRoomContext Instance { get; private set; }

    public string PlayerName { get; private set; }
    public string OrgCode { get; private set; }
    public string JoinCode { get; private set; }

    public int TeamIdHash { get; private set; }
    public int TeamSize { get; private set; }
    public int LayoutIndex { get; private set; } // 0/1/2

    public NetRoomState.Zone CurrentZone { get; private set; } = NetRoomState.Zone.Lobby;

    public NetworkObject LocalPlayerNetObj { get; private set; }

    public event Action OnLocalPlayerReady;
    public event Action<NetRoomState.Zone> OnZoneChanged;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetLobbyData(string playerName, string orgCode, string joinCode, int teamIdHash, int teamSize, int layoutIndex)
    {
        PlayerName = playerName;
        OrgCode = orgCode;
        JoinCode = joinCode;
        TeamIdHash = teamIdHash;
        TeamSize = teamSize;
        LayoutIndex = layoutIndex;
    }

    public void BindLocalPlayer(NetworkObject playerObj)
    {
        LocalPlayerNetObj = playerObj;
        OnLocalPlayerReady?.Invoke();
    }

    public void SetZone(NetRoomState.Zone zone)
    {
        if (CurrentZone == zone) return;
        CurrentZone = zone;
        OnZoneChanged?.Invoke(zone);
    }
}