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

        return BallHitResult
            .HandledWithBounce();
    }
}