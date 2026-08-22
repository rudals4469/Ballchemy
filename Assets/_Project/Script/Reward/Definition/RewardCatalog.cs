using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "RewardCatalog",
    menuName =
        "Ballchemy/Rewards/Reward Catalog"
)]
public sealed class RewardCatalog :
    ScriptableObject
{
    [Header("Registered Rewards")]

    [Tooltip(
        "이번 런에서 선택지로 사용할 수 있는 " +
        "모든 보상 Definition을 등록합니다."
    )]
    [SerializeField]
    private List<RewardDefinition>
        rewardDefinitions =
            new List<RewardDefinition>();

    [SerializeField]
    private AugmentRuleCatalog augmentRuleCatalog;

    public IReadOnlyList<RewardDefinition>
        RewardDefinitions =>
            rewardDefinitions;

    public int Count =>
        rewardDefinitions != null
            ? rewardDefinitions.Count
            : 0;

    public void GetRewards(
        RewardTier rewardTier,
        List<RewardDefinition> results)
    {
        if (results == null)
        {
            return;
        }

        results.Clear();

        if (rewardDefinitions == null)
        {
            return;
        }

        for (int i = 0;
             i < rewardDefinitions.Count;
             i++)
        {
            RewardDefinition definition =
                rewardDefinitions[i];

            if (definition == null ||
                !definition.CanBeSelected ||
                definition.RewardTier !=
                rewardTier)
            {
                continue;
            }

            BallRewardDefinition ballReward =
                definition as BallRewardDefinition;

            if (ballReward != null &&
                BallPoolPolicy
                    .IsRemovedFromPlayerPool(
                        ballReward.BallDefinition
                    ))
            {
                continue;
            }

            results.Add(
                definition
            );
        }
    }

    public void GetAugmentRewards(
        List<AugmentRewardDefinition> results)
    {
        if (results == null)
            return;

        results.Clear();
        if (rewardDefinitions == null)
            return;

        // 새 규칙형 카탈로그가 연결된 프로젝트에서는 예전 개별형 증강을
        // 후보에 섞지 않는다. 기존 에셋은 마이그레이션 참고용으로 보존한다.
        if (augmentRuleCatalog == null)
        {
            for (int i = 0; i < rewardDefinitions.Count; i++)
            {
                AugmentRewardDefinition reward =
                    rewardDefinitions[i] as AugmentRewardDefinition;

                if (reward == null ||
                    !reward.CanBeSelected ||
                    reward.AugmentDefinition == null)
                {
                    continue;
                }

                results.Add(reward);
            }
        }

        augmentRuleCatalog?.GetRuntimeRewards(results);
    }

    private void OnValidate()
    {
        if (rewardDefinitions == null)
        {
            rewardDefinitions =
                new List<RewardDefinition>();

            return;
        }

        HashSet<RewardDefinition>
            registeredDefinitions =
                new HashSet<RewardDefinition>();

        for (int i =
                 rewardDefinitions.Count - 1;
             i >= 0;
             i--)
        {
            RewardDefinition definition =
                rewardDefinitions[i];

            if (definition == null)
            {
                continue;
            }

            if (registeredDefinitions.Add(
                    definition
                ))
            {
                continue;
            }

            Debug.LogWarning(
                "RewardCatalog: " +
                $"{definition.name}이 중복 등록되어 " +
                "있습니다.",
                this
            );
        }
    }
}
