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

    private Vector2 pressScreenPosition;
    private Vector2 pressLocalPosition;
    private bool isPressed;
    private bool isDragging;

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

        isPressed = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport,
            eventData.position,
            eventData.pressEventCamera,
            out pressLocalPosition);
        pressScreenPosition = eventData.position;
        isDragging = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isPressed || viewport == null || selectionRectangle == null)
        {
            return;
        }

        if (!isDragging &&
            Vector2.Distance(pressScreenPosition, eventData.position) <
            dragThreshold)
        {
            return;
        }

        isDragging = true;
        selectionRectangle.gameObject.SetActive(true);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 currentLocalPosition);

        Vector2 minimum = Vector2.Min(pressLocalPosition, currentLocalPosition);
        Vector2 maximum = Vector2.Max(pressLocalPosition, currentLocalPosition);
        selectionRectangle.anchoredPosition = (minimum + maximum) * 0.5f;
        selectionRectangle.sizeDelta = maximum - minimum;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed || eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        bool additive = Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift) ||
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);

        if (isDragging)
        {
            Rect screenRect = Rect.MinMaxRect(
                Mathf.Min(pressScreenPosition.x, eventData.position.x),
                Mathf.Min(pressScreenPosition.y, eventData.position.y),
                Mathf.Max(pressScreenPosition.x, eventData.position.x),
                Mathf.Max(pressScreenPosition.y, eventData.position.y));
            selectionPanel?.SelectBallsInScreenRect(
                screenRect, eventData.pressEventCamera, additive);
        }
        else
        {
            selectionPanel?.ToggleBallAtScreenPosition(
                eventData.position, eventData.pressEventCamera);
        }

        isPressed = false;
        isDragging = false;
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
