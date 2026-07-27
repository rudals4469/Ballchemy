using UnityEngine;

[DisallowMultipleComponent]
public sealed class PiercingBallEffect :
    BallTraitEffect
{
    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog;

    private PiercingBallSensor
        piercingSensor;

    public override BallTraitType TraitType =>
        BallTraitType.Piercing;

    protected override void OnInitialized()
    {
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
         * 무적 블록은 피해가 들어가지 않고
         * 기존 공처럼 반사한다.
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

        /*
         * 일반·특수·쉴드·TriggerOnly 블록은
         * 피해 처리를 시도한 뒤 관통한다.
         *
         * 쉴드로 실제 체력 피해가 0이어도
         * 관통 여부에는 영향을 주지 않는다.
         */
        CombatController.ApplyDamage(
            targetBlock,
            context.DirectDamage,
            context.HitPoint
        );

        if (showDebugLog)
        {
            Debug.Log(
                "PiercingBallEffect: " +
                $"{targetBlock.name}에 타격 처리 후 관통",
                this
            );
        }

        return BallHitResult
            .HandledWithoutBounce();
    }
}