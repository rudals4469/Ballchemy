using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BlockGoldRewardController :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private StageRoomNavigator roomNavigator;

    [SerializeField]
    private BlockGridManager blockGridManager;

    [SerializeField]
    private RoomRetreatController retreatController;

    [SerializeField]
    private RunCurrencyState runCurrencyState;

    [SerializeField]
    private RoomGoldTransactionState roomGoldTransactionState;

    [SerializeField]
    private StageModifierState stageModifierState;

    [SerializeField]
    private RunCombatGoldGainState runCombatGoldGainState;

    [Header("Gold Per Clear Role")]

    [Tooltip(
        "RequiredEnemy 역할 블록 파괴 시 지급할 기본 골드입니다."
    )]
    [SerializeField, Min(0)]
    private int requiredEnemyGold =
        1;

    [Tooltip(
        "Optional 역할 블록 파괴 시 지급할 기본 골드입니다.\n" +
        "초기에는 0을 권장합니다."
    )]
    [SerializeField, Min(0)]
    private int optionalGold;

    [Tooltip(
        "Ignore 역할 블록 파괴 시 지급할 기본 골드입니다.\n" +
        "항상 0을 권장합니다."
    )]
    [SerializeField, Min(0)]
    private int ignoreGold;

    [Header("Gold Special Block")]

    [SerializeField]
    private string goldSpecialBlockId = "special_gold";

    [SerializeField, Min(1)]
    private int goldSpecialReward = 10;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog =
        true;

    private readonly HashSet<Block>
        subscribedBlocks =
            new HashSet<Block>();

    private bool isSubscribed;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
        ValidateReferences();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();
        TryPrepareCurrentRoomTransaction();
        RefreshBlockSubscriptions();
    }

    private void Start()
    {
        TryPrepareCurrentRoomTransaction();
        RefreshBlockSubscriptions();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        UnsubscribeAllBlocks();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        UnsubscribeAllBlocks();
    }

    private void OnValidate()
    {
        NormalizeSettings();
        FindReferences();
    }

    private void NormalizeSettings()
    {
        requiredEnemyGold =
            Mathf.Max(
                requiredEnemyGold,
                0
            );

        optionalGold =
            Mathf.Max(
                optionalGold,
                0
            );

        ignoreGold =
            Mathf.Max(
                ignoreGold,
                0
            );

        goldSpecialReward = Mathf.Max(goldSpecialReward, 1);

        if (string.IsNullOrWhiteSpace(goldSpecialBlockId))
        {
            goldSpecialBlockId = "special_gold";
        }
    }

    private void FindReferences()
    {
        if (roomNavigator == null)
        {
            roomNavigator =
                FindFirstObjectByType<
                    StageRoomNavigator
                >();
        }

        if (blockGridManager == null)
        {
            blockGridManager =
                FindFirstObjectByType<
                    BlockGridManager
                >();
        }

        if (retreatController == null)
        {
            retreatController =
                FindFirstObjectByType<
                    RoomRetreatController
                >();
        }

        if (runCurrencyState == null)
        {
            runCurrencyState =
                FindFirstObjectByType<
                    RunCurrencyState
                >();
        }

        if (roomGoldTransactionState == null)
        {
            roomGoldTransactionState =
                FindFirstObjectByType<
                    RoomGoldTransactionState
                >();
        }

        if (stageModifierState == null)
        {
            stageModifierState =
                FindFirstObjectByType<
                    StageModifierState
                >();
        }

        if (runCombatGoldGainState == null)
        {
            runCombatGoldGainState =
                FindFirstObjectByType<
                    RunCombatGoldGainState
                >(
                    FindObjectsInactive.Include
                );
        }
    }

    private void ValidateReferences()
    {
        if (roomNavigator == null)
        {
            Debug.LogError(
                "BlockGoldRewardController: " +
                "StageRoomNavigator가 연결되지 않았습니다.",
                this
            );
        }

        if (blockGridManager == null)
        {
            Debug.LogError(
                "BlockGoldRewardController: " +
                "BlockGridManager가 연결되지 않았습니다.",
                this
            );
        }

        if (retreatController == null)
        {
            Debug.LogError(
                "BlockGoldRewardController: " +
                "RoomRetreatController가 연결되지 않았습니다.",
                this
            );
        }

        if (runCurrencyState == null)
        {
            Debug.LogError(
                "BlockGoldRewardController: " +
                "RunCurrencyState가 연결되지 않았습니다.",
                this
            );
        }

        if (roomGoldTransactionState == null)
        {
            Debug.LogError(
                "BlockGoldRewardController: " +
                "RoomGoldTransactionState가 연결되지 않았습니다.",
                this
            );
        }

        if (stageModifierState == null)
        {
            Debug.LogWarning(
                "BlockGoldRewardController: " +
                "StageModifierState가 연결되지 않았습니다. " +
                "스테이지 골드 획득 증가 버프는 적용되지 않습니다.",
                this
            );
        }

        if (runCombatGoldGainState == null)
        {
            Debug.LogWarning(
                "BlockGoldRewardController: " +
                "RunCombatGoldGainState가 연결되지 않았습니다. " +
                "런 전체 전투방 골드 증가 효과는 적용되지 않습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (isSubscribed)
        {
            return;
        }

        if (roomNavigator != null)
        {
            roomNavigator.RoomChanged -=
                HandleRoomChanged;

            roomNavigator.RoomChanged +=
                HandleRoomChanged;

            roomNavigator.CombatRoomCleared -=
                HandleCombatRoomCleared;

            roomNavigator.CombatRoomCleared +=
                HandleCombatRoomCleared;

            roomNavigator.MapInitialized -=
                HandleMapInitialized;

            roomNavigator.MapInitialized +=
                HandleMapInitialized;
        }

        if (blockGridManager != null)
        {
            blockGridManager.WaveGenerated -=
                HandleWaveGenerated;

            blockGridManager.WaveGenerated +=
                HandleWaveGenerated;

            blockGridManager.RoomCombatReset -=
                HandleRoomCombatReset;

            blockGridManager.RoomCombatReset +=
                HandleRoomCombatReset;
        }

        if (retreatController != null)
        {
            retreatController.RetreatCompleted -=
                HandleRetreatCompleted;

            retreatController.RetreatCompleted +=
                HandleRetreatCompleted;
        }

        isSubscribed =
            true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (roomNavigator != null)
        {
            roomNavigator.RoomChanged -=
                HandleRoomChanged;

            roomNavigator.CombatRoomCleared -=
                HandleCombatRoomCleared;

            roomNavigator.MapInitialized -=
                HandleMapInitialized;
        }

        if (blockGridManager != null)
        {
            blockGridManager.WaveGenerated -=
                HandleWaveGenerated;

            blockGridManager.RoomCombatReset -=
                HandleRoomCombatReset;
        }

        if (retreatController != null)
        {
            retreatController.RetreatCompleted -=
                HandleRetreatCompleted;
        }

        isSubscribed =
            false;
    }

    private void HandleMapInitialized(
        StageMap stageMap)
    {
        if (roomGoldTransactionState != null &&
            roomGoldTransactionState
                .HasActiveTransaction)
        {
            Debug.LogWarning(
                "BlockGoldRewardController: " +
                "새 스테이지 초기화 시 " +
                "미확정 방 골드가 남아 있습니다. " +
                "회수 없이 거래 기록을 초기화합니다.",
                this
            );

            roomGoldTransactionState
                .ResetWithoutRollback();
        }

        UnsubscribeAllBlocks();
    }

    private void HandleRoomChanged(
        RoomNode previousRoom,
        RoomNode currentRoom)
    {
        CleanupInvalidBlockReferences();

        if (currentRoom == null ||
            !currentRoom.IsCombatRoom)
        {
            return;
        }

        if (roomNavigator != null &&
            roomNavigator.IsRoomCleared(
                currentRoom.RoomId
            ))
        {
            return;
        }

        TryBeginTransaction(
            currentRoom.RoomId
        );

        RefreshBlockSubscriptions();
    }

    private void HandleWaveGenerated(
        int waveNumber)
    {
        TryPrepareCurrentRoomTransaction();
        RefreshBlockSubscriptions();
    }

    private void HandleRoomCombatReset()
    {
        UnsubscribeAllBlocks();
    }

    private void HandleCombatRoomCleared(
        RoomNode clearedRoom)
    {
        if (clearedRoom == null ||
            roomGoldTransactionState == null)
        {
            return;
        }

        roomGoldTransactionState
            .TryConfirmTransaction(
                clearedRoom.RoomId
            );

        UnsubscribeAllBlocks();
    }

    private void HandleRetreatCompleted(
        int healthCost)
    {
        RollbackCurrentRoomGold();
        UnsubscribeAllBlocks();
    }

    private void TryPrepareCurrentRoomTransaction()
    {
        if (roomNavigator == null ||
            roomNavigator.CurrentRoom == null)
        {
            return;
        }

        RoomNode currentRoom =
            roomNavigator.CurrentRoom;

        if (!currentRoom.IsCombatRoom ||
            roomNavigator.IsRoomCleared(
                currentRoom.RoomId
            ))
        {
            return;
        }

        TryBeginTransaction(
            currentRoom.RoomId
        );
    }

    private void TryBeginTransaction(
        int roomId)
    {
        if (roomGoldTransactionState == null)
        {
            return;
        }

        roomGoldTransactionState
            .BeginTransaction(
                roomId
            );
    }

    private void RefreshBlockSubscriptions()
    {
        if (blockGridManager == null)
        {
            return;
        }

        CleanupInvalidBlockReferences();

        IReadOnlyList<Block> activeBlocks =
            blockGridManager.ActiveBlocks;

        if (activeBlocks == null)
        {
            return;
        }

        for (int i = 0;
             i < activeBlocks.Count;
             i++)
        {
            SubscribeBlock(
                activeBlocks[i]
            );
        }
    }

    private void SubscribeBlock(
        Block block)
    {
        if (block == null ||
            subscribedBlocks.Contains(
                block
            ))
        {
            return;
        }

        block.Destroyed -=
            HandleBlockDestroyed;

        block.Destroyed +=
            HandleBlockDestroyed;

        subscribedBlocks.Add(
            block
        );
    }

    private void HandleBlockDestroyed(
        Block block)
    {
        if (block == null)
        {
            return;
        }

        block.Destroyed -=
            HandleBlockDestroyed;

        subscribedBlocks.Remove(
            block
        );

        if (roomNavigator == null ||
            roomNavigator.CurrentRoom == null ||
            runCurrencyState == null ||
            roomGoldTransactionState == null)
        {
            return;
        }

        RoomNode currentRoom =
            roomNavigator.CurrentRoom;

        if (!currentRoom.IsCombatRoom ||
            roomNavigator.IsRoomCleared(
                currentRoom.RoomId
            ))
        {
            return;
        }

        if (!roomGoldTransactionState
                .HasActiveTransaction ||
            roomGoldTransactionState
                .ActiveRoomId !=
            currentRoom.RoomId)
        {
            bool began =
                roomGoldTransactionState
                    .BeginTransaction(
                        currentRoom.RoomId
                    );

            if (!began)
            {
                Debug.LogWarning(
                    "BlockGoldRewardController: " +
                    "활성 방 골드 거래가 없어 " +
                    "블록 골드를 지급하지 않습니다.",
                    this
                );

                return;
            }
        }

        int baseRewardAmount =
            ResolveBlockGold(
                block
            );

        int rewardAmount =
            ApplyGoldGainModifier(
                baseRewardAmount
            );

        if (rewardAmount <= 0)
        {
            return;
        }

        bool added =
            runCurrencyState.TryAddGold(
                rewardAmount
            );

        if (!added)
        {
            return;
        }

        bool recorded =
            roomGoldTransactionState
                .TryRecordEarnedGold(
                    currentRoom.RoomId,
                    rewardAmount
                );

        if (!recorded)
        {
            runCurrencyState.TryRollbackGold(
                rewardAmount
            );

            Debug.LogError(
                "BlockGoldRewardController: " +
                "방 미확정 골드 기록에 실패해 " +
                "방금 지급한 골드를 되돌렸습니다.",
                this
            );

            return;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "BlockGoldRewardController: " +
                $"블록 파괴 골드 +{rewardAmount}G, " +
                $"기본={baseRewardAmount}G, " +
                $"StageBonus=" +
                $"{ResolveStageGoldGainIncreaseRatio():P0}, " +
                $"RunBonus=" +
                $"{ResolveRunGoldGainIncreaseRatio():P0}, " +
                $"RoomId={currentRoom.RoomId}, " +
                $"BlockId={block.BlockId}",
                this
            );
        }
    }

    private int ApplyGoldGainModifier(
        int baseRewardAmount)
    {
        baseRewardAmount =
            Mathf.Max(
                baseRewardAmount,
                0
            );

        if (baseRewardAmount <= 0)
        {
            return baseRewardAmount;
        }

        float stageIncreaseRatio =
            ResolveStageGoldGainIncreaseRatio();

        float runIncreaseRatio =
            ResolveRunGoldGainIncreaseRatio();

        float totalIncreaseRatio =
            Mathf.Max(
                stageIncreaseRatio +
                runIncreaseRatio,
                0f
            );

        if (totalIncreaseRatio <= 0f)
        {
            return baseRewardAmount;
        }

        float modifiedGold =
            baseRewardAmount *
            (
                1f +
                totalIncreaseRatio
            );

        return Mathf.Max(
            Mathf.CeilToInt(
                modifiedGold
            ),
            1
        );
    }

    private float ResolveStageGoldGainIncreaseRatio()
    {
        if (stageModifierState == null)
        {
            stageModifierState =
                FindFirstObjectByType<
                    StageModifierState
                >();
        }

        if (stageModifierState == null)
        {
            return 0f;
        }

        return Mathf.Max(
            stageModifierState
                .GoldGainIncreaseRatio,
            0f
        );
    }

    private float ResolveRunGoldGainIncreaseRatio()
    {
        if (runCombatGoldGainState == null)
        {
            runCombatGoldGainState =
                FindFirstObjectByType<
                    RunCombatGoldGainState
                >(
                    FindObjectsInactive.Include
                );
        }

        if (runCombatGoldGainState == null)
        {
            return 0f;
        }

        return Mathf.Max(
            runCombatGoldGainState
                .GoldGainIncreaseRatio,
            0f
        );
    }

    private int ResolveBlockGold(
        Block block)
    {
        if (block == null ||
            block.Definition == null)
        {
            return 0;
        }

        if (string.Equals(
                block.BlockId,
                goldSpecialBlockId,
                StringComparison.Ordinal))
        {
            return goldSpecialReward;
        }

        switch (block.Definition.ClearRole)
        {
            case BlockClearRole.RequiredEnemy:
                return requiredEnemyGold;

            case BlockClearRole.Optional:
                return optionalGold;

            case BlockClearRole.Ignore:
                return ignoreGold;

            default:
                return 0;
        }
    }

    private void RollbackCurrentRoomGold()
    {
        if (roomGoldTransactionState == null ||
            runCurrencyState == null)
        {
            return;
        }

        bool hadTransaction =
            roomGoldTransactionState
                .TryConsumeRollback(
                    out int roomId,
                    out int rollbackGold
                );

        if (!hadTransaction)
        {
            return;
        }

        if (rollbackGold <= 0)
        {
            if (showDebugLog)
            {
                Debug.Log(
                    "BlockGoldRewardController: " +
                    $"Room {roomId} 후퇴, " +
                    "회수할 골드 없음",
                    this
                );
            }

            return;
        }

        bool rolledBack =
            runCurrencyState.TryRollbackGold(
                rollbackGold
            );

        if (!rolledBack)
        {
            Debug.LogError(
                "BlockGoldRewardController: " +
                $"Room {roomId}에서 획득한 " +
                $"{rollbackGold}G를 회수하지 못했습니다.",
                this
            );

            return;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "BlockGoldRewardController: " +
                $"후퇴로 방 획득 골드 회수, " +
                $"RoomId={roomId}, " +
                $"Gold={rollbackGold}G",
                this
            );
        }
    }

    private void CleanupInvalidBlockReferences()
    {
        subscribedBlocks.RemoveWhere(
            block =>
                block == null
        );
    }

    private void UnsubscribeAllBlocks()
    {
        foreach (Block block in
                 subscribedBlocks)
        {
            if (block == null)
            {
                continue;
            }

            block.Destroyed -=
                HandleBlockDestroyed;
        }

        subscribedBlocks.Clear();
    }
}
