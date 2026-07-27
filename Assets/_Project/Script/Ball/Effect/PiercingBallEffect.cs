using UnityEngine;

[DisallowMultipleComponent]
public sealed class PiercingBallEffect :
    BallTraitEffect
{
    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog;

    private PiercingBallTraitDefinition
        piercingDefinition;

    private PiercingBallSensor
        piercingSensor;

    public override BallTraitType TraitType =>
        BallTraitType.Piercing;

    protected override void OnInitialized()
    {
        piercingDefinition =
            TraitDefinition as
                PiercingBallTraitDefinition;

        if (piercingDefinition == null)
        {
            Debug.LogWarning(
                "PiercingBallEffect: " +
                "PiercingBallTraitDefinition이 " +
                "연결되지 않았습니다. " +
                "기본 직접 피해로 처리합니다.",
                this
            );
        }

        EnsurePiercingSensor();

        piercingSensor?.SetPiercingEnabled(
            true
        );
    }

    private void OnDisable()
    {
        if (piercingSensor == null)
        {
            return;
        }

        piercingSensor.SetPiercingEnabled(
            false
        );
    }

    private void OnDestroy()
    {
        if (piercingSensor == null)
        {
            return;
        }

        piercingSensor.SetPiercingEnabled(
            false
        );
    }

    private void EnsurePiercingSensor()
    {
        if (Ball == null)
        {
            return;
        }

        piercingSensor =
            Ball.GetComponent<
                PiercingBallSensor
            >();

        if (piercingSensor == null)
        {
            piercingSensor =
                Ball.gameObject
                    .AddComponent<
                        PiercingBallSensor
                    >();
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

        /*
         * 무적 블록은 관통하지 않고
         * 기존 공과 동일하게 반사한다.
         */
        if (targetBlock.IsIndestructible)
        {
            CombatController.ApplyDamage(
                targetBlock,
                context.DirectDamage,
                context.HitPoint
            );

            if (showDebugLog)
            {
                Debug.Log(
                    "PiercingBallEffect: " +
                    $"{targetBlock.name}은 무적 블록이므로 " +
                    "관통하지 않고 반사합니다.",
                    this
                );
            }

            return BallHitResult
                .HandledWithBounce();
        }

        int resolvedDamage =
            piercingDefinition != null
                ? piercingDefinition
                    .CalculateDamage(
                        context.DirectDamage
                    )
                : Mathf.Max(
                    context.DirectDamage,
                    1
                );

        /*
         * 센서의 최초 타격과 추가 타격 모두
         * 이 메서드를 통과한다.
         */
        CombatController.ApplyDamage(
            targetBlock,
            resolvedDamage,
            context.HitPoint
        );

        if (showDebugLog)
        {
            Debug.Log(
                "PiercingBallEffect: " +
                $"{targetBlock.name} 관통 타격, " +
                $"피해={resolvedDamage}",
                this
            );
        }

        return BallHitResult
            .HandledWithoutBounce();
    }
}