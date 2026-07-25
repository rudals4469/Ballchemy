using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
public sealed class RepairOnDestroyedEffect :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BlockGridManager blockGridManager;

    [Header("Repair")]
    [Tooltip(
        "수리 블록을 중심으로 몇 칸까지 " +
        "회복할지 결정합니다. " +
        "1은 3×3, 2는 5×5 범위입니다."
    )]
    [SerializeField, Min(1)]
    private int repairRadius = 2;

    [SerializeField, Min(1)]
    private int healingAmount = 3;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog = true;

    private Block block;

    private bool hasRepaired;

    public int RepairRadius =>
        repairRadius;

    public int HealingAmount =>
        healingAmount;

    public event Action<Vector3, int>
        HealingApplied;

    public event Action<int>
        RepairCompleted;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
        ValidateReferences();
    }

    private void OnEnable()
    {
        hasRepaired = false;

        FindReferences();
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        NormalizeSettings();
    }

    private void FindReferences()
    {
        if (block == null)
        {
            block =
                GetComponent<Block>();
        }

        if (blockGridManager == null &&
            Application.isPlaying)
        {
            blockGridManager =
                FindFirstObjectByType<
                    BlockGridManager
                >();
        }
    }

    private void NormalizeSettings()
    {
        repairRadius =
            Mathf.Max(
                repairRadius,
                1
            );

        healingAmount =
            Mathf.Max(
                healingAmount,
                1
            );
    }

    private void ValidateReferences()
    {
        if (block == null)
        {
            Debug.LogError(
                "RepairOnDestroyedEffect: " +
                "Block 컴포넌트를 찾지 못했습니다.",
                this
            );
        }

        if (blockGridManager == null)
        {
            Debug.LogError(
                "RepairOnDestroyedEffect: " +
                "BlockGridManager를 찾지 못했습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (block == null)
        {
            return;
        }

        block.Destroyed -=
            HandleBlockDestroyed;

        block.Destroyed +=
            HandleBlockDestroyed;
    }

    private void UnsubscribeEvents()
    {
        if (block == null)
        {
            return;
        }

        block.Destroyed -=
            HandleBlockDestroyed;
    }

    private void HandleBlockDestroyed(
        Block destroyedBlock)
    {
        if (hasRepaired ||
            destroyedBlock == null ||
            destroyedBlock != block)
        {
            return;
        }

        /*
         * 일반 스테이지의 Special 블록에서만
         * 수리 효과를 실행한다.
         */
        if (destroyedBlock.BlockType !=
            BlockType.Special)
        {
            if (showDebugLog)
            {
                Debug.LogWarning(
                    "RepairOnDestroyedEffect: " +
                    $"{destroyedBlock.name}의 타입이 " +
                    $"{destroyedBlock.BlockType}이므로 " +
                    "수리 효과를 실행하지 않습니다.",
                    destroyedBlock
                );
            }

            return;
        }

        if (blockGridManager == null)
        {
            FindReferences();

            if (blockGridManager == null)
            {
                return;
            }
        }

        hasRepaired = true;

        /*
         * repairRadius가 2이면
         * 수리 블록을 중심으로 5×5 범위를 검사한다.
         */
        List<Block> surroundingBlocks =
            BlockNeighborhoodResolver
                .FindSurroundingBlocks(
                    destroyedBlock,
                    blockGridManager.ActiveBlocks,
                    repairRadius
                );

        int repairedBlockCount = 0;

        for (int i = 0;
             i < surroundingBlocks.Count;
             i++)
        {
            Block targetBlock =
                surroundingBlocks[i];

            if (!CanRepairTarget(
                    targetBlock))
            {
                continue;
            }

            Vector3 targetPosition =
                targetBlock.transform.position;

            int appliedHealing =
                targetBlock.Heal(
                    healingAmount
                );

            if (appliedHealing <= 0)
            {
                continue;
            }

            repairedBlockCount++;

            /*
             * 실제로 회복된 모든 블록 위치에
             * 각각 회복 팝업을 표시한다.
             */
            HealingApplied?.Invoke(
                targetPosition,
                appliedHealing
            );

            if (showDebugLog)
            {
                Debug.Log(
                    "RepairOnDestroyedEffect: " +
                    $"{targetBlock.name} 체력 " +
                    $"{appliedHealing} 회복, " +
                    $"현재 체력=" +
                    $"{targetBlock.CurrentHealth}/" +
                    $"{targetBlock.MaxHealth}",
                    targetBlock
                );
            }
        }

        if (showDebugLog)
        {
            int totalSize =
                repairRadius *
                2 +
                1;

            Debug.Log(
                "RepairOnDestroyedEffect: " +
                $"{totalSize}×{totalSize} 범위 수리 완료, " +
                $"회복 대상 {repairedBlockCount}개",
                this
            );
        }

        RepairCompleted?.Invoke(
            repairedBlockCount
        );
    }

    private bool CanRepairTarget(
        Block targetBlock)
    {
        if (targetBlock == null ||
            targetBlock == block ||
            !targetBlock.IsAlive)
        {
            return false;
        }

        if (!targetBlock.IsBreakable)
        {
            return false;
        }

        if (targetBlock.CurrentHealth >=
            targetBlock.MaxHealth)
        {
            return false;
        }

        return true;
    }
}