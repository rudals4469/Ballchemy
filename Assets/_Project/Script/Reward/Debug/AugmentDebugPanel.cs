using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public sealed class AugmentDebugPanel : MonoBehaviour
{
    [SerializeField] private RoomRewardGenerator rewardGenerator;
    [SerializeField] private BallCollection ballCollection;
    [SerializeField] private RunAugmentState runAugmentState;
    [SerializeField] private bool showOnStart = true;

    private readonly List<RewardDefinition> choices =
        new List<RewardDefinition>();
    private Vector2 scrollPosition;
    private bool isVisible;
    private Rect windowRect = new Rect(20f, 20f, 430f, 650f);

    private void Awake()
    {
        FindReferences();
        isVisible = showOnStart;
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
            isVisible = !isVisible;
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.F8))
            isVisible = !isVisible;
#endif
    }

    private void OnGUI()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (!isVisible)
            return;

        windowRect = GUI.Window(
            GetInstanceID(), windowRect, DrawWindow, "증강 테스트 패널 (F8)");
#endif
    }

    private void DrawWindow(int windowId)
    {
        FindReferences();

        GUILayout.Label("지급처 가중치 후보 3장");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Boss")) GenerateChoices(AugmentRewardSource.Boss);
        if (GUILayout.Button("Secret")) GenerateChoices(AugmentRewardSource.Secret);
        if (GUILayout.Button("Augment Room")) GenerateChoices(AugmentRewardSource.AugmentRoom);
        GUILayout.EndHorizontal();

        for (int i = 0; i < choices.Count; i++)
        {
            AugmentRewardDefinition reward = choices[i] as AugmentRewardDefinition;
            DrawRewardButton(reward, "후보 획득");
        }

        GUILayout.Space(8f);
        GUILayout.Label("전체 증강 즉시 획득 / 레벨 증가");
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        RewardCatalog catalog = rewardGenerator != null
            ? rewardGenerator.RewardCatalog
            : null;
        IReadOnlyList<RewardDefinition> rewards = catalog != null
            ? catalog.RewardDefinitions
            : null;

        if (rewards != null)
        {
            for (int i = 0; i < rewards.Count; i++)
                DrawRewardButton(rewards[i] as AugmentRewardDefinition, "획득");
        }

        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 24f));
    }

    private void GenerateChoices(AugmentRewardSource source)
    {
        choices.Clear();
        if (rewardGenerator == null || runAugmentState == null)
            return;

        choices.AddRange(rewardGenerator.GenerateAugmentChoices(
            source, 3, CreateContext()));
    }

    private void DrawRewardButton(AugmentRewardDefinition reward, string action)
    {
        if (reward == null || reward.AugmentDefinition == null)
            return;

        AugmentDefinition augment = reward.AugmentDefinition;
        int level = runAugmentState != null
            ? runAugmentState.GetLevel(augment)
            : 0;

        GUILayout.BeginHorizontal("box");
        GUILayout.Label(
            $"[{augment.ValueTier}] {augment.DisplayName}  Lv.{level}/{augment.MaxLevel}",
            GUILayout.Width(300f));

        GUI.enabled = runAugmentState != null && reward.CanApply(CreateContext());
        if (GUILayout.Button(action, GUILayout.Width(90f)))
            reward.Apply(CreateContext());
        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }

    private RewardApplyContext CreateContext()
    {
        return new RewardApplyContext(ballCollection, runAugmentState);
    }

    private void FindReferences()
    {
        if (rewardGenerator == null)
            rewardGenerator = FindFirstObjectByType<RoomRewardGenerator>();
        if (ballCollection == null)
            ballCollection = FindFirstObjectByType<BallCollection>();
        if (runAugmentState == null)
            runAugmentState = FindFirstObjectByType<RunAugmentState>();
    }
}
