using UnityEngine;

[CreateAssetMenu(
    fileName = "Reward_Ball",
    menuName =
        "Ballchemy/Rewards/Ball Reward"
)]
public sealed class BallRewardDefinition :
    RewardDefinition
{
    [Header("Ball Reward")]

    [Tooltip(
        "이 보상을 선택했을 때 추가할 공입니다."
    )]
    [SerializeField]
    private BallDefinition ballDefinition;

    [Tooltip(
        "이 보상을 선택했을 때 추가할 공의 수입니다."
    )]
    [SerializeField, Min(1)]
    private int amount = 1;

    public override RewardType RewardType =>
        RewardType.Ball;

    public BallDefinition BallDefinition =>
        ballDefinition;

    public int Amount =>
        amount;

    public bool IsBasicBallReward =>
        ballDefinition != null &&
        ballDefinition.TraitType ==
        BallTraitType.Basic;

    public bool IsTraitBallReward =>
        ballDefinition != null &&
        ballDefinition.TraitType !=
        BallTraitType.Basic &&
        ballDefinition.TraitType !=
        BallTraitType.Piercing;

    public bool IsOneStarBallReward =>
        ballDefinition != null &&
        ballDefinition.StarGrade ==
        BallStarGrade.OneStar;

    public bool IsTwoStarBallReward =>
        ballDefinition != null &&
        ballDefinition.StarGrade ==
        BallStarGrade.TwoStar;

    public override bool CanApply(
        RewardApplyContext context)
    {
        if (context == null ||
            !context.HasBallCollection)
        {
            return false;
        }

        if (ballDefinition == null)
        {
            return false;
        }

        if (amount <= 0)
        {
            return false;
        }

        return context
            .BallCollection
            .IsInitialized;
    }

    public override bool Apply(
        RewardApplyContext context)
    {
        if (!CanApply(
                context
            ))
        {
            Debug.LogWarning(
                $"BallRewardDefinition: {name} 보상을 " +
                "현재 상태에서는 적용할 수 없습니다.",
                this
            );

            return false;
        }

        int addedCount =
            context
                .BallCollection
                .AddBalls(
                    amount,
                    ballDefinition
                );

        bool applied =
            addedCount > 0;

        if (!applied)
        {
            Debug.LogWarning(
                $"BallRewardDefinition: {name} 보상으로 " +
                "공을 추가하지 못했습니다.",
                this
            );

            return false;
        }

        Debug.Log(
            "BallRewardDefinition: " +
            $"{ballDefinition.DisplayName} " +
            $"{addedCount}개 지급 완료",
            this
        );

        return true;
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        amount =
            Mathf.Max(
                amount,
                1
            );

        if (ballDefinition == null)
        {
            Debug.LogWarning(
                $"BallRewardDefinition: {name}에 " +
                "Ball Definition이 연결되지 않았습니다.",
                this
            );

            return;
        }

        if (ballDefinition.TraitType ==
            BallTraitType.Piercing)
        {
            Debug.LogWarning(
                $"BallRewardDefinition: {name}은 " +
                "관통 공 보상입니다. 관통 공은 현재 " +
                "일반 1단계 특성 공 후보에서 제외됩니다.",
                this
            );
        }
    }
}