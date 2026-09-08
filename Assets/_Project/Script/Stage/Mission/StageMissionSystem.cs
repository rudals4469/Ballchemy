using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum StageMissionType
{
    MaximumHitCount,
    MaximumDamageTaken,
    MinimumFinalHealthRatio,
    MinimumBallGain,
    MinimumDistinctTraits,
    MinimumCombatRoomsCleared,
    MinimumSpecialRoomsVisited,
    MaximumBossTurns,
    MinimumRoomsVisited,
    MinimumGoldGained,
    MinimumGoldSpent,
    MaximumBossHitCount,
    MaximumRetreatCount,
    MinimumShopPurchases,
    MinimumGoldReserve
}

public enum StageMissionDifficulty
{
    Easy,
    Normal,
    Hard
}

public enum StageMissionRewardKind
{
    Gold,
    BasicBalls,
    TraitBalls,
    TwoStarBall,
    Healing,
    MaxHealth,
    NextRewardTier,
    Augment,
    BallUpgrade,
    BasicBallConversion,
    ShopDiscount,
    CombatGoldBoost,
    DirectDamageBoost
}

[Serializable]
public sealed class StageMissionRewardWeights
{
    [Min(0)] public int gold = 10;
    [Min(0)] public int basicBalls = 5;
    [Min(0)] public int traitBalls = 5;
    [Min(0)] public int twoStarBall;
    [Min(0)] public int healing = 5;
    [Min(0)] public int maxHealth;
    [Min(0)] public int nextRewardTier;
    [Min(0)] public int augment;
    [Min(0)] public int ballUpgrade;
    [Min(0)] public int basicBallConversion;
    [Min(0)] public int shopDiscount;
    [Min(0)] public int combatGoldBoost;
    [Min(0)] public int directDamageBoost;

    public int Get(StageMissionRewardKind kind) => kind switch
    {
        StageMissionRewardKind.Gold => gold,
        StageMissionRewardKind.BasicBalls => basicBalls,
        StageMissionRewardKind.TraitBalls => traitBalls,
        StageMissionRewardKind.TwoStarBall => twoStarBall,
        StageMissionRewardKind.Healing => healing,
        StageMissionRewardKind.MaxHealth => maxHealth,
        StageMissionRewardKind.NextRewardTier => nextRewardTier,
        StageMissionRewardKind.Augment => augment,
        StageMissionRewardKind.BallUpgrade => ballUpgrade,
        StageMissionRewardKind.BasicBallConversion => basicBallConversion,
        StageMissionRewardKind.ShopDiscount => shopDiscount,
        StageMissionRewardKind.CombatGoldBoost => combatGoldBoost,
        StageMissionRewardKind.DirectDamageBoost => directDamageBoost,
        _ => 0
    };
}

[Serializable]
public sealed class StageMissionRewardOffer
{
    public StageMissionRewardKind Kind;
    public int GoldAmount;
    public int Value;
    public string DisplayText;
    [NonSerialized] public RewardDefinition RuntimeReward;
}

[Serializable]
public sealed class StageMissionChoice
{
    public StageMissionType Type;
    public StageMissionDifficulty Difficulty;
    public int Target;
    public int GoldReward;
    public string Title;
    public string Description;
    public StageMissionRewardOffer Reward;
}

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class StageMissionSystem : MonoBehaviour
{
    private const int ChoiceCount = 3;

    [Header("References")]
    [SerializeField] private StageRoomNavigator navigator;
    [SerializeField] private BossEncounterController bossController;
    [SerializeField] private BlockGridManager blockGridManager;
    [SerializeField] private BallCollection ballCollection;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private RunCurrencyState currencyState;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private RoomRewardGenerator rewardGenerator;
    [SerializeField] private RunAugmentState runAugmentState;
    [SerializeField] private RunRewardState runRewardState;
    [SerializeField] private RewardSelectionUI rewardSelectionUI;
    [SerializeField] private ShopPriceDiscountState shopPriceDiscountState;
    [SerializeField] private RunCombatGoldGainState combatGoldGainState;
    [SerializeField] private BallRuntimeStats ballRuntimeStats;
    [SerializeField] private RoomRetreatController retreatController;
    [SerializeField] private ShopPurchaseController shopPurchaseController;

    [Header("Rewards")]
    [SerializeField, Min(0)] private int easyGoldReward = 25;
    [SerializeField, Min(0)] private int normalGoldReward = 45;
    [SerializeField, Min(0)] private int hardGoldReward = 70;

    [Header("Reward Pool Weights")]
    [SerializeField] private StageMissionRewardWeights easyRewardWeights = new()
    {
        gold = 12, basicBalls = 7, traitBalls = 3, healing = 6
    };
    [SerializeField] private StageMissionRewardWeights normalRewardWeights = new()
    {
        gold = 7, basicBalls = 4, traitBalls = 8, healing = 4,
        maxHealth = 3, nextRewardTier = 2, ballUpgrade = 2,
        basicBallConversion = 3, shopDiscount = 2, combatGoldBoost = 2
    };
    [SerializeField] private StageMissionRewardWeights hardRewardWeights = new()
    {
        gold = 5, basicBalls = 2, traitBalls = 5, twoStarBall = 5,
        healing = 2, maxHealth = 4, nextRewardTier = 4, augment = 2,
        ballUpgrade = 5, basicBallConversion = 3, shopDiscount = 3,
        combatGoldBoost = 3, directDamageBoost = 2
    };

    [Header("Rare Reward Run Limits")]
    [SerializeField, Min(0)] private int maximumTwoStarRewards = 3;
    [SerializeField, Min(0)] private int maximumTierUpgradeRewards = 2;
    [SerializeField, Min(0)] private int maximumAugmentRewards = 2;

    private readonly List<StageMissionChoice> choices = new();
    private readonly HashSet<int> visitedSpecialRoomIds = new();
    private readonly HashSet<int> visitedRoomIds = new();
    private StageMissionChoice activeMission;
    private StageMap activeMap;
    private int startingBallCount;
    private int damageTaken;
    private int hitCount;
    private int combatRoomsCleared;
    private int bossStartTurn;
    private int bossTurnsElapsed;
    private int goldGained;
    private int goldSpent;
    private int bossHitCount;
    private int retreatCount;
    private int shopPurchaseCount;
    private int startingGold;
    private bool bossStarted;
    private bool selectionOpen;
    private int twoStarRewardsGranted;
    private int tierUpgradeRewardsGranted;
    private int augmentRewardsGranted;

    [Header("Hierarchy UI")]
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private TMP_Text stageTitleText;
    [SerializeField] private Button[] cardButtons = new Button[ChoiceCount];
    [SerializeField] private CommonChoiceCardLayout[] cardLayouts = new CommonChoiceCardLayout[ChoiceCount];
    [SerializeField] private Image[] cardIcons = new Image[ChoiceCount];
    [SerializeField] private GameObject[] cardIconRoots = new GameObject[ChoiceCount];
    [SerializeField] private TMP_Text[] cardTitleTexts = new TMP_Text[ChoiceCount];
    [SerializeField] private TMP_Text[] cardRewardTexts = new TMP_Text[ChoiceCount];
    [SerializeField] private TMP_Text[] cardDescriptionTexts = new TMP_Text[ChoiceCount];
    [SerializeField] private TMP_Text[] cardPayoutTexts = new TMP_Text[ChoiceCount];
    [SerializeField] private GameObject trackerPanel;
    [SerializeField] private TMP_Text trackerText;
    [SerializeField] private TMP_Text resultText;
    [Header("Selected Mission Status")]
    [SerializeField] private GameObject missionStatusRoot;
    [SerializeField] private TMP_Text missionStatusNameText;
    [SerializeField] private TMP_Text missionStatusConditionText;
    [SerializeField] private TMP_Text missionStatusRewardText;

    public StageMissionChoice ActiveMission => activeMission;
    public bool HasActiveMission => activeMission != null;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
        EnsureCardReferences();
    }

    private void OnEnable()
    {
        BindCardButtons();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        UnbindCardButtons();
    }

    private void OnValidate()
    {
        NormalizeSettings();
        FindReferences();
    }

    private void Update()
    {
        if (bossStarted && blockGridManager != null)
        {
            bossTurnsElapsed =
                Mathf.Max(
                    bossTurnsElapsed,
                    blockGridManager.CurrentTurn - bossStartTurn
                );
        }

        if (activeMission != null)
        {
            RefreshTracker();
        }
    }

    private void FindReferences()
    {
        navigator ??= FindFirstObjectByType<StageRoomNavigator>();
        bossController ??= FindFirstObjectByType<BossEncounterController>();
        blockGridManager ??= FindFirstObjectByType<BlockGridManager>();
        ballCollection ??= FindFirstObjectByType<BallCollection>();
        playerHealth ??= FindFirstObjectByType<PlayerHealth>();
        currencyState ??= FindFirstObjectByType<RunCurrencyState>();
        turnManager ??= FindFirstObjectByType<TurnManager>();
        rewardGenerator ??= FindFirstObjectByType<RoomRewardGenerator>();
        runAugmentState ??= FindFirstObjectByType<RunAugmentState>();
        runRewardState ??= FindFirstObjectByType<RunRewardState>();
        rewardSelectionUI ??= FindFirstObjectByType<RewardSelectionUI>(FindObjectsInactive.Include);
        shopPriceDiscountState ??= FindFirstObjectByType<ShopPriceDiscountState>();
        combatGoldGainState ??= FindFirstObjectByType<RunCombatGoldGainState>();
        ballRuntimeStats ??= ballCollection != null ? ballCollection.RuntimeStats : null;
        retreatController ??= FindFirstObjectByType<RoomRetreatController>();
        shopPurchaseController ??= FindFirstObjectByType<ShopPurchaseController>();
    }

    private void NormalizeSettings()
    {
        easyGoldReward = Mathf.Max(easyGoldReward, 0);
        normalGoldReward = Mathf.Max(normalGoldReward, easyGoldReward);
        hardGoldReward = Mathf.Max(hardGoldReward, normalGoldReward);
        maximumTwoStarRewards = Mathf.Max(maximumTwoStarRewards, 0);
        maximumTierUpgradeRewards = Mathf.Max(maximumTierUpgradeRewards, 0);
        maximumAugmentRewards = Mathf.Max(maximumAugmentRewards, 0);
    }

    private void Subscribe()
    {
        if (navigator != null)
        {
            navigator.MapInitialized -= HandleMapInitialized;
            navigator.MapInitialized += HandleMapInitialized;
            navigator.RoomChanged -= HandleRoomChanged;
            navigator.RoomChanged += HandleRoomChanged;
            navigator.CombatRoomCleared -= HandleCombatRoomCleared;
            navigator.CombatRoomCleared += HandleCombatRoomCleared;
        }
        if (playerHealth != null)
        {
            playerHealth.Damaged -= HandleDamaged;
            playerHealth.Damaged += HandleDamaged;
        }
        if (bossController != null)
        {
            bossController.BossEncounterStarted -= HandleBossStarted;
            bossController.BossEncounterStarted += HandleBossStarted;
            bossController.BossEncounterCompleted -= HandleBossCompleted;
            bossController.BossEncounterCompleted += HandleBossCompleted;
        }
        if (currencyState != null)
        {
            currencyState.GoldAdded -= HandleGoldAdded;
            currencyState.GoldAdded += HandleGoldAdded;
            currencyState.GoldSpent -= HandleGoldSpent;
            currencyState.GoldSpent += HandleGoldSpent;
        }
        if (retreatController != null)
        {
            retreatController.RetreatCompleted -= HandleRetreatCompleted;
            retreatController.RetreatCompleted += HandleRetreatCompleted;
        }
        if (shopPurchaseController != null)
        {
            shopPurchaseController.PurchaseSucceeded -= HandlePurchaseSucceeded;
            shopPurchaseController.PurchaseSucceeded += HandlePurchaseSucceeded;
        }
    }

    private void Unsubscribe()
    {
        if (navigator != null)
        {
            navigator.MapInitialized -= HandleMapInitialized;
            navigator.RoomChanged -= HandleRoomChanged;
            navigator.CombatRoomCleared -= HandleCombatRoomCleared;
        }
        if (playerHealth != null) playerHealth.Damaged -= HandleDamaged;
        if (bossController != null)
        {
            bossController.BossEncounterStarted -= HandleBossStarted;
            bossController.BossEncounterCompleted -= HandleBossCompleted;
        }
        if (currencyState != null)
        {
            currencyState.GoldAdded -= HandleGoldAdded;
            currencyState.GoldSpent -= HandleGoldSpent;
        }
        if (retreatController != null) retreatController.RetreatCompleted -= HandleRetreatCompleted;
        if (shopPurchaseController != null) shopPurchaseController.PurchaseSucceeded -= HandlePurchaseSucceeded;
    }

    private void HandleMapInitialized(StageMap map)
    {
        if (map == null || map.StageNumber <= 0) return;

        activeMap = map;
        activeMission = null;
        ClearMissionStatus();
        startingBallCount = ballCollection != null ? ballCollection.Count : 0;
        damageTaken = 0;
        hitCount = 0;
        combatRoomsCleared = 0;
        bossStartTurn = 0;
        bossTurnsElapsed = 0;
        goldGained = 0;
        goldSpent = 0;
        bossHitCount = 0;
        retreatCount = 0;
        shopPurchaseCount = 0;
        startingGold = currencyState != null ? currencyState.CurrentGold : 0;
        bossStarted = false;
        visitedSpecialRoomIds.Clear();
        visitedRoomIds.Clear();
        GenerateChoices(map);
        ShowSelection(map.StageNumber);
    }

    private void HandleDamaged(int amount)
    {
        if (activeMission == null || amount <= 0) return;
        damageTaken += amount;
        hitCount++;
        if (bossStarted) bossHitCount++;
        RefreshTracker();
    }

    private void HandleCombatRoomCleared(RoomNode room)
    {
        if (activeMission == null || room == null || room.RoomType == RoomType.Boss) return;
        combatRoomsCleared++;
        RefreshTracker();
    }

    private void HandleRoomChanged(RoomNode previous, RoomNode current)
    {
        if (activeMission == null || current == null) return;
        visitedRoomIds.Add(current.RoomId);
        if (IsSpecialRoom(current.RoomType))
        {
            visitedSpecialRoomIds.Add(current.RoomId);
        }
        RefreshTracker();
    }

    private void HandleGoldAdded(int amount)
    {
        if (activeMission == null || amount <= 0) return;
        goldGained += amount;
        RefreshTracker();
    }

    private void HandleGoldSpent(int amount)
    {
        if (activeMission == null || amount <= 0) return;
        goldSpent += amount;
        RefreshTracker();
    }

    private void HandleRetreatCompleted(int cost)
    {
        if (activeMission == null) return;
        retreatCount++;
        RefreshTracker();
    }

    private void HandlePurchaseSucceeded(
        int roomId,
        int slotIndex,
        ShopItemDefinition item)
    {
        if (activeMission == null) return;
        shopPurchaseCount++;
        RefreshTracker();
    }

    private void HandleBossStarted()
    {
        bossStarted = true;
        bossStartTurn = blockGridManager != null ? blockGridManager.CurrentTurn : 0;
        bossTurnsElapsed = 0;
        bossHitCount = 0;
    }

    private void HandleBossCompleted()
    {
        if (activeMission == null) return;

        int finalResult = GetCurrentValue(activeMission);
        bool succeeded = IsMissionSucceeded(activeMission, finalResult);
        bool rewardApplied = false;
        if (succeeded)
        {
            if (activeMission.Reward?.Kind == StageMissionRewardKind.Augment &&
                rewardSelectionUI != null && rewardSelectionUI.IsOpen)
            {
                StartCoroutine(ApplyDeferredReward(activeMission.Reward));
                rewardApplied = true;
            }
            else
            {
                rewardApplied = ApplyMissionReward(activeMission.Reward);
            }
        }

        Debug.Log(
            "StageMissionSystem: " +
            $"Stage={activeMap?.StageNumber ?? 0}, " +
            $"Type={activeMission.Type}, " +
            $"Difficulty={activeMission.Difficulty}, " +
            $"Target={activeMission.Target}, " +
            $"Result={finalResult}, " +
            $"Succeeded={succeeded}, " +
            $"Reward={activeMission.Reward?.DisplayText}, " +
            $"RewardApplied={rewardApplied}",
            this
        );

        if (resultText != null)
        {
            resultText.text = succeeded
                ? $"계약 성공  {activeMission.Reward?.DisplayText}"
                : "계약 실패  보상 없음";
            resultText.color = succeeded
                ? new Color(0.55f, 1f, 0.62f)
                : new Color(1f, 0.48f, 0.48f);
            resultText.gameObject.SetActive(true);
        }

        activeMission = null;
        if (trackerPanel != null) trackerPanel.SetActive(false);
        ClearMissionStatus();
    }

    private IEnumerator ApplyDeferredReward(StageMissionRewardOffer offer)
    {
        while (rewardSelectionUI != null && rewardSelectionUI.IsOpen)
        {
            yield return null;
        }

        bool applied = ApplyMissionReward(offer);
        if (!applied)
        {
            Debug.LogWarning(
                "StageMissionSystem: 지연된 미션 보상을 적용하지 못했습니다. " +
                $"Reward={offer?.DisplayText}",
                this
            );
        }
    }

    private void GenerateChoices(StageMap map)
    {
        choices.Clear();
        List<StageMissionType> pool = BuildValidPool(map);
        StageMissionDifficulty[] difficulties =
        {
            StageMissionDifficulty.Easy,
            StageMissionDifficulty.Normal,
            StageMissionDifficulty.Hard
        };

        int seed = unchecked(map.StageNumber * 73856093 + CountRooms(map) * 19349663 + Environment.TickCount);
        System.Random random = new(seed);
        Shuffle(pool, random);

        HashSet<int> usedCategories = new();
        HashSet<StageMissionRewardKind> usedRewardKinds = new();
        for (int i = 0; i < pool.Count && choices.Count < ChoiceCount; i++)
        {
            StageMissionType type = pool[i];
            if (!usedCategories.Add(GetMissionCategory(type))) continue;
            StageMissionChoice choice = CreateChoice(
                type, difficulties[choices.Count], map, random, usedRewardKinds);
            choices.Add(choice);
            usedRewardKinds.Add(choice.Reward.Kind);
        }

        for (int i = 0; i < pool.Count && choices.Count < ChoiceCount; i++)
        {
            StageMissionType type = pool[i];
            bool alreadyUsed = choices.Exists(choice => choice.Type == type);
            if (!alreadyUsed)
            {
                StageMissionChoice choice = CreateChoice(
                    type, difficulties[choices.Count], map, random, usedRewardKinds);
                choices.Add(choice);
                usedRewardKinds.Add(choice.Reward.Kind);
            }
        }
    }

    private List<StageMissionType> BuildValidPool(StageMap map)
    {
        List<StageMissionType> pool = new()
        {
            StageMissionType.MaximumHitCount,
            StageMissionType.MaximumDamageTaken,
            StageMissionType.MinimumFinalHealthRatio,
            StageMissionType.MinimumBallGain,
            StageMissionType.MaximumBossTurns,
            StageMissionType.MinimumRoomsVisited,
            StageMissionType.MinimumGoldGained,
            StageMissionType.MaximumBossHitCount,
            StageMissionType.MaximumRetreatCount,
            StageMissionType.MinimumGoldReserve
        };

        if (CountDistinctTraits() < 4) pool.Add(StageMissionType.MinimumDistinctTraits);
        if (CountRooms(map, true) >= 2) pool.Add(StageMissionType.MinimumCombatRoomsCleared);
        if (CountSpecialRooms(map) >= 1) pool.Add(StageMissionType.MinimumSpecialRoomsVisited);
        if (CountRooms(map, RoomType.Shop) >= 1)
        {
            pool.Add(StageMissionType.MinimumGoldSpent);
            pool.Add(StageMissionType.MinimumShopPurchases);
        }
        return pool;
    }

    private static int GetMissionCategory(StageMissionType type)
    {
        return type switch
        {
            StageMissionType.MaximumHitCount or
            StageMissionType.MaximumDamageTaken or
            StageMissionType.MinimumFinalHealthRatio => 0,
            StageMissionType.MinimumBallGain or
            StageMissionType.MinimumDistinctTraits => 1,
            StageMissionType.MinimumCombatRoomsCleared or
            StageMissionType.MinimumSpecialRoomsVisited or
            StageMissionType.MinimumRoomsVisited => 2,
            StageMissionType.MaximumBossTurns => 3,
            StageMissionType.MaximumBossHitCount => 3,
            StageMissionType.MaximumRetreatCount => 3,
            StageMissionType.MinimumShopPurchases => 4,
            StageMissionType.MinimumGoldReserve => 4,
            _ => 4
        };
    }

    private StageMissionChoice CreateChoice(
        StageMissionType type,
        StageMissionDifficulty difficulty,
        StageMap map,
        System.Random random,
        ISet<StageMissionRewardKind> excludedRewardKinds)
    {
        int level = (int)difficulty;
        int target;
        string title;
        string description;

        switch (type)
        {
            case StageMissionType.MaximumHitCount:
                target = new[] { 5, 3, 1 }[level];
                title = "빈틈없는 방어";
                description = $"피격 {target}회 이하";
                break;
            case StageMissionType.MaximumDamageTaken:
                target = new[] { 16, 10, 6 }[level];
                title = "손실 억제";
                description = $"받는 피해 {target} 이하";
                break;
            case StageMissionType.MinimumFinalHealthRatio:
                target = new[] { 30, 50, 70 }[level];
                title = "생존 본능";
                description = $"보스 처치 시 체력 {target}% 이상";
                break;
            case StageMissionType.MinimumBallGain:
                target = new[] { 5, 10, 15 }[level];
                title = "전력 증강";
                description = $"공을 시작보다 {target}개 더 보유";
                break;
            case StageMissionType.MinimumDistinctTraits:
                target = Mathf.Clamp(new[] { 2, 3, 4 }[level], CountDistinctTraits() + 1, 4);
                title = "다채로운 조제";
                description = $"서로 다른 공 특성 {target}종 보유";
                break;
            case StageMissionType.MinimumCombatRoomsCleared:
                target = Mathf.Min(new[] { 2, 3, 4 }[level], CountRooms(map, true));
                title = "전투 탐사";
                description = $"일반 전투방 {target}개 클리어";
                break;
            case StageMissionType.MinimumSpecialRoomsVisited:
                target = Mathf.Min(new[] { 1, 2, 3 }[level], CountSpecialRooms(map));
                title = "연금술사의 여정";
                description = $"특수방 {target}곳 방문";
                break;
            case StageMissionType.MinimumRoomsVisited:
                target = Mathf.Min(new[] { 3, 5, 7 }[level], CountRooms(map));
                title = "꼼꼼한 탐사";
                description = $"서로 다른 방 {target}곳 방문";
                break;
            case StageMissionType.MinimumGoldGained:
                target = new[] { 30, 55, 85 }[level];
                title = "황금 수확";
                description = $"이번 스테이지에서 {target}G 획득";
                break;
            case StageMissionType.MinimumGoldSpent:
                target = new[] { 25, 60, 100 }[level];
                title = "과감한 투자";
                description = $"이번 스테이지에서 {target}G 사용";
                break;
            case StageMissionType.MaximumBossHitCount:
                target = new[] { 4, 2, 0 }[level];
                title = "보스 공략 전문가";
                description = $"보스전 피격 {target}회 이하";
                break;
            case StageMissionType.MaximumRetreatCount:
                target = new[] { 2, 1, 0 }[level];
                title = "전진 또 전진";
                description = $"후퇴 {target}회 이하";
                break;
            case StageMissionType.MinimumShopPurchases:
                target = new[] { 1, 2, 3 }[level];
                title = "계획적인 소비";
                description = $"상점에서 {target}회 구매";
                break;
            case StageMissionType.MinimumGoldReserve:
                int reservePercent = new[] { 50, 75, 100 }[level];
                int minimumReserve = new[] { 20, 40, 60 }[level];
                target = Mathf.Max(
                    minimumReserve,
                    Mathf.CeilToInt(startingGold * reservePercent / 100f));
                title = "비상금 확보";
                description = $"보스 처치 시 {target}G 이상 보유";
                break;
            default:
                target = new[] { 10, 7, 5 }[level];
                title = "속전속결";
                description = $"보스를 {target}턴 이내 처치";
                break;
        }

        return new StageMissionChoice
        {
            Type = type,
            Difficulty = difficulty,
            Target = target,
            GoldReward = GetGoldReward(difficulty),
            Title = title,
            Description = description,
            Reward = GenerateRewardOffer(difficulty, random, excludedRewardKinds)
        };
    }

    private void SelectChoice(int index)
    {
        if (!selectionOpen || index < 0 || index >= choices.Count) return;
        activeMission = choices[index];
        selectionOpen = false;
        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (trackerPanel != null) trackerPanel.SetActive(false);
        if (resultText != null) resultText.gameObject.SetActive(false);
        navigator?.SetNavigationLocked(false);
        turnManager?.SetInputLocked(false);
        RefreshTracker();
        RefreshMissionStatus();
    }

    private void ShowSelection(int stageNumber)
    {
        if (selectionPanel == null || choices.Count == 0) return;
        EnsureCardReferences();
        selectionOpen = true;
        selectionPanel.SetActive(true);
        if (trackerPanel != null) trackerPanel.SetActive(false);
        if (resultText != null) resultText.gameObject.SetActive(false);
        if (stageTitleText != null) stageTitleText.text = $"STAGE {stageNumber}  미션 선택";
        navigator?.SetNavigationLocked(true);
        turnManager?.SetInputLocked(true);

        for (int i = 0; i < cardButtons.Length; i++)
        {
            bool valid = i < choices.Count;
            if (cardButtons[i] == null) continue;
            cardButtons[i].gameObject.SetActive(valid);
            if (!valid) continue;
            StageMissionChoice choice = choices[i];
            if (i < cardLayouts.Length && cardLayouts[i] != null)
            {
                cardLayouts[i].Configure(
                    cardButtons[i], Get(cardIcons, i), Get(cardIconRoots, i),
                    Get(cardTitleTexts, i), Get(cardRewardTexts, i),
                    Get(cardDescriptionTexts, i));
                cardLayouts[i].SetCategory(CommonChoiceCardLayout.CardCategory.Event);
                cardLayouts[i].SetTexts(
                    choice.Title,
                    DifficultyName(choice.Difficulty),
                    $"조건 : {choice.Description}");
                CommonChoiceCardLayout.SetText(
                    Get(cardPayoutTexts, i), $"보상 : {choice.Reward.DisplayText}");
                ConfigureTruncatedTooltip(Get(cardDescriptionTexts, i),
                    $"조건 : {choice.Description}");
                ConfigureTruncatedTooltip(Get(cardPayoutTexts, i),
                    $"보상 : {choice.Reward.DisplayText}");
            }
        }
    }

    private static T Get<T>(T[] values, int index) where T : UnityEngine.Object
        => values != null && index >= 0 && index < values.Length ? values[index] : null;

    private void EnsureCardReferences()
    {
        for (int i = 0; i < cardButtons.Length; i++)
        {
            Button button = cardButtons[i];
            if (button == null) continue;
            if (Get(cardTitleTexts, i) == null)
                cardTitleTexts[i] = FindNamed<TMP_Text>(button.transform, "TitleText");
            if (Get(cardRewardTexts, i) == null)
                cardRewardTexts[i] = FindNamed<TMP_Text>(button.transform, "GrantText");
            if (Get(cardDescriptionTexts, i) == null)
                cardDescriptionTexts[i] = FindNamed<TMP_Text>(button.transform, "EffectText");
            if (Get(cardPayoutTexts, i) == null)
                cardPayoutTexts[i] = FindNamed<TMP_Text>(button.transform, "MissionPayoutText");
            if (Get(cardIcons, i) == null)
                cardIcons[i] = FindNamed<Image>(button.transform, "Icon");
            if (Get(cardIconRoots, i) == null && Get(cardIcons, i) != null)
                cardIconRoots[i] = cardIcons[i].transform.parent.gameObject;
            if (Get(cardRewardTexts, i) != null)
                cardRewardTexts[i].fontSize = 20f;
            if (Get(cardDescriptionTexts, i) != null)
                cardDescriptionTexts[i].fontSize = 22f;
            if (Get(cardPayoutTexts, i) != null)
                cardPayoutTexts[i].fontSize = 22f;
        }
    }

    private static T FindNamed<T>(Transform root, string objectName) where T : Component
    {
        foreach (T component in root.GetComponentsInChildren<T>(true))
            if (component.name == objectName) return component;
        return null;
    }

    private void RefreshTracker()
    {
        if (activeMission == null || trackerText == null) return;
        int current = GetCurrentValue(activeMission);
        bool failed = IsIrrecoverablyFailed(activeMission, current);
        trackerText.color = failed ? new Color(1f, 0.45f, 0.45f) : Color.white;
        trackerText.text =
            $"<b>{activeMission.Title}</b>  {FormatProgress(activeMission, current)}\n" +
            $"<size=18><color=#FFD66B>성공 보상 {activeMission.Reward.DisplayText}</color></size>";
        RefreshMissionStatus();
    }

    private void RefreshMissionStatus()
    {
        if (missionStatusRoot == null) return;
        missionStatusRoot.SetActive(true);
        if (activeMission == null)
        {
            ClearMissionStatusTexts();
            return;
        }

        int current = GetCurrentValue(activeMission);
        CommonChoiceCardLayout.SetText(missionStatusNameText,
            $"[{DifficultyName(activeMission.Difficulty)}] {activeMission.Title}");
        CommonChoiceCardLayout.SetText(missionStatusConditionText,
            $"조건 : {activeMission.Description}  ({FormatProgress(activeMission, current)})");
        CommonChoiceCardLayout.SetText(missionStatusRewardText,
            $"보상 : {activeMission.Reward.DisplayText}");
        ConfigureTruncatedTooltip(missionStatusConditionText,
            $"조건 : {activeMission.Description}  ({FormatProgress(activeMission, current)})");
        ConfigureTruncatedTooltip(missionStatusRewardText,
            $"보상 : {activeMission.Reward.DisplayText}");
    }

    private void ClearMissionStatus()
    {
        if (missionStatusRoot != null) missionStatusRoot.SetActive(true);
        ClearMissionStatusTexts();
    }

    private void ClearMissionStatusTexts()
    {
        CommonChoiceCardLayout.SetText(missionStatusNameText, string.Empty);
        CommonChoiceCardLayout.SetText(missionStatusConditionText, string.Empty);
        CommonChoiceCardLayout.SetText(missionStatusRewardText, string.Empty);
    }

    private static void ConfigureTruncatedTooltip(TMP_Text text, string content)
    {
        if (text == null) return;
        HoverTooltip tooltip = text.GetComponent<HoverTooltip>();
        if (tooltip == null) tooltip = text.gameObject.AddComponent<HoverTooltip>();
        tooltip.ConfigureTruncatedContent(content);
    }

    private int GetCurrentValue(StageMissionChoice mission)
    {
        return mission.Type switch
        {
            StageMissionType.MaximumHitCount => hitCount,
            StageMissionType.MaximumDamageTaken => damageTaken,
            StageMissionType.MinimumFinalHealthRatio => playerHealth != null ? Mathf.FloorToInt(playerHealth.HealthRatio * 100f) : 0,
            StageMissionType.MinimumBallGain => ballCollection != null ? Mathf.Max(ballCollection.Count - startingBallCount, 0) : 0,
            StageMissionType.MinimumDistinctTraits => CountDistinctTraits(),
            StageMissionType.MinimumCombatRoomsCleared => combatRoomsCleared,
            StageMissionType.MinimumSpecialRoomsVisited => visitedSpecialRoomIds.Count,
            StageMissionType.MaximumBossTurns => bossTurnsElapsed,
            StageMissionType.MinimumRoomsVisited => visitedRoomIds.Count,
            StageMissionType.MinimumGoldGained => goldGained,
            StageMissionType.MinimumGoldSpent => goldSpent,
            StageMissionType.MaximumBossHitCount => bossHitCount,
            StageMissionType.MaximumRetreatCount => retreatCount,
            StageMissionType.MinimumShopPurchases => shopPurchaseCount,
            StageMissionType.MinimumGoldReserve => currencyState != null ? currencyState.CurrentGold : 0,
            _ => 0
        };
    }

    private bool IsMissionSucceeded(StageMissionChoice mission)
    {
        int value = GetCurrentValue(mission);
        return IsMissionSucceeded(mission, value);
    }

    private static bool IsMissionSucceeded(StageMissionChoice mission, int value) =>
        IsMaximumMission(mission.Type) ? value <= mission.Target : value >= mission.Target;

    private static bool IsMaximumMission(StageMissionType type) =>
        type == StageMissionType.MaximumHitCount ||
        type == StageMissionType.MaximumDamageTaken ||
        type == StageMissionType.MaximumBossTurns ||
        type == StageMissionType.MaximumBossHitCount ||
        type == StageMissionType.MaximumRetreatCount;

    private static bool IsIrrecoverablyFailed(StageMissionChoice mission, int current) =>
        IsMaximumMission(mission.Type) && current > mission.Target;

    private static string FormatProgress(StageMissionChoice mission, int current)
    {
        return mission.Type switch
        {
            StageMissionType.MinimumFinalHealthRatio => $"{current}% / {mission.Target}% 이상",
            StageMissionType.MaximumHitCount or StageMissionType.MaximumDamageTaken or StageMissionType.MaximumBossTurns => $"{current} / {mission.Target} 이하",
            _ => $"{current} / {mission.Target}"
        };
    }

    private int CountDistinctTraits()
    {
        if (ballCollection == null) return 0;
        HashSet<BallTraitType> traits = new();
        IReadOnlyList<Ball> balls = ballCollection.Balls;
        for (int i = 0; i < balls.Count; i++)
        {
            Ball ball = balls[i];
            if (ball != null) traits.Add(ball.TraitType);
        }
        return traits.Count;
    }

    private static int CountRooms(StageMap map, bool combatOnly = false)
    {
        if (map?.Rooms == null) return 0;
        int count = 0;
        for (int i = 0; i < map.Rooms.Count; i++)
        {
            RoomNode room = map.Rooms[i];
            if (room == null) continue;
            if (!combatOnly || room.RoomType == RoomType.NormalCombat || room.RoomType == RoomType.NamedCombat) count++;
        }
        return count;
    }

    private static int CountSpecialRooms(StageMap map)
    {
        if (map?.Rooms == null) return 0;
        int count = 0;
        for (int i = 0; i < map.Rooms.Count; i++)
        {
            RoomNode room = map.Rooms[i];
            if (room != null && IsSpecialRoom(room.RoomType)) count++;
        }
        return count;
    }

    private static int CountRooms(StageMap map, RoomType roomType)
    {
        if (map?.Rooms == null) return 0;
        int count = 0;
        for (int i = 0; i < map.Rooms.Count; i++)
        {
            if (map.Rooms[i]?.RoomType == roomType) count++;
        }
        return count;
    }

    private static bool IsSpecialRoom(RoomType type) =>
        type == RoomType.Shop || type == RoomType.Alchemy || type == RoomType.Event ||
        type == RoomType.Secret || type == RoomType.Augment;

    private int GetGoldReward(StageMissionDifficulty difficulty) => difficulty switch
    {
        StageMissionDifficulty.Easy => easyGoldReward,
        StageMissionDifficulty.Normal => normalGoldReward,
        _ => hardGoldReward
    };

    private StageMissionRewardOffer GenerateRewardOffer(
        StageMissionDifficulty difficulty,
        System.Random random,
        ISet<StageMissionRewardKind> excludedRewardKinds)
    {
        StageMissionRewardWeights weights = difficulty switch
        {
            StageMissionDifficulty.Easy => easyRewardWeights,
            StageMissionDifficulty.Normal => normalRewardWeights,
            _ => hardRewardWeights
        };

        List<(StageMissionRewardKind kind, int weight)> candidates = new();
        foreach (StageMissionRewardKind kind in Enum.GetValues(typeof(StageMissionRewardKind)))
        {
            if (excludedRewardKinds != null && excludedRewardKinds.Contains(kind)) continue;
            int weight = weights != null ? weights.Get(kind) : 0;
            if (weight > 0 && CanGenerateReward(kind, difficulty))
            {
                candidates.Add((kind, weight));
            }
        }

        int totalWeight = 0;
        for (int i = 0; i < candidates.Count; i++) totalWeight += candidates[i].weight;
        StageMissionRewardKind selectedKind = StageMissionRewardKind.Gold;
        if (totalWeight > 0)
        {
            int roll = random.Next(totalWeight);
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= candidates[i].weight;
                if (roll < 0)
                {
                    selectedKind = candidates[i].kind;
                    break;
                }
            }
        }

        return CreateRewardOffer(selectedKind, difficulty);
    }

    private bool CanGenerateReward(
        StageMissionRewardKind kind,
        StageMissionDifficulty difficulty)
    {
        return kind switch
        {
            StageMissionRewardKind.Gold => currencyState != null,
            StageMissionRewardKind.BasicBalls => FindBallReward(true, false, difficulty) != null,
            StageMissionRewardKind.TraitBalls => FindBallReward(false, false, difficulty) != null,
            StageMissionRewardKind.TwoStarBall =>
                twoStarRewardsGranted < maximumTwoStarRewards &&
                FindBallReward(false, true, difficulty) != null,
            StageMissionRewardKind.Healing => playerHealth != null,
            StageMissionRewardKind.MaxHealth => playerHealth != null,
            StageMissionRewardKind.NextRewardTier =>
                runRewardState != null &&
                tierUpgradeRewardsGranted < maximumTierUpgradeRewards,
            StageMissionRewardKind.Augment =>
                rewardGenerator != null && runAugmentState != null &&
                augmentRewardsGranted < maximumAugmentRewards &&
                FindAugmentReward() != null,
            StageMissionRewardKind.BallUpgrade => CountUpgradeableBalls() > 0,
            StageMissionRewardKind.BasicBallConversion =>
                CountConvertibleBasicBalls() > 0 && GetTraitBallDefinitions().Count > 0,
            StageMissionRewardKind.ShopDiscount =>
                shopPriceDiscountState != null && !shopPriceDiscountState.HasDiscount,
            StageMissionRewardKind.CombatGoldBoost =>
                combatGoldGainState != null && !combatGoldGainState.IsActive,
            StageMissionRewardKind.DirectDamageBoost => ballRuntimeStats != null,
            _ => false
        };
    }

    private StageMissionRewardOffer CreateRewardOffer(
        StageMissionRewardKind kind,
        StageMissionDifficulty difficulty)
    {
        int baseGold = GetGoldReward(difficulty);
        StageMissionRewardOffer offer = new() { Kind = kind };

        switch (kind)
        {
            case StageMissionRewardKind.BasicBalls:
                offer.RuntimeReward = FindBallReward(true, false, difficulty);
                offer.GoldAmount = difficulty == StageMissionDifficulty.Easy ? 0 : baseGold / 3;
                offer.DisplayText = JoinRewardText(
                    offer.RuntimeReward?.DisplayName ?? "기본 공",
                    offer.GoldAmount);
                break;
            case StageMissionRewardKind.TraitBalls:
                offer.RuntimeReward = FindBallReward(false, false, difficulty);
                offer.GoldAmount = difficulty == StageMissionDifficulty.Hard ? baseGold / 3 : 0;
                offer.DisplayText = JoinRewardText(
                    offer.RuntimeReward?.DisplayName ?? "속성 공",
                    offer.GoldAmount);
                break;
            case StageMissionRewardKind.TwoStarBall:
                offer.RuntimeReward = FindBallReward(false, true, difficulty);
                offer.GoldAmount = baseGold / 3;
                offer.DisplayText = JoinRewardText(
                    offer.RuntimeReward?.DisplayName ?? "2성 공",
                    offer.GoldAmount);
                break;
            case StageMissionRewardKind.Healing:
                offer.Value = difficulty switch
                {
                    StageMissionDifficulty.Easy => 20,
                    StageMissionDifficulty.Normal => 25,
                    _ => 30
                };
                offer.GoldAmount = baseGold / 3;
                offer.DisplayText = $"체력 {offer.Value}% 회복 + {offer.GoldAmount}G";
                break;
            case StageMissionRewardKind.MaxHealth:
                offer.Value = difficulty switch
                {
                    StageMissionDifficulty.Easy => 3,
                    StageMissionDifficulty.Normal => 4,
                    _ => 5
                };
                offer.GoldAmount = difficulty == StageMissionDifficulty.Hard ? baseGold / 3 : 0;
                offer.DisplayText = JoinRewardText($"최대 체력 +{offer.Value}", offer.GoldAmount);
                break;
            case StageMissionRewardKind.NextRewardTier:
                offer.Value = 1;
                offer.GoldAmount = baseGold / 3;
                offer.DisplayText = $"다음 일반 보상 승급 + {offer.GoldAmount}G";
                break;
            case StageMissionRewardKind.Augment:
                offer.RuntimeReward = FindAugmentReward();
                offer.GoldAmount = baseGold / 3;
                offer.DisplayText = JoinRewardText(
                    offer.RuntimeReward?.DisplayName ?? "무작위 증강",
                    offer.GoldAmount);
                break;
            case StageMissionRewardKind.BallUpgrade:
                offer.Value = difficulty == StageMissionDifficulty.Hard ? 15 : 10;
                offer.GoldAmount = baseGold / 3;
                offer.DisplayText = JoinRewardText(
                    $"승급 가능 공 {offer.Value}% 승급",
                    offer.GoldAmount);
                break;
            case StageMissionRewardKind.BasicBallConversion:
                offer.Value = difficulty == StageMissionDifficulty.Hard ? 20 : 15;
                offer.GoldAmount = difficulty == StageMissionDifficulty.Hard ? baseGold / 3 : 0;
                offer.DisplayText = JoinRewardText(
                    $"기본 공 {offer.Value}% 속성 변환",
                    offer.GoldAmount);
                break;
            case StageMissionRewardKind.ShopDiscount:
                offer.Value = difficulty == StageMissionDifficulty.Hard ? 20 : 15;
                offer.GoldAmount = baseGold / 3;
                offer.DisplayText = $"상점 가격 {offer.Value}% 할인 + {offer.GoldAmount}G";
                break;
            case StageMissionRewardKind.CombatGoldBoost:
                offer.Value = difficulty == StageMissionDifficulty.Hard ? 25 : 15;
                offer.GoldAmount = baseGold / 3;
                offer.DisplayText = $"전투 골드 +{offer.Value}% + {offer.GoldAmount}G";
                break;
            case StageMissionRewardKind.DirectDamageBoost:
                offer.Value = difficulty == StageMissionDifficulty.Hard ? 15 : 10;
                offer.GoldAmount = baseGold / 3;
                offer.DisplayText = $"전체 직접 피해 +{offer.Value}% + {offer.GoldAmount}G";
                break;
            default:
                offer.GoldAmount = baseGold;
                offer.DisplayText = $"{baseGold}G";
                break;
        }

        return offer;
    }

    private RewardDefinition FindBallReward(
        bool requireBasic,
        bool requireTwoStar,
        StageMissionDifficulty difficulty)
    {
        RewardCatalog catalog = rewardGenerator != null ? rewardGenerator.RewardCatalog : null;
        if (catalog?.RewardDefinitions == null) return null;
        RewardApplyContext context = new(ballCollection, runAugmentState);
        List<BallRewardDefinition> candidates = new();

        for (int i = 0; i < catalog.RewardDefinitions.Count; i++)
        {
            BallRewardDefinition reward = catalog.RewardDefinitions[i] as BallRewardDefinition;
            if (reward == null || !reward.CanApply(context)) continue;
            if (reward.IsBasicBallReward != requireBasic) continue;
            if (requireTwoStar != reward.IsTwoStarBallReward) continue;
            if (!requireTwoStar && !reward.IsOneStarBallReward) continue;

            RewardTier preferredTier = difficulty == StageMissionDifficulty.Hard
                ? RewardTier.Tier2
                : RewardTier.Tier1;
            if (reward.RewardTier == preferredTier) candidates.Add(reward);
        }

        if (candidates.Count == 0) return null;
        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private RewardDefinition FindAugmentReward()
    {
        if (rewardGenerator == null || runAugmentState == null) return null;
        List<RewardDefinition> rewards = rewardGenerator.GenerateAugmentChoices(
            AugmentRewardSource.Secret,
            1,
            new RewardApplyContext(ballCollection, runAugmentState));
        return rewards != null && rewards.Count > 0 ? rewards[0] : null;
    }

    private T FindCatalogReward<T>() where T : RewardDefinition
    {
        RewardCatalog catalog = rewardGenerator != null ? rewardGenerator.RewardCatalog : null;
        if (catalog?.RewardDefinitions == null) return null;
        RewardApplyContext context = new(ballCollection, runAugmentState);
        List<T> candidates = new();
        for (int i = 0; i < catalog.RewardDefinitions.Count; i++)
        {
            T reward = catalog.RewardDefinitions[i] as T;
            if (reward != null && reward.CanApply(context)) candidates.Add(reward);
        }
        return candidates.Count > 0 ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : null;
    }

    private bool ApplyMissionReward(StageMissionRewardOffer offer)
    {
        if (offer == null) return false;
        bool primaryApplied = false;

        switch (offer.Kind)
        {
            case StageMissionRewardKind.Gold:
                primaryApplied = offer.GoldAmount > 0 && currencyState != null &&
                                 currencyState.TryAddGold(offer.GoldAmount);
                return primaryApplied;
            case StageMissionRewardKind.Healing:
                if (playerHealth != null)
                {
                    int amount = Mathf.Max(1, Mathf.CeilToInt(playerHealth.MaxHealth * offer.Value / 100f));
                    int before = playerHealth.CurrentHealth;
                    playerHealth.Heal(amount);
                    primaryApplied = playerHealth.CurrentHealth > before;
                }
                break;
            case StageMissionRewardKind.MaxHealth:
                primaryApplied = playerHealth != null && playerHealth.TryIncreaseMaxHealth(offer.Value);
                break;
            case StageMissionRewardKind.NextRewardTier:
                primaryApplied = runRewardState != null && runRewardState.AddPendingRewardTierIncrease(offer.Value);
                if (primaryApplied) tierUpgradeRewardsGranted++;
                break;
            case StageMissionRewardKind.ShopDiscount:
                primaryApplied = shopPriceDiscountState != null &&
                    shopPriceDiscountState.TryApplyDiscount(offer.Value / 100f);
                break;
            case StageMissionRewardKind.CombatGoldBoost:
                primaryApplied = combatGoldGainState != null &&
                    combatGoldGainState.TryActivate(offer.Value / 100f);
                break;
            case StageMissionRewardKind.DirectDamageBoost:
                if (ballRuntimeStats != null)
                {
                    ballRuntimeStats.AddRunDirectDamageMultiplierBonus(offer.Value / 100f);
                    primaryApplied = true;
                }
                break;
            case StageMissionRewardKind.BallUpgrade:
                primaryApplied = ApplyPercentageBallUpgrade(offer.Value) > 0;
                break;
            case StageMissionRewardKind.BasicBallConversion:
                primaryApplied = ApplyPercentageBasicConversion(offer.Value) > 0;
                break;
            default:
                if (offer.RuntimeReward != null)
                {
                    RewardApplyContext context = new(ballCollection, runAugmentState);
                    primaryApplied = offer.RuntimeReward.CanApply(context) && offer.RuntimeReward.Apply(context);
                    if (primaryApplied && offer.Kind == StageMissionRewardKind.TwoStarBall) twoStarRewardsGranted++;
                    if (primaryApplied && offer.Kind == StageMissionRewardKind.Augment) augmentRewardsGranted++;
                }
                break;
        }

        bool goldApplied = offer.GoldAmount <= 0 ||
                           (currencyState != null && currencyState.TryAddGold(offer.GoldAmount));
        return primaryApplied || goldApplied;
    }

    private int CountUpgradeableBalls()
    {
        if (ballCollection == null) return 0;
        return ballCollection.CountMatchingBalls(ball =>
            ball?.Definition != null &&
            ball.Definition.CanUpgrade &&
            ball.Definition.NextStarDefinition != null);
    }

    private int CountConvertibleBasicBalls()
    {
        if (ballCollection == null) return 0;
        return ballCollection.CountMatchingBalls(ball =>
            ball?.Definition != null &&
            ball.Definition.TraitType == BallTraitType.Basic &&
            ball.Definition.StarGrade == BallStarGrade.OneStar);
    }

    private List<BallDefinition> GetTraitBallDefinitions()
    {
        List<BallDefinition> definitions = new();
        RewardCatalog catalog = rewardGenerator != null ? rewardGenerator.RewardCatalog : null;
        if (catalog?.RewardDefinitions == null) return definitions;
        for (int i = 0; i < catalog.RewardDefinitions.Count; i++)
        {
            BallRewardDefinition reward = catalog.RewardDefinitions[i] as BallRewardDefinition;
            BallDefinition definition = reward?.BallDefinition;
            if (reward == null || definition == null ||
                !reward.IsTraitBallReward || !reward.IsOneStarBallReward ||
                definitions.Contains(definition)) continue;
            definitions.Add(definition);
        }
        return definitions;
    }

    private int ApplyPercentageBallUpgrade(int percentage)
    {
        if (ballCollection == null || percentage <= 0) return 0;
        List<Ball> candidates = new();
        IReadOnlyList<Ball> balls = ballCollection.Balls;
        for (int i = 0; i < balls.Count; i++)
        {
            Ball ball = balls[i];
            if (ball?.Definition != null && ball.Definition.CanUpgrade &&
                ball.Definition.NextStarDefinition != null) candidates.Add(ball);
        }
        ShuffleWithUnityRandom(candidates);
        int targetCount = Mathf.Min(
            Mathf.Max(1, Mathf.CeilToInt(candidates.Count * percentage / 100f)),
            candidates.Count);
        int applied = 0;
        for (int i = 0; i < targetCount; i++)
        {
            Ball ball = candidates[i];
            if (ballCollection.ReplaceBallDefinition(ball, ball.Definition.NextStarDefinition)) applied++;
        }
        return applied;
    }

    private int ApplyPercentageBasicConversion(int percentage)
    {
        if (ballCollection == null || percentage <= 0) return 0;
        List<BallDefinition> definitions = GetTraitBallDefinitions();
        if (definitions.Count == 0) return 0;
        List<Ball> candidates = new();
        IReadOnlyList<Ball> balls = ballCollection.Balls;
        for (int i = 0; i < balls.Count; i++)
        {
            Ball ball = balls[i];
            if (ball?.Definition != null &&
                ball.Definition.TraitType == BallTraitType.Basic &&
                ball.Definition.StarGrade == BallStarGrade.OneStar) candidates.Add(ball);
        }
        ShuffleWithUnityRandom(candidates);
        int targetCount = Mathf.Min(
            Mathf.Max(1, Mathf.CeilToInt(candidates.Count * percentage / 100f)),
            candidates.Count);
        int applied = 0;
        for (int i = 0; i < targetCount; i++)
        {
            BallDefinition target = definitions[UnityEngine.Random.Range(0, definitions.Count)];
            if (ballCollection.ReplaceBallDefinition(candidates[i], target)) applied++;
        }
        return applied;
    }

    private static void ShuffleWithUnityRandom<T>(IList<T> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }

    private static string JoinRewardText(string primary, int goldAmount) =>
        goldAmount > 0 ? $"{primary} + {goldAmount}G" : primary;

    private static string DifficultyName(StageMissionDifficulty difficulty) => difficulty switch
    {
        StageMissionDifficulty.Easy => "쉬움",
        StageMissionDifficulty.Normal => "보통",
        _ => "어려움"
    };

    private static void Shuffle<T>(IList<T> values, System.Random random)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }

    private void BindCardButtons()
    {
        for (int i = 0; i < cardButtons.Length; i++)
        {
            int captured = i;
            if (cardButtons[i] != null)
                cardButtons[i].onClick.AddListener(() => SelectChoice(captured));
        }
    }

    private void UnbindCardButtons()
    {
        foreach (Button button in cardButtons)
            if (button != null) button.onClick.RemoveAllListeners();
    }
}
