using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class HoverTooltip : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    private static RectTransform sharedPanel;
    private static TMP_Text sharedText;

    public static bool HasSharedView =>
        sharedPanel != null && sharedText != null;

    public static void ShowSharedAtScreenPosition(
        string nextContent,
        Vector2 screenPosition)
    {
        if (!HasSharedView ||
            string.IsNullOrWhiteSpace(nextContent))
        {
            return;
        }

        Canvas canvas =
            sharedPanel.GetComponentInParent<Canvas>();

        Canvas rootCanvas =
            canvas != null ? canvas.rootCanvas : null;

        RectTransform canvasRect =
            rootCanvas != null
                ? rootCanvas.transform as RectTransform
                : null;

        if (canvasRect != null &&
            sharedPanel.parent != canvasRect)
        {
            sharedPanel.SetParent(canvasRect, false);
        }

        sharedPanel.gameObject.SetActive(true);
        sharedPanel.SetAsLastSibling();
        PrepareView(sharedPanel, sharedText);

        sharedText.text = nextContent;
        sharedText.fontSize = 20f;
        sharedText.color =
            new Color(0.08f, 0.08f, 0.08f, 1f);
        sharedText.alignment =
            TextAlignmentOptions.TopLeft;
        sharedText.textWrappingMode =
            TextWrappingModes.Normal;
        sharedText.overflowMode =
            TextOverflowModes.Overflow;
        sharedText.enabled = true;

        const float maximumWidth = 560f;
        Vector2 padding = new Vector2(40f, 32f);

        sharedText.ForceMeshUpdate(true, true);

        float textWidth = Mathf.Min(
            sharedText.GetPreferredValues(nextContent).x,
            maximumWidth - padding.x);

        Vector2 preferred = sharedText.GetPreferredValues(
            nextContent,
            textWidth,
            Mathf.Infinity);

        sharedPanel.sizeDelta = new Vector2(
            Mathf.Min(preferred.x + padding.x, maximumWidth),
            preferred.y + padding.y);

        RectTransform textRect = sharedText.rectTransform;
        textRect.anchorMin = textRect.anchorMax =
            textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(
            Mathf.Max(1f, sharedPanel.sizeDelta.x - padding.x),
            Mathf.Max(1f, sharedPanel.sizeDelta.y - padding.y));

        sharedPanel.pivot = new Vector2(0f, 1f);

        if (canvasRect != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                rootCanvas != null &&
                rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? rootCanvas.worldCamera
                    : null,
                out Vector2 localPoint))
        {
            sharedPanel.anchoredPosition =
                localPoint + new Vector2(18f, -18f);

            Vector2 panelSize = sharedPanel.sizeDelta;
            Rect canvasBounds = canvasRect.rect;
            Vector2 position = sharedPanel.anchoredPosition;

            position.x = Mathf.Clamp(
                position.x,
                canvasBounds.xMin,
                canvasBounds.xMax - panelSize.x);
            position.y = Mathf.Clamp(
                position.y,
                canvasBounds.yMin + panelSize.y,
                canvasBounds.yMax);

            sharedPanel.anchoredPosition = position;
        }

        Canvas.ForceUpdateCanvases();
        sharedText.ForceMeshUpdate(true, true);
    }

    public static void HideShared()
    {
        if (sharedPanel != null)
        {
            sharedPanel.gameObject.SetActive(false);
        }
    }

    [TextArea(2, 8)]
    [SerializeField] private string content;
    [SerializeField] private RectTransform tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private RectTransform underline;
    [SerializeField, Min(0f)] private float underlineGap = 1f;
    [SerializeField] private Vector2 padding = new Vector2(40f, 32f);
    [SerializeField, Min(80f)] private float maximumWidth = 560f;
    [SerializeField, Min(8f)] private float fontSize = 20f;
    [SerializeField] private bool showOnlyWhenTextIsTruncated;

    private TMP_Text triggerText;
    private Transform originalPanelParent;
    private int originalPanelSiblingIndex;
    private bool usesSharedView;

    private static void PrepareView(RectTransform panel, TMP_Text text)
    {
        // Keep the label with its popup, outside the originating card's masks.
        if (text.transform.parent != panel)
            text.transform.SetParent(panel, false);
        panel.localScale = Vector3.one;
        text.rectTransform.localScale = Vector3.one;
        text.gameObject.SetActive(true);
        text.transform.SetAsLastSibling();
        text.enableAutoSizing = false;
        text.margin = Vector4.zero;
        text.canvasRenderer.SetAlpha(1f);
        foreach (Graphic graphic in panel.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = false;
            if (graphic is MaskableGraphic maskable)
            {
                maskable.maskable = false;
                maskable.RecalculateMasking();
                maskable.RecalculateClipping();
            }
        }
    }

    private void Awake()
    {
        triggerText = GetComponent<TMP_Text>();
        if (triggerText != null)
        {
            triggerText.fontStyle &= ~FontStyles.Underline;
            triggerText.ForceMeshUpdate();
            RefreshUnderline();
        }

        if (tooltipPanel != null)
        {
            originalPanelParent = tooltipPanel.parent;
            originalPanelSiblingIndex = tooltipPanel.GetSiblingIndex();
        }

        Hide();
    }

    private void RefreshUnderline()
    {
        if (triggerText == null || underline == null)
        {
            return;
        }

        triggerText.ForceMeshUpdate(true);
        Bounds bounds = triggerText.textBounds;
        float width = Mathf.Max(bounds.size.x, 8f);
        if (underline.parent != triggerText.transform)
            underline.SetParent(triggerText.transform, false);
        // textBounds uses the text pivot as its origin, not the rect's center.
        underline.anchorMin = underline.anchorMax = triggerText.rectTransform.pivot;
        underline.pivot = new Vector2(0.5f, 1f);
        underline.sizeDelta = new Vector2(width, 2f);
        underline.anchoredPosition = new Vector2(
            bounds.center.x,
            bounds.min.y - underlineGap);
        underline.gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        if (underline != null && triggerText != null)
            RefreshUnderline();
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnEnable()
    {
        if (triggerText == null)
            triggerText = GetComponent<TMP_Text>();
        RefreshUnderline();
    }

    public void Configure(string nextContent, RectTransform panel,
        TMP_Text text, RectTransform underlineGraphic)
    {
        content = nextContent ?? string.Empty;
        tooltipPanel = panel;
        tooltipText = text;
        usesSharedView = panel == null || text == null;
        underline = underlineGraphic;
        triggerText = GetComponent<TMP_Text>();
        if (panel != null && text != null)
        {
            sharedPanel = panel;
            sharedText = text;
        }
        if (tooltipPanel != null)
        {
            originalPanelParent = tooltipPanel.parent;
            originalPanelSiblingIndex = tooltipPanel.GetSiblingIndex();
            Graphic[] graphics = tooltipPanel.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++) graphics[i].raycastTarget = false;
        }
        if (underline != null && underline.TryGetComponent(out Graphic underlineGraphicComponent))
            underlineGraphicComponent.raycastTarget = false;
        RefreshUnderline();
    }

    public void ConfigureTruncatedContent(string nextContent)
    {
        content = nextContent ?? string.Empty;
        triggerText = GetComponent<TMP_Text>();
        showOnlyWhenTextIsTruncated = true;
        if (triggerText != null) triggerText.raycastTarget = true;
    }

    public void ConfigureContent(string nextContent)
    {
        content = nextContent ?? string.Empty;
        RefreshUnderline();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Show();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Hide();
    }

    public void Show()
    {
        if (usesSharedView || tooltipPanel == null || tooltipText == null)
        {
            tooltipPanel = sharedPanel;
            tooltipText = sharedText;
            usesSharedView = true;
        }
        if (tooltipPanel == null || tooltipText == null)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(content))
        {
            Hide();
            return;
        }

        if (showOnlyWhenTextIsTruncated)
        {
            if (triggerText == null) triggerText = GetComponent<TMP_Text>();
            if (triggerText == null) return;
            Canvas.ForceUpdateCanvases();
            triggerText.ForceMeshUpdate(true, true);
            if (!triggerText.isTextOverflowing) return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        Canvas rootCanvas = canvas != null ? canvas.rootCanvas : null;
        RectTransform triggerRect = transform as RectTransform;
        if (rootCanvas != null)
        {
            tooltipPanel.SetParent(rootCanvas.transform, true);
        }

        tooltipPanel.gameObject.SetActive(true);
        tooltipPanel.SetAsLastSibling();
        PrepareView(tooltipPanel, tooltipText);

        tooltipText.text = content;
        tooltipText.fontSize = fontSize;
        tooltipText.color = new Color(0.08f, 0.08f, 0.08f, 1f);
        tooltipText.alignment = TextAlignmentOptions.TopLeft;
        tooltipText.textWrappingMode = TextWrappingModes.Normal;
        tooltipText.overflowMode = TextOverflowModes.Overflow;
        tooltipText.enabled = true;
        Canvas.ForceUpdateCanvases();
        tooltipText.ForceMeshUpdate(true, true);

        float textWidth = Mathf.Min(
            tooltipText.GetPreferredValues(content).x,
            maximumWidth - padding.x);
        Vector2 preferred = tooltipText.GetPreferredValues(
            content, textWidth, Mathf.Infinity);

        tooltipPanel.sizeDelta = new Vector2(
            Mathf.Min(preferred.x + padding.x, maximumWidth),
            preferred.y + padding.y);
        RectTransform textRect = tooltipText.rectTransform;
        textRect.anchorMin = textRect.anchorMax = textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(
            Mathf.Max(1f, tooltipPanel.sizeDelta.x - padding.x),
            Mathf.Max(1f, tooltipPanel.sizeDelta.y - padding.y));

        if (rootCanvas != null && triggerRect != null)
        {
            tooltipPanel.pivot = new Vector2(0f, 1f);
            tooltipPanel.position = triggerRect.TransformPoint(
                new Vector3(
                    triggerRect.rect.xMin,
                    triggerRect.rect.yMin - 6f,
                    0f));
        }

        Canvas.ForceUpdateCanvases();
        KeepInsideCanvas(rootCanvas);
        tooltipText.ForceMeshUpdate(true, true);
    }

    public void Hide()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.gameObject.SetActive(false);
        }
    }

    private void KeepInsideCanvas(Canvas rootCanvas)
    {
        RectTransform canvasRect = rootCanvas != null
            ? rootCanvas.transform as RectTransform
            : null;
        if (canvasRect == null || tooltipPanel == null)
        {
            return;
        }

        Vector3[] panelCorners = new Vector3[4];
        Vector3[] canvasCorners = new Vector3[4];
        tooltipPanel.GetWorldCorners(panelCorners);
        canvasRect.GetWorldCorners(canvasCorners);

        Vector3 offset = Vector3.zero;
        if (panelCorners[2].x > canvasCorners[2].x)
        {
            offset.x = canvasCorners[2].x - panelCorners[2].x;
        }
        else if (panelCorners[0].x < canvasCorners[0].x)
        {
            offset.x = canvasCorners[0].x - panelCorners[0].x;
        }

        if (panelCorners[2].y > canvasCorners[2].y)
        {
            offset.y = canvasCorners[2].y - panelCorners[2].y;
        }
        else if (panelCorners[0].y < canvasCorners[0].y)
        {
            offset.y = canvasCorners[0].y - panelCorners[0].y;
        }

        tooltipPanel.position += offset;
    }
}
