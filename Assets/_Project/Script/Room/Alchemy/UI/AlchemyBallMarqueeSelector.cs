using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AlchemyBallMarqueeSelector : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler,
    IScrollHandler
{
    [SerializeField] private AlchemyBallSelectionPanel selectionPanel;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private RectTransform selectionRectangle;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField, Min(0f)] private float dragThreshold = 8f;
    [SerializeField, Min(1f)] private float autoScrollEdgeSize = 70f;
    [SerializeField, Min(0f)] private float autoScrollSpeed = 2.5f;

    private Vector2 pressScreenPosition;
    private Vector2 pressLocalPosition;
    private Vector2 pressContentPosition;
    private bool isPressed;
    private bool isDragging;
    private Vector2 currentScreenPosition;
    private Camera eventCamera;
    private Rect currentContentSelectionRect;
    private readonly HashSet<Ball> draggedBalls = new HashSet<Ball>();

    private void Awake()
    {
        HideSelectionRectangle();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left ||
            viewport == null)
        {
            return;
        }

        BeginPointer(eventData.position, eventData.pressEventCamera);
    }

    private void BeginPointer(Vector2 position, Camera pointerCamera)
    {
        isPressed = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport,
            position,
            pointerCamera,
            out pressLocalPosition);
        pressScreenPosition = position;
        currentScreenPosition = position;
        eventCamera = pointerCamera;
        selectionPanel?.TryScreenToContentPosition(
            position, pointerCamera, out pressContentPosition);
        draggedBalls.Clear();
        isDragging = false;

        bool additive = Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift) ||
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);
        if (!additive)
        {
            selectionPanel?.SetSelectedAreaVisible(false);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isPressed || viewport == null || selectionRectangle == null)
        {
            return;
        }

        ContinuePointer(eventData.position, eventData.pressEventCamera);
    }

    private void ContinuePointer(Vector2 position, Camera pointerCamera)
    {
        if (!isPressed || viewport == null || selectionRectangle == null)
        {
            return;
        }

        if (!isDragging &&
            Vector2.Distance(pressScreenPosition, position) <
            dragThreshold)
        {
            return;
        }

        isDragging = true;
        selectionRectangle.gameObject.SetActive(true);
        selectionRectangle.SetAsFirstSibling();

        currentScreenPosition = position;
        eventCamera = pointerCamera;
        RefreshMarquee();
    }

    private void Update()
    {
        if (!isDragging || viewport == null || scrollRect == null)
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewport, currentScreenPosition, eventCamera,
                out Vector2 localPointer))
        {
            return;
        }

        Rect viewportRect = viewport.rect;
        float direction = 0f;
        float strength = 0f;
        if (localPointer.y <= viewportRect.yMin + autoScrollEdgeSize)
        {
            direction = -1f;
            strength = Mathf.Clamp01(
                (viewportRect.yMin + autoScrollEdgeSize - localPointer.y) /
                autoScrollEdgeSize);
        }
        else if (localPointer.y >= viewportRect.yMax - autoScrollEdgeSize)
        {
            direction = 1f;
            strength = Mathf.Clamp01(
                (localPointer.y - viewportRect.yMax + autoScrollEdgeSize) /
                autoScrollEdgeSize);
        }

        if (Mathf.Approximately(direction, 0f))
        {
            return;
        }

        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            scrollRect.verticalNormalizedPosition +
            direction * autoScrollSpeed *
            Mathf.Lerp(0.45f, 1f, strength) * Time.unscaledDeltaTime);
        RefreshMarquee();
    }

    private void RefreshMarquee()
    {
        if (selectionPanel != null &&
            selectionPanel.TryScreenToContentPosition(
                currentScreenPosition,
                eventCamera,
                out Vector2 currentContentPosition))
        {
            currentContentSelectionRect = Rect.MinMaxRect(
                Mathf.Min(pressContentPosition.x, currentContentPosition.x),
                Mathf.Min(pressContentPosition.y, currentContentPosition.y),
                Mathf.Max(pressContentPosition.x, currentContentPosition.x),
                Mathf.Max(pressContentPosition.y, currentContentPosition.y));
            selectionPanel.CollectBallsInContentRect(
                currentContentSelectionRect, draggedBalls);
        }

        Vector2 minimum;
        Vector2 maximum;
        if (selectionPanel != null &&
            selectionPanel.TryGetBallBounds(
                draggedBalls,
                eventCamera,
                out Rect includedBounds))
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewport,
                includedBounds.min,
                eventCamera,
                out minimum);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewport,
                includedBounds.max,
                eventCamera,
                out maximum);
        }
        else
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewport,
                currentScreenPosition,
                eventCamera,
                out Vector2 currentLocalPosition);
            minimum = Vector2.Min(pressLocalPosition, currentLocalPosition);
            maximum = Vector2.Max(pressLocalPosition, currentLocalPosition);
        }

        selectionRectangle.anchoredPosition = (minimum + maximum) * 0.5f;
        selectionRectangle.sizeDelta = maximum - minimum + Vector2.one * 6f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        EndPointer(eventData.position, eventData.pressEventCamera);
    }

    private void EndPointer(Vector2 position, Camera pointerCamera)
    {
        bool additive = Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift) ||
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);

        if (isDragging)
        {
            selectionPanel?.SelectBalls(draggedBalls, additive);
        }
        else
        {
            selectionPanel?.ToggleBallAtScreenPosition(
                position, pointerCamera);
        }

        isPressed = false;
        isDragging = false;
        draggedBalls.Clear();
        HideSelectionRectangle();
    }

    public void OnScroll(PointerEventData eventData)
    {
        scrollRect?.OnScroll(eventData);
    }

    private void OnDisable()
    {
        isPressed = false;
        isDragging = false;
        draggedBalls.Clear();
        HideSelectionRectangle();
    }

    private void HideSelectionRectangle()
    {
        if (selectionRectangle != null)
        {
            selectionRectangle.gameObject.SetActive(false);
        }
    }
}
