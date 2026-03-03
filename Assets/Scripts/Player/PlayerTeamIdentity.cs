using Unity.Netcode;
using UnityEngine;

public class PlayerTeamIdentity : NetworkBehaviour
{
    // نخزن TeamId كـ int hash عشان نتفادى FixedString delta bugs
    public NetworkVariable<int> TeamIdHash =
        new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> TeamSize =
        new NetworkVariable<int>(8, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // 0 Small, 1 Med, 2 Large
    public NetworkVariable<int> LayoutIndex =
        new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        string teamStr = GameSessionData.Instance != null ? GameSessionData.Instance.TeamId : PlayerPrefs.GetString("TEAM_ID", "TECH");
        int teamSize = GameSessionData.Instance != null ? GameSessionData.Instance.TeamSize : PlayerPrefs.GetInt("TEAM_SIZE", 8);

        teamStr = string.IsNullOrWhiteSpace(teamStr) ? "TECH" : teamStr.Trim().ToUpperInvariant();
        teamSize = Mathf.Clamp(teamSize, 1, 50);

        int teamHash = Animator.StringToHash(teamStr);

        SubmitTeamServerRpc(teamHash, teamSize);

        Debug.Log($"[TEAM][LOCAL] team='{teamStr}' hash={teamHash} size={teamSize}");
    }

    [ServerRpc(RequireOwnership = true)]
    private void SubmitTeamServerRpc(int teamHash, int teamSize)
    {
        TeamIdHash.Value = teamHash;
        TeamSize.Value = Mathf.Clamp(teamSize, 1, 50);

        int idx = (TeamSize.Value <= 8) ? 0 : (TeamSize.Value <= 12 ? 1 : 2);
        LayoutIndex.Value = idx;

        Debug.Log($"[TEAM] client {OwnerClientId} teamHash={teamHash} size={TeamSize.Value} layout={idx}");
    }
}