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
    private int minimumEnemyHealth = 1;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog = true;

    private readonly List<
        RoomEntryHealthReductionResult
    > healthReductionResults =
        new List<
            RoomEntryHealthReductionResult
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
        ApplyRoomEntryAugments(
            currentRoom
        );
    }

    private void HandleRoomCombatReset()
    {
        RoomNode currentRoom =
            roomNavigator != null
                ? roomNavigator.CurrentRoom
                : null;

        ApplyRoomEntryAugments(
            currentRoom
        );
    }

    private void ApplyRoomEntryAugments(
        RoomNode room)
    {
        healthReductionResults.Clear();

        if (room == null ||
            !room.IsCombatRoom ||
            runAugmentState == null ||
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

        IReadOnlyList<AugmentRuntimeEntry>
            activeAugments =
                runAugmentState.ActiveAugments;

        if (activeAugments == null ||
            activeAugments.Count == 0)
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

        int totalReducedHealth = 0;

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
                ApplyHealthReduction(
                    blocks,
                    reductionPercent
                );
        }

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
            $"{room.RoomType} 방 입장 증강 적용, " +
            $"대상 {healthReductionResults.Count}개, " +
            $"총 체력 감소 {totalReducedHealth}",
            this
        );
    }

    private int ApplyHealthReduction(
        IReadOnlyList<Block> blocks,
        float reductionPercent)
    {
        int totalReducedHealth = 0;

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

            healthReductionResults.Add(
                new RoomEntryHealthReductionResult(
                    block,
                    reducedHealth
                )
            );

            totalReducedHealth +=
                reducedHealth;
        }

        return totalReducedHealth;
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