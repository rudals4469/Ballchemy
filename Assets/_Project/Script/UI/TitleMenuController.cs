using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TitleMenuController : MonoBehaviour
{
    private const float SuctionDuration = 0.68f;

    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject optionsPanel;
    private AsyncOperation gameSceneLoad;
    private GameObject loadingOverlay;
    private bool isStartingGame;

    private void Awake()
    {
        newGameButton?.onClick.AddListener(StartNewGame);
        continueButton?.onClick.AddListener(ContinueGame);
        optionsButton?.onClick.AddListener(ToggleOptions);
        quitButton?.onClick.AddListener(QuitGame);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (continueButton != null) continueButton.interactable = RunSaveSystem.HasSave;
        BuildLoadingOverlay();
        StartCoroutine(PreloadGameScene());
    }

    private void StartNewGame()
    {
        RunSaveSystem.BeginNewRun();
        StartGameScene();
    }

    private void ContinueGame()
    {
        if (!RunSaveSystem.HasSave) return;
        RunSaveSystem.RequestContinue();
        StartGameScene();
    }

    private void StartGameScene()
    {
        if (isStartingGame) return;
        isStartingGame = true;
        Time.timeScale = 1f;
        if (optionsPanel != null) optionsPanel.SetActive(false);
        SetMenuInteractable(false);
        StartCoroutine(ActivateGameScene());
    }

    private IEnumerator PreloadGameScene()
    {
        // Give the title screen one rendered frame before starting disk work.
        yield return null;
        gameSceneLoad = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
        if (gameSceneLoad != null) gameSceneLoad.allowSceneActivation = false;
    }

    private IEnumerator ActivateGameScene()
    {
        yield return PlayPaperSuction();

        if (gameSceneLoad == null)
        {
            gameSceneLoad = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            if (gameSceneLoad == null) yield break;
            gameSceneLoad.allowSceneActivation = false;
        }

        // Usually the scene is ready during the transition. Show the loading card
        // only when disk work actually takes longer than the visual effect.
        if (gameSceneLoad.progress < 0.9f && loadingOverlay != null)
            loadingOverlay.SetActive(true);

        while (gameSceneLoad.progress < 0.9f) yield return null;
        SceneRevealOverlay.Create();
        gameSceneLoad.allowSceneActivation = true;
    }

    private IEnumerator PlayPaperSuction()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) yield break;

        RectTransform canvasRect = canvas.transform as RectTransform;
        Vector3 targetPosition = canvasRect != null
            ? canvasRect.TransformPoint(new Vector3(0f, -170f, 0f))
            : canvas.transform.position;

        Transform background = canvas.transform.Find("Background");
        Transform logo = canvas.transform.Find("BallchemyLogo");
        Transform menu = canvas.transform.Find("Menu");
        Transform[] targets = { background, logo, menu };
        Vector3[] startPositions = new Vector3[targets.Length];
        Vector3[] startScales = new Vector3[targets.Length];
        Quaternion[] startRotations = new Quaternion[targets.Length];
        CanvasGroup[] groups = new CanvasGroup[targets.Length];

        for (int i = 0; i < targets.Length; i++)
        {
            Transform target = targets[i];
            if (target == null) continue;
            startPositions[i] = target.position;
            startScales[i] = target.localScale;
            startRotations[i] = target.localRotation;
            groups[i] = target.GetComponent<CanvasGroup>();
            if (groups[i] == null) groups[i] = target.gameObject.AddComponent<CanvasGroup>();
            groups[i].blocksRaycasts = false;
        }

        float elapsed = 0f;
        while (elapsed < SuctionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / SuctionDuration);
            float pull = normalized * normalized * (3f - 2f * normalized);
            float collapse = Mathf.Pow(normalized, 2.7f);

            for (int i = 0; i < targets.Length; i++)
            {
                Transform target = targets[i];
                if (target == null) continue;

                float direction = i == 1 ? -1f : 1f;
                float arc = Mathf.Sin(normalized * Mathf.PI) * (42f + i * 12f) * direction;
                Vector3 curvedTarget = targetPosition + canvas.transform.right * arc * (1f - pull);
                target.position = Vector3.LerpUnclamped(startPositions[i], curvedTarget, pull);

                Vector3 endScale = new Vector3(
                    Mathf.Max(0.012f, startScales[i].x * 0.018f),
                    Mathf.Max(0.004f, startScales[i].y * 0.006f),
                    startScales[i].z);
                target.localScale = Vector3.LerpUnclamped(startScales[i], endScale, collapse);
                target.localRotation = startRotations[i] * Quaternion.Euler(0f, 0f, 11f * pull * direction);

                if (groups[i] != null)
                    groups[i].alpha = 1f - Mathf.Clamp01((normalized - 0.72f) / 0.28f);
            }

            yield return null;
        }
    }

    private void SetMenuInteractable(bool value)
    {
        if (newGameButton != null) newGameButton.interactable = value;
        if (continueButton != null) continueButton.interactable = value && RunSaveSystem.HasSave;
        if (optionsButton != null) optionsButton.interactable = value;
        if (quitButton != null) quitButton.interactable = value;
    }

    private void BuildLoadingOverlay()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        loadingOverlay = new GameObject("GameSceneLoadingOverlay", typeof(RectTransform), typeof(Image));
        loadingOverlay.transform.SetParent(canvas.transform, false);
        RectTransform overlayRect = loadingOverlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = overlayRect.offsetMax = Vector2.zero;
        loadingOverlay.GetComponent<Image>().color = new Color(0.12f, 0.065f, 0.025f, 0.62f);

        GameObject card = new GameObject("LoadingCard", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(loadingOverlay.transform, false);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(380f, 110f);
        Image cardImage = card.GetComponent<Image>();
        cardImage.sprite = Resources.Load<Sprite>("UI/Parchment/Panel_Parchment_MissionClean");
        cardImage.type = Image.Type.Sliced;
        cardImage.pixelsPerUnitMultiplier = 3f;
        cardImage.color = new Color(1f, 0.92f, 0.75f, 1f);

        GameObject labelObject = new GameObject("LoadingText", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(card.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(20f, 12f);
        labelRect.offsetMax = new Vector2(-20f, -12f);
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = Resources.Load<TMP_FontAsset>("Art/Font/BMJUA_ttf SDF");
        label.text = "연성 준비 중...";
        label.fontSize = 29f;
        label.color = new Color32(74, 42, 24, 255);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        loadingOverlay.SetActive(false);
    }

    private void ToggleOptions()
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(!optionsPanel.activeSelf);
    }

    private static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

/// <summary>
/// Keeps a short, hierarchy-independent transition over the scene swap and
/// reveals the gameplay from the centre.
/// </summary>
public sealed class SceneRevealOverlay : MonoBehaviour
{
    private const float RevealDuration = 0.4f;

    private RectTransform left;
    private RectTransform right;
    private RectTransform top;
    private RectTransform bottom;

    public static void Create()
    {
        if (FindFirstObjectByType<SceneRevealOverlay>() != null) return;

        GameObject root = new GameObject("SceneRevealOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(SceneRevealOverlay));
        DontDestroyOnLoad(root);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        SceneRevealOverlay overlay = root.GetComponent<SceneRevealOverlay>();
        overlay.BuildPanels();
    }

    private void BuildPanels()
    {
        Color coverColor = new Color32(43, 24, 13, 255);
        left = CreatePanel("Left", coverColor);
        right = CreatePanel("Right", coverColor);
        top = CreatePanel("Top", coverColor);
        bottom = CreatePanel("Bottom", coverColor);
        SetReveal(0f);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private RectTransform CreatePanel(string panelName, Color color)
    {
        GameObject panel = new GameObject(panelName, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        Image image = panel.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return panel.GetComponent<RectTransform>();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StartCoroutine(Reveal());
    }

    private IEnumerator Reveal()
    {
        yield return null;

        float elapsed = 0f;
        while (elapsed < RevealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / RevealDuration);
            SetReveal(1f - Mathf.Pow(1f - t, 3f));
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SetReveal(float amount)
    {
        float horizontal = Mathf.Clamp01(amount * 1.12f) * 0.5f;
        float vertical = Mathf.Clamp01(amount) * 0.5f;

        SetAnchors(left, Vector2.zero, new Vector2(0.5f - horizontal, 1f));
        SetAnchors(right, new Vector2(0.5f + horizontal, 0f), Vector2.one);
        SetAnchors(bottom, new Vector2(0.5f - horizontal, 0f), new Vector2(0.5f + horizontal, 0.5f - vertical));
        SetAnchors(top, new Vector2(0.5f - horizontal, 0.5f + vertical), new Vector2(0.5f + horizontal, 1f));
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
