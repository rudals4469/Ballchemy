using UnityEngine;

[CreateAssetMenu(
    fileName = "UnknownEvent_UpgradeNextCombatReward",
    menuName =
        "Ballchemy/Events/Unknown/Positive/Upgrade Next Combat Reward"
)]
public sealed class
    UpgradeNextCombatRewardUnknownEventDefinition :
        UnknownEventDefinition
{
    [Header("Upgrade Next Combat Reward")]

    [Tooltip(
        "다음 전투방 보상에 추가할 보상 등급 단계입니다.\n" +
        "1이면 Tier1은 Tier2로, " +
        "Tier2는 Tier3로 증가합니다."
    )]
    [SerializeField, Min(1)]
    private int tierIncrease = 1;

    public int TierIncrease =>
        tierIncrease;

    public override bool CanApply(
        UnknownEventApplyContext context)
    {
        if (context == null ||
            !context.HasRunRewardState)
        {
            return false;
        }

        return tierIncrease > 0;
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
                "다음 전투방 보상 등급 증가를 예약할 수 없습니다."
            );
        }

        RunRewardState runRewardState =
            context.RunRewardState;

        int previousIncrease =
            runRewardState
                .PendingRewardTierIncrease;

        bool applied =
            runRewardState
                .AddPendingRewardTierIncrease(
                    tierIncrease
                );

        if (!applied)
        {
            return new UnknownEventResult(
                this,
                false,
                "다음 전투방 보상 등급 증가 예약에 실패했습니다."
            );
        }

        int actualIncrease =
            runRewardState
                .PendingRewardTierIncrease -
            previousIncrease;

        if (actualIncrease <= 0)
        {
            return new UnknownEventResult(
                this,
                false,
                "보상 등급 증가 예약 수치가 변경되지 않았습니다."
            );
        }

        string resultText =
            actualIncrease == 1
                ? "다음 전투방의 보상이 1단계 증가합니다."
                : $"다음 전투방의 보상이 " +
                  $"{actualIncrease}단계 증가합니다.";

        Debug.Log(
            "UpgradeNextCombatRewardUnknownEventDefinition: " +
            resultText +
            $" 현재 예약 단계=" +
            $"{runRewardState.PendingRewardTierIncrease}",
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

        tierIncrease =
            Mathf.Max(
                tierIncrease,
                1
            );
    }
}