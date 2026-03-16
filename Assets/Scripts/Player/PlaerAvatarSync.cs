using Unity.Netcode;
using UnityEngine;

public class PlayerAvatarSync : NetworkBehaviour
{

    private readonly NetworkVariable<int> avatarIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Avatar Visuals (GameObjects with Animator)")]
    [SerializeField] private GameObject[] boyAvatars;  // حطي هنا الـ 3 شخصيات للولاد
    [SerializeField] private GameObject[] girlAvatars; // حطي هنا الـ 3 شخصيات للبنات

    public override void OnNetworkSpawn()
    {
        
        UpdateAvatarVisuals(0, avatarIndex.Value);


        avatarIndex.OnValueChanged += UpdateAvatarVisuals;
    }

    public override void OnNetworkDespawn()
    {
        avatarIndex.OnValueChanged -= UpdateAvatarVisuals;
    }

    private void UpdateAvatarVisuals(int oldIndex, int newIndex)
    {
        
        foreach (var boy in boyAvatars) { if (boy != null) boy.SetActive(false); }
        foreach (var girl in girlAvatars) { if (girl != null) girl.SetActive(false); }

        
        if (newIndex < 3 && boyAvatars.Length > newIndex)
        {
            boyAvatars[newIndex].SetActive(true);
        }
        else if (newIndex >= 3 && girlAvatars.Length > (newIndex - 3))
        {
            girlAvatars[newIndex - 3].SetActive(true);
        }


        var animController = GetComponent<PlayerAnimationController>();
        if (animController != null)
        {
            animController.UpdateAnimatorReference();
        }
    }

  
    public void ServerSetGenderAndRandomize(bool isGirl)
    {
        if (!IsServer) return;

        int randomIdx = isGirl ? Random.Range(3, 6) : Random.Range(0, 3);
        avatarIndex.Value = randomIdx;
    }
}