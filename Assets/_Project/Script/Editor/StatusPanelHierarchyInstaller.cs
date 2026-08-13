#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class StatusPanelHierarchyInstaller
{
    // Versioned scene migration for the always-visible LeftPanel status board.
    private const string VersionMarker = "StatusPanel_Left_V11";
    static StatusPanelHierarchyInstaller() => EditorApplication.delayCall += RunSceneMaintenance;

    private static void RunSceneMaintenance()
    {
        CleanupLegacyStageInfo();
        InstallIfNeeded();
    }

    private static void CleanupLegacyStageInfo()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.path.EndsWith("SampleScene.unity")) return;
        bool changed = false;
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (!go.scene.IsValid() || (go.name != "StageText" && go.name != "PlayTimeText")) continue;
            if (go.transform.parent != null && go.transform.parent.name == "StageInfoSection") continue;
            Object.DestroyImmediate(go);
            changed = true;
        }
        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }

    [MenuItem("Ballchemy/UI/Rebuild Left Status Panel")]
    public static void InstallIfNeeded()
    {
        if (Application.isPlaying) return;
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.path.EndsWith("SampleScene.unity")) return;
        GameObject leftPanel = FindSceneObject("LeftPanel");
        if (leftPanel == null || FindSceneObject(VersionMarker) != null) return;

        DestroyNamed("StatusPanelSystem"); DestroyNamed("StatusOpenButton"); DestroyNamed("StatusPanel");
        TMP_FontAsset font = FindFont();
        GameObject marker = CreateRect(VersionMarker, leftPanel.transform); marker.SetActive(false);
        GameObject panel = CreateImage("StatusPanel", leftPanel.transform, new Color(0.055f, 0.065f, 0.075f, 0.96f));
        Stretch(panel.GetComponent<RectTransform>());
        StatusPanelPresenter presenter = panel.AddComponent<StatusPanelPresenter>();

        GameObject stageSection = Section("StageInfoSection", panel.transform, new Vector2(0f, 465f), new Vector2(555f, 90f));
        TMP_Text stageText = Text("StageText", stageSection.transform, "Stage 1", font, 30f, new Vector2(-10f, 20f), new Vector2(500f, 40f), TextAlignmentOptions.Left);
        TMP_Text playTimeText = Text("PlayTimeText", stageSection.transform, "Play Time  00:00", font, 23f, new Vector2(-10f, -22f), new Vector2(500f, 34f), TextAlignmentOptions.Left);

        GameObject ballSection = Section("BallInfoSection", panel.transform, new Vector2(-95f, 260f), new Vector2(360f, 300f));
        TMP_Text totalBallCount = Text("TotalBallCountText", ballSection.transform, "총 0개", font, 21f, new Vector2(108f, 125f), new Vector2(100f, 36f), TextAlignmentOptions.Right);
        Text("SectionTitle", ballSection.transform, "공 정보", font, 27f, new Vector2(0f, 115f), new Vector2(510f, 42f), TextAlignmentOptions.Left);
        Transform ballContent = Content("BallInfoContent", ballSection.transform, new Vector2(0f, -42f), new Vector2(330f, 210f));
        RectTransform ballContentRect = ballContent as RectTransform;
        ballContentRect.anchorMin = ballContentRect.anchorMax = new Vector2(0.5f, 1f);
        ballContentRect.pivot = new Vector2(0.5f, 1f);
        ballContentRect.anchoredPosition = new Vector2(0f, -96f);
        ballContent.GetComponent<VerticalLayoutGroup>().spacing = 2f;
        StatusEntryView ballTemplate = Entry("BallTypeRowTemplate", ballContent, font, 320f, true);
        TMP_Text ballEmpty = Text("BallEmptyText", ballContent, "보유한 공이 없습니다.", font, 16f, Vector2.zero, new Vector2(490f, 34f), TextAlignmentOptions.Left);

        SetRect(ballSection.transform.Find("SectionTitle") as RectTransform, new Vector2(-45f, 125f), new Vector2(220f, 42f));
        Text("AttributeHeader", ballSection.transform, "\uC18D\uC131", font, 20f, new Vector2(-110f, 82f), new Vector2(80f, 30f), TextAlignmentOptions.Center);
        Text("Grade1Header", ballSection.transform, "1★", font, 20f, new Vector2(-25f, 82f), new Vector2(58f, 30f), TextAlignmentOptions.Center);
        Text("Grade2Header", ballSection.transform, "2★", font, 20f, new Vector2(45f, 82f), new Vector2(58f, 30f), TextAlignmentOptions.Center);
        Text("Grade3Header", ballSection.transform, "3★", font, 20f, new Vector2(115f, 82f), new Vector2(58f, 30f), TextAlignmentOptions.Center);

        GameObject effectSection = CreateRect("StatusBody", panel.transform);
        SetRect(effectSection.GetComponent<RectTransform>(), Vector2.zero, new Vector2(555f, 1f));
        Text("SectionTitle", effectSection.transform, "현재 적용 효과", font, 27f, new Vector2(0f, 235f), new Vector2(510f, 42f), TextAlignmentOptions.Left);
        GameObject columns = CreateRect("EffectColumns", effectSection.transform); SetRect(columns.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
        effectSection.transform.Find("SectionTitle")?.gameObject.SetActive(false);
        GameObject augmentColumn = Section("AugmentColumn", panel.transform, new Vector2(180f, 260f), new Vector2(180f, 300f));
        Text("ColumnTitle", augmentColumn.transform, "증강", font, 22f, new Vector2(0f, 195f), new Vector2(220f, 34f), TextAlignmentOptions.Left);
        Transform augmentContent = ScrollContent("AugmentScrollView", "AugmentContent", augmentColumn.transform, new Vector2(0f, -18f), new Vector2(150f, 225f));
        SetRect(augmentColumn.transform.Find("ColumnTitle") as RectTransform, new Vector2(0f, 125f), new Vector2(150f, 42f));
        StatusEntryView augmentTemplate = Entry("AugmentRowTemplate", augmentContent, font, 140f, false);
        TMP_Text augmentEmpty = Text("AugmentEmptyText", augmentContent, string.Empty, font, 15f, Vector2.zero, new Vector2(210f, 30f), TextAlignmentOptions.Left);
        GameObject beneficialColumn = Section("BeneficialEffectColumn", panel.transform, new Vector2(0f, -125f), new Vector2(555f, 450f));
        Text("ColumnTitle", beneficialColumn.transform, "이로운 효과", font, 22f, new Vector2(0f, 195f), new Vector2(220f, 34f), TextAlignmentOptions.Left);
        Transform beneficialContent = ScrollContent("BeneficialEffectScrollView", "BeneficialEffectContent", beneficialColumn.transform, new Vector2(0f, -20f), new Vector2(515f, 365f));
        SetRect(beneficialColumn.transform.Find("ColumnTitle") as RectTransform, new Vector2(0f, 198f), new Vector2(510f, 42f));
        StatusEntryView beneficialTemplate = Entry("BeneficialEffectRowTemplate", beneficialContent, font, 500f, false);
        TMP_Text beneficialName = beneficialTemplate.transform.Find("NameText")?.GetComponent<TMP_Text>();
        if (beneficialName != null) beneficialName.fontSize = 23f;
        LayoutElement beneficialLayout = beneficialTemplate.GetComponent<LayoutElement>();
        if (beneficialLayout != null) beneficialLayout.preferredHeight = 40f;
        TMP_Text beneficialEmpty = Text("BeneficialEffectEmptyText", beneficialContent, string.Empty, font, 15f, Vector2.zero, new Vector2(210f, 30f), TextAlignmentOptions.Left);

        GameObject professionSection = Section("ProfessionSection", panel.transform, new Vector2(0f, -470f), new Vector2(555f, 105f));
        Text("ProfessionTitle", professionSection.transform, "전직", font, 26f, new Vector2(0f, 28f), new Vector2(510f, 38f), TextAlignmentOptions.Left);
        TMP_Text professionValue = Text("ProfessionValueText", professionSection.transform, string.Empty, font, 22f, new Vector2(0f, -18f), new Vector2(510f, 36f), TextAlignmentOptions.Left);

        GameObject tooltip = CreateImage("StatusTooltipPanel", panel.transform, Color.white);
        tooltip.AddComponent<Outline>().effectColor = Color.black;
        RectTransform tooltipRect = tooltip.GetComponent<RectTransform>(); SetRect(tooltipRect, Vector2.zero, new Vector2(360f, 120f));
        TMP_Text tooltipText = Text("TooltipText", tooltip.transform, string.Empty, font, 18f, Vector2.zero, new Vector2(330f, 95f), TextAlignmentOptions.TopLeft);
        tooltip.SetActive(false);

        SerializedObject so = new(presenter);
        Set(so,"stageText",stageText); Set(so,"playTimeText",playTimeText);
        Set(so,"ballContent",ballContent); Set(so,"ballEntryTemplate",ballTemplate); Set(so,"ballEmptyText",ballEmpty); Set(so,"totalBallCountText",totalBallCount);
        Set(so,"augmentContent",augmentContent); Set(so,"augmentEntryTemplate",augmentTemplate); Set(so,"augmentEmptyText",augmentEmpty);
        Set(so,"beneficialContent",beneficialContent); Set(so,"beneficialEntryTemplate",beneficialTemplate); Set(so,"beneficialEmptyText",beneficialEmpty);
        Set(so,"professionValueText",professionValue); Set(so,"tooltipPanel",tooltipRect); Set(so,"tooltipText",tooltipText);
        so.ApplyModifiedPropertiesWithoutUndo();
        ballTemplate.gameObject.SetActive(false); augmentTemplate.gameObject.SetActive(false); beneficialTemplate.gameObject.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("StatusPanelHierarchyInstaller: LeftPanel 상시 Status 보드를 생성했습니다.");
    }

    private static StatusEntryView Entry(string name, Transform parent, TMP_FontAsset font, float width, bool showIcon)
    {
        GameObject row = CreateRect(name, parent); LayoutElement le = row.AddComponent<LayoutElement>(); le.preferredHeight = showIcon ? 27f : 30f;
        if (showIcon)
        {
            GameObject backgroundObject = CreateImage("RowBackground", row.transform, Color.white);
            Image rowBackground = backgroundObject.GetComponent<Image>();
            rowBackground.color = new Color(1f, 1f, 1f, 0.055f);
            rowBackground.raycastTarget = false;
            rowBackground.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            rowBackground.type = Image.Type.Sliced;
            SetRect(rowBackground.rectTransform, Vector2.zero, new Vector2(width - 8f, 19f));
        }
        Image icon = CreateImage("Icon", row.transform, Color.white).GetComponent<Image>(); SetRect(icon.rectTransform, new Vector2(-width/2+16f,0f), new Vector2(24f,24f)); icon.gameObject.SetActive(false);
        TMP_Text title = Text("NameText", row.transform, "항목", font, showIcon?20f:17f, new Vector2(showIcon?-135f:-45f,0f), new Vector2(showIcon?150f:120f,30f), TextAlignmentOptions.Left);
        title.gameObject.AddComponent<HoverTooltip>();
        SetRect(title.rectTransform,
            new Vector2(showIcon ? -110f : 0f, 0f),
            new Vector2(showIcon ? 80f : Mathf.Max(80f, width - 20f), 30f));
        if (showIcon) title.alignment = TextAlignmentOptions.Center;
        GameObject underline = CreateImage("Underline", title.transform, new Color(0.78f,0.68f,1f,1f)); SetRect(underline.GetComponent<RectTransform>(), new Vector2(0f,-11f), new Vector2(60f,1.5f));
        TMP_Text value = Text("ValueText", row.transform, string.Empty, font, showIcon?18f:17f, new Vector2(showIcon?100f:0f,0f), new Vector2(showIcon?190f:Mathf.Max(80f,width-20f),30f), TextAlignmentOptions.Right);
        value.raycastTarget = false;
        GameObject gradeRoot = CreateRect("GradeSlots", row.transform);
        SetRect(gradeRoot.GetComponent<RectTransform>(), new Vector2(45f, 0f), new Vector2(220f, 30f));
        TMP_Text[] gradeTexts = new TMP_Text[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject slot = CreateImage($"Grade{i + 1}Slot", gradeRoot.transform, Color.clear);
            slot.GetComponent<Image>().raycastTarget = false;
            SetRect(slot.GetComponent<RectTransform>(), new Vector2((i - 1) * 70f, 0f), new Vector2(58f, 27f));
            gradeTexts[i] = Text("CountText", slot.transform, string.Empty, font, 18f, Vector2.zero, new Vector2(58f, 25f), TextAlignmentOptions.Center);
            gradeTexts[i].raycastTarget = false;
        }
        gradeRoot.SetActive(showIcon);
        TMP_Text description = Text("DescriptionText", row.transform, string.Empty, font, 1f, Vector2.zero, Vector2.one, TextAlignmentOptions.Left); description.gameObject.SetActive(false);
        StatusEntryView view = row.AddComponent<StatusEntryView>(); SerializedObject so = new(view);
        Set(so,"icon",icon); Set(so,"nameText",title); Set(so,"valueText",value); Set(so,"descriptionText",description); Set(so,"hoverTooltip",title.GetComponent<HoverTooltip>()); Set(so,"gradeSlotsRoot",gradeRoot);
        SerializedProperty gradeArray = so.FindProperty("gradeValueTexts"); gradeArray.arraySize = 3;
        for (int i = 0; i < 3; i++) gradeArray.GetArrayElementAtIndex(i).objectReferenceValue = gradeTexts[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    private static Transform Content(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject go = CreateRect(name,parent); SetRect(go.GetComponent<RectTransform>(),pos,size);
        VerticalLayoutGroup layout=go.AddComponent<VerticalLayoutGroup>(); layout.spacing=3f; layout.childControlHeight=true; layout.childControlWidth=true; layout.childForceExpandHeight=false;
        ContentSizeFitter fit=go.AddComponent<ContentSizeFitter>(); fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize; return go.transform;
    }
    private static Transform ScrollContent(string scrollName, string contentName,
        Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject root = CreateImage(scrollName, parent, new Color(0f, 0f, 0f, 0.08f));
        SetRect(root.GetComponent<RectTransform>(), pos, size);
        ScrollRect scroll = root.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24f;
        GameObject viewport = CreateRect("Viewport", root.transform);
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();
        Transform content = Content(contentName, viewport.transform, Vector2.zero, size);
        RectTransform contentRect = content as RectTransform;
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = Vector2.one;
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRect;
        return content;
    }
    private static GameObject Section(string name, Transform parent, Vector2 pos, Vector2 size) { GameObject go=CreateImage(name,parent,new Color(1f,1f,1f,0.055f)); SetRect(go.GetComponent<RectTransform>(),pos,size); return go; }
    private static TMP_Text Text(string name,Transform parent,string value,TMP_FontAsset font,float size,Vector2 pos,Vector2 rectSize,TextAlignmentOptions alignment) { GameObject go=CreateRect(name,parent); SetRect(go.GetComponent<RectTransform>(),pos,rectSize); TextMeshProUGUI text=go.AddComponent<TextMeshProUGUI>(); if(font!=null)text.font=font; text.text=value;text.fontSize=size;text.color=Color.white;text.alignment=alignment;text.textWrappingMode=TextWrappingModes.Normal;return text; }
    private static GameObject CreateImage(string name,Transform parent,Color color){GameObject go=CreateRect(name,parent);go.AddComponent<Image>().color=color;return go;}
    private static GameObject CreateRect(string name,Transform parent){GameObject go=new(name,typeof(RectTransform));go.transform.SetParent(parent,false);return go;}
    private static void Set(SerializedObject so,string name,Object value)=>so.FindProperty(name).objectReferenceValue=value;
    private static void SetRect(RectTransform rect,Vector2 pos,Vector2 size){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=pos;rect.sizeDelta=size;}
    private static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    private static void DestroyNamed(string name){GameObject go=FindSceneObject(name);if(go!=null)Object.DestroyImmediate(go);}
    private static GameObject FindSceneObject(string name){foreach(GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())if(go.scene.IsValid()&&go.name==name)return go;return null;}
    private static TMP_FontAsset FindFont(){foreach(TMP_Text text in Resources.FindObjectsOfTypeAll<TMP_Text>())if(text.gameObject.scene.IsValid()&&text.font!=null)return text.font;return null;}
}
#endif
