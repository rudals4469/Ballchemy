using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TitleSceneInstaller
{
    private const string ScenePath = "Assets/_Project/Scene/TitleScene.unity";
    private const string InstalledVersionKey = "Ballchemy.TitleSceneInstaller.Version";
    private const int InstallerVersion = 11;
    private static readonly Color Ink = new Color(0.16f, 0.1f, 0.06f, 1f);

    [InitializeOnLoadMethod]
    private static void ScheduleInstall()
    {
        if (!File.Exists(ScenePath) || EditorPrefs.GetInt(InstalledVersionKey, 0) < InstallerVersion)
            EditorApplication.delayCall += Install;
    }

    [MenuItem("Tools/Ballchemy/Create Title Scene")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            return;
        }

        EnsureTexture("Assets/_Project/Resources/UI/Title/Title_Background.png", false);
        EnsureTexture("Assets/_Project/Resources/UI/Title/Title_Logo.png", true);
        EnsureTexture("Assets/_Project/Resources/UI/Title/Slider_Track.png", true);
        EnsureSlicedTexture(
            "Assets/_Project/Resources/UI/Title/Slider_Track.png",
            new Vector4(52f, 20f, 52f, 20f));

        Scene previousScene = SceneManager.GetActiveScene();
        Scene scene = FindLoadedTitleScene();
        bool wasAlreadyLoaded = scene.IsValid();

        if (!wasAlreadyLoaded)
        {
            scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        }

        SceneManager.SetActiveScene(scene);
        foreach (GameObject root in scene.GetRootGameObjects())
            Object.DestroyImmediate(root);

        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;

        GameObject canvasObject = new GameObject("TitleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Image background = Image("Background", canvasObject.transform,
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Resources/UI/Title/Title_Background.png"));
        Stretch(background.rectTransform);
        background.preserveAspect = false;
        background.color = Color.white;

        Image logo = Image("BallchemyLogo", canvasObject.transform,
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Resources/UI/Title/Title_Logo.png"));
        logo.preserveAspect = true;
        logo.raycastTarget = false;
        SetRect(logo.rectTransform, new Vector2(0f, 150f), new Vector2(1120f, 420f),
            new Vector2(0.5f, 0.5f));

        GameObject menu = Rect("Menu", canvasObject.transform);
        RectTransform menuRect = menu.GetComponent<RectTransform>();
        menuRect.anchorMin = menuRect.anchorMax = new Vector2(1f, 0f);
        menuRect.pivot = new Vector2(1f, 0f);
        menuRect.anchoredPosition = new Vector2(-96f, 76f);
        menuRect.sizeDelta = new Vector2(480f, 430f);
        VerticalLayoutGroup layout = menu.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.LowerRight;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        Button newGame = Button("NewGameButton", menu.transform, "새로하기");
        Button continueButton = Button("ContinueButton", menu.transform, "이어하기");
        Button options = Button("OptionsButton", menu.transform, "옵션");
        Button quit = Button("QuitButton", menu.transform, "나가기");

        GameObject optionsPanel = Rect("OptionsPanel", canvasObject.transform);
        Image optionsImage = optionsPanel.AddComponent<Image>();
        optionsImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/_Project/Resources/UI/Parchment/Panel_Parchment_MissionClean.png");
        optionsImage.type = UnityEngine.UI.Image.Type.Sliced;
        optionsImage.color = new Color(1f, 0.93f, 0.78f, 1f);
        SetRect(optionsPanel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(700f, 420f), new Vector2(0.5f, 0.5f));
        TMP_Text optionsTitle = Text(optionsPanel.transform, "오디오 설정", 38f);
        SetRect(optionsTitle.rectTransform, new Vector2(0f, 145f), new Vector2(500f, 55f), new Vector2(0.5f, 0.5f));
        TMP_Text musicLabel = Text(optionsPanel.transform, "배경음", 28f);
        SetRect(musicLabel.rectTransform, new Vector2(-230f, 55f), new Vector2(130f, 45f), new Vector2(0.5f, 0.5f));
        Slider musicSlider = CreateSlider("MusicVolumeSlider", optionsPanel.transform, new Vector2(80f, 55f));
        TMP_Text sfxLabel = Text(optionsPanel.transform, "효과음", 28f);
        SetRect(sfxLabel.rectTransform, new Vector2(-230f, -35f), new Vector2(130f, 45f), new Vector2(0.5f, 0.5f));
        Slider sfxSlider = CreateSlider("SfxVolumeSlider", optionsPanel.transform, new Vector2(80f, -35f));
        Toggle muteToggle = CreateToggle("MuteToggle", optionsPanel.transform, new Vector2(-105f, -125f));
        TMP_Text muteLabel = Text(optionsPanel.transform, "전체 음소거", 27f);
        SetRect(muteLabel.rectTransform, new Vector2(35f, -125f), new Vector2(210f, 45f), new Vector2(0.5f, 0.5f));
        optionsPanel.AddComponent<AudioOptionsPanelController>().Configure(musicSlider, sfxSlider, muteToggle);
        optionsPanel.SetActive(false);

        TitleMenuController controller = canvasObject.AddComponent<TitleMenuController>();
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("newGameButton").objectReferenceValue = newGame;
        serialized.FindProperty("continueButton").objectReferenceValue = continueButton;
        serialized.FindProperty("optionsButton").objectReferenceValue = options;
        serialized.FindProperty("quitButton").objectReferenceValue = quit;
        serialized.FindProperty("optionsPanel").objectReferenceValue = optionsPanel;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        EditorSceneManager.SaveScene(scene, ScenePath);
        UpdateBuildSettings();
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        EditorPrefs.SetInt(InstalledVersionKey, InstallerVersion);
        if (previousScene.IsValid() && previousScene != scene)
            SceneManager.SetActiveScene(previousScene);
        if (!wasAlreadyLoaded && previousScene.IsValid())
            EditorSceneManager.CloseScene(scene, true);
        Debug.Log("TitleSceneInstaller: 타이틀 씬 생성 완료");
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode)
            return;

        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.delayCall += Install;
    }

    private static Scene FindLoadedTitleScene()
    {
        for (int index = 0; index < SceneManager.sceneCount; index++)
        {
            Scene candidate = SceneManager.GetSceneAt(index);
            if (candidate.path == ScenePath)
                return candidate;
        }

        return default;
    }

    private static Button Button(string name, Transform parent, string label)
    {
        GameObject item = Rect(name, parent);
        Image image = item.AddComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/_Project/Resources/UI/Parchment/Panel_Parchment_MissionClean.png");
        image.type = UnityEngine.UI.Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 2f;
        image.color = new Color(1f, 0.91f, 0.72f, 1f);
        Shadow shadow = item.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.16f, 0.08f, 0.035f, 0.72f);
        shadow.effectDistance = new Vector2(6f, -6f);
        shadow.useGraphicAlpha = true;
        Outline outline = item.AddComponent<Outline>();
        outline.effectColor = new Color(0.23f, 0.1f, 0.035f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;
        Button button = item.AddComponent<Button>();
        button.targetGraphic = image;
        LayoutElement element = item.AddComponent<LayoutElement>();
        element.preferredWidth = 480f;
        element.preferredHeight = 92f;
        TMP_Text labelText = Text(item.transform, label, 36f);
        labelText.fontStyle = FontStyles.Bold;
        return button;
    }

    private static TMP_Text Text(Transform parent, string value, float size)
    {
        GameObject item = Rect("Label", parent);
        Stretch(item.GetComponent<RectTransform>());
        TextMeshProUGUI text = item.AddComponent<TextMeshProUGUI>();
        text.font = Resources.Load<TMP_FontAsset>("Art/Font/BMJUA_ttf SDF");
        text.text = value;
        text.fontSize = size;
        text.color = Ink;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static Slider CreateSlider(string name, Transform parent, Vector2 position)
    {
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        Sprite trackSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/_Project/Resources/UI/Title/Slider_Track.png");
        GameObject root = Rect(name, parent);
        SetRect(root.GetComponent<RectTransform>(), position, new Vector2(420f, 30f), new Vector2(0.5f, 0.5f));

        Image background = Image("Background", root.transform, trackSprite);
        Stretch(background.rectTransform);
        background.type = UnityEngine.UI.Image.Type.Sliced;
        background.pixelsPerUnitMultiplier = 2f;
        background.color = Color.white;

        GameObject fillArea = Rect("Fill Area", root.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero; fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(8f, 10f); fillAreaRect.offsetMax = new Vector2(-8f, -10f);
        Image fill = Image("Fill", fillArea.transform, uiSprite);
        Stretch(fill.rectTransform);
        fill.type = UnityEngine.UI.Image.Type.Sliced;
        fill.color = new Color(0.78f, 0.39f, 0.15f, 0.9f);

        GameObject handleArea = Rect("Handle Slide Area", root.transform);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        Stretch(handleAreaRect); handleAreaRect.offsetMin = new Vector2(10f, 0f); handleAreaRect.offsetMax = new Vector2(-10f, 0f);
        Image handle = Image("Handle", handleArea.transform, knobSprite);
        SetRect(handle.rectTransform, Vector2.zero, new Vector2(28f, 28f), new Vector2(0f, 0.5f));
        handle.color = new Color(0.38f, 0.19f, 0.07f, 1f);

        Slider slider = root.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = 0.8f;
        return slider;
    }

    private static Toggle CreateToggle(string name, Transform parent, Vector2 position)
    {
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite checkSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
        GameObject root = Rect(name, parent);
        SetRect(root.GetComponent<RectTransform>(), position, new Vector2(44f, 44f), new Vector2(0.5f, 0.5f));
        Image background = Image("Background", root.transform, uiSprite);
        Stretch(background.rectTransform);
        background.color = new Color(0.35f, 0.16f, 0.055f, 1f);
        Image checkmark = Image("Checkmark", background.transform, checkSprite);
        Stretch(checkmark.rectTransform);
        checkmark.rectTransform.offsetMin = new Vector2(7f, 7f);
        checkmark.rectTransform.offsetMax = new Vector2(-7f, -7f);
        checkmark.color = new Color(1f, 0.82f, 0.35f, 1f);
        Toggle toggle = root.AddComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = checkmark;
        return toggle;
    }

    private static Image Image(string name, Transform parent, Sprite sprite)
    {
        GameObject item = Rect(name, parent);
        Image image = item.AddComponent<Image>();
        image.sprite = sprite;
        return image;
    }

    private static GameObject Rect(string name, Transform parent)
    {
        GameObject item = new GameObject(name, typeof(RectTransform));
        item.transform.SetParent(parent, false);
        return item;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size, Vector2 anchor)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void EnsureTexture(string path, bool alpha)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = alpha;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.compressionQuality = 100;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
    }

    private static void EnsureSlicedTexture(string path, Vector4 border)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || importer.spriteBorder == border) return;
        importer.spriteBorder = border;
        importer.SaveAndReimport();
    }

    private static void UpdateBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene("Assets/_Project/Scene/SampleScene.unity", true)
        };
    }
}
