using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CommonChoiceCardLayout : MonoBehaviour
{
    public enum CardCategory { Ball, Augment, Shop, Event, Alchemy, Secret }

    [Header("Authored category pennant (layout stays in the hierarchy)")]
    [SerializeField] private CardCategory category;
    [SerializeField] private Image categoryPennant;
    [SerializeField] private Image categorySymbol;
    [SerializeField] private Image authoredIcon;
    [SerializeField] private Sprite fallbackIcon;
    [SerializeField] private bool useTemporaryQuestionIcon;

    private static Sprite QuestionIcon =>
        Resources.Load<Sprite>("UI/MapIcons/Map_Event_Readable");

    public void SetCategory(CardCategory value)
    {
        category = value;
        Color32[] colors = {
            new Color32(130, 187, 231, 255), new Color32(116, 205, 192, 255),
            new Color32(242, 204, 113, 255), new Color32(231, 150, 133, 255),
            new Color32(162, 205, 133, 255), new Color32(188, 172, 218, 255)
        };
        string[] paths = {
            "UI/MapIcons/Map_Combat", "UI/MapIcons/Map_Augment_Readable",
            "UI/MapIcons/CardSymbol_ShopDollar", "UI/MapIcons/Map_Event_Readable",
            "UI/MapIcons/Map_Alchemy_Readable", "UI/MapIcons/Map_Secret_Readable"
        };
        int index = Mathf.Clamp((int)value, 0, paths.Length - 1);
        if (categoryPennant != null) categoryPennant.color = colors[index];
        if (categorySymbol != null)
        {
            categorySymbol.sprite = Resources.Load<Sprite>(paths[index]);
            categorySymbol.enabled = categorySymbol.sprite != null;
        }
    }

    public void SetBallRewardSymbol(BallDefinition definition)
    {
        SetCategory(CardCategory.Ball);

        ElementalBallTraitDefinition elemental = definition != null
            ? definition.TraitDefinition as ElementalBallTraitDefinition
            : null;
        if (elemental == null) return;

        string path;
        switch (elemental.ElementType)
        {
            case ElementType.Water:
                path = "VFX/ElementSymbols/Icon_Element_Water";
                break;
            case ElementType.Electric:
                path = "VFX/ElementSymbols/Icon_Element_Lightning";
                break;
            case ElementType.Fire:
                path = "VFX/ElementSymbols/Icon_Element_Fire";
                break;
            case ElementType.Ice:
                path = "VFX/ElementSymbols/Icon_Element_Ice";
                break;
            default:
                return;
        }

        SetCategorySymbol(Resources.Load<Sprite>(path));
    }

    private void SetCategorySymbol(Sprite sprite)
    {
        if (categorySymbol == null) return;
        categorySymbol.sprite = sprite;
        categorySymbol.enabled = sprite != null;
        categorySymbol.preserveAspect = true;
    }
    public static readonly Color Bronze = new Color(0.53f, 0.39f, 0.28f, 1f);
    public static readonly Color Silver = new Color(0.47f, 0.50f, 0.54f, 1f);
    public static readonly Color Gold = new Color(0.58f, 0.49f, 0.25f, 1f);

    private Button button;
    private Image background;
    private Image icon;
    private GameObject iconRoot;
    private TMP_Text title;
    private TMP_Text auxiliary;
    private TMP_Text description;
    private GameObject iconAuxiliaryPanel;
    private GameObject iconAuxiliaryDivider;
    private LayoutSnapshot titleLayout;
    private LayoutSnapshot auxiliaryLayout;
    private LayoutSnapshot descriptionLayout;
    private bool hasCapturedAuthoredLayout;

    private struct LayoutSnapshot
    {
        public LayoutElement Element;
        public float MinHeight;
        public float PreferredHeight;
        public float FlexibleHeight;
    }

    public void Configure(
        Button cardButton,
        Image iconImage,
        GameObject iconContainer,
        TMP_Text titleText,
        TMP_Text auxiliaryText,
        TMP_Text descriptionText)
    {
        button = cardButton != null ? cardButton : GetComponent<Button>();
        background = button != null ? button.targetGraphic as Image : GetComponent<Image>();
        icon = iconImage != null ? iconImage : authoredIcon;
        iconRoot = iconContainer != null ? iconContainer : (icon != null ? icon.transform.parent.gameObject : null);
        title = titleText;
        auxiliary = auxiliaryText;
        description = descriptionText;

        ConfigureTextPreservingAlignment(title);
        ConfigureTextPreservingAlignment(auxiliary);
        ConfigureTextPreservingAlignment(description);
        CaptureAuthoredLayout();
        SetCategory(category);
        SetIcon(icon != null ? icon.sprite : null);
    }

    public void SetAugmentLayoutEnabled(bool enabled)
    {
        CaptureAuthoredLayout();

        // 카드 위치와 크기는 씬의 하이어라키에서 편집한 값을 그대로 사용한다.
        // 이전 구현은 증강 카드가 바인딩될 때만 고정 높이를 적용하여
        // 에디터 미리보기와 실제 런타임 배치가 달라졌다.
        RestoreLayout(titleLayout);
        RestoreLayout(auxiliaryLayout);
        RestoreLayout(descriptionLayout);
    }

    public void SetTexts(string titleValue, string auxiliaryValue, string descriptionValue)
    {
        SetText(title, titleValue);
        SetText(auxiliary, auxiliaryValue);
        SetText(description, descriptionValue);
    }

    public void SetIcon(Sprite sprite)
    {
        if (sprite == null) sprite = QuestionIcon;
        if (sprite == null) sprite = fallbackIcon;
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            icon.preserveAspect = true;
        }
        if (iconRoot != null) iconRoot.SetActive(sprite != null);
    }

    public void SetBackgroundColor(Color color)
    {
        if (background != null)
            background.color = IsWorkbenchCard(background.sprite) ? Color.white : color;
    }

    public static bool IsWorkbenchCard(Sprite sprite)
    {
        if (sprite == null) return false;

        return sprite.name.StartsWith("Card_Workbench", System.StringComparison.Ordinal) ||
               sprite.name.StartsWith("Panel_Parchment_MissionClean", System.StringComparison.Ordinal);
    }

    public static Color WorkbenchAugmentBorderColor(AugmentValueTier tier)
    {
        switch (tier)
        {
            case AugmentValueTier.Value1: return new Color32(176, 105, 52, 255);
            case AugmentValueTier.Value2: return new Color32(190, 203, 221, 255);
            default: return new Color32(235, 182, 42, 255);
        }
    }

    public static Color WorkbenchTierColor(int tier)
    {
        // Keep the paper readable while retaining the blue / green / yellow value distinction.
        switch (tier)
        {
            case 1: return new Color32(220, 237, 255, 255);
            case 2: return new Color32(223, 250, 223, 255);
            case 3: return new Color32(255, 239, 194, 255);
            default: return Color.white;
        }
    }

    public void SetIconAuxiliaryPanelVisible(bool visible, bool showDivider)
    {
        // 아이콘/별 패널 또한 씬에 배치된 오브젝트만 제어한다.
        // 런타임 생성은 하이어라키에서 확인한 위치와 다른 UI를 만들기 때문에
        // 더 이상 여기서 새 패널을 만들지 않는다.
        if (iconAuxiliaryPanel == null) return;
        iconAuxiliaryPanel.SetActive(visible);
        if (iconAuxiliaryDivider != null)
            iconAuxiliaryDivider.SetActive(visible && showDivider);
    }

    private void EnsureIconAuxiliaryPanel()
    {
        if (iconAuxiliaryPanel != null) return;

        iconAuxiliaryPanel = new GameObject(
            "CommonIconAuxiliaryPanel",
            typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Outline));
        RectTransform panelRect = iconAuxiliaryPanel.GetComponent<RectTransform>();
        panelRect.SetParent(transform, false);
        panelRect.SetAsFirstSibling();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(70f, 0f);
        panelRect.sizeDelta = new Vector2(116f, -20f);

        Image panelImage = iconAuxiliaryPanel.GetComponent<Image>();
        panelImage.color = new Color(0.18f, 0.18f, 0.17f, 0.42f);
        panelImage.raycastTarget = false;
        Outline outline = iconAuxiliaryPanel.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = false;

        iconAuxiliaryDivider = new GameObject(
            "IconAuxiliaryDivider",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform dividerRect = iconAuxiliaryDivider.GetComponent<RectTransform>();
        dividerRect.SetParent(panelRect, false);
        dividerRect.anchorMin = new Vector2(0.08f, 0.24f);
        dividerRect.anchorMax = new Vector2(0.92f, 0.24f);
        dividerRect.pivot = new Vector2(0.5f, 0.5f);
        dividerRect.sizeDelta = new Vector2(0f, 2f);
        Image divider = iconAuxiliaryDivider.GetComponent<Image>();
        divider.color = new Color(0f, 0f, 0f, 0.8f);
        divider.raycastTarget = false;
    }

    public static Color ForRewardTier(RewardTier tier, Color fallback)
    {
        switch (tier)
        {
            case RewardTier.Tier1: return Bronze;
            case RewardTier.Tier2: return Silver;
            case RewardTier.Tier3: return Gold;
            default: return fallback;
        }
    }

    public static Color ForValueTier(AugmentValueTier tier)
    {
        switch (tier)
        {
            case AugmentValueTier.Value1: return Bronze;
            case AugmentValueTier.Value2: return Silver;
            default: return Gold;
        }
    }

    public static void ConfigureText(TMP_Text text, TextAlignmentOptions alignment)
    {
        if (text == null) return;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.enableAutoSizing = false;
        text.alignment = alignment;
    }

    private static void ConfigureTextPreservingAlignment(TMP_Text text)
    {
        if (text == null) return;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.enableAutoSizing = false;
    }

    private void CaptureAuthoredLayout()
    {
        if (hasCapturedAuthoredLayout) return;
        titleLayout = CaptureLayout(title);
        auxiliaryLayout = CaptureLayout(auxiliary);
        descriptionLayout = CaptureLayout(description);
        hasCapturedAuthoredLayout = true;
    }

    private static LayoutSnapshot CaptureLayout(TMP_Text text)
    {
        LayoutElement layout = FindLayoutElement(text);
        return new LayoutSnapshot
        {
            Element = layout,
            MinHeight = layout != null ? layout.minHeight : -1f,
            PreferredHeight = layout != null ? layout.preferredHeight : -1f,
            FlexibleHeight = layout != null ? layout.flexibleHeight : -1f
        };
    }

    private static LayoutElement FindLayoutElement(TMP_Text text)
    {
        if (text == null) return null;
        LayoutElement layout = text.GetComponent<LayoutElement>();
        if (layout == null && text.transform.parent != null)
            layout = text.transform.parent.GetComponent<LayoutElement>();
        return layout;
    }

    private static void ReserveLayoutHeight(
        LayoutElement layout,
        float preferredHeight)
    {
        if (layout == null) return;
        layout.minHeight = preferredHeight;
        layout.preferredHeight = preferredHeight;
        layout.flexibleHeight = 0f;
    }

    private static void RestoreLayout(LayoutSnapshot snapshot)
    {
        if (snapshot.Element == null) return;
        snapshot.Element.minHeight = snapshot.MinHeight;
        snapshot.Element.preferredHeight = snapshot.PreferredHeight;
        snapshot.Element.flexibleHeight = snapshot.FlexibleHeight;
    }

    public static void SetText(TMP_Text text, string value)
    {
        if (text == null) return;
        text.text = FormatText(value);
        HoverTooltip tooltip = text.GetComponent<HoverTooltip>();
        if (tooltip == null) tooltip = text.gameObject.AddComponent<HoverTooltip>();
        tooltip.ConfigureTruncatedContent(value);
    }

    public static string FormatText(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;
        return Regex.Replace(value, @"\S+", match => $"<nobr>{match.Value}</nobr>");
    }
}
