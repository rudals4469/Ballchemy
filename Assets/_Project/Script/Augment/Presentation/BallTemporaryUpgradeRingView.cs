using System;
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class
    BallTemporaryUpgradeRingView :
        MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private SpriteRenderer ringRenderer;

    private Sequence activeSequence;

    private Action<
        BallTemporaryUpgradeRingView
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
        if (ringRenderer == null)
        {
            ringRenderer =
                GetComponent<SpriteRenderer>();
        }
    }

    private void CacheOriginalValues()
    {
        originalScale =
            transform.localScale;

        originalColor =
            ringRenderer != null
                ? ringRenderer.color
                : Color.white;
    }

    public void Play(
        Vector3 worldPosition,
        float duration,
        float startScale,
        float endScale,
        Action<
            BallTemporaryUpgradeRingView
        > onCompleted)
    {
        StopAnimation();

        duration =
            Mathf.Max(
                duration,
                0.01f
            );

        startScale =
            Mathf.Max(
                startScale,
                0.01f
            );

        endScale =
            Mathf.Max(
                endScale,
                startScale
            );

        completedCallback =
            onCompleted;

        transform.position =
            worldPosition;

        transform.localScale =
            Vector3.one *
            startScale;

        if (ringRenderer != null)
        {
            Color startColor =
                originalColor;

            startColor.a =
                originalColor.a;

            ringRenderer.color =
                startColor;

            ringRenderer.enabled =
                true;
        }

        gameObject.SetActive(
            true
        );

        activeSequence =
            DOTween.Sequence();

        activeSequence.SetLink(
            gameObject,
            LinkBehaviour.KillOnDisable
        );

        activeSequence.Join(
            transform.DOScale(
                Vector3.one *
                endScale,
                duration
            )
            .SetEase(
                Ease.OutCubic
            )
        );

        if (ringRenderer != null)
        {
            activeSequence.Join(
                ringRenderer.DOFade(
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
            BallTemporaryUpgradeRingView
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

        if (ringRenderer != null)
        {
            ringRenderer.color =
                originalColor;

            ringRenderer.enabled =
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