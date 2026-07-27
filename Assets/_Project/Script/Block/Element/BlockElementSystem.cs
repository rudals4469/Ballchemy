using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BlockElementSystem :
    MonoBehaviour
{
    [Header("Burn Damage")]
    [Tooltip(
        "불 공 직접 피해에 곱하는 " +
        "화상 1스택당 피해 배율입니다."
    )]
    [SerializeField, Min(0f)]
    private float burnDamageMultiplierPerStack =
        0.25f;

    [SerializeField, Min(1)]
    private int burnStackDecayPerTurn = 1;

    [SerializeField]
    private BallDamageTextStyleDefinition
        burnDamageTextStyle;

    [Header("Burn Spread")]
    [Tooltip(
        "화상 전염이 시작되는 최소 잔여 스택입니다."
    )]
    [SerializeField, Min(1)]
    private int mediumSpreadMinimumStack = 3;

    [Tooltip(
        "강한 화상 전염이 시작되는 최소 잔여 스택입니다."
    )]
    [SerializeField, Min(1)]
    private int strongSpreadMinimumStack = 5;

    [SerializeField, Min(0)]
    private int mediumSpreadTargetCount = 1;

    [SerializeField, Min(0)]
    private int strongSpreadTargetCount = 2;

    [SerializeField, Min(1)]
    private int spreadBurnStackAmount = 1;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog;

    private readonly List<Block>
        burningBlockSnapshot =
            new List<Block>();

    private readonly List<Block>
        spreadCandidates =
            new List<Block>();

    private readonly HashSet<Block>
        receivedSpreadThisTurn =
            new HashSet<Block>();

    public event Action<Block, int>
        BurnDamageApplied;

    public event Action<Block, Block>
        BurnSpreadApplied;

    public event Action<Block>
        FrozenAttackCancelled;

    private void OnValidate()
    {
        burnDamageMultiplierPerStack =
            Mathf.Max(
                burnDamageMultiplierPerStack,
                0f
            );

        burnStackDecayPerTurn =
            Mathf.Max(
                burnStackDecayPerTurn,
                1
            );

        mediumSpreadMinimumStack =
            Mathf.Max(
                mediumSpreadMinimumStack,
                1
            );

        strongSpreadMinimumStack =
            Mathf.Max(
                strongSpreadMinimumStack,
                mediumSpreadMinimumStack
            );

        mediumSpreadTargetCount =
            Mathf.Max(
                mediumSpreadTargetCount,
                0
            );

        strongSpreadTargetCount =
            Mathf.Max(
                strongSpreadTargetCount,
                0
            );

        spreadBurnStackAmount =
            Mathf.Max(
                spreadBurnStackAmount,
                1
            );
    }

    public void ResolveTurnEffects(
        IReadOnlyList<Block> activeBlocks)
    {
        CreateBurningBlockSnapshot(
            activeBlocks
        );

        receivedSpreadThisTurn.Clear();

        for (int i = 0;
             i < burningBlockSnapshot.Count;
             i++)
        {
            Block sourceBlock =
                burningBlockSnapshot[i];

            ResolveBurnForBlock(
                sourceBlock,
                activeBlocks
            );
        }

        burningBlockSnapshot.Clear();
        spreadCandidates.Clear();
        receivedSpreadThisTurn.Clear();
    }

    public bool TryConsumeFrozenAttack(
        Block attackingBlock)
    {
        if (attackingBlock == null ||
            !attackingBlock.IsAlive)
        {
            return false;
        }

        BlockElementStatus status =
            attackingBlock.GetComponent<
                BlockElementStatus
            >();

        if (status == null ||
            !status.IsFrozen)
        {
            return false;
        }

        bool consumed =
            status.ConsumeFrozen();

        if (!consumed)
        {
            return false;
        }

        FrozenAttackCancelled?.Invoke(
            attackingBlock
        );

        if (showDebugLog)
        {
            Debug.Log(
                "BlockElementSystem: " +
                $"{attackingBlock.name}의 동결을 소비해 " +
                "공격을 취소했습니다.",
                attackingBlock
            );
        }

        return true;
    }

    private void CreateBurningBlockSnapshot(
        IReadOnlyList<Block> activeBlocks)
    {
        burningBlockSnapshot.Clear();

        if (activeBlocks == null)
        {
            return;
        }

        for (int i = 0;
             i < activeBlocks.Count;
             i++)
        {
            Block block =
                activeBlocks[i];

            if (block == null ||
                !block.IsAlive ||
                !block.IsBreakable)
            {
                continue;
            }

            BlockElementStatus status =
                block.GetComponent<
                    BlockElementStatus
                >();

            if (status == null ||
                !status.HasBurn)
            {
                continue;
            }

            burningBlockSnapshot.Add(
                block
            );
        }
    }

    private void ResolveBurnForBlock(
        Block sourceBlock,
        IReadOnlyList<Block> activeBlocks)
    {
        if (sourceBlock == null ||
            !sourceBlock.IsAlive)
        {
            return;
        }

        BlockElementStatus sourceStatus =
            sourceBlock.GetComponent<
                BlockElementStatus
            >();

        if (sourceStatus == null ||
            !sourceStatus.HasBurn)
        {
            return;
        }

        int burnStackBefore =
            sourceStatus.BurnStack;

        int sourceDirectDamage =
            Mathf.Max(
                sourceStatus
                    .BurnSourceDirectDamage,
                1
            );

        int burnDamage =
            CalculateBurnDamage(
                sourceDirectDamage,
                burnStackBefore
            );

        int appliedDamage =
            ApplyStatusDamage(
                sourceBlock,
                burnDamage,
                burnDamageTextStyle
            );

        if (appliedDamage > 0)
        {
            BurnDamageApplied?.Invoke(
                sourceBlock,
                appliedDamage
            );
        }

        /*
         * 화상 피해로 블록이 파괴되면
         * 스택 감소와 전염은 처리하지 않습니다.
         */
        if (sourceBlock == null ||
            !sourceBlock.IsAlive)
        {
            return;
        }

        sourceStatus.ConsumeBurn(
            burnStackDecayPerTurn
        );

        int remainingBurnStack =
            sourceStatus.BurnStack;

        int spreadTargetCount =
            ResolveSpreadTargetCount(
                remainingBurnStack
            );

        if (spreadTargetCount <= 0)
        {
            return;
        }

        SpreadBurn(
            sourceBlock,
            sourceStatus,
            activeBlocks,
            spreadTargetCount
        );

        if (showDebugLog)
        {
            Debug.Log(
                "BlockElementSystem: " +
                $"{sourceBlock.name} 화상 처리, " +
                $"피해={burnDamage}, " +
                $"실제피해={appliedDamage}, " +
                $"잔여스택={remainingBurnStack}, " +
                $"전염대상수={spreadTargetCount}",
                sourceBlock
            );
        }
    }

    private int CalculateBurnDamage(
        int sourceDirectDamage,
        int burnStack)
    {
        if (sourceDirectDamage <= 0 ||
            burnStack <= 0)
        {
            return 0;
        }

        return Mathf.Max(
            Mathf.FloorToInt(
                sourceDirectDamage *
                burnDamageMultiplierPerStack *
                burnStack +
                0.5f
            ),
            1
        );
    }

    private int ResolveSpreadTargetCount(
        int remainingBurnStack)
    {
        if (remainingBurnStack >=
            strongSpreadMinimumStack)
        {
            return strongSpreadTargetCount;
        }

        if (remainingBurnStack >=
            mediumSpreadMinimumStack)
        {
            return mediumSpreadTargetCount;
        }

        return 0;
    }

    private void SpreadBurn(
        Block sourceBlock,
        BlockElementStatus sourceStatus,
        IReadOnlyList<Block> activeBlocks,
        int maximumTargetCount)
    {
        if (sourceBlock == null ||
            sourceStatus == null ||
            activeBlocks == null ||
            maximumTargetCount <= 0)
        {
            return;
        }

        List<Block> adjacentBlocks =
            BlockNeighborhoodResolver
                .FindPatternBlocks(
                    sourceBlock,
                    activeBlocks,
                    ExplosionPatternType.Cross,
                    1
                );

        spreadCandidates.Clear();

        for (int i = 0;
             i < adjacentBlocks.Count;
             i++)
        {
            Block targetBlock =
                adjacentBlocks[i];

            if (!CanReceiveBurnSpread(
                    targetBlock))
            {
                continue;
            }

            if (receivedSpreadThisTurn.Contains(
                    targetBlock))
            {
                continue;
            }

            spreadCandidates.Add(
                targetBlock
            );
        }

        /*
         * 화상이 없는 블록을 먼저 선택하고,
         * 이후 그리드 위치 순서로 정렬합니다.
         */
        spreadCandidates.Sort(
            CompareSpreadCandidates
        );

        int appliedTargetCount = 0;

        for (int i = 0;
             i < spreadCandidates.Count;
             i++)
        {
            if (appliedTargetCount >=
                maximumTargetCount)
            {
                break;
            }

            Block targetBlock =
                spreadCandidates[i];

            BlockElementStatus targetStatus =
                GetOrAddElementStatus(
                    targetBlock
                );

            if (targetStatus == null)
            {
                continue;
            }

            /*
             * 화상 전염 자체는 열충격을 발생시키지 않습니다.
             * 냉기나 동결이 있는 블록은 전염 대상에서 제외합니다.
             */
            if (targetStatus.HasFrost ||
                targetStatus.IsFrozen)
            {
                continue;
            }

            int appliedStack =
                targetStatus.AddBurn(
                    spreadBurnStackAmount,
                    sourceStatus
                        .BurnSourceDirectDamage
                );

            if (appliedStack <= 0)
            {
                continue;
            }

            receivedSpreadThisTurn.Add(
                targetBlock
            );

            appliedTargetCount++;

            BurnSpreadApplied?.Invoke(
                sourceBlock,
                targetBlock
            );

            if (showDebugLog)
            {
                Debug.Log(
                    "BlockElementSystem: " +
                    $"{sourceBlock.name} → " +
                    $"{targetBlock.name} 화상 전염, " +
                    $"적용스택={appliedStack}",
                    targetBlock
                );
            }
        }
    }

    private bool CanReceiveBurnSpread(
        Block targetBlock)
    {
        if (targetBlock == null ||
            !targetBlock.IsAlive ||
            !targetBlock.IsBreakable)
        {
            return false;
        }

        BlockElementStatus status =
            targetBlock.GetComponent<
                BlockElementStatus
            >();

        if (status == null)
        {
            return true;
        }

        return
            !status.HasFrost &&
            !status.IsFrozen;
    }

    private int CompareSpreadCandidates(
        Block left,
        Block right)
    {
        if (left == right)
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        BlockElementStatus leftStatus =
            left.GetComponent<
                BlockElementStatus
            >();

        BlockElementStatus rightStatus =
            right.GetComponent<
                BlockElementStatus
            >();

        bool leftHasBurn =
            leftStatus != null &&
            leftStatus.HasBurn;

        bool rightHasBurn =
            rightStatus != null &&
            rightStatus.HasBurn;

        if (leftHasBurn != rightHasBurn)
        {
            return leftHasBurn
                ? 1
                : -1;
        }

        int rowCompare =
            left.StartRow.CompareTo(
                right.StartRow
            );

        if (rowCompare != 0)
        {
            return rowCompare;
        }

        return left.StartColumn.CompareTo(
            right.StartColumn
        );
    }

    private BlockElementStatus
        GetOrAddElementStatus(
            Block targetBlock)
    {
        if (targetBlock == null)
        {
            return null;
        }

        BlockElementStatus status =
            targetBlock.GetComponent<
                BlockElementStatus
            >();

        if (status == null)
        {
            status =
                targetBlock.gameObject
                    .AddComponent<
                        BlockElementStatus
                    >();
        }

        BlockElementSurfaceStatusView
            surfaceView =
                targetBlock.GetComponent<
                    BlockElementSurfaceStatusView
                >();

        if (surfaceView == null)
        {
            targetBlock.gameObject
                .AddComponent<
                    BlockElementSurfaceStatusView
                >();
        }

        return status;
    }

    private int ApplyStatusDamage(
        Block targetBlock,
        int calculatedDamage,
        BallDamageTextStyleDefinition style)
    {
        if (targetBlock == null ||
            !targetBlock.IsAlive ||
            calculatedDamage <= 0)
        {
            return 0;
        }

        int healthBeforeDamage =
            Mathf.Max(
                targetBlock.CurrentHealth,
                0
            );

        Vector2 damagePosition =
            targetBlock.transform.position;

        targetBlock.TakeDamage(
            calculatedDamage
        );

        int healthAfterDamage =
            targetBlock != null
                ? Mathf.Max(
                    targetBlock.CurrentHealth,
                    0
                )
                : 0;

        int appliedHealthDamage =
            Mathf.Clamp(
                healthBeforeDamage -
                healthAfterDamage,
                0,
                healthBeforeDamage
            );

        if (appliedHealthDamage <= 0)
        {
            return 0;
        }

        BallDamageEvents.Publish(
            new BallDamageEvent(
                null,
                null,
                targetBlock,
                calculatedDamage,
                appliedHealthDamage,
                damagePosition,
                style
            )
        );

        return appliedHealthDamage;
    }
}