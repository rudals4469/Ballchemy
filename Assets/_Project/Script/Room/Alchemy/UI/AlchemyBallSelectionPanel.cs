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
    [FormerlySerializedAs("selectedCountText")]
    [SerializeField] private TMP_Text stabilityValueText;

    [Header("Stability Preview")]
    [SerializeField, Min(0)] private int previewStability = 100;

    private readonly List<GameObject> itemObjects = new List<GameObject>();
    private readonly List<RectTransform> itemRects = new List<RectTransform>();
    private readonly List<Ball> itemBalls = new List<Ball>();
    private AlchemyBallSelectionModel model;

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
                Destroy(itemObjects[i]);
            }
        }
        itemObjects.Clear();
        itemRects.Clear();
        itemBalls.Clear();

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

        RectTransform ring = CreateRect("SelectionRing", item);
        SetTopCentered(ring, new Vector2(0f, -10f), new Vector2(32f, 32f));
        Image ringImage = ring.gameObject.AddComponent<Image>();
        ringImage.sprite = GetFallbackSprite();
        ringImage.color = entry.IsSelected
            ? new Color(0.35f, 0.95f, 1f, 0.42f)
            : Color.clear;
        ringImage.raycastTarget = false;

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
    }

    private void RefreshStability()
    {
        if (stabilityValueText != null)
        {
            stabilityValueText.text = Mathf.Max(previewStability, 0).ToString();
        }
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

    private static Sprite GetFallbackSprite()
    {
        return Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
    }

    private void OnDestroy()
    {
        if (model != null)
        {
            model.Changed -= Refresh;
        }
    }
}
