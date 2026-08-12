using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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
    [SerializeField, Min(0f)] private float underlineGap = 3f;
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

        Vector2 preferred = triggerText.GetPreferredValues(triggerText.text);
        underline.sizeDelta = new Vector2(Mathf.Max(preferred.x, 8f), 2f);
        underline.anchoredPosition = new Vector2(
            0f,
            -(preferred.y * 0.5f + underlineGap));
        underline.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        Hide();
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

        if (rootCanvas != null && triggerRect != null)
        {
            tooltipPanel.position = triggerRect.TransformPoint(
                new Vector3(
                    triggerRect.rect.xMax + 8f,
                    triggerRect.rect.yMax,
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
