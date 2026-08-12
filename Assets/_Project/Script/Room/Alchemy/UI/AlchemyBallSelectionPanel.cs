using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AlchemyBallSelectionPanel : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private RectTransform content;
    [SerializeField] private TMP_Text selectedCountText;

    private readonly List<GameObject> itemObjects =
        new List<GameObject>();
    private AlchemyBallSelectionModel model;

    public void Initialize(AlchemyBallSelectionModel selectionModel)
    {
        if (model != null)
        {
            model.Changed -= Refresh;
        }

        model = selectionModel;

        if (content == null || selectedCountText == null)
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

        if (model == null)
        {
            SetSelectedCount(0);
            return;
        }

        IReadOnlyList<AlchemyBallSelectionEntry> entries = model.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            CreateItem(entries[i], i);
        }

        SetSelectedCount(model.SelectedCount);
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

        Image hitArea = item.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear;

        Button button = item.gameObject.AddComponent<Button>();
        button.targetGraphic = hitArea;
        Ball selectedBall = entry.Ball;
        button.onClick.AddListener(() => model?.Toggle(selectedBall));

        RectTransform ring = CreateRect("SelectionRing", item);
        SetTopCentered(ring, new Vector2(0f, -3f), new Vector2(56f, 56f));
        Image ringImage = ring.gameObject.AddComponent<Image>();
        ringImage.sprite = GetFallbackSprite();
        ringImage.color = entry.IsSelected
            ? new Color(0.35f, 0.95f, 1f, 0.42f)
            : Color.clear;
        ringImage.raycastTarget = false;

        RectTransform icon = CreateRect("BallIcon", item);
        SetTopCentered(icon, new Vector2(0f, -7f), new Vector2(48f, 48f));
        Image iconImage = icon.gameObject.AddComponent<Image>();
        iconImage.sprite = entry.Definition.Sprite != null
            ? entry.Definition.Sprite
            : GetFallbackSprite();
        iconImage.color = entry.Definition.Color;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        int grade = Mathf.Clamp((int)entry.Definition.StarGrade, 1, 3);
        RectTransform starRect = CreateRect("StarText", item);
        starRect.anchorMin = new Vector2(0f, 0f);
        starRect.anchorMax = new Vector2(1f, 0f);
        starRect.pivot = new Vector2(0.5f, 0f);
        starRect.anchoredPosition = new Vector2(0f, 3f);
        starRect.sizeDelta = new Vector2(0f, 22f);
        TextMeshProUGUI starText =
            starRect.gameObject.AddComponent<TextMeshProUGUI>();
        starText.text = new string('\u2605', grade);
        starText.fontSize = 16f;
        starText.alignment = TextAlignmentOptions.Center;
        starText.color = new Color(1f, 0.84f, 0.25f, 1f);
        starText.raycastTarget = false;
    }

    private void SetSelectedCount(int count)
    {
        if (selectedCountText != null)
        {
            selectedCountText.text = $"선택 {Mathf.Max(count, 0)}개";
        }
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
