using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ElementalBallEffect : BallTraitEffect
{
    [SerializeField] private bool showDebugLog;

    private ElementalBallTraitDefinition elementalDefinition;
    private static BlockGridManager cachedGrid;
    private static ElementRuntimeParameters cachedParameters;

    public override BallTraitType TraitType => BallTraitType.Elemental;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticCache()
    {
        cachedGrid = null;
        cachedParameters = null;
    }

    protected override void OnInitialized()
    {
        elementalDefinition = TraitDefinition as ElementalBallTraitDefinition;
    }

    public override BallHitResult ResolveHit(BallHitContext context)
    {
        if (context == null || context.Block == null ||
            !context.Block.IsAlive || CombatController == null)
            return BallHitResult.NotHandled();

        Block target = context.Block;
        BlockElementStatus status = target.GetComponent<BlockElementStatus>();
        bool pendingThermalShock = elementalDefinition != null &&
            elementalDefinition.ElementType == ElementType.Fire &&
            status != null && status.IsFrozen;
        int appliedDirectDamage = CombatController.ApplyDamage(
            target, context.DirectDamage, context.HitPoint);

        if (appliedDirectDamage <= 0 || target == null ||
            elementalDefinition == null ||
            (!target.IsAlive && !pendingThermalShock))
            return BallHitResult.HandledWithBounce();

        if (status == null && target.IsAlive)
            status = GetOrAddElementStatus(target);
        ElementRuntimeParameters parameters = FindParameters();
        BlockGridManager grid = FindGrid();
        if (status == null || parameters == null)
            return BallHitResult.HandledWithBounce();

        BallStarGrade grade = context.Definition != null
            ? context.Definition.StarGrade
            : BallStarGrade.OneStar;
        grade = AugmentCombatModifiers.GetEffectiveStarGrade(context.Ball);
        int stackAmount = elementalDefinition.ElementType == ElementType.Water
            ? parameters.GetWaterStackAmount(grade)
            : elementalDefinition.ElementType == ElementType.Ice
                ? parameters.GetIceStackAmount(grade)
                : elementalDefinition.GetStackAmount(grade);
        stackAmount += AugmentCombatModifiers.GetAppliedStackBonus(
            context.Ball, elementalDefinition.ElementType);
        stackAmount = Mathf.Max(stackAmount, 1);

        switch (elementalDefinition.ElementType)
        {
            case ElementType.Water:
                ResolveWater(target, status, grid, context, stackAmount, parameters, grade);
                break;
            case ElementType.Electric:
                ResolveLightning(target, status, grid, context, parameters, grade);
                break;
            case ElementType.Ice:
                bool wasFrozen = status.IsFrozen;
                status.AddFrost(stackAmount);
                if (!wasFrozen && status.IsFrozen)
                {
                    AugmentCombatModifiers.NotifyFrozenApplied();
                    SpreadFrostOnFreeze(target, grid);
                    ResolveIntrinsicIceGradeEffect(target, grid, context, grade);
                }
                ElementVisualEvents.RaiseImpact(ElementType.Ice, target);
                break;
            case ElementType.Fire:
                ResolveFire(target, status, grid, context, stackAmount, parameters,
                    pendingThermalShock, grade);
                break;
        }

        if (showDebugLog)
            Debug.Log(
                $"ElementalBallEffect: {target.name}, 속성={elementalDefinition.ElementType}, " +
                $"Wet={status.WetStack}, Fire={status.BurnStack}, " +
                $"Ice={status.StoredFrostStack}, Frozen={status.IsFrozen}", this);

        return BallHitResult.HandledWithBounce();
    }

    private void ResolveWater(
        Block source, BlockElementStatus sourceStatus,
        BlockGridManager grid, BallHitContext context, int amount,
        ElementRuntimeParameters parameters,
        BallStarGrade grade)
    {
        int beforeHit = sourceStatus.WetStack;
        sourceStatus.AddWet(amount);
        ElementVisualEvents.RaiseImpact(ElementType.Water, source);

        ResolveIntrinsicWaterGradeEffect(
            source, sourceStatus, grid, context.Ball, grade, beforeHit,
            sourceStatus.GetMaximumStack(ElementType.Water));

        if (AugmentCombatModifiers.NotifyWaterHitAndShouldFlood() && grid != null)
        {
            IReadOnlyList<Block> allBlocks = grid.ActiveBlocks;
            for (int i = 0; i < allBlocks.Count; i++)
            {
                Block block = allBlocks[i];
                if (block == null || !block.IsAlive || !block.IsBreakable) continue;
                GetOrAddElementStatus(block)?.AddWet(1);
            }
        }

        int wetMaximum = sourceStatus.GetMaximumStack(ElementType.Water);
        if (beforeHit < wetMaximum || grid == null)
            return;

        int pressure = AugmentCombatModifiers.GetRuleInteger(
            RuleAugmentEffectKind.WaterPressure);
        if (pressure > 0)
            ApplyIndirectDamage(source,
                Mathf.Max(1, Mathf.RoundToInt(context.DirectDamage * pressure / 100f)),
                null);

        int spreadAmount = amount + Mathf.Max(0,
            AugmentCombatModifiers.GetRuleInteger(RuleAugmentEffectKind.WetSpread));
        List<Block> targets = ElementGridResolver.SpreadWetThroughMaximumBlocks(
            source, grid.ActiveBlocks, spreadAmount,
            wetMaximum);

        for (int i = 0; i < targets.Count; i++)
        {
            BlockElementStatus targetStatus = GetOrAddElementStatus(targets[i]);
            if (targetStatus == null) continue;
            targetStatus.AddWet(spreadAmount);
            ElementVisualEvents.RaiseTravel(ElementType.Water, source, targets[i]);
        }

        if (AugmentCombatModifiers.GetRuleInteger(
                RuleAugmentEffectKind.TsunamiSpread) > 0)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                List<Block> outer = BlockNeighborhoodResolver.FindSurroundingBlocks(
                    targets[i], grid.ActiveBlocks, 1);
                for (int j = 0; j < outer.Count; j++)
                    GetOrAddElementStatus(outer[j])?.AddWet(1);
            }
        }
    }

    private void ResolveLightning(
        Block source, BlockElementStatus sourceStatus,
        BlockGridManager grid, BallHitContext context,
        ElementRuntimeParameters parameters,
        BallStarGrade grade)
    {
        ElementVisualEvents.RaiseImpact(ElementType.Electric, source);
        bool isElectrocution = sourceStatus.HasWet && grid != null;
        int reactionBonus = 0;
        bool alchemyBoosted = isElectrocution &&
            AugmentCombatModifiers.TryConsumeAlchemyChain(out reactionBonus);

        ResidualChargeStatus residual = source.GetComponent<ResidualChargeStatus>();
        if (residual != null && residual.TryConsume())
        {
            int residualPercent = AugmentCombatModifiers.GetRuleInteger(
                RuleAugmentEffectKind.ResidualCharge);
            ApplyIndirectDamage(source,
                Mathf.Max(1, Mathf.RoundToInt(context.DirectDamage * residualPercent / 100f)),
                elementalDefinition.ElectrocutionDamageTextStyle);
        }

        ApplyIndirectDamage(source,
            parameters.CurrentLightningAdditionalDamage + reactionBonus,
            elementalDefinition.ElectrocutionDamageTextStyle);

        if (!isElectrocution) return;

        AugmentCombatModifiers.NotifyElectrocution();
        AugmentCombatModifiers.MarkResidualCharge(source);

        int maximumTargets = parameters.CurrentChainLightningMaximumTargets +
            AugmentCombatModifiers.GetConductionTargetBonus() +
            AugmentCombatModifiers.GetOverconductionTargetBonus() +
            (alchemyBoosted ? 1 : 0) +
            (AugmentCombatModifiers.GetRuleInteger(
                RuleAugmentEffectKind.LightningStorm) > 0 &&
             AugmentCombatModifiers.GetElectrocutionCount() >= 5 ? 2 : 0) +
            (grade == BallStarGrade.ThreeStar ? 2 :
             grade == BallStarGrade.TwoStar ? 1 : 0);
        List<ElementChainLink> links = ElementGridResolver.FindConnectedWetChain(
            source, grid.ActiveBlocks, maximumTargets);

        for (int i = 0; i < links.Count; i++)
        {
            ElementChainLink link = links[i];
            int voltage = AugmentCombatModifiers.GetRuleInteger(
                RuleAugmentEffectKind.ConductionDamageRamp);
            int chainDamage = AugmentCombatModifiers.ApplyPercentage(
                parameters.CurrentChainLightningDamage + reactionBonus,
                i * voltage);
            ApplyIndirectDamage(link.Target,
                chainDamage,
                elementalDefinition.ElectrocutionDamageTextStyle);
            AugmentCombatModifiers.MarkResidualCharge(link.Target);
            ElementVisualEvents.RaiseTravel(
                ElementType.Electric, link.Source, link.Target);
        }

        if (links.Count > 0 &&
            (grade == BallStarGrade.TwoStar ||
             grade == BallStarGrade.ThreeStar))
            BallGradeVisualEvents.RaiseActivated(context.Ball);

        if (links.Count > 0 && grade == BallStarGrade.ThreeStar)
        {
            Block last = links[links.Count - 1].Target;
            ApplyIndirectDamage(last,
                Mathf.Max(1, context.DirectDamage / 2),
                elementalDefinition.ElectrocutionDamageTextStyle);
        }

        int closedCircuit = AugmentCombatModifiers.GetRuleInteger(
            RuleAugmentEffectKind.ClosedCircuitStrike);
        if (links.Count >= 3 && closedCircuit > 0)
            ApplyIndirectDamage(source,
                Mathf.Max(1, Mathf.RoundToInt(
                    parameters.CurrentChainLightningDamage * closedCircuit / 100f)),
                elementalDefinition.ElectrocutionDamageTextStyle);

        int chainStrike = AugmentCombatModifiers.GetRuleInteger(
            RuleAugmentEffectKind.ChainLightning);
        if (links.Count > 0 && chainStrike > 0)
        {
            Block last = links[links.Count - 1].Target;
            List<Block> neighbors = BlockNeighborhoodResolver.FindSurroundingBlocks(
                last, grid.ActiveBlocks, 1);
            int strikeDamage = Mathf.Max(1, Mathf.RoundToInt(
                parameters.CurrentChainLightningDamage * chainStrike / 100f));
            for (int i = 0; i < neighbors.Count; i++)
                ApplyIndirectDamage(neighbors[i], strikeDamage,
                    elementalDefinition.ElectrocutionDamageTextStyle);
        }

        ApplyReactionAugmentSideEffects(context, source);
    }

    private void SpreadFrostOnFreeze(Block source, BlockGridManager grid)
    {
        int amount = AugmentCombatModifiers.GetRuleInteger(
            RuleAugmentEffectKind.FrostSpreadOnShatter);
        bool absoluteZero = AugmentCombatModifiers.GetRuleInteger(
            RuleAugmentEffectKind.MassFreeze) > 0;
        if ((amount <= 0 && !absoluteZero) || source == null || grid == null) return;
        List<Block> neighbors = BlockNeighborhoodResolver.FindSurroundingBlocks(
            source, grid.ActiveBlocks, 1);
        for (int i = 0; i < neighbors.Count; i++)
        {
            BlockElementStatus status = GetOrAddElementStatus(neighbors[i]);
            if (status == null) continue;
            if (absoluteZero && !status.IsFrozen &&
                status.StoredFrostStack >=
                    status.GetMaximumStack(ElementType.Ice) - 1)
                status.AddFrost(1);
            else if (amount > 0)
                status.AddFrost(amount);
        }
    }

    private void ResolveFire(
        Block source, BlockElementStatus sourceStatus,
        BlockGridManager grid, BallHitContext context, int amount,
        ElementRuntimeParameters parameters, bool forceThermalShock,
        BallStarGrade grade)
    {
        bool hadBurn = sourceStatus.HasBurn;
        if (sourceStatus.IsFrozen || forceThermalShock)
        {
            bool alchemyBoosted =
                AugmentCombatModifiers.TryConsumeAlchemyChain(out int reactionBonus);
            sourceStatus.ConsumeFrozen();
            bool amplified = AugmentCombatModifiers.GetRuleInteger(
                RuleAugmentEffectKind.ThermalShockAmplify) > 0;
            int centerDamage = AugmentCombatModifiers.ApplyPercentage(
                parameters.CurrentThermalShockCenterDamage + reactionBonus,
                amplified ? 100 : 0);
            int neighborDamage = AugmentCombatModifiers.ApplyPercentage(
                parameters.CurrentThermalShockNeighborDamage + reactionBonus,
                amplified ? 50 : 0);
            ApplyIndirectDamage(source,
                centerDamage,
                elementalDefinition.ThermalShockDamageTextStyle);

            if (grid != null)
            {
                List<Block> neighbors = amplified || alchemyBoosted
                    ? BlockNeighborhoodResolver.FindSurroundingBlocks(
                        source, grid.ActiveBlocks, 2)
                    : ElementGridResolver.FindEightNeighbors(
                        source, grid.ActiveBlocks);
                for (int i = 0; i < neighbors.Count; i++)
                {
                    ApplyIndirectDamage(neighbors[i],
                        neighborDamage,
                        elementalDefinition.ThermalShockDamageTextStyle);
                }
            }

            ElementVisualEvents.RaiseThermalShock(source);
            ApplyReactionAugmentSideEffects(context, source);
        }

        int burnBefore = sourceStatus.BurnStack;
        if (source != null && source.IsAlive)
            sourceStatus.AddBurn(amount, context.DirectDamage);

        ResolveIntrinsicFireGradeEffect(
            source, sourceStatus, grid, context, grade, burnBefore);

        int fireSpread = AugmentCombatModifiers.GetRuleInteger(
            RuleAugmentEffectKind.FireSpread);
        if (hadBurn && fireSpread > 0 && grid != null)
        {
            List<Block> neighbors = BlockNeighborhoodResolver.FindSurroundingBlocks(
                source, grid.ActiveBlocks, 1);
            for (int i = 0; i < neighbors.Count; i++)
                GetOrAddElementStatus(neighbors[i])?.AddBurn(
                    fireSpread, context.DirectDamage);
        }
        ElementVisualEvents.RaiseImpact(ElementType.Fire, source);
    }

    private void ApplyReactionAugmentSideEffects(
        BallHitContext context, Block source)
    {
        int poison = AugmentCombatModifiers.GetRuleInteger(
            RuleAugmentEffectKind.PoisonReactionSpread);
        if (poison <= 0 || source == null) return;
        PoisonBlockStatus poisonStatus = source.GetComponent<PoisonBlockStatus>();
        if (poisonStatus == null)
            poisonStatus = source.gameObject.AddComponent<PoisonBlockStatus>();
        poisonStatus.AddStacks(poison,
            10 + AugmentCombatModifiers.GetPoisonMaximumStackBonus());
    }

    private void ResolveIntrinsicWaterGradeEffect(
        Block source, BlockElementStatus sourceStatus, BlockGridManager grid,
        Ball sourceBall, BallStarGrade grade, int beforeHit, int wetMaximum)
    {
        if ((grade != BallStarGrade.TwoStar &&
             grade != BallStarGrade.ThreeStar) ||
            source == null || grid == null)
            return;
        List<Block> neighbors = ElementGridResolver.FindEightNeighbors(
            source, grid.ActiveBlocks);
        int targetCount = grade == BallStarGrade.ThreeStar &&
            beforeHit < wetMaximum && sourceStatus.WetStack >= wetMaximum
                ? neighbors.Count : Mathf.Min(1, neighbors.Count);
        for (int i = 0; i < targetCount; i++)
        {
            GetOrAddElementStatus(neighbors[i])?.AddWet(1);
            ElementVisualEvents.RaiseTravel(ElementType.Water, source, neighbors[i]);
        }
        if (targetCount > 0)
            BallGradeVisualEvents.RaiseActivated(sourceBall);
    }

    private void ResolveIntrinsicIceGradeEffect(
        Block source, BlockGridManager grid, BallHitContext context,
        BallStarGrade grade)
    {
        if ((grade != BallStarGrade.TwoStar &&
             grade != BallStarGrade.ThreeStar) ||
            source == null || grid == null)
            return;
        List<Block> neighbors = ElementGridResolver.FindEightNeighbors(
            source, grid.ActiveBlocks);
        int count = grade == BallStarGrade.ThreeStar
            ? neighbors.Count : Mathf.Min(1, neighbors.Count);
        for (int i = 0; i < count; i++)
        {
            BlockElementStatus neighborStatus =
                GetOrAddElementStatus(neighbors[i]);
            neighborStatus?.AddFrost(1);
            if (grade == BallStarGrade.ThreeStar)
                ApplyIndirectDamage(neighbors[i],
                    Mathf.Max(1, context.DirectDamage / 2), null);
        }
        if (count > 0)
            BallGradeVisualEvents.RaiseActivated(context.Ball);
    }

    private void ResolveIntrinsicFireGradeEffect(
        Block source, BlockElementStatus sourceStatus, BlockGridManager grid,
        BallHitContext context, BallStarGrade grade, int beforeHit)
    {
        if ((grade != BallStarGrade.TwoStar &&
             grade != BallStarGrade.ThreeStar) ||
            source == null || grid == null)
            return;
        List<Block> neighbors = ElementGridResolver.FindEightNeighbors(
            source, grid.ActiveBlocks);
        if (neighbors.Count <= 0)
            return;

        bool reachedMaximum = beforeHit <
            sourceStatus.GetMaximumStack(ElementType.Fire) &&
            sourceStatus.BurnStack >=
            sourceStatus.GetMaximumStack(ElementType.Fire);
        int count = grade == BallStarGrade.ThreeStar && reachedMaximum
            ? neighbors.Count : 1;
        count = Mathf.Min(count, neighbors.Count);
        for (int i = 0; i < count; i++)
        {
            GetOrAddElementStatus(neighbors[i])?.AddBurn(
                1, context.DirectDamage);
            if (grade == BallStarGrade.ThreeStar && reachedMaximum)
                ApplyIndirectDamage(neighbors[i],
                    Mathf.Max(1, context.DirectDamage / 2), null);
        }
        BallGradeVisualEvents.RaiseActivated(context.Ball);
    }

    private void ApplyIndirectDamage(
        Block target, int damage, BallDamageTextStyleDefinition style)
    {
        if (target == null || !target.IsAlive || damage <= 0) return;
        CombatController.ApplyDamage(
            target, damage, target.transform.position, style);
    }

    private static ElementRuntimeParameters FindParameters()
    {
        if (cachedParameters == null)
            cachedParameters = ElementRuntimeParameters.Current;
        return cachedParameters;
    }

    private static BlockGridManager FindGrid()
    {
        if (cachedGrid == null)
            cachedGrid = FindFirstObjectByType<BlockGridManager>();
        return cachedGrid;
    }

    private static BlockElementStatus GetOrAddElementStatus(Block block)
    {
        if (block == null) return null;
        BlockElementStatus status = block.GetComponent<BlockElementStatus>();
        if (status == null)
            status = block.gameObject.AddComponent<BlockElementStatus>();
        if (block.GetComponent<BlockWetChargeStatusView>() == null)
            block.gameObject.AddComponent<BlockWetChargeStatusView>();
        if (block.GetComponent<BlockBurnFrostStatusView>() == null)
            block.gameObject.AddComponent<BlockBurnFrostStatusView>();
        return status;
    }
}
