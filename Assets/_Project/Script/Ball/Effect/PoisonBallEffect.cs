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

        int maximumStacks = poisonDefinition.MaximumStacks +
            AugmentCombatModifiers.GetPoisonMaximumStackBonus();
        bool wasAtMaximum = status.StackCount >= maximumStacks;

        if (wasAtMaximum)
        {
            SpreadPoison(context.Block, maximumStacks);

            if (TryCollapsePoison(context, status))
                return BallHitResult.HandledWithBounce();
        }

        status.AddStacks(
            poisonDefinition.GetStackAmount(starGrade) +
            AugmentCombatModifiers.GetPoisonAppliedStackBonus(context.Ball),
            maximumStacks);

        return BallHitResult.HandledWithBounce();
    }

    private void SpreadPoison(Block sourceBlock, int maximumStacks)
    {
        if (!AugmentCombatModifiers.TryGetPoisonContagion(
                out PoisonContagionAugmentDefinition definition,
                out int level)) return;

        BlockGridManager grid = FindFirstObjectByType<BlockGridManager>();
        if (grid == null) return;

        List<Block> targets = BlockNeighborhoodResolver.FindSurroundingBlocks(
            sourceBlock, grid.ActiveBlocks, 1);
        int applied = 0;
        for (int i = 0; i < targets.Count && applied < definition.GetMaximumTargets(level); i++)
        {
            Block target = targets[i];
            if (target == null || !target.IsAlive || !target.IsBreakable) continue;
            PoisonBlockStatus targetStatus = target.GetComponent<PoisonBlockStatus>();
            if (targetStatus == null) targetStatus = target.gameObject.AddComponent<PoisonBlockStatus>();
            targetStatus.AddStacks(definition.GetSpreadStacks(level), maximumStacks);
            applied++;
        }
    }

    private bool TryCollapsePoison(BallHitContext context, PoisonBlockStatus status)
    {
        if (!AugmentCombatModifiers.TryGetPoisonCollapse(
                out PoisonCollapseAugmentDefinition definition,
                out int level)) return false;

        int damage = status.StackCount * definition.GetDamageMultiplier(level);
        status.ClearStacks();

        BlockGridManager grid = FindFirstObjectByType<BlockGridManager>();
        List<Block> targets = grid != null
            ? BlockNeighborhoodResolver.FindSurroundingBlocks(context.Block, grid.ActiveBlocks, definition.ExplosionRadius)
            : new List<Block>();
        targets.Insert(0, context.Block);

        for (int i = 0; i < targets.Count; i++)
        {
            Block target = targets[i];
            if (target != null && target.IsAlive && target.IsBreakable)
                CombatController.ApplyDamage(target, damage, target.transform.position);
        }

        return true;
    }
}
