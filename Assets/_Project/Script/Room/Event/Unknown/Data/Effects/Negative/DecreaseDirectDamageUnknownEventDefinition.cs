using UnityEngine;

[CreateAssetMenu(
    fileName = "UnknownEvent_DecreaseDirectDamage",
    menuName =
        "Ballchemy/Events/Unknown/Negative/Decrease Direct Damage"
)]
public sealed class
    DecreaseDirectDamageUnknownEventDefinition :
        UnknownEventDefinition
{
    [Header("Decrease Direct Damage")]

    [Tooltip(
        "모든 공의 현재 런 고정 직접 피해 보너스에서 " +
        "감소시킬 수치입니다."
    )]
    [SerializeField, Min(1)]
    private int damagePenalty = 1;

    [Tooltip(
        "런 고정 직접 피해 보너스가 " +
        "이 값보다 낮아지지 않도록 제한합니다."
    )]
    [SerializeField, Min(0)]
    private int minimumRunDamageBonus = 0;

    public int DamagePenalty =>
        damagePenalty;

    public int MinimumRunDamageBonus =>
        minimumRunDamageBonus;

    public override bool CanApply(
        UnknownEventApplyContext context)
    {
        if (context == null ||
            !context.HasBallRuntimeStats)
        {
            return false;
        }

        if (damagePenalty <= 0)
        {
            return false;
        }

        BallRuntimeStats runtimeStats =
            context.BallRuntimeStats;

        int currentBonus =
            runtimeStats.RunDirectDamageBonus;

        return currentBonus >
               minimumRunDamageBonus;
    }

    public override UnknownEventResult Apply(
        UnknownEventApplyContext context)
    {
        if (!CanApply(
                context
            ))
        {
            return new UnknownEventResult(
                this,
                false,
                "감소시킬 런 피해 보너스가 없습니다."
            );
        }

        BallRuntimeStats runtimeStats =
            context.BallRuntimeStats;

        int previousBonus =
            runtimeStats.RunDirectDamageBonus;

        int targetBonus =
            Mathf.Max(
                previousBonus -
                damagePenalty,
                minimumRunDamageBonus
            );

        runtimeStats.SetRunDirectDamageBonus(
            targetBonus
        );

        int actualDecrease =
            previousBonus -
            runtimeStats.RunDirectDamageBonus;

        if (actualDecrease <= 0)
        {
            return new UnknownEventResult(
                this,
                false,
                "모든 공의 피해가 감소하지 않았습니다."
            );
        }

        string resultText =
            $"모든 공의 직접 피해가 " +
            $"{actualDecrease} 감소했습니다.";

        Debug.Log(
            "DecreaseDirectDamageUnknownEventDefinition: " +
            resultText +
            $" 현재 런 피해 보너스=" +
            $"{runtimeStats.RunDirectDamageBonus}",
            this
        );

        return new UnknownEventResult(
            this,
            true,
            resultText
        );
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        damagePenalty =
            Mathf.Max(
                damagePenalty,
                1
            );

        minimumRunDamageBonus =
            Mathf.Max(
                minimumRunDamageBonus,
                0
            );
    }
}