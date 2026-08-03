using UnityEngine;

[DisallowMultipleComponent]
public sealed class RunRewardState :
    MonoBehaviour
{
    [Header("Reward Tier Upgrade")]

    [Tooltip(
        "다음 전투방 보상에 적용할 " +
        "보상 등급 증가 단계입니다.\n" +
        "Tier1은 Tier2로, " +
        "Tier2는 Tier3로 증가합니다.\n" +
        "Tier3은 더 증가하지 않습니다."
    )]
    [SerializeField, Min(0)]
    private int pendingRewardTierIncrease;

    public int PendingRewardTierIncrease =>
        pendingRewardTierIncrease;

    public bool HasPendingRewardUpgrade =>
        pendingRewardTierIncrease > 0;

    private void OnValidate()
    {
        pendingRewardTierIncrease =
            Mathf.Max(
                pendingRewardTierIncrease,
                0
            );
    }

    public bool AddPendingRewardTierIncrease(
        int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        pendingRewardTierIncrease +=
            amount;

        Debug.Log(
            "RunRewardState: " +
            $"다음 전투방 보상 등급 증가 예약 +{amount}, " +
            $"현재 예약={pendingRewardTierIncrease}",
            this
        );

        return true;
    }

    public RewardTier ApplyPendingUpgrade(
        RewardTier baseRewardTier)
    {
        if (baseRewardTier ==
                RewardTier.None ||
            pendingRewardTierIncrease <= 0)
        {
            return baseRewardTier;
        }

        RewardTier upgradedTier =
            baseRewardTier;

        for (int i = 0;
             i < pendingRewardTierIncrease;
             i++)
        {
            upgradedTier =
                UpgradeTier(
                    upgradedTier
                );

            if (upgradedTier ==
                RewardTier.Tier3)
            {
                break;
            }
        }

        return upgradedTier;
    }

    public bool ConsumePendingRewardUpgrade()
    {
        if (pendingRewardTierIncrease <= 0)
        {
            return false;
        }

        int consumedAmount =
            pendingRewardTierIncrease;

        pendingRewardTierIncrease =
            0;

        Debug.Log(
            "RunRewardState: " +
            $"다음 전투방 보상 등급 증가 예약 " +
            $"{consumedAmount}단계 소비 완료",
            this
        );

        return true;
    }

    public void ClearPendingRewardUpgrade()
    {
        if (pendingRewardTierIncrease <= 0)
        {
            return;
        }

        pendingRewardTierIncrease =
            0;

        Debug.Log(
            "RunRewardState: " +
            "보상 등급 증가 예약 초기화",
            this
        );
    }

    private static RewardTier UpgradeTier(
        RewardTier rewardTier)
    {
        switch (rewardTier)
        {
            case RewardTier.Tier1:
                return RewardTier.Tier2;

            case RewardTier.Tier2:
                return RewardTier.Tier3;

            case RewardTier.Tier3:
                return RewardTier.Tier3;

            default:
                return rewardTier;
        }
    }
}