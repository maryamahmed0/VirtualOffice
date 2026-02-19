using Unity.Netcode;
using UnityEngine;

public class PlayerVoiceState : NetworkBehaviour
{
    // Everyone reads, server writes (أكثر أمان)
    public NetworkVariable<bool> IsMicMuted = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [ServerRpc(RequireOwnership = true)]
    public void SetMutedServerRpc(bool muted)
    {
        IsMicMuted.Value = muted;
    }
}