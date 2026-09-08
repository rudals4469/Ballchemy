using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CombatPauseMenuController : MonoBehaviour
{
    private static readonly Color Ink = new Color32(74, 42, 24, 255);
    private GameObject overlay;
    private float previousTimeScale = 1f;
    private bool isPaused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "SampleScene" ||
            FindFirstObjectByType<CombatPauseMenuController>() != null)
            return;

        GameObject host = new GameObject("CombatPauseMenuSystem");
        host.AddComponent<CombatPauseMenuController>().Build();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(!isPaused);
    }

    private void Build()
    {
        Transform midPanel = FindTransform("MidPanelRoot");
        Canvas canvas = midPanel != null ? midPanel.GetComponentInParent<Canvas>() : FindFirstObjectByType<Canvas>();
        if (midPanel == null || canvas == null) return;

        Image headerStyle = null;
        foreach (Transform child in midPanel)
        {
            if (!child.name.StartsWith("CombatHeaderInfoBox")) continue;
            RectTransform rect = child as RectTransform;
            if (rect == null) continue;

            if (child.name == "CombatHeaderInfoBox")
            {
                // Keep the left edge aligned while reserving a separate cell
                // for the settings button on the right.
                rect.anchoredPosition = new Vector2(-13.7f, 490f);
                rect.sizeDelta = new Vector2(520f, 50f);
                headerStyle = child.GetComponent<Image>();
            }
            else
            {
                rect.anchoredPosition = new Vector2(-324.9f, 490f);
            }
        }

        Button settings = CreateButton("CombatSettingsButton", midPanel, string.Empty,
            new Vector2(289f, 490f), new Vector2(50f, 50f));
        RectTransform settingsRect = settings.GetComponent<RectTransform>();
        settingsRect.anchorMin = settingsRect.anchorMax = settingsRect.pivot = new Vector2(0.5f, 0.5f);
        settingsRect.anchoredPosition = new Vector2(289f, 490f);
        Image settingsBackground = settings.GetComponent<Image>();
        if (headerStyle != null)
        {
            settingsBackground.sprite = headerStyle.sprite;
            settingsBackground.type = headerStyle.type;
            settingsBackground.pixelsPerUnitMultiplier = headerStyle.pixelsPerUnitMultiplier;
            settingsBackground.color = headerStyle.color;
        }
        Outline settingsOutline = settings.gameObject.AddComponent<Outline>();
        settingsOutline.effectColor = Color.black;
        settingsOutline.effectDistance = new Vector2(2f, -2f);
        settingsOutline.useGraphicAlpha = true;
        Image icon = CreateImage("SettingsIcon", settings.transform,
            Resources.Load<Sprite>("UI/Common/SettingsIcon"), new Color32(74, 42, 24, 255));
        Stretch(icon.rectTransform, 10f);
        settings.onClick.AddListener(() => SetPaused(true));

        overlay = CreateRect("CombatPauseOverlay", canvas.transform);
        Stretch(overlay.GetComponent<RectTransform>(), 0f);
        Image dim = overlay.AddComponent<Image>();
        dim.color = new Color(0.08f, 0.045f, 0.02f, 0.48f);

        GameObject panel = CreateRect("PauseOptionsPanel", overlay.transform);
        SetRect(panel.GetComponent<RectTransform>(), Vector2.zero, new Vector2(700f, 500f));
        Image panelImage = panel.AddComponent<Image>();
        panelImage.sprite = Resources.Load<Sprite>("UI/Parchment/Panel_Parchment_MissionClean");
        panelImage.type = Image.Type.Sliced;
        panelImage.pixelsPerUnitMultiplier = 2f;
        panelImage.color = new Color(1f, 0.94f, 0.8f, 1f);

        Text("Title", panel.transform, "일시정지", 38f, new Vector2(0f, 185f), new Vector2(500f, 50f));
        Text("MusicLabel", panel.transform, "배경음", 28f, new Vector2(-225f, 85f), new Vector2(130f, 42f));
        Slider music = CreateSlider("MusicVolumeSlider", panel.transform, new Vector2(75f, 85f));
        Text("SfxLabel", panel.transform, "효과음", 28f, new Vector2(-225f, 0f), new Vector2(130f, 42f));
        Slider sfx = CreateSlider("SfxVolumeSlider", panel.transform, new Vector2(75f, 0f));
        Toggle mute = CreateToggle("MuteToggle", panel.transform, new Vector2(-105f, -82f));
        Text("MuteLabel", panel.transform, "전체 음소거", 27f, new Vector2(35f, -82f), new Vector2(220f, 42f));

        Button resume = CreateButton("ResumeButton", panel.transform, "계속하기",
            new Vector2(-150f, -175f), new Vector2(250f, 64f));
        Button title = CreateButton("ReturnToTitleButton", panel.transform, "메인 화면",
            new Vector2(150f, -175f), new Vector2(250f, 64f));
        resume.onClick.AddListener(() => SetPaused(false));
        title.onClick.AddListener(ReturnToTitle);
        panel.AddComponent<AudioOptionsPanelController>().Configure(music, sfx, mute);
        overlay.SetActive(false);
    }

    private void SetPaused(bool paused)
    {
        if (overlay == null || isPaused == paused) return;
        isPaused = paused;
        if (paused)
        {
            previousTimeScale = Mathf.Max(0.01f, Time.timeScale);
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = previousTimeScale;
        }
        overlay.SetActive(paused);
    }

    private void ReturnToTitle()
    {
        FindFirstObjectByType<RunSaveCoordinator>()?.SaveNow();
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
    }

    private void OnDestroy()
    {
        if (isPaused) Time.timeScale = previousTimeScale;
    }

    private static Slider CreateSlider(string name, Transform parent, Vector2 position)
    {
        GameObject root = CreateRect(name, parent);
        SetRect(root.GetComponent<RectTransform>(), position, new Vector2(420f, 30f));
        Sprite trackSprite = Resources.Load<Sprite>("UI/Title/Slider_Track");
        Image background = CreateImage("Background", root.transform, trackSprite, Color.white);
        Stretch(background.rectTransform, 0f);
        background.type = Image.Type.Sliced;
        background.pixelsPerUnitMultiplier = 2f;

        GameObject fillArea = CreateRect("Fill Area", root.transform);
        Stretch(fillArea.GetComponent<RectTransform>(), 10f, 8f);
        Image fill = CreateImage("Fill", fillArea.transform, null, new Color32(188, 91, 36, 230));
        Stretch(fill.rectTransform, 0f);

        GameObject handleArea = CreateRect("Handle Slide Area", root.transform);
        Stretch(handleArea.GetComponent<RectTransform>(), 10f, 0f);
        Image handle = CreateImage("Handle", handleArea.transform, null, new Color32(92, 43, 15, 255));
        SetRect(handle.rectTransform, Vector2.zero, new Vector2(22f, 34f));

        Slider slider = root.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.8f;
        return slider;
    }

    private static Toggle CreateToggle(string name, Transform parent, Vector2 position)
    {
        GameObject root = CreateRect(name, parent);
        SetRect(root.GetComponent<RectTransform>(), position, new Vector2(42f, 42f));
        Image background = root.AddComponent<Image>();
        background.color = new Color32(92, 43, 15, 255);
        Image check = CreateImage("Checkmark", root.transform, null, new Color32(239, 181, 76, 255));
        Stretch(check.rectTransform, 9f);
        Toggle toggle = root.AddComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = check;
        return toggle;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
    {
        GameObject root = CreateRect(name, parent);
        SetRect(root.GetComponent<RectTransform>(), position, size);
        Image image = root.AddComponent<Image>();
        image.sprite = Resources.Load<Sprite>("UI/Parchment/Panel_Parchment_MissionClean");
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 3f;
        image.color = new Color(1f, 0.91f, 0.72f, 1f);
        Button button = root.AddComponent<Button>();
        button.targetGraphic = image;
        if (!string.IsNullOrEmpty(label)) Text("Label", root.transform, label, 27f, Vector2.zero, size - new Vector2(20f, 10f));
        return button;
    }

    private static TMP_Text Text(string name, Transform parent, string value, float size, Vector2 position, Vector2 rectSize)
    {
        GameObject root = CreateRect(name, parent);
        SetRect(root.GetComponent<RectTransform>(), position, rectSize);
        TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
        text.font = Resources.Load<TMP_FontAsset>("Art/Font/BMJUA_ttf SDF");
        text.text = value;
        text.fontSize = size;
        text.color = Ink;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject root = CreateRect(name, parent);
        Image image = root.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

    private static GameObject CreateRect(string name, Transform parent)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        return root;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect, float inset) => Stretch(rect, inset, inset);

    private static void Stretch(RectTransform rect, float horizontalInset, float verticalInset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontalInset, verticalInset);
        rect.offsetMax = new Vector2(-horizontalInset, -verticalInset);
    }

    private static Transform FindTransform(string name)
    {
        foreach (Transform transform in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (transform.name == name) return transform;
        return null;
    }
}
