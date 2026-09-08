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
    private SpriteRenderer elementIconRenderer;
    private ElementType? activeElement;

    private const float ElementIconTextHeightRatio = 0.92f;
    private const float ElementIconGap = 0.055f;

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

        ApplyElementIcon(damageEvent);

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

        ApplyElementIcon(damageEvent);

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

        UpdateElementIconPosition();
    }

    private void ApplyElementIcon(BallDamageEvent damageEvent)
    {
        activeElement = ResolveElement(damageEvent.SourceDefinition);
        if (!activeElement.HasValue)
        {
            SetElementIconVisible(false);
            return;
        }

        EnsureElementIconRenderer();
        Sprite sprite = Resources.Load<Sprite>(GetElementIconPath(activeElement.Value));
        if (sprite == null)
        {
            SetElementIconVisible(false);
            return;
        }

        elementIconRenderer.sprite = sprite;
        elementIconRenderer.color = ResolveElementColor(activeElement.Value);
        elementIconRenderer.enabled = true;

        UpdateElementIconPosition();
    }

    private void EnsureElementIconRenderer()
    {
        if (elementIconRenderer != null) return;

        Transform existing = transform.Find("ElementIcon");
        GameObject iconObject = existing != null
            ? existing.gameObject
            : new GameObject("ElementIcon");
        iconObject.transform.SetParent(transform, false);
        elementIconRenderer = iconObject.GetComponent<SpriteRenderer>();
        if (elementIconRenderer == null)
            elementIconRenderer = iconObject.AddComponent<SpriteRenderer>();
        if (textRenderer != null)
        {
            elementIconRenderer.sortingLayerName = textRenderer.sortingLayerName;
            elementIconRenderer.sortingOrder = textRenderer.sortingOrder + 1;
        }
        elementIconRenderer.enabled = false;
    }

    private void UpdateElementIconPosition()
    {
        if (elementIconRenderer == null || !elementIconRenderer.enabled || damageText == null)
            return;

        damageText.ForceMeshUpdate();
        ResizeElementIconToTextHeight();
        float halfTextWidth = damageText.textBounds.size.x * 0.5f;
        float halfIconWidth = elementIconRenderer.bounds.size.x * 0.5f;
        elementIconRenderer.transform.localPosition = new Vector3(
            -(halfTextWidth + halfIconWidth + ElementIconGap),
            0.02f,
            0f);
    }

    private void ResizeElementIconToTextHeight()
    {
        if (elementIconRenderer == null || elementIconRenderer.sprite == null)
            return;

        float spriteSize = Mathf.Max(
            elementIconRenderer.sprite.bounds.size.x,
            elementIconRenderer.sprite.bounds.size.y);
        float textHeight = damageText.textBounds.size.y;
        if (spriteSize <= 0.001f || textHeight <= 0.001f)
            return;

        float scale = textHeight * ElementIconTextHeightRatio / spriteSize;
        elementIconRenderer.transform.localScale = Vector3.one * scale;
    }

    private void SetElementIconVisible(bool visible)
    {
        if (elementIconRenderer != null)
            elementIconRenderer.enabled = visible;
    }

    private static ElementType? ResolveElement(BallDefinition definition)
    {
        return definition != null &&
               definition.TraitDefinition is ElementalBallTraitDefinition elemental
            ? elemental.ElementType
            : (ElementType?)null;
    }

    private static string GetElementIconPath(ElementType element)
    {
        switch (element)
        {
            case ElementType.Electric: return "VFX/ElementSymbols/Icon_Element_Lightning";
            case ElementType.Water: return "VFX/ElementSymbols/Icon_Element_Water";
            case ElementType.Ice: return "VFX/ElementSymbols/Icon_Element_Ice";
            case ElementType.Fire: return "VFX/ElementSymbols/Icon_Element_Fire";
            default: return string.Empty;
        }
    }

    private static Color ResolveElementColor(ElementType element)
    {
        switch (element)
        {
            case ElementType.Electric: return new Color(1f, 0.94f, 0.58f, 1f);
            case ElementType.Water: return new Color(0.32f, 0.78f, 1f, 1f);
            case ElementType.Ice: return new Color(0.68f, 0.96f, 1f, 1f);
            case ElementType.Fire: return new Color(1f, 0.38f, 0.08f, 1f);
            default: return Color.white;
        }
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

        // Preserve each damage type's face color while keeping bright text
        // readable over the parchment combat surface.
        damageText.outlineColor =
            new Color32(45, 28, 18, 230);
        damageText.outlineWidth =
            0.18f;

        damageText.fontSize =
            style != null
                ? style.FontSize + 1f
                : defaultFontSize + 1f;

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

        if (elementIconRenderer != null)
        {
            elementIconRenderer.sortingLayerName = textRenderer.sortingLayerName;
            elementIconRenderer.sortingOrder = textRenderer.sortingOrder + 1;
        }
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

        if (elementIconRenderer != null && elementIconRenderer.enabled)
        {
            exitSequence.Insert(
                fadeStartTime,
                elementIconRenderer.DOFade(0f, fadeDuration).SetEase(fadeEase));
        }

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
        activeElement = null;
        SetElementIconVisible(false);
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
