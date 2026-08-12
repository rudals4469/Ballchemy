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

        status.AddStacks(
            poisonDefinition.GetStackAmount(starGrade),
            poisonDefinition.MaximumStacks);

        return BallHitResult.HandledWithBounce();
    }
}
