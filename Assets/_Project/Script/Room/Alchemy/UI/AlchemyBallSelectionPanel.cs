using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AlchemyBallSelectionPanel : MonoBehaviour
{
    private readonly List<GameObject> itemObjects =
        new List<GameObject>();

    private AlchemyBallSelectionModel model;
    private RectTransform content;
    private TMP_Text selectedCountText;
    private bool isBuilt;

    public void Initialize(AlchemyBallSelectionModel selectionModel)
    {
        if (model != null)
        {
            model.Changed -= Refresh;
        }

        model = selectionModel;
        BuildIfNeeded();

        if (model != null)
        {
            model.Changed -= Refresh;
            model.Changed += Refresh;
        }

        Refresh();
    }

    private void BuildIfNeeded()
    {
        if (isBuilt)
        {
            return;
        }

        isBuilt = true;

        RectTransform root = transform as RectTransform;

        if (root == null)
        {
            Debug.LogError(
                "AlchemyBallSelectionPanel: RectTransform이 필요합니다.",
                this);
            return;
        }

        Image background = root.GetComponent<Image>();
        if (background != null)
        {
            background.color = new Color(0.055f, 0.075f, 0.1f, 0.94f);
        }

        RectTransform title = CreateText(
            "Title",
            root,
            "보유 공",
            26f,
            TextAlignmentOptions.Left);
        SetAnchoredRect(title, 18f, -12f, -18f, 44f);

        RectTransform count = CreateText(
            "SelectedCountText",
            root,
            "선택 0개",
            22f,
            TextAlignmentOptions.Right);
        SetAnchoredRect(count, 18f, -12f, -18f, 44f);
        selectedCountText = count.GetComponent<TMP_Text>();

        RectTransform scrollRoot = CreateRect("BallScrollView", root);
        scrollRoot.anchorMin = Vector2.zero;
        scrollRoot.anchorMax = Vector2.one;
        scrollRoot.offsetMin = new Vector2(16f, 16f);
        scrollRoot.offsetMax = new Vector2(-16f, -62f);
        Image scrollBackground = scrollRoot.gameObject.AddComponent<Image>();
        scrollBackground.color = new Color(0.02f, 0.03f, 0.045f, 0.82f);

        ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 28f;

        RectTransform viewport = CreateRect("Viewport", scrollRoot);
        Stretch(viewport, 5f);
        Image viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = Color.white;
        viewportImage.raycastTarget = true;
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

        content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;

        GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(68f, 82f);
        grid.spacing = new Vector2(8f, 8f);
        grid.padding = new RectOffset(10, 10, 10, 10);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 6;
        grid.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter fitter =
            content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = content;
    }

    private void Refresh()
    {
        if (!isBuilt || content == null)
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
        hitArea.color = new Color(1f, 1f, 1f, 0f);

        Button button = item.gameObject.AddComponent<Button>();
        button.targetGraphic = hitArea;
        Ball selectedBall = entry.Ball;
        button.onClick.AddListener(() => model?.Toggle(selectedBall));

        RectTransform icon = CreateRect("BallIcon", item);
        icon.anchorMin = new Vector2(0.5f, 1f);
        icon.anchorMax = new Vector2(0.5f, 1f);
        icon.pivot = new Vector2(0.5f, 1f);
        icon.anchoredPosition = new Vector2(0f, -7f);
        icon.sizeDelta = new Vector2(48f, 48f);

        RectTransform selectionRing = CreateRect("SelectionRing", item);
        selectionRing.anchorMin = new Vector2(0.5f, 1f);
        selectionRing.anchorMax = new Vector2(0.5f, 1f);
        selectionRing.pivot = new Vector2(0.5f, 1f);
        selectionRing.anchoredPosition = new Vector2(0f, -3f);
        selectionRing.sizeDelta = new Vector2(56f, 56f);
        Image ringImage = selectionRing.gameObject.AddComponent<Image>();
        ringImage.sprite = Resources.GetBuiltinResource<Sprite>(
            "UI/Skin/Knob.psd");
        ringImage.color = entry.IsSelected
            ? new Color(0.35f, 0.95f, 1f, 0.42f)
            : Color.clear;
        ringImage.raycastTarget = false;

        selectionRing.SetAsFirstSibling();

        Image iconImage = icon.gameObject.AddComponent<Image>();
        iconImage.sprite = entry.Definition.Sprite != null
            ? entry.Definition.Sprite
            : Resources.GetBuiltinResource<Sprite>(
                "UI/Skin/Knob.psd");
        iconImage.color = entry.Definition.Color;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        string stars = new string('★', Mathf.Clamp(
            (int)entry.Definition.StarGrade, 1, 3));
        RectTransform starText = CreateText(
            "StarText", item, stars, 16f, TextAlignmentOptions.Center);
        starText.anchorMin = new Vector2(0f, 0f);
        starText.anchorMax = new Vector2(1f, 0f);
        starText.pivot = new Vector2(0.5f, 0f);
        starText.anchoredPosition = new Vector2(0f, 3f);
        starText.sizeDelta = new Vector2(0f, 22f);
        starText.GetComponent<TMP_Text>().color =
            new Color(1f, 0.84f, 0.25f, 1f);
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
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }

    private static RectTransform CreateText(
        string name,
        Transform parent,
        string value,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return rect;
    }

    private static void Stretch(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }

    private static void SetAnchoredRect(
        RectTransform rect,
        float left,
        float top,
        float right,
        float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(left, -top - height);
        rect.offsetMax = new Vector2(right, -top);
    }

    private void OnDestroy()
    {
        if (model != null)
        {
            model.Changed -= Refresh;
        }
    }
}
