using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MeetingChatMessageRow : MonoBehaviour
{
    [SerializeField] private TMP_Text senderNameText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Image bubbleImage;

    [SerializeField] private bool localRow = false;
    [SerializeField] private bool showYouForLocal = true;

    public void Bind(string senderName, string message, bool isLocal)
    {
        if (senderNameText != null)
        {
            if (localRow && showYouForLocal)
                senderNameText.text = "You";
            else
                senderNameText.text = senderName;
        }

        if (messageText != null)
            messageText.text = message;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
    }
}