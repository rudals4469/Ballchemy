using UnityEngine;

[CreateAssetMenu(
    fileName = "Reward_Augment",
    menuName =
        "Ballchemy/Rewards/Augment Reward"
)]
public sealed class AugmentRewardDefinition :
    RewardDefinition
{
    [Header("Augment Reward")]

    [SerializeField]
    private AugmentDefinition
        augmentDefinition;

    public override RewardType RewardType =>
        RewardType.Passive;

    public AugmentDefinition AugmentDefinition =>
        augmentDefinition;

    public void ConfigureRuntime(
        string id,
        string title,
        string body,
        AugmentDefinition definition,
        int weight)
    {
        ConfigureBase(id, title, body, RewardTier.Tier3, weight);
        augmentDefinition = definition;
        name = $"RuntimeReward_{id}";
    }

    public override bool CanApply(
        RewardApplyContext context)
    {
        if (context == null ||
            !context.HasRunAugmentState ||
            augmentDefinition == null)
        {
            return false;
        }

        return context
            .RunAugmentState
            .CanIncreaseLevel(
                augmentDefinition
            );
    }

    public override bool Apply(
        RewardApplyContext context)
    {
        if (!CanApply(context))
        {
            Debug.LogWarning(
                $"AugmentRewardDefinition: {name}은 " +
                "현재 상태에서 적용할 수 없습니다.",
                this
            );

            return false;
        }

        bool applied =
            context
                .RunAugmentState
                .TryIncreaseLevel(
                    augmentDefinition
                );

        if (!applied)
        {
            Debug.LogWarning(
                $"AugmentRewardDefinition: {name}의 " +
                "증강 레벨 증가에 실패했습니다.",
                this
            );

            return false;
        }

        int currentLevel =
            context
                .RunAugmentState
                .GetLevel(
                    augmentDefinition
                );

        Debug.Log(
            "AugmentRewardDefinition: " +
            $"{augmentDefinition.DisplayName} " +
            $"Lv.{currentLevel} 획득 완료",
            this
        );

        return true;
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        if (augmentDefinition != null)
        {
            return;
        }

        Debug.LogWarning(
            $"AugmentRewardDefinition: {name}에 " +
            "Augment Definition이 연결되지 않았습니다.",
            this
        );
    }
}
