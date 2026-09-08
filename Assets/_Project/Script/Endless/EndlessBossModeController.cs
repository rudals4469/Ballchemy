using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class EndlessBossModeController : MonoBehaviour
{
    private static readonly Color Ink = new Color32(74, 42, 24, 255);
    private readonly List<BossDefinition> bosses = new List<BossDefinition>();
    private BossEncounterController bossEncounter;
    private BlockGridManager gridManager;
    private TurnManager turnManager;
    private RoomRewardController rewardController;
    private StageRoomNavigator navigator;
    private GameObject clearPanel;
    private GameObject gameOverPanel;
    private GameObject waveTracker;
    private TMP_Text waveText;
    private bool endlessActive;
    private bool rewardPending;
    private bool nextBossPending;
    private int endlessWave;
    private int endlessTurn;
    private int lastBossIndex = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "SampleScene" ||
            FindFirstObjectByType<EndlessBossModeController>() != null)
            return;

        new GameObject("EndlessBossModeSystem")
            .AddComponent<EndlessBossModeController>()
            .Build();
    }

    private void Build()
    {
        bossEncounter = FindFirstObjectByType<BossEncounterController>();
        gridManager = FindFirstObjectByType<BlockGridManager>();
        turnManager = FindFirstObjectByType<TurnManager>();
        rewardController = FindFirstObjectByType<RoomRewardController>();
        navigator = FindFirstObjectByType<StageRoomNavigator>();
        Transform midPanel = FindTransform("MidPanelRoot");
        Canvas canvas = midPanel != null ? midPanel.GetComponentInParent<Canvas>() : FindFirstObjectByType<Canvas>();
        if (bossEncounter == null || gridManager == null || turnManager == null || canvas == null)
        {
            Debug.LogError("EndlessBossModeController: 필수 전투/UI 참조를 찾지 못했습니다.", this);
            return;
        }

        bossEncounter.CopyDebugBossesTo(bosses);
        bosses.RemoveAll(candidate => candidate == null || candidate.PatternDefinition == null || candidate.IsDescendingWave);
        bossEncounter.BossEncounterCompleted += HandleBossCompleted;
        turnManager.StateChanged += HandleTurnStateChanged;
        BuildClearPanel(canvas.transform);
        BuildGameOverPanel(canvas.transform);
        BuildWaveTracker(canvas.transform);
    }

    private void HandleBossCompleted()
    {
        if (endlessActive)
        {
            endlessWave++;
            nextBossPending = true;
            UpdateTracker();
            StartCoroutine(ContinueAfterTransition());
            return;
        }

        if (gridManager.IsFinalStage)
            StartCoroutine(ShowClearPanelAfterReward());
    }

    private IEnumerator ShowClearPanelAfterReward()
    {
        yield return null;
        while (rewardController != null && rewardController.IsRewardPending)
            yield return null;

        turnManager.SetInputLocked(true);
        navigator?.SetNavigationLocked(true);
        clearPanel.SetActive(true);
        clearPanel.transform.SetAsLastSibling();
    }

    private void EnterEndlessMode()
    {
        if (bosses.Count == 0)
        {
            Debug.LogError("EndlessBossModeController: 무한 모드에 사용할 보스가 없습니다.", this);
            return;
        }

        clearPanel.SetActive(false);
        endlessActive = true;
        endlessWave = 1;
        endlessTurn = 0;
        rewardPending = false;
        nextBossPending = true;
        gridManager.SetEndlessBossMode(true);
        waveTracker.SetActive(true);
        UpdateTracker();
        StartCoroutine(ContinueAfterTransition());
    }

    private IEnumerator ContinueAfterTransition()
    {
        yield return null;
        yield return new WaitForSeconds(0.35f);
        TryStartPendingBoss();
    }

    private void TryStartPendingBoss()
    {
        if (!endlessActive || !nextBossPending || rewardPending || bossEncounter.IsTransitioning)
            return;

        int index = PickNextBossIndex();
        int progression = Mathf.Max(endlessWave - 1, 0);
        float healthMultiplier = 1f + progression * 0.14f + progression * progression * 0.006f;
        float damageMultiplier = 1f + progression * 0.06f;
        int encounterId = -10000 - endlessWave;

        if (bossEncounter.StartEndlessBossEncounter(
                encounterId,
                bosses[index],
                healthMultiplier,
                damageMultiplier))
        {
            lastBossIndex = index;
            nextBossPending = false;
            UpdateTracker();
        }
        else
        {
            StartCoroutine(RetryPendingBoss());
        }
    }

    private IEnumerator RetryPendingBoss()
    {
        yield return new WaitForSeconds(0.5f);
        TryStartPendingBoss();
    }

    private int PickNextBossIndex()
    {
        if (bosses.Count <= 1) return 0;
        int index = Random.Range(0, bosses.Count - 1);
        if (index >= lastBossIndex) index++;
        return index;
    }

    private void HandleTurnStateChanged(TurnState state)
    {
        if (state == TurnState.GameOver)
        {
            ShowGameOverPanel();
            return;
        }

        if (!endlessActive || state != TurnState.Aiming || !bossEncounter.IsEncounterActive)
            return;

        endlessTurn++;
        UpdateTracker();
        if (endlessTurn % 6 != 0 || rewardPending || rewardController == null)
            return;

        rewardPending = rewardController.TryOpenEndlessAugmentReward(HandleEndlessRewardCompleted);
    }

    private void HandleEndlessRewardCompleted()
    {
        rewardPending = false;
        TryStartPendingBoss();
        UpdateTracker();
    }

    private void ReturnToTitle()
    {
        FindFirstObjectByType<RunSaveCoordinator>()?.SaveNow();
        gridManager?.SetEndlessBossMode(false);
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
    }

    private void RestartAfterGameOver()
    {
        RunSaveSystem.BeginNewRun();
        Time.timeScale = 1f;
        SceneManager.LoadScene("SampleScene");
    }

    private void ReturnToTitleAfterGameOver()
    {
        RunSaveSystem.BeginNewRun();
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
    }

    private void ShowGameOverPanel()
    {
        if (gameOverPanel == null) return;
        clearPanel?.SetActive(false);
        navigator?.SetNavigationLocked(true);
        gameOverPanel.SetActive(true);
        gameOverPanel.transform.SetAsLastSibling();
    }

    private void UpdateTracker()
    {
        if (waveText == null) return;
        int turnsToReward = 6 - endlessTurn % 6;
        waveText.text = $"무한 웨이브 {endlessWave}  ·  누적 {endlessTurn}턴  ·  증강까지 {turnsToReward}턴";
    }

    private void BuildClearPanel(Transform canvas)
    {
        clearPanel = Rect("GameClearPanel", canvas);
        Stretch(clearPanel.GetComponent<RectTransform>());
        Image dim = clearPanel.AddComponent<Image>();
        dim.color = new Color(0.08f, 0.04f, 0.015f, 0.58f);

        GameObject card = Rect("ClearCard", clearPanel.transform);
        SetRect(card.GetComponent<RectTransform>(), Vector2.zero, new Vector2(720f, 390f));
        Image background = card.AddComponent<Image>();
        background.sprite = Resources.Load<Sprite>("UI/Parchment/Panel_Parchment_MissionClean");
        background.type = Image.Type.Sliced;
        background.pixelsPerUnitMultiplier = 2f;
        background.color = new Color(1f, 0.93f, 0.77f, 1f);

        Text("Title", card.transform, "게임 클리어", 46f, new Vector2(0f, 115f), new Vector2(580f, 60f));
        Text("Description", card.transform, "연금술 여정을 완성했습니다.", 25f, new Vector2(0f, 48f), new Vector2(580f, 42f));
        Button exit = Button("ExitButton", card.transform, "종료", new Vector2(-155f, -90f));
        Button endless = Button("EndlessButton", card.transform, "무한 웨이브", new Vector2(155f, -90f));
        exit.onClick.AddListener(ReturnToTitle);
        endless.onClick.AddListener(EnterEndlessMode);
        clearPanel.SetActive(false);
    }

    private void BuildGameOverPanel(Transform canvas)
    {
        gameOverPanel = Rect("GameOverPanel", canvas);
        Stretch(gameOverPanel.GetComponent<RectTransform>());
        Image dim = gameOverPanel.AddComponent<Image>();
        dim.color = new Color(0.08f, 0.04f, 0.015f, 0.68f);

        GameObject card = Rect("GameOverCard", gameOverPanel.transform);
        SetRect(card.GetComponent<RectTransform>(), Vector2.zero, new Vector2(720f, 390f));
        Image background = card.AddComponent<Image>();
        background.sprite = Resources.Load<Sprite>("UI/Parchment/Panel_Parchment_MissionClean");
        background.type = Image.Type.Sliced;
        background.pixelsPerUnitMultiplier = 2f;
        background.color = new Color(1f, 0.91f, 0.73f, 1f);

        Text("Title", card.transform, "게임 오버", 46f, new Vector2(0f, 115f), new Vector2(580f, 60f));
        Text("Description", card.transform, "연성에 실패했습니다.", 25f, new Vector2(0f, 48f), new Vector2(580f, 42f));
        Button restart = Button("RestartButton", card.transform, "다시 시작", new Vector2(-155f, -90f));
        Button title = Button("TitleButton", card.transform, "메인 화면", new Vector2(155f, -90f));
        restart.onClick.AddListener(RestartAfterGameOver);
        title.onClick.AddListener(ReturnToTitleAfterGameOver);
        gameOverPanel.SetActive(false);
    }

    private void BuildWaveTracker(Transform canvas)
    {
        waveTracker = Rect("EndlessWaveTracker", canvas);
        RectTransform rect = waveTracker.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -14f);
        rect.sizeDelta = new Vector2(510f, 48f);
        Image image = waveTracker.AddComponent<Image>();
        image.sprite = Resources.Load<Sprite>("UI/Parchment/Panel_Parchment_MissionClean");
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 3f;
        image.color = new Color(1f, 0.91f, 0.72f, 0.96f);
        waveText = Text("WaveText", waveTracker.transform, string.Empty, 20f, Vector2.zero, new Vector2(480f, 36f));
        waveTracker.SetActive(false);
    }

    private static Button Button(string name, Transform parent, string label, Vector2 position)
    {
        GameObject root = Rect(name, parent);
        SetRect(root.GetComponent<RectTransform>(), position, new Vector2(260f, 72f));
        Image image = root.AddComponent<Image>();
        image.sprite = Resources.Load<Sprite>("UI/Parchment/Panel_Parchment_MissionClean");
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 3f;
        image.color = new Color(1f, 0.87f, 0.65f, 1f);
        Button button = root.AddComponent<Button>();
        button.targetGraphic = image;
        Text("Label", root.transform, label, 28f, Vector2.zero, new Vector2(230f, 54f));
        return button;
    }

    private static TMP_Text Text(string name, Transform parent, string value, float size, Vector2 position, Vector2 dimensions)
    {
        GameObject root = Rect(name, parent);
        SetRect(root.GetComponent<RectTransform>(), position, dimensions);
        TextMeshProUGUI text = root.AddComponent<TextMeshProUGUI>();
        text.font = Resources.Load<TMP_FontAsset>("Art/Font/BMJUA_ttf SDF");
        text.text = value;
        text.fontSize = size;
        text.color = Ink;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject Rect(string name, Transform parent)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        return root;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 dimensions)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static Transform FindTransform(string objectName)
    {
        foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (candidate.name == objectName) return candidate;
        return null;
    }

    private void OnDestroy()
    {
        if (bossEncounter != null) bossEncounter.BossEncounterCompleted -= HandleBossCompleted;
        if (turnManager != null) turnManager.StateChanged -= HandleTurnStateChanged;
    }
}
