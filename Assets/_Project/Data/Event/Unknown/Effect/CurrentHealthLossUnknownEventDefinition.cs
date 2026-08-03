using UnityEngine;

[CreateAssetMenu(
    fileName = "UnknownEvent_CurrentHealthLoss",
    menuName =
        "Ballchemy/Events/Unknown/Negative/Current Health Loss"
)]
public sealed class
    CurrentHealthLossUnknownEventDefinition :
        UnknownEventDefinition
{
    [Header("Current Health Loss")]

    [Tooltip(
        "현재 체력을 기준으로 잃는 비율입니다. " +
        "0.2는 현재 체력의 20%입니다."
    )]
    [SerializeField, Range(0.01f, 1f)]
    private float currentHealthLossRatio = 0.2f;

    [Tooltip(
        "이 결과로 플레이어가 사망하지 않도록 " +
        "반드시 남겨둘 최소 체력입니다."
    )]
    [SerializeField, Min(1)]
    private int minimumRemainingHealth = 1;

    public float CurrentHealthLossRatio =>
        currentHealthLossRatio;

    public int MinimumRemainingHealth =>
        minimumRemainingHealth;

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

        if (playerHealth.IsDead)
        {
            return false;
        }

        return playerHealth.CurrentHealth >
               minimumRemainingHealth;
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
                "현재 체력을 감소시킬 수 없습니다."
            );
        }

        PlayerHealth playerHealth =
            context.PlayerHealth;

        int previousHealth =
            playerHealth.CurrentHealth;

        int calculatedDamage =
            Mathf.CeilToInt(
                previousHealth *
                currentHealthLossRatio
            );

        int maximumAllowedDamage =
            Mathf.Max(
                previousHealth -
                minimumRemainingHealth,
                0
            );

        int appliedDamage =
            Mathf.Clamp(
                calculatedDamage,
                1,
                maximumAllowedDamage
            );

        if (appliedDamage <= 0)
        {
            return new UnknownEventResult(
                this,
                false,
                "남은 체력이 너무 낮아 피해를 적용하지 않았습니다."
            );
        }

        playerHealth.TakeDamage(
            appliedDamage
        );

        int actualDamage =
            previousHealth -
            playerHealth.CurrentHealth;

        if (actualDamage <= 0)
        {
            return new UnknownEventResult(
                this,
                false,
                "현재 체력이 감소하지 않았습니다."
            );
        }

        string resultText =
            $"현재 체력을 {actualDamage} 잃었습니다. " +
            $"남은 체력은 " +
            $"{playerHealth.CurrentHealth}/" +
            $"{playerHealth.MaxHealth}입니다.";

        Debug.Log(
            "CurrentHealthLossUnknownEventDefinition: " +
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

        currentHealthLossRatio =
            Mathf.Clamp(
                currentHealthLossRatio,
                0.01f,
                1f
            );

        minimumRemainingHealth =
            Mathf.Max(
                minimumRemainingHealth,
                1
            );
    }
}