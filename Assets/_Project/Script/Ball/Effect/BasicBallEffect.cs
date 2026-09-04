using UnityEngine;

[DisallowMultipleComponent]
public sealed class BasicBallEffect :
    BallTraitEffect
{
    public override BallTraitType TraitType =>
        BallTraitType.Basic;

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

        CombatController.ApplyDamage(
            context.Block,
            context.DirectDamage,
            context.HitPoint
        );

        if (!context.Block.IsAlive &&
            (context.Ball.StarGrade == BallStarGrade.TwoStar ||
             context.Ball.StarGrade == BallStarGrade.ThreeStar))
        {
            BlockGridManager grid = FindFirstObjectByType<BlockGridManager>();
            if (grid != null)
            {
                System.Collections.Generic.List<Block> neighbors =
                    BlockNeighborhoodResolver.FindSurroundingBlocks(
                        context.Block, grid.ActiveBlocks, 1);
                int count = context.Ball.StarGrade == BallStarGrade.ThreeStar
                    ? neighbors.Count : Mathf.Min(1, neighbors.Count);
                int splashDamage = context.Ball.StarGrade == BallStarGrade.ThreeStar
                    ? Mathf.Max(1, context.DirectDamage / 2) : 1;
                for (int i = 0; i < count; i++)
                    CombatController.ApplyDamage(
                        neighbors[i], splashDamage,
                        neighbors[i].transform.position, null, false);
                if (count > 0)
                    BallGradeVisualEvents.RaiseActivated(context.Ball);
            }
        }

        return BallHitResult
            .HandledWithBounce();
    }
}
