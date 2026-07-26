using UnityEngine;

[DisallowMultipleComponent]
public sealed class CriticalBallEffect :
    BallTraitEffect
{
    private CriticalBallTraitDefinition
        criticalDefinition;

    public override BallTraitType TraitType =>
        BallTraitType.Critical;

    protected override void OnInitialized()
    {
        criticalDefinition =
            TraitDefinition as
                CriticalBallTraitDefinition;

        if (criticalDefinition != null)
        {
            return;
        }

        Debug.LogWarning(
            "CriticalBallEffect: " +
            "CriticalBallTraitDefinition이 " +
            "연결되지 않았습니다. " +
            "직접 피해만 적용됩니다.",
            this
        );
    }

    public override BallHitResult ResolveHit(
        BallHitContext context)
    {
        if (context == null ||
            context.Block == null ||
            !context.Block.IsAlive)
        {
            return BallHitResult.NotHandled();
        }

        BallStarGrade starGrade =
            context.Definition != null
                ? context.Definition.StarGrade
                : BallStarGrade.None;

        float gradeMultiplier =
            criticalDefinition != null
                ? criticalDefinition
                    .GetMultiplier(
                        starGrade
                    )
                : 1f;

        float finalCriticalMultiplier =
            Mathf.Max(
                gradeMultiplier +
                context
                    .CriticalDamageMultiplierBonus,
                1f
            );

        int criticalDamage =
            Mathf.Max(
                Mathf.RoundToInt(
                    context.DirectDamage *
                    finalCriticalMultiplier
                ),
                1
            );

        context.Block.TakeDamage(
            criticalDamage
        );

        return BallHitResult
            .HandledWithBounce();
    }
}