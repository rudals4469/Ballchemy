using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

    public void PulseBallCountChanges(int[] previousCounts, int[] nextCounts)
    {
        bool anyChange = false;
        int previousTotal = 0;
        int nextTotal = 0;
        for (int i = 0; i < gradeValueTexts.Length; i++)
        {
            int previous = previousCounts != null && i < previousCounts.Length ? previousCounts[i] : 0;
            int next = nextCounts != null && i < nextCounts.Length ? nextCounts[i] : 0;
            previousTotal += previous;
            nextTotal += next;
            if (previous == next || gradeValueTexts[i] == null) continue;
            anyChange = true;
            StartCoroutine(PulseRoutine(gradeValueTexts[i].rectTransform, next > previous ? 1.28f : 0.78f));
        }
        if (anyChange && nameText != null)
            StartCoroutine(PulseRoutine(nameText.rectTransform, nextTotal >= previousTotal ? 1.1f : 0.9f));
    }

    private static IEnumerator PulseRoutine(RectTransform target, float peakScale)
    {
        if (target == null) yield break;
        const float halfDuration = 0.11f;
        for (int phase = 0; phase < 2; phase++)
        {
            float elapsed = 0f;
            float from = phase == 0 ? 1f : peakScale;
            float to = phase == 0 ? peakScale : 1f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / halfDuration));
                target.localScale = Vector3.one * Mathf.LerpUnclamped(from, to, t);
                yield return null;
            }
        }
        target.localScale = Vector3.one;
    }
}
