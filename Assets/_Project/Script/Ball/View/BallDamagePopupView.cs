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

    private Sequence pulseSequence;
    private Sequence exitSequence;
    private Tween exitDelayTween;

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

    private BallDamageTextStyleDefinition
        activeStyle;

    private int accumulatedDisplayedDamage;
    private int accumulatedAppliedHealthDamage;

    private float activeAggregationWindow;

    private bool isAcceptingDamage;

    public bool IsAcceptingDamage =>
        isAcceptingDamage;

    public int AccumulatedDisplayedDamage =>
        accumulatedDisplayedDamage;

    public int AccumulatedAppliedHealthDamage =>
        accumulatedAppliedHealthDamage;

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

    public void BeginDisplay(
        BallDamageEvent damageEvent,
        BallDamageTextStyleDefinition style,
        Vector3 worldPosition,
        float aggregationWindow,
        Action<BallDamagePopupView>
            onCompleted)
    {
        CancelWithoutCallback();

        activeStyle =
            style;

        activeAggregationWindow =
            Mathf.Max(
                aggregationWindow,
                0f
            );

        completedCallback =
            onCompleted;

        accumulatedDisplayedDamage =
            Mathf.Max(
                damageEvent.DisplayedDamage,
                0
            );

        accumulatedAppliedHealthDamage =
            Mathf.Max(
                damageEvent.AppliedHealthDamage,
                0
            );

        gameObject.SetActive(
            true
        );

        transform.position =
            worldPosition;

        ApplyVisualStyle(
            activeStyle
        );

        RefreshText();

        float visibleAlpha =
            activeStyle != null
                ? activeStyle.TextColor.a
                : defaultColor.a;

        damageText.alpha =
            visibleAlpha;

        isAcceptingDamage =
            activeAggregationWindow > 0f;

        PlayPulse(
            true
        );

        if (isAcceptingDamage)
        {
            ScheduleExit();
        }
        else
        {
            StartExit();
        }
    }

    public bool TryAccumulate(
        BallDamageEvent damageEvent)
    {
        if (!isAcceptingDamage)
        {
            return false;
        }

        accumulatedDisplayedDamage +=
            Mathf.Max(
                damageEvent.DisplayedDamage,
                0
            );

        accumulatedAppliedHealthDamage +=
            Mathf.Max(
                damageEvent.AppliedHealthDamage,
                0
            );

        RefreshText();

        PlayPulse(
            false
        );

        ScheduleExit();

        return true;
    }

    private void RefreshText()
    {
        if (damageText == null)
        {
            return;
        }

        string prefix =
            activeStyle != null
                ? activeStyle.Prefix
                : string.Empty;

        string suffix =
            activeStyle != null
                ? activeStyle.Suffix
                : string.Empty;

        damageText.text =
            $"{prefix}" +
            $"{accumulatedDisplayedDamage}" +
            $"{suffix}";
    }

    private void ApplyVisualStyle(
        BallDamageTextStyleDefinition style)
    {
        if (damageText == null)
        {
            return;
        }

        TMP_FontAsset resolvedFont =
            style != null &&
            style.FontAsset != null
                ? style.FontAsset
                : defaultFontAsset;

        damageText.font =
            resolvedFont;

        if (style != null &&
            style.FontMaterialPreset != null)
        {
            damageText.fontSharedMaterial =
                style.FontMaterialPreset;
        }
        else if (style != null &&
                 style.FontAsset != null &&
                 style.FontAsset.material != null)
        {
            damageText.fontSharedMaterial =
                style.FontAsset.material;
        }
        else
        {
            damageText.fontSharedMaterial =
                defaultFontMaterial;
        }

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

    private void PlayPulse(
        bool isInitialPulse)
    {
        if (pulseSequence != null)
        {
            pulseSequence.Kill();

            pulseSequence = null;
        }

        float duration =
            activeStyle != null
                ? activeStyle.Duration
                : 0.55f;

        float startScale =
            activeStyle != null
                ? activeStyle.StartScale
                : 0.9f;

        float peakScale =
            activeStyle != null
                ? activeStyle.PeakScale
                : 1.15f;

        float endScale =
            activeStyle != null
                ? activeStyle.EndScale
                : 1f;

        float popDurationRatio =
            activeStyle != null
                ? activeStyle.PopDurationRatio
                : 0.25f;

        Ease popEase =
            activeStyle != null
                ? activeStyle.PopEase
                : Ease.OutBack;

        Ease settleEase =
            activeStyle != null
                ? activeStyle.SettleEase
                : Ease.OutQuad;

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

        transform.localScale =
            Vector3.one *
            (
                isInitialPulse
                    ? startScale
                    : endScale
            );

        pulseSequence =
            DOTween.Sequence();

        pulseSequence.Append(
            transform.DOScale(
                    Vector3.one *
                    peakScale,
                    popDuration
                )
                .SetEase(
                    popEase
                )
        );

        pulseSequence.Append(
            transform.DOScale(
                    Vector3.one *
                    endScale,
                    settleDuration
                )
                .SetEase(
                    settleEase
                )
        );
    }

    private void ScheduleExit()
    {
        if (exitDelayTween != null)
        {
            exitDelayTween.Kill();

            exitDelayTween = null;
        }

        exitDelayTween =
            DOVirtual.DelayedCall(
                activeAggregationWindow,
                StartExit
            );
    }

    private void StartExit()
    {
        if (exitSequence != null)
        {
            return;
        }

        isAcceptingDamage =
            false;

        if (exitDelayTween != null)
        {
            exitDelayTween.Kill();

            exitDelayTween = null;
        }

        if (pulseSequence != null)
        {
            pulseSequence.Kill();

            pulseSequence = null;
        }

        float duration =
            activeStyle != null
                ? activeStyle.Duration
                : 0.55f;

        float riseDistance =
            activeStyle != null
                ? activeStyle.RiseDistance
                : 0.7f;

        float endScale =
            activeStyle != null
                ? activeStyle.EndScale
                : 1f;

        float fadeStartRatio =
            activeStyle != null
                ? activeStyle.FadeStartRatio
                : 0.35f;

        Ease moveEase =
            activeStyle != null
                ? activeStyle.MoveEase
                : Ease.OutCubic;

        Ease fadeEase =
            activeStyle != null
                ? activeStyle.FadeEase
                : Ease.InQuad;

        Vector2 driftRange =
            activeStyle != null
                ? activeStyle.HorizontalDriftRange
                : new Vector2(
                    -0.08f,
                    0.08f
                );

        float horizontalDrift =
            UnityEngine.Random.Range(
                driftRange.x,
                driftRange.y
            );

        Vector3 endPosition =
            transform.position +
            new Vector3(
                horizontalDrift,
                riseDistance,
                0f
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

        transform.localScale =
            Vector3.one *
            endScale;

        exitSequence =
            DOTween.Sequence();

        exitSequence.Join(
            transform.DOMove(
                    endPosition,
                    duration
                )
                .SetEase(
                    moveEase
                )
        );

        exitSequence.Insert(
            fadeStartTime,
            damageText.DOFade(
                    0f,
                    fadeDuration
                )
                .SetEase(
                    fadeEase
                )
        );

        exitSequence.OnComplete(
            Complete
        );
    }

    public void CancelWithoutCallback()
    {
        if (pulseSequence != null)
        {
            pulseSequence.Kill();

            pulseSequence = null;
        }

        if (exitSequence != null)
        {
            exitSequence.Kill();

            exitSequence = null;
        }

        if (exitDelayTween != null)
        {
            exitDelayTween.Kill();

            exitDelayTween = null;
        }

        completedCallback = null;

        activeStyle = null;

        activeAggregationWindow = 0f;

        accumulatedDisplayedDamage = 0;
        accumulatedAppliedHealthDamage = 0;

        isAcceptingDamage = false;
    }

    private void Complete()
    {
        exitSequence = null;

        isAcceptingDamage =
            false;

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