using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PoisonBallEffect : BallTraitEffect
{
    private PoisonBallTraitDefinition poisonDefinition;

    public override BallTraitType TraitType =>
        BallTraitType.Poison;

    protected override void OnInitialized()
    {
        poisonDefinition =
            TraitDefinition as PoisonBallTraitDefinition;
    }

    public override BallHitResult ResolveHit(BallHitContext context)
    {
        if (context == null ||
            context.Block == null ||
            !context.Block.IsAlive ||
            poisonDefinition == null)
        {
            return BallHitResult.NotHandled();
        }

        PoisonBlockStatus status =
            context.Block.GetComponent<PoisonBlockStatus>();

        if (status == null)
        {
            status = context.Block.gameObject
                .AddComponent<PoisonBlockStatus>();
        }

        BallStarGrade starGrade =
            context.Definition != null
                ? context.Definition.StarGrade
                : BallStarGrade.OneStar;
        starGrade = AugmentCombatModifiers.GetEffectiveStarGrade(context.Ball);

        int maximumStacks = poisonDefinition.MaximumStacks +
            AugmentCombatModifiers.GetPoisonMaximumStackBonus();
        bool wasAtMaximum = status.StackCount >= maximumStacks;

        int appliedDamage = CombatController.ApplyDamage(
            context.Block,
            context.DirectDamage,
            context.HitPoint);

        if (appliedDamage <= 0 || context.Block == null)
            return BallHitResult.HandledWithBounce();

        if (wasAtMaximum)
        {
            ResolvePoisonCollapse(context, status, maximumStacks);
            return BallHitResult.HandledWithBounce();
        }

        status.AddStacks(
            poisonDefinition.GetStackAmount(starGrade) +
            AugmentCombatModifiers.GetPoisonAppliedStackBonus(context.Ball),
            maximumStacks);

        return BallHitResult.HandledWithBounce();
    }

    private void SpreadPoison(
        Block sourceBlock, int maximumStacks, int spreadStacks, int maximumTargets)
    {
        if (spreadStacks <= 0 || maximumTargets <= 0) return;

        BlockGridManager grid = FindFirstObjectByType<BlockGridManager>();
        if (grid == null) return;

        List<Block> targets = BlockNeighborhoodResolver.FindSurroundingBlocks(
            sourceBlock, grid.ActiveBlocks, 1);
        int applied = 0;
        for (int i = 0; i < targets.Count && applied < maximumTargets; i++)
        {
            Block target = targets[i];
            if (target == null || !target.IsAlive || !target.IsBreakable) continue;
            PoisonBlockStatus targetStatus = target.GetComponent<PoisonBlockStatus>();
            if (targetStatus == null) targetStatus = target.gameObject.AddComponent<PoisonBlockStatus>();
            targetStatus.AddStacks(spreadStacks, maximumStacks);
            applied++;
        }
    }

    private void ResolvePoisonCollapse(
        BallHitContext context, PoisonBlockStatus status, int maximumStacks)
    {
        int consumedStacks = status.StackCount;
        status.ClearStacks();

        BlockGridManager grid = FindFirstObjectByType<BlockGridManager>();
        int plagueDamage = AugmentCombatModifiers.GetRuleInteger(
            RuleAugmentEffectKind.PlagueCollapse);
        int radius = plagueDamage > 0 ? 2 : 1;
        int damage = plagueDamage > 0 ? plagueDamage :
            consumedStacks * 5 + AugmentCombatModifiers.GetPoisonCollapseBonus();
        List<Block> targets = grid != null
            ? BlockNeighborhoodResolver.FindSurroundingBlocks(
                context.Block, grid.ActiveBlocks, radius)
            : new List<Block>();

        for (int i = 0; i < targets.Count; i++)
        {
            Block target = targets[i];
            if (target != null && target.IsAlive && target.IsBreakable)
            {
                CombatController.ApplyDamage(
                    target, damage, target.transform.position, null, false);
                if (plagueDamage > 0)
                {
                    PoisonBlockStatus targetStatus =
                        target.GetComponent<PoisonBlockStatus>();
                    if (targetStatus == null)
                        targetStatus = target.gameObject.AddComponent<PoisonBlockStatus>();
                    targetStatus.AddStacks(1, maximumStacks);
                }
            }
        }

        int contagionStacks = AugmentCombatModifiers.GetRuleInteger(
            RuleAugmentEffectKind.PoisonReactionSpread);
        SpreadPoison(context.Block, maximumStacks,
            contagionStacks, contagionStacks * 2);
        AugmentCombatModifiers.NotifyPoisonCollapse();
    }
}
