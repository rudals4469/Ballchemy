using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AlchemyBallSelectionPanel : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform selectedAreaRectangle;
    [FormerlySerializedAs("selectedCountText")]
    [SerializeField] private TMP_Text stabilityValueText;

    [Header("Stability Preview")]
    [SerializeField, Min(0)] private int previewStability = 100;

    private readonly List<GameObject> itemObjects = new List<GameObject>();
    private readonly List<RectTransform> itemRects = new List<RectTransform>();
    private readonly List<Ball> itemBalls = new List<Ball>();
    private readonly List<Image> itemIcons = new List<Image>();
    private readonly List<TMP_Text> itemGradeTexts = new List<TMP_Text>();
    private readonly List<GameObject> resultTextObjects = new List<GameObject>();
    private AlchemyBallSelectionModel model;

    public event Action SelectionCommitted;

    public void Initialize(AlchemyBallSelectionModel selectionModel)
    {
        if (model != null)
        {
            model.Changed -= Refresh;
        }

        model = selectionModel;

        if (content == null || stabilityValueText == null)
        {
            Debug.LogError(
                "AlchemyBallSelectionPanel: Scene UI references are missing.",
                this);
            return;
        }

        if (model != null)
        {
            model.Changed -= Refresh;
            model.Changed += Refresh;
        }

        Refresh();
    }

    public void SetStability(int value)
    {
        previewStability = Mathf.Max(value, 0);
        RefreshStability();
    }

    public void SetSelectedAreaVisible(bool visible)
    {
        if (selectedAreaRectangle != null)
        {
            selectedAreaRectangle.gameObject.SetActive(visible);
        }
    }

    private void Refresh()
    {
        if (content == null)
        {
            return;
        }

        for (int i = 0; i < itemObjects.Count; i++)
        {
            if (itemObjects[i] != null)
            {
                // Destroy는 프레임 끝에 처리되므로 먼저 비활성화해
                // 새 항목의 Layout 계산에 이전 항목이 포함되지 않게 합니다.
                itemObjects[i].SetActive(false);
                Destroy(itemObjects[i]);
            }
        }
        itemObjects.Clear();
        itemRects.Clear();
        itemBalls.Clear();
        itemIcons.Clear();
        itemGradeTexts.Clear();

        if (model != null)
        {
            IReadOnlyList<AlchemyBallSelectionEntry> entries = model.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                CreateItem(entries[i], i);
            }
        }

        RefreshStability();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        RefreshSelectedArea();
    }

    private void CreateItem(AlchemyBallSelectionEntry entry, int index)
    {
        if (entry == null || entry.Definition == null)
        {
            return;
        }

        RectTransform item = CreateRect($"BallItem_{index:000}", content);
        itemObjects.Add(item.gameObject);
        itemRects.Add(item);
        itemBalls.Add(entry.Ball);

        Image hitArea = item.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear;
        hitArea.raycastTarget = false;

        RectTransform icon = CreateRect("BallIcon", item);
        SetTopCentered(icon, new Vector2(0f, -14f), new Vector2(24f, 24f));
        Image iconImage = icon.gameObject.AddComponent<Image>();
        SpriteRenderer ballRenderer = entry.Ball != null
            ? entry.Ball.GetComponentInChildren<SpriteRenderer>(true)
            : null;
        iconImage.sprite = entry.Definition.Sprite != null
            ? entry.Definition.Sprite
            : ballRenderer != null
                ? ballRenderer.sprite
                : null;
        iconImage.color = entry.Definition.Color;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.enabled = iconImage.sprite != null;
        itemIcons.Add(iconImage);

        int grade = Mathf.Clamp((int)entry.Definition.StarGrade, 1, 3);
        RectTransform starRect = CreateRect("StarText", item);
        starRect.anchorMin = new Vector2(0f, 0f);
        starRect.anchorMax = new Vector2(1f, 0f);
        starRect.pivot = new Vector2(0.5f, 0f);
        starRect.anchoredPosition = new Vector2(0f, 3f);
        starRect.sizeDelta = new Vector2(0f, 22f);
        TextMeshProUGUI starText =
            starRect.gameObject.AddComponent<TextMeshProUGUI>();
        starText.text = grade.ToString();
        starText.fontSize = 16f;
        starText.alignment = TextAlignmentOptions.Center;
        starText.color = new Color(1f, 0.84f, 0.25f, 1f);
        starText.raycastTarget = false;
        itemGradeTexts.Add(starText);
    }

    private void RefreshStability()
    {
        if (stabilityValueText != null)
        {
            stabilityValueText.text = Mathf.Max(previewStability, 0).ToString();
        }
    }

    private void RefreshSelectedArea()
    {
        if (selectedAreaRectangle == null || content == null)
        {
            return;
        }

        bool hasSelection = false;
        Vector2 minimum = Vector2.zero;
        Vector2 maximum = Vector2.zero;
        Vector3[] corners = new Vector3[4];

        for (int i = 0; i < itemRects.Count; i++)
        {
            if (model == null ||
                i >= model.Entries.Count ||
                !model.Entries[i].IsSelected)
            {
                continue;
            }

            itemRects[i].GetWorldCorners(corners);
            Vector2 bottomLeft = content.InverseTransformPoint(corners[0]);
            Vector2 topRight = content.InverseTransformPoint(corners[2]);

            if (!hasSelection)
            {
                minimum = bottomLeft;
                maximum = topRight;
                hasSelection = true;
            }
            else
            {
                minimum = Vector2.Min(minimum, bottomLeft);
                maximum = Vector2.Max(maximum, topRight);
            }
        }

        selectedAreaRectangle.gameObject.SetActive(hasSelection);
        if (!hasSelection)
        {
            return;
        }

        const float padding = 3f;
        selectedAreaRectangle.localPosition =
            (minimum + maximum) * 0.5f;
        selectedAreaRectangle.sizeDelta =
            maximum - minimum + Vector2.one * (padding * 2f);
        selectedAreaRectangle.SetAsFirstSibling();
    }

    public void ToggleBallAtScreenPosition(
        Vector2 screenPosition,
        Camera eventCamera)
    {
        for (int i = itemRects.Count - 1; i >= 0; i--)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                    itemRects[i], screenPosition, eventCamera))
            {
                model?.Toggle(itemBalls[i]);
                SelectionCommitted?.Invoke();
                return;
            }
        }

        model?.ClearSelection();
    }

    public void SelectBallsInScreenRect(
        Rect screenRect,
        Camera eventCamera,
        bool additive)
    {
        List<Ball> balls = new List<Ball>();
        Vector3[] corners = new Vector3[4];

        for (int i = 0; i < itemRects.Count; i++)
        {
            RectTransform itemRect = itemRects[i];
            itemRect.GetWorldCorners(corners);
            Vector2 minimum = RectTransformUtility.WorldToScreenPoint(
                eventCamera, corners[0]);
            Vector2 maximum = RectTransformUtility.WorldToScreenPoint(
                eventCamera, corners[2]);
            Rect itemScreenRect = Rect.MinMaxRect(
                minimum.x, minimum.y, maximum.x, maximum.y);

            if (screenRect.Overlaps(itemScreenRect, true))
            {
                balls.Add(itemBalls[i]);
            }
        }

        model?.SelectRange(balls, additive);
    }

    public void CollectBallsInScreenRect(
        Rect screenRect,
        Camera eventCamera,
        ISet<Ball> result)
    {
        if (result == null)
        {
            return;
        }

        Vector3[] corners = new Vector3[4];
        for (int i = 0; i < itemRects.Count; i++)
        {
            itemRects[i].GetWorldCorners(corners);
            Vector2 minimum = RectTransformUtility.WorldToScreenPoint(
                eventCamera, corners[0]);
            Vector2 maximum = RectTransformUtility.WorldToScreenPoint(
                eventCamera, corners[2]);
            Rect itemScreenRect = Rect.MinMaxRect(
                minimum.x, minimum.y, maximum.x, maximum.y);

            if (screenRect.Overlaps(itemScreenRect, true))
            {
                result.Add(itemBalls[i]);
            }
        }
    }

    public bool TryScreenToContentPosition(
        Vector2 screenPosition,
        Camera eventCamera,
        out Vector2 contentPosition)
    {
        if (content == null)
        {
            contentPosition = default;
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            content, screenPosition, eventCamera, out contentPosition);
    }

    public void CollectBallsInContentRect(
        Rect contentRect,
        ISet<Ball> result)
    {
        if (result == null)
        {
            return;
        }

        result.Clear();
        Vector3[] corners = new Vector3[4];
        for (int i = 0; i < itemRects.Count; i++)
        {
            itemRects[i].GetWorldCorners(corners);
            Vector2 minimum = content.InverseTransformPoint(corners[0]);
            Vector2 maximum = content.InverseTransformPoint(corners[2]);
            Rect itemContentRect = Rect.MinMaxRect(
                minimum.x, minimum.y, maximum.x, maximum.y);

            if (contentRect.Overlaps(itemContentRect, true))
            {
                result.Add(itemBalls[i]);
            }
        }
    }

    public void SelectBalls(IEnumerable<Ball> balls, bool additive)
    {
        model?.SelectRange(balls, additive);
        SelectionCommitted?.Invoke();
    }

    public void RefreshBallVisual(Ball ball)
    {
        int index = itemBalls.IndexOf(ball);
        if (index < 0 ||
            index >= itemRects.Count ||
            index >= itemIcons.Count ||
            itemRects[index] == null ||
            itemIcons[index] == null)
        {
            return;
        }

        Image icon = itemIcons[index];
        BallDefinition definition = ball != null ? ball.Definition : null;
        if (definition != null)
        {
            SpriteRenderer ballRenderer = ball != null
                ? ball.GetComponentInChildren<SpriteRenderer>(true)
                : null;
            icon.sprite = definition.Sprite != null
                ? definition.Sprite
                : ballRenderer != null
                    ? ballRenderer.sprite
                    : icon.sprite;
            icon.color = definition.Color;
            icon.enabled = icon.sprite != null;

            if (index < itemGradeTexts.Count && itemGradeTexts[index] != null)
            {
                itemGradeTexts[index].text = Mathf.Clamp(
                    (int)definition.StarGrade, 1, 3).ToString();
            }
        }
    }

    public void PlaySuccessFeedback(
        Ball ball,
        float animationDuration,
        float textDuration)
    {
        int index = itemBalls.IndexOf(ball);
        if (index < 0 ||
            index >= itemRects.Count ||
            index >= itemIcons.Count ||
            itemRects[index] == null ||
            itemIcons[index] == null)
        {
            return;
        }

        RefreshBallVisual(ball);

        StartCoroutine(ConversionPulseRoutine(
            itemRects[index], itemIcons[index], animationDuration));
        PlayResultText(
            ball,
            "성공!",
            new Color(0.55f, 1f, 0.68f, 1f),
            textDuration);
    }

    public void PlayResultText(
        Ball ball,
        string message,
        Color color,
        float duration)
    {
        int index = itemBalls.IndexOf(ball);
        if (index < 0 || index >= itemRects.Count || itemRects[index] == null)
        {
            return;
        }

        StartCoroutine(ResultTextRoutine(
            itemRects[index], message, color, duration));
    }

    public void PlayDestructionPulse(Ball ball, float duration)
    {
        int index = itemBalls.IndexOf(ball);
        if (index < 0 ||
            index >= itemRects.Count ||
            index >= itemIcons.Count ||
            itemRects[index] == null ||
            itemIcons[index] == null)
        {
            return;
        }

        StartCoroutine(DestructionPulseRoutine(
            itemRects[index], itemIcons[index], duration));
    }

    private static IEnumerator ConversionPulseRoutine(
        RectTransform item,
        Graphic icon,
        float duration)
    {
        duration = Mathf.Max(duration, 0.01f);
        float elapsed = 0f;
        item.localScale = Vector3.one * 0.72f;
        Color targetColor = icon != null ? icon.color : Color.white;
        if (icon != null)
        {
            icon.color = new Color(
                targetColor.r, targetColor.g, targetColor.b, 0.25f);
        }

        while (item != null && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            float scale = Mathf.LerpUnclamped(0.72f, 1f, eased);
            item.localScale = Vector3.one * scale;

            if (icon != null)
            {
                icon.color = new Color(
                    targetColor.r,
                    targetColor.g,
                    targetColor.b,
                    Mathf.Lerp(0.25f, targetColor.a, eased));
            }
            yield return null;
        }

        if (item != null)
        {
            item.localScale = Vector3.one;
        }

        if (icon != null)
        {
            icon.color = targetColor;
        }
    }

    private static IEnumerator DestructionPulseRoutine(
        RectTransform item,
        Graphic icon,
        float duration)
    {
        duration = Mathf.Max(duration, 0.01f);
        float elapsed = 0f;
        Color startColor = icon.color;

        while (item != null && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(
                0f, 1f, Mathf.Clamp01(elapsed / duration));
            item.localScale = Vector3.one * Mathf.Lerp(1f, 0.15f, progress);

            if (icon != null)
            {
                icon.color = Color.Lerp(
                    startColor,
                    new Color(1f, 0.2f, 0.25f, 0f),
                    progress);
            }

            yield return null;
        }
    }

    private IEnumerator ResultTextRoutine(
        RectTransform item,
        string message,
        Color textColor,
        float duration)
    {
        RectTransform textParent = item.parent as RectTransform;
        if (textParent == null)
        {
            yield break;
        }

        RectTransform textRect = CreateRect("AlchemyResultText", textParent);
        resultTextObjects.Add(textRect.gameObject);
        LayoutElement layoutElement =
            textRect.gameObject.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;
        textRect.anchorMin = item.anchorMin;
        textRect.anchorMax = item.anchorMax;
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.anchoredPosition = item.anchoredPosition + new Vector2(8f, 28f);
        textRect.sizeDelta = new Vector2(58f, 24f);
        textRect.SetAsLastSibling();

        TextMeshProUGUI successText =
            textRect.gameObject.AddComponent<TextMeshProUGUI>();
        successText.text = message;
        successText.font = stabilityValueText != null
            ? stabilityValueText.font
            : null;
        successText.fontSize = 14f;
        successText.fontStyle = FontStyles.Bold;
        successText.alignment = TextAlignmentOptions.Bottom;
        successText.color = textColor;
        successText.raycastTarget = false;

        duration = Mathf.Max(duration, 0.01f);
        float elapsed = 0f;
        Vector2 startPosition = textRect.anchoredPosition;
        Color startColor = successText.color;

        while (textRect != null && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, progress);
            textRect.anchoredPosition = startPosition + Vector2.up * (8f * eased);
            float alpha = progress < 0.55f
                ? 1f
                : 1f - Mathf.InverseLerp(0.55f, 1f, progress);
            successText.color = new Color(
                startColor.r,
                startColor.g,
                startColor.b,
                alpha);
            yield return null;
        }

        if (textRect != null)
        {
            resultTextObjects.Remove(textRect.gameObject);
            Destroy(textRect.gameObject);
        }
    }

    public bool TryGetIncludedBallBounds(
        Rect selectionScreenRect,
        Camera eventCamera,
        out Rect includedBounds)
    {
        bool hasIncludedBall = false;
        includedBounds = default;
        Vector3[] corners = new Vector3[4];

        for (int i = 0; i < itemRects.Count; i++)
        {
            itemRects[i].GetWorldCorners(corners);
            Vector2 minimum = RectTransformUtility.WorldToScreenPoint(
                eventCamera, corners[0]);
            Vector2 maximum = RectTransformUtility.WorldToScreenPoint(
                eventCamera, corners[2]);
            Rect itemBounds = Rect.MinMaxRect(
                minimum.x, minimum.y, maximum.x, maximum.y);

            if (!selectionScreenRect.Overlaps(itemBounds, true))
            {
                continue;
            }

            if (!hasIncludedBall)
            {
                includedBounds = itemBounds;
                hasIncludedBall = true;
            }
            else
            {
                includedBounds = Rect.MinMaxRect(
                    Mathf.Min(includedBounds.xMin, itemBounds.xMin),
                    Mathf.Min(includedBounds.yMin, itemBounds.yMin),
                    Mathf.Max(includedBounds.xMax, itemBounds.xMax),
                    Mathf.Max(includedBounds.yMax, itemBounds.yMax));
            }
        }

        return hasIncludedBall;
    }

    public bool TryGetBallBounds(
        IEnumerable<Ball> balls,
        Camera eventCamera,
        out Rect bounds)
    {
        bounds = default;
        if (balls == null)
        {
            return false;
        }

        HashSet<Ball> targets = new HashSet<Ball>(balls);
        bool hasBall = false;
        Vector3[] corners = new Vector3[4];

        for (int i = 0; i < itemRects.Count; i++)
        {
            if (!targets.Contains(itemBalls[i]))
            {
                continue;
            }

            itemRects[i].GetWorldCorners(corners);
            Vector2 minimum = RectTransformUtility.WorldToScreenPoint(
                eventCamera, corners[0]);
            Vector2 maximum = RectTransformUtility.WorldToScreenPoint(
                eventCamera, corners[2]);
            Rect itemBounds = Rect.MinMaxRect(
                minimum.x, minimum.y, maximum.x, maximum.y);

            if (!hasBall)
            {
                bounds = itemBounds;
                hasBall = true;
            }
            else
            {
                bounds = Rect.MinMaxRect(
                    Mathf.Min(bounds.xMin, itemBounds.xMin),
                    Mathf.Min(bounds.yMin, itemBounds.yMin),
                    Mathf.Max(bounds.xMax, itemBounds.xMax),
                    Mathf.Max(bounds.yMax, itemBounds.yMax));
            }
        }

        return hasBall;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private static void SetTopCentered(
        RectTransform rect,
        Vector2 position,
        Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void OnDestroy()
    {
        if (model != null)
        {
            model.Changed -= Refresh;
        }
    }

    private void OnDisable()
    {
        for (int i = resultTextObjects.Count - 1; i >= 0; i--)
        {
            if (resultTextObjects[i] != null)
            {
                Destroy(resultTextObjects[i]);
            }
        }

        resultTextObjects.Clear();
    }
}
