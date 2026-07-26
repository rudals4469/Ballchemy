using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshPro))]
public sealed class BallDamagePopupView :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private TextMeshPro damageText;

    private Sequence animationSequence;

    private Action<BallDamagePopupView>
        completedCallback;

    private Renderer textRenderer;

    private TMP_FontAsset defaultFontAsset;
    private Material defaultFontMaterial;
    private Color defaultColor;
    private float defaultFontSize;
    private FontStyles defaultFontStyle;

    private string defaultSortingLayerName;
    private int defaultSortingOrder;

    private void Awake()
    {
        FindReferences();
        CachePrefabDefaults();
    }

    private void OnValidate()
    {
        FindReferences();
    }

    private void FindReferences()
    {
        if (damageText == null)
        {
            damageText =
                GetComponent<TextMeshPro>();
        }

        if (textRenderer == null)
        {
            textRenderer =
                GetComponent<Renderer>();
        }
    }

    private void CachePrefabDefaults()
    {
        if (damageText == null)
        {
            return;
        }

        defaultFontAsset =
            damageText.font;

        defaultFontMaterial =
            damageText.fontSharedMaterial;

        defaultColor =
            damageText.color;

        defaultFontSize =
            damageText.fontSize;

        defaultFontStyle =
            damageText.fontStyle;

        if (textRenderer == null)
        {
            return;
        }

        defaultSortingLayerName =
            textRenderer.sortingLayerName;

        defaultSortingOrder =
            textRenderer.sortingOrder;
    }

    public void Play(
        BallDamageEvent damageEvent,
        BallDamageTextStyleDefinition style,
        Vector3 worldPosition,
        Action<BallDamagePopupView>
            onCompleted)
    {
        CancelWithoutCallback();

        completedCallback =
            onCompleted;

        gameObject.SetActive(
            true
        );

        ApplyTextStyle(
            damageEvent,
            style
        );

        float duration =
            style != null
                ? style.Duration
                : 0.55f;

        float riseDistance =
            style != null
                ? style.RiseDistance
                : 0.7f;

        float startScale =
            style != null
                ? style.StartScale
                : 0.9f;

        float peakScale =
            style != null
                ? style.PeakScale
                : 1.15f;

        float endScale =
            style != null
                ? style.EndScale
                : 1f;

        float popDurationRatio =
            style != null
                ? style.PopDurationRatio
                : 0.25f;

        float fadeStartRatio =
            style != null
                ? style.FadeStartRatio
                : 0.35f;

        Ease moveEase =
            style != null
                ? style.MoveEase
                : Ease.OutCubic;

        Ease popEase =
            style != null
                ? style.PopEase
                : Ease.OutBack;

        Ease settleEase =
            style != null
                ? style.SettleEase
                : Ease.OutQuad;

        Ease fadeEase =
            style != null
                ? style.FadeEase
                : Ease.InQuad;

        Vector2 driftRange =
            style != null
                ? style.HorizontalDriftRange
                : new Vector2(
                    -0.08f,
                    0.08f
                );

        float horizontalDrift =
            UnityEngine.Random.Range(
                driftRange.x,
                driftRange.y
            );

        transform.position =
            worldPosition;

        transform.localScale =
            Vector3.one *
            startScale;

        damageText.alpha = 1f;

        Vector3 endPosition =
            worldPosition +
            new Vector3(
                horizontalDrift,
                riseDistance,
                0f
            );

        float popDuration =
            Mathf.Max(
                duration *
                popDurationRatio,
                0.01f
            );

        float settleDuration =
            Mathf.Max(
                duration -
                popDuration,
                0.01f
            );

        float fadeStartTime =
            duration *
            fadeStartRatio;

        float fadeDuration =
            Mathf.Max(
                duration -
                fadeStartTime,
                0.01f
            );

        animationSequence =
            DOTween.Sequence();

        animationSequence.Join(
            transform.DOMove(
                    endPosition,
                    duration
                )
                .SetEase(
                    moveEase
                )
        );

        animationSequence.Join(
            transform.DOScale(
                    Vector3.one *
                    peakScale,
                    popDuration
                )
                .SetEase(
                    popEase
                )
        );

        animationSequence.Insert(
            popDuration,
            transform.DOScale(
                    Vector3.one *
                    endScale,
                    settleDuration
                )
                .SetEase(
                    settleEase
                )
        );

        animationSequence.Insert(
            fadeStartTime,
            damageText.DOFade(
                    0f,
                    fadeDuration
                )
                .SetEase(
                    fadeEase
                )
        );

        animationSequence.OnComplete(
            Complete
        );
    }

    private void ApplyTextStyle(
        BallDamageEvent damageEvent,
        BallDamageTextStyleDefinition style)
    {
        if (damageText == null)
        {
            return;
        }

        string prefix =
            style != null
                ? style.Prefix
                : string.Empty;

        string suffix =
            style != null
                ? style.Suffix
                : string.Empty;

        damageText.text =
            $"{prefix}" +
            $"{damageEvent.Damage}" +
            $"{suffix}";

        damageText.font =
            style != null &&
            style.FontAsset != null
                ? style.FontAsset
                : defaultFontAsset;

        damageText.fontSharedMaterial =
            style != null &&
            style.FontMaterialPreset != null
                ? style.FontMaterialPreset
                : defaultFontMaterial;

        damageText.color =
            style != null
                ? style.TextColor
                : defaultColor;

        damageText.fontSize =
            style != null
                ? style.FontSize
                : defaultFontSize;

        damageText.fontStyle =
            style != null
                ? style.FontStyle
                : defaultFontStyle;

        if (textRenderer == null)
        {
            return;
        }

        string sortingLayerName =
            style != null &&
            !string.IsNullOrWhiteSpace(
                style.SortingLayerName
            )
                ? style.SortingLayerName
                : defaultSortingLayerName;

        textRenderer.sortingLayerName =
            sortingLayerName;

        textRenderer.sortingOrder =
            style != null
                ? style.SortingOrder
                : defaultSortingOrder;
    }

    public void CancelWithoutCallback()
    {
        if (animationSequence != null)
        {
            animationSequence.Kill();

            animationSequence = null;
        }

        completedCallback = null;
    }

    private void Complete()
    {
        animationSequence = null;

        Action<BallDamagePopupView>
            callback =
                completedCallback;

        completedCallback = null;

        callback?.Invoke(
            this
        );
    }

    private void OnDisable()
    {
        CancelWithoutCallback();
    }

    private void OnDestroy()
    {
        CancelWithoutCallback();
    }
}