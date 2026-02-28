using TMPro;
using UnityEngine;

public class RoomJoinCodeUI : MonoBehaviour
{
    [SerializeField] private TMP_Text joinCodeText;

    private void Start()
    {
        var s = GameSessionData.Instance;
        if (s == null || joinCodeText == null) return;

        joinCodeText.text = (s.IsHost && !string.IsNullOrEmpty(s.LastJoinCode))
            ? $"Join Code: {s.LastJoinCode}"
            : "";
    }
}