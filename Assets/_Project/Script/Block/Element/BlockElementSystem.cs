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

    [Header("Frozen Shatter")]
    [Tooltip(
        "일반 공이 동결 블록을 파쇄할 때 " +
        "직접 피해에 곱하는 배율입니다."
    )]
    [SerializeField, Min(0f)]
    private float frozenShatterDamageMultiplier =
        1f;

    [Tooltip(
        "불 공이 동결 블록을 파쇄할 때 " +
        "직접 피해에 곱하는 배율입니다."
    )]
    [SerializeField, Min(0f)]
    private float fireFrozenShatterDamageMultiplier =
        1.5f;

    [SerializeField]
    private BallDamageTextStyleDefinition
        frozenShatterDamageTextStyle;

    [SerializeField]
    private BallDamageTextStyleDefinition
        fireFrozenShatterDamageTextStyle;

    [Header("Frozen Shatter Spread")]
    [Tooltip(
        "일반 파쇄 시 주변 블록에 전파할 " +
        "냉기 스택입니다."
    )]
    [SerializeField, Min(1)]
    private int frozenShatterFrostSpreadAmount =
        1;

    [Tooltip(
        "불 강화 파쇄 시 주변 블록에 전파할 " +
        "화상 스택입니다."
    )]
    [SerializeField, Min(1)]
    private int fireShatterBurnSpreadAmount =
        1;

    [Header("References")]
    [SerializeField]
    private BlockGridManager blockGridManager;

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

    public event Action<Block, int, bool>
        FrozenShatterDamageApplied;

    public event Action<
        Block,
        Block,
        ElementType
    > FrozenShatterSpreadApplied;

    private void Awake()
    {
        FindReferences();
    }

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

        frozenShatterDamageMultiplier =
            Mathf.Max(
                frozenShatterDamageMultiplier,
                0f
            );

        fireFrozenShatterDamageMultiplier =
            Mathf.Max(
                fireFrozenShatterDamageMultiplier,
                0f
            );

        frozenShatterFrostSpreadAmount =
            Mathf.Max(
                frozenShatterFrostSpreadAmount,
                1
            );

        fireShatterBurnSpreadAmount =
            Mathf.Max(
                fireShatterBurnSpreadAmount,
                1
            );
    }

    private void FindReferences()
    {
        if (blockGridManager == null)
        {
            blockGridManager =
                GetComponent<
                    BlockGridManager
                >();
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

    public bool ResolveFrozenShatter(
        BallCombatController sourceController,
        Block targetBlock,
        int calculatedDirectDamage)
    {
        if (sourceController == null ||
            targetBlock == null ||
            !targetBlock.IsAlive ||
            calculatedDirectDamage <= 0)
        {
            return false;
        }

        BlockElementStatus targetStatus =
            targetBlock.GetComponent<
                BlockElementStatus
            >();

        if (targetStatus == null ||
            !targetStatus.IsFrozen)
        {
            return false;
        }

        bool isFireShatter =
            IsFireBall(
                sourceController
            );

        /*
         * 동결은 추가 피해를 적용하기 전에
         * 먼저 소비합니다.
         *
         * 이후 ElementalBallEffect가 계속 실행되면
         * 불 또는 얼음 스택을 정상적으로 새로 적용할 수 있습니다.
         */
        if (!targetStatus.ConsumeFrozen())
        {
            return false;
        }

        float damageMultiplier =
            isFireShatter
                ? fireFrozenShatterDamageMultiplier
                : frozenShatterDamageMultiplier;

        int shatterDamage =
            Mathf.Max(
                Mathf.FloorToInt(
                    calculatedDirectDamage *
                    damageMultiplier +
                    0.5f
                ),
                1
            );

        BallDamageTextStyleDefinition
            damageStyle =
                isFireShatter
                    ? fireFrozenShatterDamageTextStyle
                    : frozenShatterDamageTextStyle;

        int appliedShatterDamage =
            ApplyDamageAndPublish(
                targetBlock,
                shatterDamage,
                damageStyle,
                sourceController.SourceBall,
                sourceController.Definition
            );

        ElementType spreadElement =
            isFireShatter
                ? ElementType.Fire
                : ElementType.Ice;

        int spreadAmount =
            isFireShatter
                ? fireShatterBurnSpreadAmount
                : frozenShatterFrostSpreadAmount;

        /*
         * 파쇄 추가 피해로 중심 블록이 파괴돼도
         * 파쇄 자체는 이미 발생했으므로 주변 전파는 실행합니다.
         */
        SpreadFrozenShatterElement(
            targetBlock,
            spreadElement,
            spreadAmount,
            calculatedDirectDamage
        );

        FrozenShatterDamageApplied?.Invoke(
            targetBlock,
            appliedShatterDamage,
            isFireShatter
        );

        if (showDebugLog)
        {
            Debug.Log(
                "BlockElementSystem: " +
                $"{targetBlock.name} 동결 파쇄, " +
                $"불 강화 파쇄={isFireShatter}, " +
                $"계산 피해={shatterDamage}, " +
                $"실제 피해={appliedShatterDamage}, " +
                $"전파 속성={spreadElement}",
                targetBlock
            );
        }

        return true;
    }

    private bool IsFireBall(
        BallCombatController sourceController)
    {
        if (sourceController == null ||
            sourceController.TraitType !=
            BallTraitType.Elemental)
        {
            return false;
        }

        ElementalBallTraitDefinition
            elementalDefinition =
                sourceController.TraitDefinition as
                    ElementalBallTraitDefinition;

        return
            elementalDefinition != null &&
            elementalDefinition.ElementType ==
            ElementType.Fire;
    }

    private void SpreadFrozenShatterElement(
        Block sourceBlock,
        ElementType spreadElement,
        int spreadAmount,
        int sourceDirectDamage)
    {
        if (sourceBlock == null ||
            spreadAmount <= 0)
        {
            return;
        }

        FindReferences();

        if (blockGridManager == null)
        {
            if (showDebugLog)
            {
                Debug.LogWarning(
                    "BlockElementSystem: " +
                    "BlockGridManager를 찾지 못해 " +
                    "파쇄 속성을 주변에 전파하지 않습니다.",
                    this
                );
            }

            return;
        }

        IReadOnlyList<Block> activeBlocks =
            blockGridManager.ActiveBlocks;

        List<Block> adjacentBlocks =
            BlockNeighborhoodResolver
                .FindPatternBlocks(
                    sourceBlock,
                    activeBlocks,
                    ExplosionPatternType.Cross,
                    1
                );

        adjacentBlocks.Sort(
            CompareGridPosition
        );

        for (int i = 0;
             i < adjacentBlocks.Count;
             i++)
        {
            Block targetBlock =
                adjacentBlocks[i];

            if (targetBlock == null ||
                !targetBlock.IsAlive ||
                !targetBlock.IsBreakable)
            {
                continue;
            }

            BlockElementStatus targetStatus =
                GetOrAddElementStatus(
                    targetBlock
                );

            if (targetStatus == null)
            {
                continue;
            }

            int appliedStack = 0;

            switch (spreadElement)
            {
                case ElementType.Ice:
                {
                    /*
                     * 파쇄 냉기 전파는 화상과 반응하지 않습니다.
                     * 숨겨진 간접 열충격을 방지하기 위해
                     * 화상 블록은 전파 대상에서 제외합니다.
                     */
                    if (targetStatus.HasBurn ||
                        targetStatus.IsFrozen)
                    {
                        continue;
                    }

                    appliedStack =
                        targetStatus.AddFrost(
                            spreadAmount
                        );

                    break;
                }

                case ElementType.Fire:
                {
                    /*
                     * 강화 파쇄의 화상 전파 역시
                     * 냉기와 간접 열충격을 일으키지 않습니다.
                     */
                    if (targetStatus.HasFrost ||
                        targetStatus.IsFrozen)
                    {
                        continue;
                    }

                    appliedStack =
                        targetStatus.AddBurn(
                            spreadAmount,
                            sourceDirectDamage
                        );

                    break;
                }
            }

            if (appliedStack <= 0)
            {
                continue;
            }

            FrozenShatterSpreadApplied?.Invoke(
                sourceBlock,
                targetBlock,
                spreadElement
            );

            if (showDebugLog)
            {
                Debug.Log(
                    "BlockElementSystem: " +
                    $"{sourceBlock.name} → " +
                    $"{targetBlock.name} 파쇄 전파, " +
                    $"속성={spreadElement}, " +
                    $"스택={appliedStack}",
                    targetBlock
                );
            }
        }
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
            ApplyDamageAndPublish(
                sourceBlock,
                burnDamage,
                burnDamageTextStyle,
                null,
                null
            );

        if (appliedDamage > 0)
        {
            BurnDamageApplied?.Invoke(
                sourceBlock,
                appliedDamage
            );
        }

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

        return CompareGridPosition(
            left,
            right
        );
    }

    private static int CompareGridPosition(
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

    private int ApplyDamageAndPublish(
        Block targetBlock,
        int calculatedDamage,
        BallDamageTextStyleDefinition style,
        Ball sourceBall,
        BallDefinition sourceDefinition)
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
                sourceBall,
                sourceDefinition,
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