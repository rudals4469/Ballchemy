using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class OneShotSelectionVisual : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)]
    private float selectedDarkenAmount = 0.28f;

    private Button button;
    private Graphic targetGraphic;
    private Color originalColor;
    private Selectable.Transition originalTransition;
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
            targetGraphic.color = originalColor;
    }

    public void ShowResult(bool isSelected)
    {
        Initialize();

        if (button != null)
        {
            button.interactable = false;
            button.transition = Selectable.Transition.None;
        }

        if (targetGraphic != null)
        {
            targetGraphic.color = isSelected
                ? Color.Lerp(originalColor, Color.black, selectedDarkenAmount)
                : originalColor;
        }
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
        initialized = true;
    }
}
