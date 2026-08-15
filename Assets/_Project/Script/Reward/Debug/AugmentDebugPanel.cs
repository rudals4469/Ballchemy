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
    [SerializeField] private bool showOnStart;

    private readonly List<RewardDefinition> choices =
        new List<RewardDefinition>();
    private readonly List<AugmentRewardDefinition> allAugments =
        new List<AugmentRewardDefinition>();
    private Vector2 scrollPosition;
    private Rect scrollViewRect;
    private bool isVisible;
    private Rect windowRect = new Rect(20f, 20f, 430f, 650f);
    private GUIStyle wrappedEntryStyle;

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

        Color previousColor = GUI.color;
        GUI.color = new Color(0.025f, 0.03f, 0.04f, 0.97f);
        GUI.DrawTexture(
            new Rect(
                windowRect.x - 3f,
                windowRect.y - 3f,
                windowRect.width + 6f,
                windowRect.height + 6f),
            Texture2D.whiteTexture);
        GUI.color = new Color(0.09f, 0.1f, 0.13f, 1f);
        GUI.DrawTexture(windowRect, Texture2D.whiteTexture);
        GUI.color = previousColor;

        windowRect = GUI.Window(
            GetInstanceID(), windowRect, DrawWindow, "증강 테스트 패널 (F8)");
#endif
    }

    private void DrawWindow(int windowId)
    {
        FindReferences();
        EnsureStyles();

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
        scrollPosition = GUILayout.BeginScrollView(
            scrollPosition,
            false,
            true,
            GUILayout.ExpandHeight(true));

        RewardCatalog catalog = rewardGenerator != null
            ? rewardGenerator.RewardCatalog
            : null;
        allAugments.Clear();
        catalog?.GetAugmentRewards(allAugments);
        for (int i = 0; i < allAugments.Count; i++)
        {
            DrawRewardButton(allAugments[i], "획득");
        }

        GUILayout.EndScrollView();
        scrollViewRect = GUILayoutUtility.GetLastRect();
        HandleScrollDrag();
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

        Color previousBackground = GUI.backgroundColor;
        Color previousContent = GUI.contentColor;
        GUI.backgroundColor = ResolveEntryColor(augment);
        GUI.contentColor = Color.white;
        GUILayout.BeginHorizontal("box");
        GUILayout.Label(
            $"{ResolveEntryPrefix(augment)} {augment.DisplayName}  Lv.{level}/{augment.MaxLevel}",
            wrappedEntryStyle,
            GUILayout.Width(290f));

        GUI.enabled = runAugmentState != null && reward.CanApply(CreateContext());
        if (GUILayout.Button(action, GUILayout.Width(90f)))
            reward.Apply(CreateContext());
        GUI.enabled = true;
        GUILayout.EndHorizontal();
        GUI.backgroundColor = previousBackground;
        GUI.contentColor = previousContent;
    }

    private void HandleScrollDrag()
    {
        Event current = Event.current;
        if (current == null || current.button != 0 ||
            current.type != EventType.MouseDrag ||
            !scrollViewRect.Contains(current.mousePosition))
            return;

        scrollPosition.y = Mathf.Max(0f, scrollPosition.y - current.delta.y);
        current.Use();
    }

    private static string ResolveEntryPrefix(AugmentDefinition augment)
    {
        if (augment is RuleAugmentDefinition rule)
            return $"[{rule.BuildTag} / {augment.ValueTier}]";
        return $"[기본 / {augment.ValueTier}]";
    }

    private static Color ResolveEntryColor(AugmentDefinition augment)
    {
        switch (augment.ValueTier)
        {
            case AugmentValueTier.Value1: return new Color(0.60f, 0.46f, 0.34f, 1f);
            case AugmentValueTier.Value2: return new Color(0.56f, 0.59f, 0.63f, 1f);
            default: return new Color(0.66f, 0.57f, 0.32f, 1f);
        }
    }

    private void EnsureStyles()
    {
        if (wrappedEntryStyle != null)
            return;

        wrappedEntryStyle = new GUIStyle(GUI.skin.label)
        {
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(4, 4, 3, 3)
        };
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
