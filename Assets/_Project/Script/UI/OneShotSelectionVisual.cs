using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class OneShotSelectionVisual : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)]
    private float selectedDarkenAmount = 0.18f;

    [SerializeField]
    private Color selectedOutlineColor = Color.black;

    [SerializeField]
    private Vector2 selectedOutlineDistance = new Vector2(4f, -4f);

    private Button button;
    private Graphic targetGraphic;
    private Color originalColor;
    private Selectable.Transition originalTransition;
    private Outline selectionOutline;
    private bool initialized;

    private void Awake()
    {
        Initialize();
    }

    public void ResetVisual()
    {
        Initialize();

        if (button != null)
            button.transition = originalTransition;
        if (targetGraphic != null)
        {
            targetGraphic.color = originalColor;
            targetGraphic.canvasRenderer.SetColor(originalColor);
        }
        if (selectionOutline != null)
            selectionOutline.enabled = false;
    }

    public void ShowResult(bool isSelected)
    {
        Initialize();
        Color currentCardColor = targetGraphic != null
            ? targetGraphic.color
            : originalColor;

        if (button != null)
        {
            button.interactable = false;
            button.transition = Selectable.Transition.None;
        }

        if (targetGraphic != null)
        {
            originalColor = currentCardColor;
            Color displayedColor = isSelected
                ? Color.Lerp(originalColor, Color.black, selectedDarkenAmount)
                : originalColor;
            targetGraphic.color = displayedColor;
            targetGraphic.canvasRenderer.SetColor(displayedColor);
        }

        if (selectionOutline != null)
            selectionOutline.enabled = isSelected;
    }

    private void Initialize()
    {
        if (initialized)
            return;

        button = GetComponent<Button>();
        targetGraphic = button != null ? button.targetGraphic : null;
        originalTransition = button != null
            ? button.transition
            : Selectable.Transition.ColorTint;
        originalColor = targetGraphic != null
            ? targetGraphic.color
            : Color.white;
        selectionOutline = GetComponent<Outline>();
        if (selectionOutline == null)
            selectionOutline = gameObject.AddComponent<Outline>();
        selectionOutline.effectColor = selectedOutlineColor;
        selectionOutline.effectDistance = selectedOutlineDistance;
        selectionOutline.useGraphicAlpha = false;
        selectionOutline.enabled = false;
        initialized = true;
    }
}
