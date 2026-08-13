using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class StatusEntryView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private HoverTooltip hoverTooltip;
    [SerializeField] private TMP_Text[] gradeValueTexts;
    [SerializeField] private GameObject gradeSlotsRoot;

    public void SetContent(Sprite sprite, Color color, string title,
        string value, string description)
    {
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.color = sprite != null ? color : new Color(0f, 0f, 0f, 0f);
            icon.preserveAspect = true;
        }

        if (nameText != null) nameText.text = title;
        if (valueText != null) valueText.text = value;
        if (descriptionText != null) descriptionText.text = description;
        if (hoverTooltip != null) hoverTooltip.ConfigureContent(description);
    }

    public void SetBallContent(Sprite sprite, Color color, string title,
        int[] gradeCounts, string description)
    {
        SetContent(sprite, color, title, string.Empty, description);
        if (gradeSlotsRoot != null) gradeSlotsRoot.SetActive(true);
        if (valueText != null) valueText.gameObject.SetActive(false);
        for (int i = 0; i < gradeValueTexts.Length; i++)
        {
            int count = gradeCounts != null && i < gradeCounts.Length ? gradeCounts[i] : 0;
            if (gradeValueTexts[i] != null)
                gradeValueTexts[i].text = count > 0 ? count.ToString() : string.Empty;
        }
    }

    public void ConfigureTooltip(RectTransform panel, TMP_Text text,
        RectTransform underline)
    {
        if (hoverTooltip == null && nameText != null)
            hoverTooltip = nameText.GetComponent<HoverTooltip>();
        hoverTooltip?.Configure(string.Empty, panel, text, underline);
    }
}
