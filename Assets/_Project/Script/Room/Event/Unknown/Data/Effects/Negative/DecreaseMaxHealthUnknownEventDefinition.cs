using UnityEngine;

[CreateAssetMenu(
    fileName = "UnknownEvent_DecreaseMaxHealth",
    menuName =
        "Ballchemy/Events/Unknown/Negative/Decrease Max Health"
)]
public sealed class
    DecreaseMaxHealthUnknownEventDefinition :
        UnknownEventDefinition
{
    [Header("Decrease Max Health")]

    [Tooltip(
        "감소시킬 최대 체력입니다."
    )]
    [SerializeField, Min(1)]
    private int amount = 5;

    [Tooltip(
        "감소 후에도 보장할 최소 최대 체력입니다."
    )]
    [SerializeField, Min(1)]
    private int minimumMaxHealth = 1;

    public int Amount =>
        amount;

    public int MinimumMaxHealth =>
        minimumMaxHealth;

    public override bool CanApply(
        UnknownEventApplyContext context)
    {
        if (context == null ||
            !context.HasPlayerHealth)
        {
            return false;
        }

        PlayerHealth playerHealth =
            context.PlayerHealth;

        if (playerHealth.IsDead ||
            amount <= 0)
        {
            return false;
        }

        return playerHealth.MaxHealth >
               minimumMaxHealth;
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
                "최대 체력을 감소시킬 수 없습니다."
            );
        }

        PlayerHealth playerHealth =
            context.PlayerHealth;

        int previousMaxHealth =
            playerHealth.MaxHealth;

        bool applied =
            playerHealth.TryDecreaseMaxHealth(
                amount,
                minimumMaxHealth
            );

        if (!applied)
        {
            return new UnknownEventResult(
                this,
                false,
                "최대 체력이 감소하지 않았습니다."
            );
        }

        int actualDecrease =
            previousMaxHealth -
            playerHealth.MaxHealth;

        string resultText =
            $"최대 체력이 {actualDecrease} 감소했습니다. " +
            $"현재 체력은 " +
            $"{playerHealth.CurrentHealth}/" +
            $"{playerHealth.MaxHealth}입니다.";

        Debug.Log(
            "DecreaseMaxHealthUnknownEventDefinition: " +
            resultText,
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

        minimumMaxHealth =
            Mathf.Max(
                minimumMaxHealth,
                1
            );
    }
}