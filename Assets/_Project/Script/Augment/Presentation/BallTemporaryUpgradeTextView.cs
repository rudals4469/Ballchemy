using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshPro))]
public sealed class
    BallTemporaryUpgradeTextView :
        MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private TextMeshPro textRenderer;

    private Sequence activeSequence;

    private Action<
        BallTemporaryUpgradeTextView
    > completedCallback;

    private Vector3 originalScale;
    private Color originalColor;

    private void Awake()
    {
        FindReferences();
        CacheOriginalValues();
    }

    private void OnValidate()
    {
        FindReferences();
    }

    private void FindReferences()
    {
        if (textRenderer == null)
        {
            textRenderer =
                GetComponent<TextMeshPro>();
        }
    }

    private void CacheOriginalValues()
    {
        originalScale =
            transform.localScale;

        originalColor =
            textRenderer != null
                ? textRenderer.color
                : Color.white;
    }

    public void Play(
        Vector3 worldPosition,
        Vector2 startOffset,
        float riseDistance,
        float duration,
        string displayText,
        Action<
            BallTemporaryUpgradeTextView
        > onCompleted)
    {
        StopAnimation();

        duration =
            Mathf.Max(
                duration,
                0.01f
            );

        riseDistance =
            Mathf.Max(
                riseDistance,
                0f
            );

        completedCallback =
            onCompleted;

        transform.position =
            worldPosition +
            (Vector3)startOffset;

        transform.localScale =
            originalScale;

        if (textRenderer != null)
        {
            textRenderer.text =
                string.IsNullOrWhiteSpace(
                    displayText
                )
                    ? "UP"
                    : displayText;

            Color startColor =
                originalColor;

            startColor.a =
                originalColor.a;

            textRenderer.color =
                startColor;

            textRenderer.enabled =
                true;
        }

        gameObject.SetActive(
            true
        );

        Vector3 targetPosition =
            transform.position +
            Vector3.up *
            riseDistance;

        activeSequence =
            DOTween.Sequence();

        activeSequence.SetLink(
            gameObject,
            LinkBehaviour.KillOnDisable
        );

        activeSequence.Join(
            transform.DOMove(
                targetPosition,
                duration
            )
            .SetEase(
                Ease.OutCubic
            )
        );

        if (textRenderer != null)
        {
            activeSequence.Join(
                textRenderer.DOFade(
                    0f,
                    duration
                )
                .SetEase(
                    Ease.InQuad
                )
            );
        }

        activeSequence.OnComplete(
            CompletePlayback
        );
    }

    public void StopAnimation()
    {
        if (activeSequence != null &&
            activeSequence.IsActive())
        {
            activeSequence.Kill(
                false
            );
        }

        activeSequence = null;
        completedCallback = null;

        RestoreOriginalValues();
    }

    private void CompletePlayback()
    {
        activeSequence = null;

        Action<
            BallTemporaryUpgradeTextView
        > callback =
            completedCallback;

        completedCallback = null;

        RestoreOriginalValues();

        gameObject.SetActive(
            false
        );

        callback?.Invoke(
            this
        );
    }

    private void RestoreOriginalValues()
    {
        transform.localScale =
            originalScale;

        if (textRenderer != null)
        {
            textRenderer.color =
                originalColor;

            textRenderer.enabled =
                true;
        }
    }

    private void OnDisable()
    {
        if (activeSequence != null &&
            activeSequence.IsActive())
        {
            activeSequence.Kill(
                false
            );
        }

        activeSequence = null;
    }

    private void OnDestroy()
    {
        if (activeSequence != null &&
            activeSequence.IsActive())
        {
            activeSequence.Kill(
                false
            );
        }

        activeSequence = null;
        completedCallback = null;
    }
}