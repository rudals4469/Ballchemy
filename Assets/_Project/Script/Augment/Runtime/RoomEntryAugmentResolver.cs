using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RoomEntryAugmentResolver :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private RunAugmentState
        runAugmentState;

    [SerializeField]
    private StageModifierState
        stageModifierState;

    [SerializeField]
    private StageRoomNavigator
        roomNavigator;

    [SerializeField]
    private BlockGridManager
        blockGridManager;

    [SerializeField]
    private RoomEntryHealthReductionPresenter
        healthReductionPresenter;

    [Header("Settings")]

    [Tooltip(
        "방 입장 체력 감소로 적이 내려갈 수 있는 " +
        "최소 체력입니다."
    )]
    [SerializeField, Min(1)]
    private int minimumEnemyHealth =
        1;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog =
        true;

    private readonly List<
        RoomEntryHealthReductionResult
    > healthReductionResults =
        new List<
            RoomEntryHealthReductionResult
        >();

    private readonly Dictionary<
        Block,
        int
    > reducedHealthByBlock =
        new Dictionary<
            Block,
            int
        >();

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
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        FindReferences();
        NormalizeSettings();
    }

    private void FindReferences()
    {
        if (runAugmentState == null)
        {
            runAugmentState =
                FindFirstObjectByType<
                    RunAugmentState
                >();
        }

        if (stageModifierState == null)
        {
            stageModifierState =
                FindFirstObjectByType<
                    StageModifierState
                >();
        }

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

        if (healthReductionPresenter == null)
        {
            healthReductionPresenter =
                GetComponent<
                    RoomEntryHealthReductionPresenter
                >();
        }
    }

    private void NormalizeSettings()
    {
        minimumEnemyHealth =
            Mathf.Max(
                minimumEnemyHealth,
                1
            );
    }

    private void ValidateReferences()
    {
        if (runAugmentState == null)
        {
            Debug.LogError(
                "RoomEntryAugmentResolver: " +
                "RunAugmentState가 연결되지 않았습니다.",
                this
            );
        }

        if (stageModifierState == null)
        {
            Debug.LogWarning(
                "RoomEntryAugmentResolver: " +
                "StageModifierState가 연결되지 않았습니다. " +
                "상점 스테이지 버프는 적용되지 않습니다.",
                this
            );
        }

        if (roomNavigator == null)
        {
            Debug.LogError(
                "RoomEntryAugmentResolver: " +
                "StageRoomNavigator가 연결되지 않았습니다.",
                this
            );
        }

        if (blockGridManager == null)
        {
            Debug.LogError(
                "RoomEntryAugmentResolver: " +
                "BlockGridManager가 연결되지 않았습니다.",
                this
            );
        }

        if (healthReductionPresenter == null)
        {
            Debug.LogWarning(
                "RoomEntryAugmentResolver: " +
                "RoomEntryHealthReductionPresenter가 " +
                "연결되지 않았습니다. " +
                "체력 감소는 적용되지만 연출은 나오지 않습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (roomNavigator != null)
        {
            roomNavigator.RoomChanged -=
                HandleRoomChanged;

            roomNavigator.RoomChanged +=
                HandleRoomChanged;
        }

        if (blockGridManager != null)
        {
            blockGridManager.RoomCombatReset -=
                HandleRoomCombatReset;

            blockGridManager.RoomCombatReset +=
                HandleRoomCombatReset;
        }
    }

    private void UnsubscribeEvents()
    {
        if (roomNavigator != null)
        {
            roomNavigator.RoomChanged -=
                HandleRoomChanged;
        }

        if (blockGridManager != null)
        {
            blockGridManager.RoomCombatReset -=
                HandleRoomCombatReset;
        }
    }

    private void HandleRoomChanged(
        RoomNode previousRoom,
        RoomNode currentRoom)
    {
        ApplyRoomEntryModifiers(
            currentRoom
        );
    }

    private void HandleRoomCombatReset()
    {
        RoomNode currentRoom =
            roomNavigator != null
                ? roomNavigator.CurrentRoom
                : null;

        ApplyRoomEntryModifiers(
            currentRoom
        );
    }

    private void ApplyRoomEntryModifiers(
        RoomNode room)
    {
        healthReductionResults.Clear();
        reducedHealthByBlock.Clear();

        if (room == null ||
            !room.IsCombatRoom ||
            blockGridManager == null)
        {
            return;
        }

        if (room.RoomType !=
                RoomType.NormalCombat &&
            room.RoomType !=
                RoomType.NamedCombat &&
            room.RoomType !=
                RoomType.Boss)
        {
            return;
        }

        IReadOnlyList<Block> blocks =
            blockGridManager.ActiveBlocks;

        if (blocks == null ||
            blocks.Count == 0)
        {
            return;
        }

        int totalReducedHealth =
            0;

        totalReducedHealth +=
            ApplyStageEnemyMaxHealthReduction(
                room,
                blocks
            );

        totalReducedHealth +=
            ApplyRunAugmentHealthReduction(
                room,
                blocks
            );

        BuildPresentationResults();

        if (healthReductionResults.Count > 0)
        {
            healthReductionPresenter?.Play(
                healthReductionResults
            );
        }

        if (!showDebugLog ||
            healthReductionResults.Count <= 0)
        {
            return;
        }

        Debug.Log(
            "RoomEntryAugmentResolver: " +
            $"{room.RoomType} 방 입장 효과 적용, " +
            $"대상 {healthReductionResults.Count}개, " +
            $"총 체력 감소 {totalReducedHealth}",
            this
        );
    }

    private int ApplyStageEnemyMaxHealthReduction(
        RoomNode room,
        IReadOnlyList<Block> blocks)
    {
        if (room == null ||
            stageModifierState == null ||
            !stageModifierState
                .HasEnemyMaxHealthReduction)
        {
            return 0;
        }

        /*
         * 상점의 ReduceEnemyMaxHealth는
         * 일반 전투방과 네임드 전투방에만 적용합니다.
         *
         * 보스방은 명시적으로 제외합니다.
         */
        if (room.RoomType !=
                RoomType.NormalCombat &&
            room.RoomType !=
                RoomType.NamedCombat)
        {
            return 0;
        }

        float reductionRatio =
            stageModifierState
                .EnemyMaxHealthReductionRatio;

        if (reductionRatio <= 0f)
        {
            return 0;
        }

        int totalReducedHealth =
            0;

        for (int i = 0;
             i < blocks.Count;
             i++)
        {
            Block block =
                blocks[i];

            if (!IsValidTarget(
                    block
                ))
            {
                continue;
            }

            int reducedHealth =
                block.ReduceMaxHealthByPercent(
                    reductionRatio,
                    minimumEnemyHealth
                );

            if (reducedHealth <= 0)
            {
                continue;
            }

            RegisterReduction(
                block,
                reducedHealth
            );

            totalReducedHealth +=
                reducedHealth;
        }

        return totalReducedHealth;
    }

    private int ApplyRunAugmentHealthReduction(
        RoomNode room,
        IReadOnlyList<Block> blocks)
    {
        if (room == null ||
            runAugmentState == null)
        {
            return 0;
        }

        IReadOnlyList<AugmentRuntimeEntry>
            activeAugments =
                runAugmentState.ActiveAugments;

        if (activeAugments == null ||
            activeAugments.Count == 0)
        {
            return 0;
        }

        int totalReducedHealth =
            0;

        for (int augmentIndex = 0;
             augmentIndex < activeAugments.Count;
             augmentIndex++)
        {
            AugmentRuntimeEntry entry =
                activeAugments[augmentIndex];

            if (entry == null ||
                entry.Definition == null ||
                entry.Level <= 0)
            {
                continue;
            }

            RoomEntryHealthReductionAugmentDefinition
                healthReductionDefinition =
                    entry.Definition as
                        RoomEntryHealthReductionAugmentDefinition;

            if (healthReductionDefinition == null)
            {
                continue;
            }

            float reductionPercent =
                healthReductionDefinition
                    .GetReductionPercent(
                        entry.Level,
                        room.RoomType
                    );

            if (reductionPercent <= 0f)
            {
                continue;
            }

            totalReducedHealth +=
                ApplyCurrentHealthReduction(
                    blocks,
                    reductionPercent
                );
        }

        return totalReducedHealth;
    }

    private int ApplyCurrentHealthReduction(
        IReadOnlyList<Block> blocks,
        float reductionPercent)
    {
        int totalReducedHealth =
            0;

        for (int i = 0;
             i < blocks.Count;
             i++)
        {
            Block block =
                blocks[i];

            if (!IsValidTarget(
                    block
                ))
            {
                continue;
            }

            int reducedHealth =
                block.ReduceCurrentHealthByPercent(
                    reductionPercent,
                    minimumEnemyHealth
                );

            if (reducedHealth <= 0)
            {
                continue;
            }

            RegisterReduction(
                block,
                reducedHealth
            );

            totalReducedHealth +=
                reducedHealth;
        }

        return totalReducedHealth;
    }

    private void RegisterReduction(
        Block block,
        int reducedHealth)
    {
        if (block == null ||
            reducedHealth <= 0)
        {
            return;
        }

        if (reducedHealthByBlock.TryGetValue(
                block,
                out int previousReduction
            ))
        {
            reducedHealthByBlock[
                block
            ] =
                previousReduction +
                reducedHealth;

            return;
        }

        reducedHealthByBlock.Add(
            block,
            reducedHealth
        );
    }

    private void BuildPresentationResults()
    {
        healthReductionResults.Clear();

        foreach (
            KeyValuePair<Block, int> pair
            in reducedHealthByBlock)
        {
            if (pair.Key == null ||
                pair.Value <= 0)
            {
                continue;
            }

            healthReductionResults.Add(
                new RoomEntryHealthReductionResult(
                    pair.Key,
                    pair.Value
                )
            );
        }
    }

    private static bool IsValidTarget(
        Block block)
    {
        if (block == null ||
            !block.IsAlive ||
            !block.IsBreakable ||
            block.Definition == null)
        {
            return false;
        }

        return
            block.Definition.ClearRole ==
            BlockClearRole.RequiredEnemy;
    }
}