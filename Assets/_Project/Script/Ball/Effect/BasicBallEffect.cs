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
            !context.Block.IsAlive)
        {
            return BallHitResult.NotHandled();
        }

        context.Block.TakeDamage(
            context.DirectDamage
        );

        return BallHitResult
            .HandledWithBounce();
    }
}