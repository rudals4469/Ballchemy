#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StageMissionPanelHierarchyInstaller
{
    private const string ScenePath = "Assets/_Project/Scene/SampleScene.unity";
    private const string VersionMarker = "StageMissionPanel_Hierarchy_V10";
    private static readonly Color Ink = new Color32(74, 42, 24, 255);

    static StageMissionPanelHierarchyInstaller() => EditorApplication.delayCall += InstallIfNeeded;

    [MenuItem("Ballchemy/UI/Rebuild Stage Mission Panel")]
    public static void InstallFromCommandLine() => Install(true);

    private static void InstallIfNeeded() => Install(false);

    private static void Install(bool force)
    {
        if (Application.isPlaying) return;
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isBatchMode && scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath);
        if (!scene.IsValid() || scene.path != ScenePath) return;
        if (!force && FindSceneObject(VersionMarker) != null) return;

        GameObject rightPanel = FindSceneObject("RightPanel");
        GameObject shopPanel = FindSceneObject("ShopPanel");
        StageMissionSystem system = Object.FindFirstObjectByType<StageMissionSystem>();
        if (rightPanel == null || shopPanel == null || system == null) return;

        DestroyNamed("StageMissionPanel");
        DestroyNamed("StageMissionTracker");
        DestroyNamed("StageMissionResultText");
        DestroyNamed("MissionStatusSection");
        DestroyMarkers();

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Art/Font/BMJUA_ttf SDF");
        Sprite panelSprite = shopPanel.GetComponent<Image>()?.sprite;
        Sprite missionIcon = Resources.Load<Sprite>("UI/MapIcons/Map_Event_Readable");
        Sprite statusParchment = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/_Project/Resources/UI/Parchment/Panel_Parchment_MissionClean.png");
        ApplyStatusPanelSprites();
        GameObject cardTemplate = FindSceneObject("RewardCard_01");
        if (cardTemplate == null) return;

        GameObject marker = Rect(VersionMarker, rightPanel.transform);
        marker.SetActive(false);

        GameObject panel = ImageObject("StageMissionPanel", rightPanel.transform, panelSprite, Color.white);
        CopyRect(shopPanel.GetComponent<RectTransform>(), panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().pixelsPerUnitMultiplier = 6f;

        TMP_Text title = Text("StageMissionTitle", panel.transform, "STAGE 1  미션 선택", font,
            22f, new Vector2(0f, 301f), new Vector2(405f, 28f), TextAlignmentOptions.Center);

        GameObject cardContainer = Rect("StageMissionCardContainer", panel.transform);
        SetRect(cardContainer.GetComponent<RectTransform>(), new Vector2(0f, -8f), new Vector2(435f, 578f));

        Button[] buttons = new Button[3];
        CommonChoiceCardLayout[] layouts = new CommonChoiceCardLayout[3];
        Image[] icons = new Image[3];
        GameObject[] iconRoots = new GameObject[3];
        TMP_Text[] titleTexts = new TMP_Text[3];
        TMP_Text[] rewardTexts = new TMP_Text[3];
        TMP_Text[] descriptionTexts = new TMP_Text[3];
        TMP_Text[] payoutTexts = new TMP_Text[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject card = Object.Instantiate(cardTemplate, cardContainer.transform);
            card.name = $"StageMissionCard0{i + 1}";
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0f, 1f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2(217.5f, -90f - i * 191f);
            cardRect.sizeDelta = new Vector2(405f, 170f);
            Image cardBackground = card.GetComponent<Image>();
            if (cardBackground != null && statusParchment != null)
            {
                cardBackground.sprite = statusParchment;
                cardBackground.type = Image.Type.Sliced;
                cardBackground.pixelsPerUnitMultiplier = 2f;
            }
            RewardCardUI rewardCard = card.GetComponent<RewardCardUI>();
            SerializedObject rewardSerialized = new(rewardCard);
            Button button = Reference<Button>(rewardSerialized, "selectButton");
            Image icon = Reference<Image>(rewardSerialized, "iconImage");
            GameObject iconRoot = Reference<GameObject>(rewardSerialized, "iconRoot");
            TMP_Text cardTitle = Reference<TMP_Text>(rewardSerialized, "titleText");
            TMP_Text auxiliary = Reference<TMP_Text>(rewardSerialized, "grantText");
            TMP_Text description = Reference<TMP_Text>(rewardSerialized, "effectText");
            GameObject stars = Reference<GameObject>(rewardSerialized, "levelStarsRoot");
            if (stars != null) stars.SetActive(false);
            Object.DestroyImmediate(rewardCard);

            RectTransform descriptionRect = description.rectTransform;
            descriptionRect.anchoredPosition = new Vector2(descriptionRect.anchoredPosition.x, -92f);
            descriptionRect.sizeDelta = new Vector2(descriptionRect.sizeDelta.x, 34f);
            auxiliary.rectTransform.anchoredPosition = new Vector2(
                auxiliary.rectTransform.anchoredPosition.x, -60f);
            auxiliary.rectTransform.sizeDelta = new Vector2(
                auxiliary.rectTransform.sizeDelta.x, 26f);
            TMP_Text payout = Object.Instantiate(description, card.transform);
            payout.name = "MissionPayoutText";
            payout.rectTransform.anchoredPosition = new Vector2(
                payout.rectTransform.anchoredPosition.x, -125f);
            payout.rectTransform.sizeDelta = new Vector2(payout.rectTransform.sizeDelta.x, 28f);

            CommonChoiceCardLayout layout = card.GetComponent<CommonChoiceCardLayout>();
            if (layout == null) layout = card.AddComponent<CommonChoiceCardLayout>();
            layout.Configure(button, icon, iconRoot, cardTitle, auxiliary, description);
            layout.SetCategory(CommonChoiceCardLayout.CardCategory.Event);
            layout.SetIcon(missionIcon);
            auxiliary.fontSize = 20f;
            description.fontSize = 22f;
            payout.fontSize = 22f;
            layout.SetTexts("미션 제목", "보통", "조건 : 미션 내용");
            CommonChoiceCardLayout.SetText(payout, "보상 : 보상 내용");
            buttons[i] = button;
            layouts[i] = layout;
            icons[i] = icon;
            iconRoots[i] = iconRoot;
            titleTexts[i] = cardTitle;
            rewardTexts[i] = auxiliary;
            descriptionTexts[i] = description;
            payoutTexts[i] = payout;
        }

        TMP_Text hint = Text("StageMissionHint", panel.transform,
            "실패 페널티 없음 · 보스 처치 시 판정", font, 17f,
            new Vector2(0f, -305f), new Vector2(405f, 24f), TextAlignmentOptions.Center);
        hint.color = new Color32(111, 76, 48, 255);

        GameObject tracker = ImageObject("StageMissionTracker", rightPanel.transform, panelSprite, Color.white);
        RectTransform trackerRect = tracker.GetComponent<RectTransform>();
        trackerRect.anchorMin = trackerRect.anchorMax = new Vector2(1f, 1f);
        trackerRect.pivot = new Vector2(1f, 1f);
        trackerRect.anchoredPosition = new Vector2(-18f, -18f);
        trackerRect.sizeDelta = new Vector2(500f, 98f);
        TMP_Text trackerText = Text("StageMissionTrackerText", tracker.transform, string.Empty, font,
            20f, Vector2.zero, new Vector2(460f, 70f), TextAlignmentOptions.Center);

        TMP_Text resultText = Text("StageMissionResultText", rightPanel.transform, string.Empty, font,
            28f, new Vector2(0f, 360f), new Vector2(540f, 62f), TextAlignmentOptions.Center);

        GameObject statusPanel = FindSceneObject("StatusPanel");
        GameObject detailStats = FindSceneObject("BeneficialEffectColumn");
        GameObject missionStatus = null;
        TMP_Text missionStatusName = null;
        TMP_Text missionStatusCondition = null;
        TMP_Text missionStatusReward = null;
        if (statusPanel != null && detailStats != null)
        {
            SetRect(detailStats.GetComponent<RectTransform>(), new Vector2(0f, -145f), new Vector2(555f, 420f));
            Transform detailTitle = detailStats.transform.Find("ColumnTitle");
            if (detailTitle is RectTransform detailTitleRect)
                SetRect(detailTitleRect, new Vector2(0f, 183f), new Vector2(510f, 34f));
            Transform detailScroll = detailStats.transform.Find("BeneficialEffectScrollView");
            if (detailScroll is RectTransform detailScrollRect)
                SetRect(detailScrollRect, new Vector2(0f, -10f), new Vector2(515f, 330f));

            missionStatus = ImageObject("MissionStatusSection", statusPanel.transform, statusParchment, Color.white);
            SetRect(missionStatus.GetComponent<RectTransform>(), new Vector2(0f, -440f), new Vector2(555f, 150f));
            Image missionStatusImage = missionStatus.GetComponent<Image>();
            missionStatusImage.type = Image.Type.Sliced;
            missionStatusImage.pixelsPerUnitMultiplier = 2f;
            Text("MissionStatusHeader", missionStatus.transform, "미션", font, 22f,
                new Vector2(0f, 52f), new Vector2(510f, 30f), TextAlignmentOptions.Left);
            missionStatusName = Text("MissionStatusNameText", missionStatus.transform, string.Empty, font, 19f,
                new Vector2(0f, 20f), new Vector2(500f, 26f), TextAlignmentOptions.Left);
            missionStatusCondition = Text("MissionStatusConditionText", missionStatus.transform, string.Empty, font, 18f,
                new Vector2(0f, -12f), new Vector2(500f, 30f), TextAlignmentOptions.Left);
            missionStatusReward = Text("MissionStatusRewardText", missionStatus.transform, string.Empty, font, 18f,
                new Vector2(0f, -45f), new Vector2(500f, 26f), TextAlignmentOptions.Left);
            CommonChoiceCardLayout.ConfigureText(missionStatusName, TextAlignmentOptions.Left);
            CommonChoiceCardLayout.ConfigureText(missionStatusCondition, TextAlignmentOptions.Left);
            CommonChoiceCardLayout.ConfigureText(missionStatusReward, TextAlignmentOptions.Left);
        }

        SerializedObject serialized = new(system);
        Set(serialized, "selectionPanel", panel);
        Set(serialized, "stageTitleText", title);
        SetArray(serialized, "cardButtons", buttons);
        SetArray(serialized, "cardLayouts", layouts);
        SetArray(serialized, "cardIcons", icons);
        SetArray(serialized, "cardIconRoots", iconRoots);
        SetArray(serialized, "cardTitleTexts", titleTexts);
        SetArray(serialized, "cardRewardTexts", rewardTexts);
        SetArray(serialized, "cardDescriptionTexts", descriptionTexts);
        SetArray(serialized, "cardPayoutTexts", payoutTexts);
        Set(serialized, "trackerPanel", tracker);
        Set(serialized, "trackerText", trackerText);
        Set(serialized, "resultText", resultText);
        Set(serialized, "missionStatusRoot", missionStatus);
        Set(serialized, "missionStatusNameText", missionStatusName);
        Set(serialized, "missionStatusConditionText", missionStatusCondition);
        Set(serialized, "missionStatusRewardText", missionStatusReward);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
        tracker.SetActive(false);
        resultText.gameObject.SetActive(false);
        if (missionStatus != null) missionStatus.SetActive(true);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("StageMissionPanelHierarchyInstaller: bottom-right mission panel and three authored cards installed.");
    }

    private static GameObject FindSceneObject(string name)
    {
        foreach (GameObject value in Resources.FindObjectsOfTypeAll<GameObject>())
            if (value.scene.IsValid() && value.name == name) return value;
        return null;
    }

    private static void DestroyNamed(string name)
    {
        GameObject value = FindSceneObject(name);
        if (value != null) Object.DestroyImmediate(value);
    }

    private static void DestroyMarkers()
    {
        foreach (GameObject value in Resources.FindObjectsOfTypeAll<GameObject>())
            if (value.scene.IsValid() && value.name.StartsWith("StageMissionPanel_Hierarchy_"))
                Object.DestroyImmediate(value);
    }

    private static void ApplyStatusPanelSprites()
    {
        SetAuthoredSprite("StageInfoSection", "Panel_Parchment_StageClean");
        SetAuthoredSprite("BallInfoSection", "Panel_Parchment_BallInfoClean");
        SetAuthoredSprite("AugmentColumn", "Panel_Parchment_AugmentClean");
        SetAuthoredSprite("BeneficialEffectColumn", "Panel_Parchment_DetailStatsClean");
    }

    private static void SetAuthoredSprite(string objectName, string assetName)
    {
        GameObject target = FindSceneObject(objectName);
        Image image = target != null ? target.GetComponent<Image>() : null;
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            $"Assets/_Project/Resources/UI/Parchment/{assetName}.png");
        if (image == null || sprite == null) return;
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 2f;
    }

    private static GameObject Rect(string name, Transform parent)
    {
        GameObject value = new(name, typeof(RectTransform));
        value.transform.SetParent(parent, false);
        return value;
    }

    private static GameObject ImageObject(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject value = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        value.transform.SetParent(parent, false);
        Image image = value.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        return value;
    }

    private static TMP_Text Text(string name, Transform parent, string value, TMP_FontAsset font,
        float fontSize, Vector2 position, Vector2 size, TextAlignmentOptions alignment)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.GetComponent<TMP_Text>();
        if (font != null) text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = Ink;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        SetRect(text.rectTransform, position, size);
        return text;
    }

    private static void CopyRect(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = Vector2.one * -inset;
    }

    private static void Set(SerializedObject serialized, string property, Object value)
        => serialized.FindProperty(property).objectReferenceValue = value;

    private static T Reference<T>(SerializedObject serialized, string property) where T : Object
        => serialized.FindProperty(property).objectReferenceValue as T;

    private static void SetArray<T>(SerializedObject serialized, string property, T[] values) where T : Object
    {
        SerializedProperty array = serialized.FindProperty(property);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
#endif
