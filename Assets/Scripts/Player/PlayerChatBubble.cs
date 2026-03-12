using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerChatBubble : NetworkBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject bubbleRoot;
    [SerializeField] private RectTransform bubbleContainer;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Canvas worldCanvas;

    [Header("Behavior")]
    [SerializeField] private int maxChars = 120;
    [SerializeField] private bool rotateTowardCamera = false;
    [SerializeField] private float shortMessageDuration = 2.5f;
    [SerializeField] private float mediumMessageDuration = 4f;
    [SerializeField] private float longMessageDuration = 5.5f;

    private Coroutine hideRoutine;
    private Camera cachedCamera;

    private void Awake()
    {
        HideImmediate();
    }
   
    private void LateUpdate()
    {
        if (!rotateTowardCamera || worldCanvas == null || bubbleRoot == null || !bubbleRoot.activeSelf)
            return;

        if (cachedCamera == null)
            cachedCamera = Camera.main;

        if (cachedCamera == null)
            return;

        worldCanvas.transform.forward = cachedCamera.transform.forward;
    }

    public void ShowMessage(string message)
    {
        Debug.Log($"[PLAYER BUBBLE] ShowMessage called with: {message}");

        if (string.IsNullOrWhiteSpace(message))
        {
            Debug.LogWarning("[PLAYER BUBBLE] Empty message");
            return;
        }

        if (bubbleRoot == null || messageText == null)
        {
            Debug.LogWarning($"[PLAYER BUBBLE] Missing refs bubbleRoot={(bubbleRoot != null)} messageText={(messageText != null)}");
            return;
        }

        string finalMessage = SanitizeMessage(message);

        messageText.text = finalMessage;
        bubbleRoot.SetActive(true);

        Debug.Log($"[PLAYER BUBBLE] bubbleRoot active={bubbleRoot.activeSelf} text={finalMessage}");

        RefreshLayout();

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(HideAfterDelay(GetVisibleDuration(finalMessage)));
    }

    public void HideImmediate()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        if (messageText != null)
            messageText.text = string.Empty;

        if (bubbleRoot != null)
            bubbleRoot.SetActive(false);
    }

    private void RefreshLayout()
    {
        Canvas.ForceUpdateCanvases();

        if (messageText != null)
        {
            RectTransform textRect = messageText.rectTransform;
            if (textRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);
        }

        if (bubbleContainer != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleContainer);
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (messageText != null)
            messageText.text = string.Empty;

        if (bubbleRoot != null)
            bubbleRoot.SetActive(false);

        hideRoutine = null;
    }

    private float GetVisibleDuration(string message)
    {
        int len = message.Length;

        if (len <= 20) return shortMessageDuration;
        if (len <= 60) return mediumMessageDuration;
        return longMessageDuration;
    }

    private string SanitizeMessage(string message)
    {
        string result = message.Trim();
        result = result.Replace("\r\n", "\n").Replace("\r", "\n");

        while (result.Contains("\n\n\n"))
            result = result.Replace("\n\n\n", "\n\n");

        if (result.Length > maxChars)
            result = result.Substring(0, maxChars).TrimEnd() + "...";

        return result;
    }
}