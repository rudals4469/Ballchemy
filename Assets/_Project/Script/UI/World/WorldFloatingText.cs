using DG.Tweening;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshPro))]
public sealed class WorldFloatingText :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private TextMeshPro label;

    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float riseDistance = 0.65f;

    [SerializeField, Min(0.01f)]
    private float duration = 0.65f;

    [Header("Fade")]
    [Tooltip(
        "전체 애니메이션 중 몇 퍼센트 지점부터 " +
        "투명해지기 시작할지 결정합니다."
    )]
    [SerializeField, Range(0f, 1f)]
    private float fadeStartRatio = 0.25f;

    [Header("Scale")]
    [SerializeField, Range(0.1f, 2f)]
    private float startScaleMultiplier = 0.8f;

    [SerializeField, Range(0.1f, 2f)]
    private float peakScaleMultiplier = 1.15f;

    [SerializeField, Range(0.01f, 0.9f)]
    private float peakScaleTimeRatio = 0.2f;

    [Header("Sorting")]
    [SerializeField]
    private string sortingLayerName = "UI";

    [SerializeField]
    private int sortingOrder = 100;

    private Sequence animationSequence;

    private Vector3 originalScale;

    private void Awake()
    {
        FindReferences();

        originalScale =
            transform.localScale;

        ApplySorting();
    }

    private void OnValidate()
    {
        riseDistance =
            Mathf.Max(
                riseDistance,
                0f
            );

        duration =
            Mathf.Max(
                duration,
                0.01f
            );

        fadeStartRatio =
            Mathf.Clamp01(
                fadeStartRatio
            );

        startScaleMultiplier =
            Mathf.Max(
                startScaleMultiplier,
                0.1f
            );

        peakScaleMultiplier =
            Mathf.Max(
                peakScaleMultiplier,
                0.1f
            );

        peakScaleTimeRatio =
            Mathf.Clamp(
                peakScaleTimeRatio,
                0.01f,
                0.9f
            );

        FindReferences();
        ApplySorting();
    }

    private void FindReferences()
    {
        if (label == null)
        {
            label =
                GetComponent<TextMeshPro>();
        }
    }

    private void ApplySorting()
    {
        if (label == null)
        {
            return;
        }

        Renderer textRenderer =
            label.GetComponent<Renderer>();

        if (textRenderer == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(
                sortingLayerName))
        {
            textRenderer.sortingLayerName =
                sortingLayerName;
        }

        textRenderer.sortingOrder =
            sortingOrder;
    }

    public void Show(
        string message,
        Color textColor)
    {
        FindReferences();

        if (label == null)
        {
            Debug.LogError(
                "WorldFloatingText: " +
                "TextMeshPro를 찾지 못했습니다.",
                this
            );

            Destroy(
                gameObject
            );

            return;
        }

        StopCurrentAnimation();

        Vector3 startPosition =
            transform.position;

        textColor.a = 1f;

        label.text =
            message;

        label.color =
            textColor;

        transform.localScale =
            originalScale *
            startScaleMultiplier;

        float peakScaleDuration =
            Mathf.Max(
                duration *
                peakScaleTimeRatio,
                0.01f
            );

        float restoreScaleDuration =
            Mathf.Max(
                duration -
                peakScaleDuration,
                0.01f
            );

        float fadeDelay =
            duration *
            fadeStartRatio;

        float fadeDuration =
            Mathf.Max(
                duration -
                fadeDelay,
                0.01f
            );

        animationSequence =
            DOTween.Sequence();

        animationSequence.Insert(
            0f,
            transform
                .DOMove(
                    startPosition +
                    Vector3.up *
                    riseDistance,
                    duration
                )
                .SetEase(
                    Ease.OutCubic
                )
        );

        animationSequence.Insert(
            0f,
            transform
                .DOScale(
                    originalScale *
                    peakScaleMultiplier,
                    peakScaleDuration
                )
                .SetEase(
                    Ease.OutBack
                )
        );

        animationSequence.Insert(
            peakScaleDuration,
            transform
                .DOScale(
                    originalScale,
                    restoreScaleDuration
                )
                .SetEase(
                    Ease.OutQuad
                )
        );

        animationSequence.Insert(
            fadeDelay,
            label
                .DOFade(
                    0f,
                    fadeDuration
                )
                .SetEase(
                    Ease.InQuad
                )
        );

        animationSequence
            .SetLink(
                gameObject,
                LinkBehaviour.KillOnDestroy
            )
            .OnComplete(
                HandleAnimationCompleted
            );
    }

    private void HandleAnimationCompleted()
    {
        animationSequence = null;

        Destroy(
            gameObject
        );
    }

    private void StopCurrentAnimation()
    {
        if (animationSequence == null ||
            !animationSequence.IsActive())
        {
            animationSequence = null;

            return;
        }

        animationSequence.Kill(
            false
        );

        animationSequence = null;
    }

    private void OnDisable()
    {
        StopCurrentAnimation();
    }

    private void OnDestroy()
    {
        StopCurrentAnimation();
    }
}