using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerReactionBubble : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject bubbleRoot;
    [SerializeField] private Image reactionIcon;

    [Header("Sprites")]
    [SerializeField] private Sprite heartSprite;
    [SerializeField] private Sprite likeSprite;
    [SerializeField] private Sprite laughSprite;
    [SerializeField] private Sprite sadSprite;
    [SerializeField] private Sprite clapSprite;
    [SerializeField] private Sprite partySprite;

    [Header("Behavior")]
    [SerializeField] private float visibleDuration = 2.25f;

    private Coroutine hideRoutine;

    private void Awake()
    {
        HideImmediate();
    }

    public void ShowReaction(MeetingReactionType reactionType)
    {
        if (bubbleRoot == null || reactionIcon == null)
            return;

        Sprite spriteToShow = GetSpriteForReaction(reactionType);
        if (spriteToShow == null)
        {
            Debug.LogWarning($"[REACTION BUBBLE] No sprite assigned for reaction {reactionType}");
            return;
        }

        reactionIcon.sprite = spriteToShow;
        reactionIcon.enabled = true;
        bubbleRoot.SetActive(true);

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    public void HideImmediate()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        if (reactionIcon != null)
        {
            reactionIcon.sprite = null;
            reactionIcon.enabled = false;
        }

        if (bubbleRoot != null)
            bubbleRoot.SetActive(false);
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(visibleDuration);

        if (reactionIcon != null)
        {
            reactionIcon.sprite = null;
            reactionIcon.enabled = false;
        }

        if (bubbleRoot != null)
            bubbleRoot.SetActive(false);

        hideRoutine = null;
    }

    private Sprite GetSpriteForReaction(MeetingReactionType reactionType)
    {
        switch (reactionType)
        {
            case MeetingReactionType.Heart: return heartSprite;
            case MeetingReactionType.Like: return likeSprite;
            case MeetingReactionType.Laugh: return laughSprite;
            case MeetingReactionType.Sad: return sadSprite;
            case MeetingReactionType.Clap: return clapSprite;
            case MeetingReactionType.Party: return partySprite;
            default: return null;
        }
    }
}