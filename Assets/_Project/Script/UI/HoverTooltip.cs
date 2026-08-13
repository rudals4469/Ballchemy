using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class HoverTooltip : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [TextArea(2, 8)]
    [SerializeField] private string content;
    [SerializeField] private RectTransform tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private RectTransform underline;
    [SerializeField, Min(0f)] private float underlineGap = 1f;
    [SerializeField] private Vector2 padding = new Vector2(24f, 18f);
    [SerializeField, Min(80f)] private float maximumWidth = 560f;
    [SerializeField, Min(8f)] private float fontSize = 20f;

    private TMP_Text triggerText;
    private Transform originalPanelParent;
    private int originalPanelSiblingIndex;

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

        triggerText.ForceMeshUpdate();
        Bounds bounds = triggerText.textBounds;
        float width = Mathf.Max(bounds.size.x, 8f);
        underline.sizeDelta = new Vector2(width, 2f);
        underline.anchoredPosition = new Vector2(
            bounds.center.x,
            bounds.min.y - underlineGap);
        underline.gameObject.SetActive(true);
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
        underline = underlineGraphic;
        triggerText = GetComponent<TMP_Text>();
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
        if (tooltipPanel == null || tooltipText == null)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(content))
        {
            Hide();
            return;
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
    }

    public void Hide()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.gameObject.SetActive(false);

            if (originalPanelParent != null &&
                tooltipPanel.parent != originalPanelParent)
            {
                tooltipPanel.SetParent(originalPanelParent, false);
                tooltipPanel.SetSiblingIndex(Mathf.Clamp(
                    originalPanelSiblingIndex,
                    0,
                    originalPanelParent.childCount - 1));
            }
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
