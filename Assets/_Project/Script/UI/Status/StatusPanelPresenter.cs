using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StatusPanelPresenter : MonoBehaviour
{
    [Header("Stage")]
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private TMP_Text playTimeText;
    [Header("Ball Composition")]
    [SerializeField] private Transform ballContent;
    [SerializeField] private StatusEntryView ballEntryTemplate;
    [SerializeField] private TMP_Text ballEmptyText;
    [SerializeField] private TMP_Text totalBallCountText;
    [Header("Two Column Effects")]
    [SerializeField] private Transform augmentContent;
    [SerializeField] private StatusEntryView augmentEntryTemplate;
    [SerializeField] private TMP_Text augmentEmptyText;
    [SerializeField] private Transform beneficialContent;
    [SerializeField] private StatusEntryView beneficialEntryTemplate;
    [SerializeField] private TMP_Text beneficialEmptyText;
    [Header("Profession")]
    [SerializeField] private TMP_Text professionValueText;
    [Header("Shared Tooltip")]
    [SerializeField] private RectTransform tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;
    [Header("Runtime References")]
    [SerializeField] private StageRoomNavigator roomNavigator;
    [SerializeField] private BallCollection ballCollection;
    [SerializeField] private RunAugmentState runAugmentState;
    [SerializeField] private StageModifierState stageModifierState;
    [SerializeField] private RunRewardState runRewardState;
    [SerializeField] private SecretRoomKeyState secretRoomKeyState;
    [SerializeField] private BallRuntimeStats ballRuntimeStats;
    [SerializeField] private EventRoomController eventRoomController;
    [SerializeField] private StageMapRevealState stageMapRevealState;
    [SerializeField] private RoomClearHealingState roomClearHealingState;
    [SerializeField] private RunCombatGoldGainState runCombatGoldGainState;
    [SerializeField] private RetreatCostDiscountState retreatCostDiscountState;
    [SerializeField] private ShopPriceDiscountState shopPriceDiscountState;
    [SerializeField] private StageBuffAmplificationState stageBuffAmplificationState;

    private readonly List<StatusEntryView> ballEntries = new();
    private readonly List<StatusEntryView> augmentEntries = new();
    private readonly List<StatusEntryView> beneficialEntries = new();
    private float runPlayTime;
    private int displayedSecond = -1;

    private sealed class BallGroup
    {
        public string Name, Description;
        public Sprite Sprite;
        public Color Color = Color.white;
        public readonly int[] Counts = new int[3];
    }

    private readonly struct EffectInfo
    {
        public readonly string Name, Description, DurationLabel;
        public EffectInfo(string name, string description, string durationLabel = "")
        { Name = name; Description = description; DurationLabel = durationLabel; }
    }

    private void Awake()
    {
        FindReferences();
        InitializeTemplate(ballEntryTemplate);
        InitializeTemplate(augmentEntryTemplate);
        InitializeTemplate(beneficialEntryTemplate);
    }

    private void OnEnable()
    {
        FindReferences();
        if (ballCollection != null)
        {
            ballCollection.BallCountChanged += HandleCountChanged;
            ballCollection.BallDefinitionsReplaced += HandleCountChanged;
        }
        if (runAugmentState != null) runAugmentState.AugmentLevelChanged += HandleAugmentChanged;
        if (stageModifierState != null) stageModifierState.StateChanged += RefreshBeneficialEffects;
        if (secretRoomKeyState != null) secretRoomKeyState.KeyStateChanged += HandleKeyChanged;
        if (roomNavigator != null) roomNavigator.RoomChanged += HandleRoomChanged;
        if (ballRuntimeStats != null) ballRuntimeStats.StatsChanged += RefreshBeneficialEffects;
        if (eventRoomController != null) eventRoomController.UnknownEventApplied += HandleUnknownEventApplied;
        if (stageMapRevealState != null) stageMapRevealState.StateChanged += RefreshBeneficialEffects;
        if (roomClearHealingState != null) roomClearHealingState.HealingAmountChanged += HandleIntStateChanged;
        if (runCombatGoldGainState != null) runCombatGoldGainState.GoldGainIncreaseRatioChanged += HandleFloatStateChanged;
        if (retreatCostDiscountState != null) retreatCostDiscountState.DiscountRatioChanged += HandleFloatStateChanged;
        if (shopPriceDiscountState != null) shopPriceDiscountState.DiscountRatioChanged += HandleFloatStateChanged;
        if (stageBuffAmplificationState != null) stageBuffAmplificationState.AmplificationRatioChanged += HandleFloatStateChanged;
        RefreshAll();
    }

    private void OnDisable()
    {
        if (ballCollection != null)
        {
            ballCollection.BallCountChanged -= HandleCountChanged;
            ballCollection.BallDefinitionsReplaced -= HandleCountChanged;
        }
        if (runAugmentState != null) runAugmentState.AugmentLevelChanged -= HandleAugmentChanged;
        if (stageModifierState != null) stageModifierState.StateChanged -= RefreshBeneficialEffects;
        if (secretRoomKeyState != null) secretRoomKeyState.KeyStateChanged -= HandleKeyChanged;
        if (roomNavigator != null) roomNavigator.RoomChanged -= HandleRoomChanged;
        if (ballRuntimeStats != null) ballRuntimeStats.StatsChanged -= RefreshBeneficialEffects;
        if (eventRoomController != null) eventRoomController.UnknownEventApplied -= HandleUnknownEventApplied;
        if (stageMapRevealState != null) stageMapRevealState.StateChanged -= RefreshBeneficialEffects;
        if (roomClearHealingState != null) roomClearHealingState.HealingAmountChanged -= HandleIntStateChanged;
        if (runCombatGoldGainState != null) runCombatGoldGainState.GoldGainIncreaseRatioChanged -= HandleFloatStateChanged;
        if (retreatCostDiscountState != null) retreatCostDiscountState.DiscountRatioChanged -= HandleFloatStateChanged;
        if (shopPriceDiscountState != null) shopPriceDiscountState.DiscountRatioChanged -= HandleFloatStateChanged;
        if (stageBuffAmplificationState != null) stageBuffAmplificationState.AmplificationRatioChanged -= HandleFloatStateChanged;
    }

    private void Update()
    {
        runPlayTime += Time.deltaTime;
        int second = Mathf.FloorToInt(runPlayTime);
        if (second == displayedSecond) return;
        displayedSecond = second;
        if (playTimeText != null) playTimeText.text = $"Play Time  {second / 60:00}:{second % 60:00}";
    }

    private void FindReferences()
    {
        if (roomNavigator == null) roomNavigator = FindFirstObjectByType<StageRoomNavigator>();
        if (ballCollection == null) ballCollection = FindFirstObjectByType<BallCollection>();
        if (runAugmentState == null) runAugmentState = FindFirstObjectByType<RunAugmentState>();
        if (stageModifierState == null) stageModifierState = FindFirstObjectByType<StageModifierState>();
        if (runRewardState == null) runRewardState = FindFirstObjectByType<RunRewardState>();
        if (secretRoomKeyState == null) secretRoomKeyState = FindFirstObjectByType<SecretRoomKeyState>();
        if (ballRuntimeStats == null && ballCollection != null) ballRuntimeStats = ballCollection.RuntimeStats;
        if (eventRoomController == null) eventRoomController = FindFirstObjectByType<EventRoomController>();
        if (stageMapRevealState == null) stageMapRevealState = FindFirstObjectByType<StageMapRevealState>();
        if (roomClearHealingState == null) roomClearHealingState = FindFirstObjectByType<RoomClearHealingState>();
        if (runCombatGoldGainState == null) runCombatGoldGainState = FindFirstObjectByType<RunCombatGoldGainState>();
        if (retreatCostDiscountState == null) retreatCostDiscountState = FindFirstObjectByType<RetreatCostDiscountState>();
        if (shopPriceDiscountState == null) shopPriceDiscountState = FindFirstObjectByType<ShopPriceDiscountState>();
        if (stageBuffAmplificationState == null) stageBuffAmplificationState = FindFirstObjectByType<StageBuffAmplificationState>();
    }

    private void RefreshAll()
    {
        RefreshStage(); RefreshBalls(); RefreshAugments(); RefreshBeneficialEffects();
        if (professionValueText != null) professionValueText.text = string.Empty;
    }

    private void RefreshStage()
    {
        int number = roomNavigator != null && roomNavigator.CurrentMap != null
            ? roomNavigator.CurrentMap.StageNumber : 1;
        if (stageText != null) stageText.text = $"Stage {number}";
    }

    private void RefreshBalls()
    {
        Dictionary<string, BallGroup> groups = new();
        int totalBallCount = 0;
        if (ballCollection != null)
        {
            foreach (Ball ball in ballCollection.Balls)
            {
                BallDefinition definition = ball != null ? ball.Definition : null;
                if (definition == null) continue;
                totalBallCount++;
                ResolveBallGroup(definition, out string key, out string description);
                if (!groups.TryGetValue(key, out BallGroup group))
                {
                    group = new BallGroup { Name = key, Description = description,
                        Sprite = definition.Sprite, Color = definition.Color };
                    groups.Add(key, group);
                }
                int grade = definition.StarGrade switch
                { BallStarGrade.OneStar => 0, BallStarGrade.TwoStar => 1, _ => 2 };
                group.Counts[grade]++;
            }
        }
        List<BallGroup> list = new(groups.Values);
        if (totalBallCountText != null) totalBallCountText.text = $"총 {totalBallCount}개";
        list.Sort((a, b) => ResolveBallOrder(a.Name).CompareTo(ResolveBallOrder(b.Name)));
        EnsureEntries(ballContent, ballEntryTemplate, ballEntries, list.Count);
        for (int i = 0; i < list.Count; i++)
        {
            BallGroup group = list[i];
            ballEntries[i].SetBallContent(group.Sprite, group.Color, group.Name,
                group.Counts, group.Description);
        }
        if (ballEmptyText != null) ballEmptyText.gameObject.SetActive(list.Count == 0);
    }

    private void RefreshAugments()
    {
        IReadOnlyList<AugmentRuntimeEntry> source = runAugmentState != null ? runAugmentState.ActiveAugments : null;
        int count = source != null ? source.Count : 0;
        EnsureEntries(augmentContent, augmentEntryTemplate, augmentEntries, count);
        for (int i = 0; i < count; i++)
        {
            AugmentRuntimeEntry entry = source[i];
            AugmentDefinition definition = entry.Definition;
            augmentEntries[i].SetContent(definition != null ? definition.Icon : null, Color.white,
                definition != null ? definition.DisplayName : "알 수 없는 증강", $"Lv.{entry.Level}",
                definition != null ? definition.GetLevelDescription(entry.Level) : string.Empty);
        }
        if (augmentEmptyText != null) augmentEmptyText.gameObject.SetActive(count == 0);
    }

    private void RefreshBeneficialEffects()
    {
        List<EffectInfo> effects = new();
        if (stageModifierState != null)
        {
            if (stageModifierState.HasDirectDamageIncrease) effects.Add(new("공격 촉매", $"이번 스테이지 동안 모든 공의 직접 피해가 {stageModifierState.DirectDamageIncreaseRatio:P0} 증가합니다.", "- 스테이지 한정"));
            if (stageModifierState.HasEnemyMaxHealthReduction) effects.Add(new("무력화 용액", $"일반·네임드 적의 최대 체력이 {stageModifierState.EnemyMaxHealthReductionRatio:P0} 감소합니다.", "- 스테이지 한정"));
            if (stageModifierState.HasEnemyAttackDamageReduction) effects.Add(new("약화 연무", $"적이 주는 피해가 {stageModifierState.EnemyAttackDamageReductionRatio:P0} 감소합니다.", "- 스테이지 한정"));
            if (stageModifierState.HasEnemyAttackIntervalBonus) effects.Add(new("시간 점성제", $"적의 공격 주기가 {stageModifierState.EnemyAttackIntervalBonusTurns}턴 증가합니다.", "- 스테이지 한정"));
            if (stageModifierState.HasGoldGainIncrease) effects.Add(new("황금 촉매", $"이번 스테이지 동안 블럭 파괴 골드가 {stageModifierState.GoldGainIncreaseRatio:P0} 증가합니다.", "- 스테이지 한정"));
        }
        if (runRewardState != null && runRewardState.HasPendingRewardUpgrade) effects.Add(new("다음 보상 등급 상승", "다음 전투방 보상의 등급이 한 단계 상승합니다.", "- 일시적"));
        if (secretRoomKeyState != null && secretRoomKeyState.HasKey) effects.Add(new("비밀방 열쇠", "잠긴 비밀방에 최초 입장할 때 사용됩니다.", "- 일시적"));
        if (stageMapRevealState != null && stageMapRevealState.IsEntireStageMapRevealed)
            effects.Add(new("별자리 지도", "현재 스테이지의 모든 방 정보를 공개합니다.", "- 스테이지 한정"));
        if (roomClearHealingState != null && roomClearHealingState.IsActive)
            effects.Add(new("생존자의 문장", $"일반·네임드 전투방 클리어 시 체력을 {roomClearHealingState.HealingAmountPerCombatRoomClear} 회복합니다."));
        if (runCombatGoldGainState != null && runCombatGoldGainState.IsActive)
            effects.Add(new("탐욕의 반지", $"전투에서 획득하는 골드가 {runCombatGoldGainState.GoldGainIncreaseRatio:P0} 증가합니다."));
        if (retreatCostDiscountState != null && retreatCostDiscountState.HasDiscount)
            effects.Add(new("후퇴 허가증", $"다음 후퇴 비용이 {retreatCostDiscountState.DiscountRatio:P0} 감소합니다.", "- 일시적"));
        if (shopPriceDiscountState != null && shopPriceDiscountState.HasDiscount)
            effects.Add(new("상인의 계약서", $"모든 상점 상품 가격이 {shopPriceDiscountState.DiscountRatio:P0} 감소합니다."));
        if (stageBuffAmplificationState != null && stageBuffAmplificationState.IsActive)
            effects.Add(new("연금 촉매", $"이후 구매하는 스테이지 버프 효과가 {stageBuffAmplificationState.AmplificationRatio:P0} 증가합니다."));
        if (ballRuntimeStats != null)
        {
            int augmentDamage = 0;
            if (runAugmentState != null)
            {
                foreach (AugmentRuntimeEntry entry in runAugmentState.ActiveAugments)
                    if (entry.Definition is DirectDamageAugmentDefinition direct)
                        augmentDamage += direct.GetTotalDirectDamageBonus(entry.Level);
            }
            int eventDamage = ballRuntimeStats.RunDirectDamageBonus - augmentDamage;
            if (eventDamage > 0)
                effects.Add(new("이벤트 직접 피해 증가", $"이벤트 효과로 모든 공의 직접 피해가 +{eventDamage} 증가합니다."));
        }
        EnsureEntries(beneficialContent, beneficialEntryTemplate, beneficialEntries, effects.Count);
        for (int i = 0; i < effects.Count; i++) beneficialEntries[i].SetContent(null, Color.clear,
            effects[i].Name, effects[i].DurationLabel, effects[i].Description);
        if (beneficialEmptyText != null) beneficialEmptyText.gameObject.SetActive(effects.Count == 0);
    }

    private void InitializeTemplate(StatusEntryView template)
    {
        if (template == null) return;
        template.ConfigureTooltip(tooltipPanel, tooltipText,
            template.transform.Find("NameText/Underline") as RectTransform);
        template.gameObject.SetActive(false);
    }

    private void EnsureEntries(Transform content, StatusEntryView template,
        List<StatusEntryView> entries, int count)
    {
        if (content == null || template == null) return;
        while (entries.Count < count)
        {
            StatusEntryView entry = Instantiate(template, content);
            entry.name = template.name.Replace("Template", $"{entries.Count + 1:00}");
            entry.ConfigureTooltip(tooltipPanel, tooltipText,
                entry.transform.Find("NameText/Underline") as RectTransform);
            entries.Add(entry);
        }
        for (int i = 0; i < entries.Count; i++) entries[i].gameObject.SetActive(i < count);
    }

    private void HandleCountChanged(int _) => RefreshBalls();
    private void HandleAugmentChanged(AugmentDefinition _, int __, int ___) => RefreshAugments();
    private void HandleKeyChanged(bool _) => RefreshBeneficialEffects();
    private void HandleUnknownEventApplied(UnknownEventDefinition _) => RefreshBeneficialEffects();
    private void HandleFloatStateChanged(float _) => RefreshBeneficialEffects();
    private void HandleIntStateChanged(int _) => RefreshBeneficialEffects();
    private void HandleRoomChanged(RoomNode _, RoomNode __) { RefreshStage(); RefreshBeneficialEffects(); }

    private static void ResolveBallGroup(BallDefinition definition, out string name, out string description)
    {
        if (definition.TraitType == BallTraitType.Poison)
        { name = "독"; description = "직접 피해 없이 독을 부여합니다. 독 1중첩마다 이후 직접 충돌 피해 +1 (최대 10, 턴 종료 시 제거)"; return; }
        if (definition.TraitDefinition is ElementalBallTraitDefinition elemental)
        {
            name = elemental.ElementType switch { ElementType.Water => "물", ElementType.Electric => "전기", ElementType.Fire => "불", ElementType.Ice => "얼음", _ => "원소" };
            description = elemental.ElementType switch { ElementType.Water => "젖음 상태를 부여하며 전기와 감전 반응을 일으킵니다.", ElementType.Electric => "충전 상태를 부여하며 물과 감전 반응을 일으킵니다.", ElementType.Fire => "화상을 부여하며 얼음과 열충격 반응을 일으킵니다.", ElementType.Ice => "서리를 부여하며 불과 열충격 반응을 일으킵니다.", _ => "원소 상태를 부여합니다." };
            return;
        }
        name = "기본"; description = "별도의 속성 효과가 없는 기본 공입니다.";
    }

    private static int ResolveBallOrder(string name) => name switch
    { "기본" => 0, "물" => 1, "불" => 2, "얼음" => 3, "전기" => 4, "독" => 5, _ => 99 };
}
