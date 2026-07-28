using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ElementalBallEffect :
    BallTraitEffect
{
    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog;

    private static ElementReactionVfxSpawner
        cachedReactionVfxSpawner;

    private static ElementConductionVfxSpawner
        cachedConductionVfxSpawner;

    private static BlockGridManager
        cachedBlockGridManager;

    private static bool
        hasWarnedMissingReactionVfxSpawner;

    private static bool
        hasWarnedMissingConductionVfxSpawner;

    private ElementalBallTraitDefinition
        elementalDefinition;

    public override BallTraitType TraitType =>
        BallTraitType.Elemental;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType
            .SubsystemRegistration
    )]
    private static void ResetStaticCache()
    {
        cachedReactionVfxSpawner =
            null;

        cachedConductionVfxSpawner =
            null;

        cachedBlockGridManager =
            null;

        hasWarnedMissingReactionVfxSpawner =
            false;

        hasWarnedMissingConductionVfxSpawner =
            false;
    }

    protected override void OnInitialized()
    {
        elementalDefinition =
            TraitDefinition as
                ElementalBallTraitDefinition;

        if (elementalDefinition == null)
        {
            Debug.LogWarning(
                "ElementalBallEffect: " +
                "ElementalBallTraitDefinition이 " +
                "연결되지 않았습니다. " +
                "직접 피해만 적용됩니다.",
                this
            );
        }
    }

    public override BallHitResult ResolveHit(
        BallHitContext context)
    {
        if (context == null ||
            context.Block == null ||
            !context.Block.IsAlive ||
            CombatController == null)
        {
            return BallHitResult.NotHandled();
        }

        Block targetBlock =
            context.Block;

        int appliedDirectDamage =
            CombatController.ApplyDamage(
                targetBlock,
                context.DirectDamage,
                context.HitPoint
            );

        /*
         * 쉴드나 무적으로 직접 피해가 막히면
         *Controller.ApplyDamage(
                targetBlock,
                context.DirectDamage,
                context.Hit 속성 스택과 반응도 발생하지 않습니다.
         */
        if (appliedDirectDamage <= 0)
        {
            if (showDebugLog)
            {
                Debug.Log(
                    "ElementalBallEffect: " +
                    $"{targetBlock.name}의 직접 피해가 " +
                    "막혀 속성 스택을 적용하지 않습니다.",
                    this
                );
            }

            return BallHitResult
                .HandledWithBounce();
        }

        /*
         * 직접 피해로 블록이 파괴되었다면
         * 신규 스택과 반응을 처리하지 않습니다.
         */
        if (targetBlock == null ||
            !targetBlock.IsAlive)
        {
            return BallHitResult
                .HandledWithBounce();
        }

        if (elementalDefinition == null)
        {
            return BallHitResult
                .HandledWithBounce();
        }

        BlockElementStatus elementStatus =
            GetOrAddElementStatus(
                targetBlock
            );

        if (elementStatus == null)
        {
            return BallHitResult
                .HandledWithBounce();
        }

        BallStarGrade starGrade =
            context.Definition != null
                ? context.Definition.StarGrade
                : BallStarGrade.OneStar;

        int requestedStackAmount =
            elementalDefinition.GetStackAmount(
                starGrade
            );

        ElementReactionResult reactionResult =
            ElementReactionResolver
                .ApplyElementAndResolve(
                    elementStatus,
                    elementalDefinition.ElementType,
                    requestedStackAmount,
                    context.DirectDamage,
                    elementalDefinition
                        .ElectrocutionDamageMultiplier,
                    elementalDefinition
                        .ThermalShockDamageMultiplier
                );

        if (showDebugLog)
        {
            Debug.Log(
                "ElementalBallEffect: " +
                $"{targetBlock.name}, " +
                $"속성={elementalDefinition.ElementType}, " +
                $"적용 스택={reactionResult.AppliedElementStack}, " +
                $"반응={reactionResult.ReactionKind}, " +
                $"반응 횟수={reactionResult.ReactionCount}, " +
                $"젖음={reactionResult.WetStackAfter}, " +
                $"전하={reactionResult.ChargeStackAfter}",
                this
            );
        }

        if (!reactionResult.HasReaction)
        {
            return BallHitResult
                .HandledWithBounce();
        }

        /*
         * 중심 블록이 감전 피해로 파괴되기 전에
         * 연결된 젖은 블록 목록을 먼저 확보합니다.
         */
        List<Block> conductionTargets =
            PrepareConductionTargets(
                targetBlock,
                reactionResult
            );

        PlayReactionVfx(
            targetBlock,
            reactionResult
        );

        PlayConductionVfx(
            targetBlock,
            conductionTargets
        );

        BallDamageTextStyleDefinition
            reactionDamageStyle =
                ResolveReactionDamageStyle(
                    reactionResult
                );

        /*
         * 중심 블록의 감전 또는 열충격 추가 피해입니다.
         */
        if (targetBlock != null &&
            targetBlock.IsAlive)
        {
            CombatController.ApplyDamage(
                targetBlock,
                reactionResult.TotalDamage,
                targetBlock.transform.position,
                reactionDamageStyle
            );
        }

        /*
         * 전도 피해는 별도의 속성 반응을 일으키지 않고
         * 데미지만 직접 적용합니다.
         */
        ApplyConductionDamage(
            conductionTargets,
            reactionResult,
            reactionDamageStyle
        );

        return BallHitResult
            .HandledWithBounce();
    }

    private List<Block> PrepareConductionTargets(
        Block sourceBlock,
        ElementReactionResult reactionResult)
    {
        List<Block> emptyResult =
            new List<Block>();

        if (sourceBlock == null ||
            elementalDefinition == null ||
            !elementalDefinition
                .EnableWetConduction)
        {
            return emptyResult;
        }

        /*
         * 물 공으로 전하를 터뜨렸을 때는
         * 일반 감전만 발생합니다.
         *
         * 물을 준비하고 번개로 터뜨리는 플레이를
         * 명확하게 만들기 위해 번개 공 직접 적중일 때만
         * 전도망이 실행됩니다.
         */
        if (elementalDefinition.ElementType !=
            ElementType.Electric)
        {
            return emptyResult;
        }

        if (!reactionResult.IsElectrocution ||
            reactionResult.ReactionCount <= 0 ||
            reactionResult.DamagePerReaction <= 0)
        {
            return emptyResult;
        }

        BlockGridManager blockGridManager =
            FindBlockGridManager();

        if (blockGridManager == null)
        {
            if (showDebugLog)
            {
                Debug.LogWarning(
                    "ElementalBallEffect: " +
                    "BlockGridManager를 찾지 못해 " +
                    "젖음 전도를 실행하지 않습니다.",
                    this
                );
            }

            return emptyResult;
        }

        int maximumTargetCount =
            Mathf.Min(
                reactionResult.ReactionCount,
                elementalDefinition
                    .MaximumConductionTargets
            );

        return ElementConductionResolver
            .FindWetConductionTargets(
                sourceBlock,
                blockGridManager.ActiveBlocks,
                maximumTargetCount,
                elementalDefinition
                    .WetStackCostPerTarget
            );
    }

    private void ApplyConductionDamage(
        List<Block> conductionTargets,
        ElementReactionResult reactionResult,
        BallDamageTextStyleDefinition damageStyle)
    {
        if (conductionTargets == null ||
            conductionTargets.Count <= 0 ||
            reactionResult.DamagePerReaction <= 0)
        {
            return;
        }

        int wetStackCost =
            elementalDefinition != null
                ? elementalDefinition
                    .WetStackCostPerTarget
                : 1;

        for (int i = 0;
             i < conductionTargets.Count;
             i++)
        {
            Block targetBlock =
                conductionTargets[i];

            if (targetBlock == null ||
                !targetBlock.IsAlive ||
                !targetBlock.IsBreakable)
            {
                continue;
            }

            BlockElementStatus targetStatus =
                targetBlock.GetComponent<
                    BlockElementStatus
                >();

            if (targetStatus == null ||
                targetStatus.WetStack <
                wetStackCost)
            {
                continue;
            }

            Vector2 damagePosition =
                targetBlock.transform.position;

            int appliedDamage =
                CombatController.ApplyDamage(
                    targetBlock,
                    reactionResult
                        .DamagePerReaction,
                    damagePosition,
                    damageStyle
                );

            /*
             * 쉴드에 막힌 경우 젖음은 소비하지 않습니다.
             */
            if (appliedDamage <= 0)
            {
                continue;
            }

            /*
             * 피해로 살아남은 블록만 직접 젖음을 소비합니다.
             * 파괴된 블록은 BlockElementStatus의 파괴 이벤트에서
             * 전체 상태가 자동 초기화됩니다.
             */
            if (targetBlock.IsAlive)
            {
                targetStatus.ConsumeWet(
                    wetStackCost
                );
            }

            if (showDebugLog)
            {
                Debug.Log(
                    "ElementalBallEffect: " +
                    $"{targetBlock.name} 전도 피해 " +
                    $"{appliedDamage}, " +
                    $"젖음 소비={wetStackCost}",
                    this
                );
            }
        }
    }

    private BallDamageTextStyleDefinition
        ResolveReactionDamageStyle(
            ElementReactionResult reactionResult)
    {
        if (elementalDefinition == null)
        {
            return null;
        }

        switch (reactionResult.ReactionKind)
        {
            case ElementReactionKind
                .Electrocution:

                return elementalDefinition
                    .ElectrocutionDamageTextStyle;

            case ElementReactionKind
                .ThermalShock:

                return elementalDefinition
                    .ThermalShockDamageTextStyle;

            default:

                return null;
        }
    }

    private void PlayReactionVfx(
        Block targetBlock,
        ElementReactionResult reactionResult)
    {
        if (targetBlock == null)
        {
            return;
        }

        switch (reactionResult.ReactionKind)
        {
            case ElementReactionKind
                .Electrocution:

                PlayElectrocutionVfx(
                    targetBlock,
                    reactionResult.ReactionCount
                );

                break;

            case ElementReactionKind
                .ThermalShock:

                /*
                 * 불·얼음 작업 단계에서
                 * 열충격 VFX를 연결합니다.
                 */
                break;
        }
    }

    private void PlayElectrocutionVfx(
        Block targetBlock,
        int reactionCount)
    {
        if (targetBlock == null ||
            reactionCount <= 0)
        {
            return;
        }

        ElementReactionVfxSpawner vfxSpawner =
            FindReactionVfxSpawner();

        if (vfxSpawner != null)
        {
            vfxSpawner.PlayElectrocution(
                targetBlock,
                reactionCount
            );

            return;
        }

        if (hasWarnedMissingReactionVfxSpawner)
        {
            return;
        }

        hasWarnedMissingReactionVfxSpawner =
            true;

        if (showDebugLog)
        {
            Debug.LogWarning(
                "ElementalBallEffect: " +
                "ElementReactionVfxSpawner를 " +
                "찾지 못했습니다.",
                this
            );
        }
    }

    private void PlayConductionVfx(
        Block sourceBlock,
        List<Block> conductionTargets)
    {
        if (sourceBlock == null ||
            conductionTargets == null ||
            conductionTargets.Count <= 0)
        {
            return;
        }

        ElementConductionVfxSpawner vfxSpawner =
            FindConductionVfxSpawner();

        if (vfxSpawner == null)
        {
            if (!hasWarnedMissingConductionVfxSpawner)
            {
                hasWarnedMissingConductionVfxSpawner =
                    true;

                if (showDebugLog)
                {
                    Debug.LogWarning(
                        "ElementalBallEffect: " +
                        "ElementConductionVfxSpawner를 " +
                        "찾지 못했습니다.",
                        this
                    );
                }
            }

            return;
        }

        for (int i = 0;
             i < conductionTargets.Count;
             i++)
        {
            Block targetBlock =
                conductionTargets[i];

            if (targetBlock == null)
            {
                continue;
            }

            vfxSpawner.Play(
                sourceBlock,
                targetBlock,
                i
            );
        }
    }

    private ElementReactionVfxSpawner
        FindReactionVfxSpawner()
    {
        if (cachedReactionVfxSpawner != null)
        {
            return cachedReactionVfxSpawner;
        }

        cachedReactionVfxSpawner =
            UnityEngine.Object
                .FindFirstObjectByType<
                    ElementReactionVfxSpawner
                >();

        return cachedReactionVfxSpawner;
    }

    private ElementConductionVfxSpawner
        FindConductionVfxSpawner()
    {
        if (cachedConductionVfxSpawner != null)
        {
            return cachedConductionVfxSpawner;
        }

        cachedConductionVfxSpawner =
            UnityEngine.Object
                .FindFirstObjectByType<
                    ElementConductionVfxSpawner
                >();

        return cachedConductionVfxSpawner;
    }

    private BlockGridManager
        FindBlockGridManager()
    {
        if (cachedBlockGridManager != null)
        {
            return cachedBlockGridManager;
        }

        cachedBlockGridManager =
            UnityEngine.Object
                .FindFirstObjectByType<
                    BlockGridManager
                >();

        return cachedBlockGridManager;
    }

    private static BlockElementStatus
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

        EnsureStatusViews(
            targetBlock
        );

        return status;
    }

    private static void EnsureStatusViews(
        Block targetBlock)
    {
        if (targetBlock == null)
        {
            return;
        }

        BlockWetChargeStatusView
            wetChargeStatusView =
                targetBlock.GetComponent<
                    BlockWetChargeStatusView
                >();

        if (wetChargeStatusView == null)
        {
            targetBlock.gameObject
                .AddComponent<
                    BlockWetChargeStatusView
                >();
        }

        BlockBurnFrostStatusView
            burnFrostStatusView =
                targetBlock.GetComponent<
                    BlockBurnFrostStatusView
                >();

        if (burnFrostStatusView == null)
        {
            targetBlock.gameObject
                .AddComponent<
                    BlockBurnFrostStatusView
                >();
        }
    }
}