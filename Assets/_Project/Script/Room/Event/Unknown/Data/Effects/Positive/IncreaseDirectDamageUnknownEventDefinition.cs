using UnityEngine;

[CreateAssetMenu(
    fileName = "UnknownEvent_IncreaseDirectDamage",
    menuName =
        "Ballchemy/Events/Unknown/Positive/Increase Direct Damage"
)]
public sealed class
    IncreaseDirectDamageUnknownEventDefinition :
        UnknownEventDefinition
{
    [Header("Increase Direct Damage")]

    [Tooltip(
        "모든 공의 런 고정 직접 피해에 " +
        "추가할 수치입니다."
    )]
    [SerializeField, Min(1)]
    private int damageBonus = 1;

    public int DamageBonus =>
        damageBonus;

    public override bool CanApply(
        UnknownEventApplyContext context)
    {
        if (context == null ||
            !context.HasBallRuntimeStats)
        {
            return false;
        }

        return damageBonus > 0;
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
                "모든 공의 피해를 증가시킬 수 없습니다."
            );
        }

        BallRuntimeStats runtimeStats =
            context.BallRuntimeStats;

        int previousBonus =
            runtimeStats.RunDirectDamageBonus;

        runtimeStats.AddRunDirectDamageBonus(
            damageBonus
        );

        int actualIncrease =
            runtimeStats.RunDirectDamageBonus -
            previousBonus;

        if (actualIncrease <= 0)
        {
            return new UnknownEventResult(
                this,
                false,
                "모든 공의 피해가 증가하지 않았습니다."
            );
        }

        string resultText =
            $"모든 공의 직접 피해가 " +
            $"{actualIncrease} 증가했습니다.";

        Debug.Log(
            "IncreaseDirectDamageUnknownEventDefinition: " +
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

        damageBonus =
            Mathf.Max(
                damageBonus,
                1
            );
    }
}