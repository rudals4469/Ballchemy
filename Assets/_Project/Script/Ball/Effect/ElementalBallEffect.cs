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
        int appliedDirectDamage = CombatController.ApplyDamage(
            target, context.DirectDamage, context.HitPoint);

        if (appliedDirectDamage <= 0 || target == null || !target.IsAlive ||
            elementalDefinition == null)
            return BallHitResult.HandledWithBounce();

        BlockElementStatus status = GetOrAddElementStatus(target);
        ElementRuntimeParameters parameters = FindParameters();
        BlockGridManager grid = FindGrid();
        if (status == null || parameters == null)
            return BallHitResult.HandledWithBounce();

        BallStarGrade grade = context.Definition != null
            ? context.Definition.StarGrade
            : BallStarGrade.OneStar;
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
                ResolveWater(target, status, grid, stackAmount, parameters);
                break;
            case ElementType.Electric:
                ResolveLightning(target, status, grid, context, parameters);
                break;
            case ElementType.Ice:
                status.AddFrost(stackAmount);
                ElementVisualEvents.RaiseImpact(ElementType.Ice, target);
                break;
            case ElementType.Fire:
                ResolveFire(target, status, grid, context, stackAmount, parameters);
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
        BlockGridManager grid, int amount,
        ElementRuntimeParameters parameters)
    {
        int beforeHit = sourceStatus.WetStack;
        sourceStatus.AddWet(amount);
        ElementVisualEvents.RaiseImpact(ElementType.Water, source);

        if (beforeHit < parameters.CurrentWetMaxStack || grid == null)
            return;

        int spreadAmount = amount + Mathf.Max(0,
            AugmentCombatModifiers.GetRuleInteger(RuleAugmentEffectKind.WetSpread));
        List<Block> targets = ElementGridResolver.SpreadWetThroughMaximumBlocks(
            source, grid.ActiveBlocks, spreadAmount,
            parameters.CurrentWetMaxStack);

        for (int i = 0; i < targets.Count; i++)
        {
            BlockElementStatus targetStatus = GetOrAddElementStatus(targets[i]);
            if (targetStatus == null) continue;
            targetStatus.AddWet(spreadAmount);
            ElementVisualEvents.RaiseTravel(ElementType.Water, source, targets[i]);
        }
    }

    private void ResolveLightning(
        Block source, BlockElementStatus sourceStatus,
        BlockGridManager grid, BallHitContext context,
        ElementRuntimeParameters parameters)
    {
        ElementVisualEvents.RaiseImpact(ElementType.Electric, source);
        ApplyIndirectDamage(source,
            parameters.CurrentLightningAdditionalDamage,
            elementalDefinition.ElectrocutionDamageTextStyle);

        if (!sourceStatus.HasWet || grid == null) return;

        int maximumTargets = parameters.CurrentChainLightningMaximumTargets +
            AugmentCombatModifiers.GetConductionTargetBonus() +
            AugmentCombatModifiers.GetOverconductionTargetBonus();
        List<ElementChainLink> links = ElementGridResolver.FindConnectedWetChain(
            source, grid.ActiveBlocks, maximumTargets);

        for (int i = 0; i < links.Count; i++)
        {
            ElementChainLink link = links[i];
            ApplyIndirectDamage(link.Target,
                parameters.CurrentChainLightningDamage,
                elementalDefinition.ElectrocutionDamageTextStyle);
            ElementVisualEvents.RaiseTravel(
                ElementType.Electric, link.Source, link.Target);
        }

        ApplyReactionAugmentSideEffects(context, source);
    }

    private void ResolveFire(
        Block source, BlockElementStatus sourceStatus,
        BlockGridManager grid, BallHitContext context, int amount,
        ElementRuntimeParameters parameters)
    {
        if (sourceStatus.IsFrozen)
        {
            sourceStatus.ConsumeFrozen();
            ApplyIndirectDamage(source,
                parameters.CurrentThermalShockCenterDamage,
                elementalDefinition.ThermalShockDamageTextStyle);

            if (grid != null)
            {
                List<Block> neighbors = ElementGridResolver.FindEightNeighbors(
                    source, grid.ActiveBlocks);
                for (int i = 0; i < neighbors.Count; i++)
                {
                    ApplyIndirectDamage(neighbors[i],
                        parameters.CurrentThermalShockNeighborDamage,
                        elementalDefinition.ThermalShockDamageTextStyle);
                    ElementVisualEvents.RaiseImpact(ElementType.Fire, neighbors[i]);
                }
            }

            ElementVisualEvents.RaiseThermalShock(source);
            ApplyReactionAugmentSideEffects(context, source);
        }

        if (source != null && source.IsAlive)
            sourceStatus.AddBurn(amount, context.DirectDamage);
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
