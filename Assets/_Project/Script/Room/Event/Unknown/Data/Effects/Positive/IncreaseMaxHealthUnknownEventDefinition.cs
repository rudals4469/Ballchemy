using UnityEngine;

[CreateAssetMenu(
    fileName = "UnknownEvent_IncreaseMaxHealth",
    menuName =
        "Ballchemy/Events/Unknown/Positive/Increase Max Health"
)]
public sealed class
    IncreaseMaxHealthUnknownEventDefinition :
        UnknownEventDefinition
{
    [Header("Increase Max Health")]

    [Tooltip(
        "증가시킬 최대 체력입니다. " +
        "현재 체력은 증가하지 않습니다."
    )]
    [SerializeField, Min(1)]
    private int amount = 5;

    public int Amount =>
        amount;

    public override bool CanApply(
        UnknownEventApplyContext context)
    {
        if (context == null ||
            !context.HasPlayerHealth)
        {
            return false;
        }

        if (amount <= 0)
        {
            return false;
        }

        return !context
            .PlayerHealth
            .IsDead;
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
                "최대 체력을 증가시킬 수 없습니다."
            );
        }

        PlayerHealth playerHealth =
            context.PlayerHealth;

        int previousMaxHealth =
            playerHealth.MaxHealth;

        bool applied =
            playerHealth.TryIncreaseMaxHealth(
                amount
            );

        if (!applied)
        {
            return new UnknownEventResult(
                this,
                false,
                "최대 체력이 증가하지 않았습니다."
            );
        }

        int actualIncrease =
            playerHealth.MaxHealth -
            previousMaxHealth;

        string resultText =
            $"최대 체력이 {actualIncrease} 증가했습니다. " +
            $"현재 체력은 유지됩니다.";

        Debug.Log(
            "IncreaseMaxHealthUnknownEventDefinition: " +
            resultText +
            $" 현재 체력=" +
            $"{playerHealth.CurrentHealth}/" +
            $"{playerHealth.MaxHealth}",
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

        amount =
            Mathf.Max(
                amount,
                1
            );
    }
}