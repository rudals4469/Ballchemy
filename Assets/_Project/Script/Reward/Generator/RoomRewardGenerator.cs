using System.Collections.Generic;
using UnityEngine;

public sealed class RoomRewardGenerator :
    MonoBehaviour
{
    private const int DefaultChoiceCount = 3;

    [Header("References")]

    [SerializeField]
    private RewardCatalog rewardCatalog;

    [Header("Generation")]

    [Tooltip(
        "한 번에 생성하는 보상 선택지 수입니다. " +
        "현재 기본 규칙은 3개입니다."
    )]
    [SerializeField, Min(1)]
    private int choiceCount =
        DefaultChoiceCount;

    private readonly List<RewardDefinition>
        tierCandidates =
            new List<RewardDefinition>();

    private readonly List<RewardDefinition>
        workingCandidates =
            new List<RewardDefinition>();

    private readonly List<RewardDefinition>
        generatedChoices =
            new List<RewardDefinition>();

    private readonly List<BallRewardDefinition>
        basicOneStarCandidates =
            new List<BallRewardDefinition>();

    private readonly List<BallRewardDefinition>
        traitOneStarCandidates =
            new List<BallRewardDefinition>();

    public RewardCatalog RewardCatalog =>
        rewardCatalog;

    public int ChoiceCount =>
        choiceCount;

    private void Awake()
    {
        NormalizeSettings();
        ValidateReferences();
    }

    private void OnValidate()
    {
        NormalizeSettings();
    }

    private void NormalizeSettings()
    {
        choiceCount =
            Mathf.Max(
                choiceCount,
                1
            );
    }

    private bool ValidateReferences()
    {
        if (rewardCatalog != null)
        {
            return true;
        }

        Debug.LogError(
            "RoomRewardGenerator: " +
            "Reward Catalog가 연결되지 않았습니다.",
            this
        );

        return false;
    }

    public List<RewardDefinition>
        GenerateChoices(
            RewardTier rewardTier)
    {
        return GenerateChoices(
            rewardTier,
            choiceCount
        );
    }

    public List<RewardDefinition>
        GenerateChoices(
            RewardTier rewardTier,
            int requestedChoiceCount)
    {
        generatedChoices.Clear();

        requestedChoiceCount =
            Mathf.Max(
                requestedChoiceCount,
                1
            );

        if (!ValidateReferences())
        {
            return new List<RewardDefinition>();
        }

        if (rewardTier ==
            RewardTier.None)
        {
            Debug.LogWarning(
                "RoomRewardGenerator: " +
                "RewardTier.None으로는 선택지를 " +
                "생성할 수 없습니다.",
                this
            );

            return new List<RewardDefinition>();
        }

        rewardCatalog.GetRewards(
            rewardTier,
            tierCandidates
        );

        if (tierCandidates.Count == 0)
        {
            Debug.LogWarning(
                "RoomRewardGenerator: " +
                $"{rewardTier}에 등록된 보상이 없습니다.",
                this
            );

            return new List<RewardDefinition>();
        }

        if (rewardTier ==
            RewardTier.Tier1 &&
            requestedChoiceCount ==
            DefaultChoiceCount)
        {
            GenerateTierOneChoices();

            FillRemainingChoices(
                requestedChoiceCount
            );
        }
        else
        {
            GenerateGenericChoices(
                requestedChoiceCount
            );
        }

        if (generatedChoices.Count <
            requestedChoiceCount)
        {
            Debug.LogWarning(
                "RoomRewardGenerator: " +
                $"{rewardTier} 선택지를 " +
                $"{requestedChoiceCount}개 요청했지만 " +
                $"{generatedChoices.Count}개만 생성했습니다.",
                this
            );
        }

        return new List<RewardDefinition>(
            generatedChoices
        );
    }

    private void GenerateTierOneChoices()
    {
        basicOneStarCandidates.Clear();
        traitOneStarCandidates.Clear();

        for (int i = 0;
             i < tierCandidates.Count;
             i++)
        {
            BallRewardDefinition ballReward =
                tierCandidates[i] as
                    BallRewardDefinition;

            if (ballReward == null ||
                ballReward.BallDefinition == null)
            {
                continue;
            }

            if (!ballReward.IsOneStarBallReward)
            {
                continue;
            }

            if (ballReward.IsBasicBallReward &&
                ballReward.Amount == 2)
            {
                basicOneStarCandidates.Add(
                    ballReward
                );

                continue;
            }

            if (ballReward.IsTraitBallReward &&
                ballReward.Amount == 1)
            {
                traitOneStarCandidates.Add(
                    ballReward
                );
            }
        }

        BallRewardDefinition basicReward =
            SelectWeighted(
                basicOneStarCandidates
            );

        AddChoice(
            basicReward
        );

        BallRewardDefinition firstTraitReward =
            SelectWeighted(
                traitOneStarCandidates
            );

        AddChoice(
            firstTraitReward
        );

        BallRewardDefinition secondTraitReward =
            SelectWeightedExcluding(
                traitOneStarCandidates,
                firstTraitReward
            );

        AddChoice(
            secondTraitReward
        );

        if (basicReward == null)
        {
            Debug.LogWarning(
                "RoomRewardGenerator: " +
                "Tier1의 '1성 기본 공 2개' 보상이 " +
                "등록되지 않았습니다.",
                this
            );
        }

        if (firstTraitReward == null)
        {
            Debug.LogWarning(
                "RoomRewardGenerator: " +
                "Tier1의 '1성 특성 공 1개' 보상이 " +
                "등록되지 않았습니다.",
                this
            );
        }

        if (secondTraitReward == null)
        {
            Debug.LogWarning(
                "RoomRewardGenerator: " +
                "Tier1에 서로 다른 1성 특성 공 보상이 " +
                "2개 이상 필요합니다.",
                this
            );
        }
    }

    private void FillRemainingChoices(
        int requestedChoiceCount)
    {
        if (generatedChoices.Count >=
            requestedChoiceCount)
        {
            return;
        }

        workingCandidates.Clear();

        for (int i = 0;
             i < tierCandidates.Count;
             i++)
        {
            RewardDefinition candidate =
                tierCandidates[i];

            if (!IsValidCandidate(
                    candidate
                ))
            {
                continue;
            }

            if (generatedChoices.Contains(
                    candidate
                ))
            {
                continue;
            }

            workingCandidates.Add(
                candidate
            );
        }

        while (generatedChoices.Count <
                   requestedChoiceCount &&
               workingCandidates.Count > 0)
        {
            RewardDefinition selected =
                SelectWeighted(
                    workingCandidates
                );

            if (selected == null)
            {
                break;
            }

            AddChoice(
                selected
            );

            workingCandidates.Remove(
                selected
            );
        }
    }

    private void GenerateGenericChoices(
        int requestedChoiceCount)
    {
        workingCandidates.Clear();

        for (int i = 0;
             i < tierCandidates.Count;
             i++)
        {
            RewardDefinition candidate =
                tierCandidates[i];

            if (!IsValidCandidate(
                    candidate
                ))
            {
                continue;
            }

            workingCandidates.Add(
                candidate
            );
        }

        while (generatedChoices.Count <
                   requestedChoiceCount &&
               workingCandidates.Count > 0)
        {
            RewardDefinition selected =
                SelectWeighted(
                    workingCandidates
                );

            if (selected == null)
            {
                break;
            }

            AddChoice(
                selected
            );

            workingCandidates.Remove(
                selected
            );
        }
    }

    private void AddChoice(
        RewardDefinition rewardDefinition)
    {
        if (!IsValidCandidate(
                rewardDefinition
            ))
        {
            return;
        }

        if (generatedChoices.Contains(
                rewardDefinition
            ))
        {
            return;
        }

        generatedChoices.Add(
            rewardDefinition
        );
    }

    private static bool IsValidCandidate(
        RewardDefinition rewardDefinition)
    {
        return
            rewardDefinition != null &&
            rewardDefinition.CanBeSelected &&
            rewardDefinition.SelectionWeight > 0;
    }

    private static T SelectWeighted<T>(
        List<T> candidates)
        where T : RewardDefinition
    {
        if (candidates == null ||
            candidates.Count == 0)
        {
            return null;
        }

        int totalWeight = 0;

        for (int i = 0;
             i < candidates.Count;
             i++)
        {
            T candidate =
                candidates[i];

            if (!IsValidCandidate(
                    candidate
                ))
            {
                continue;
            }

            totalWeight +=
                candidate.SelectionWeight;
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        int randomValue =
            Random.Range(
                0,
                totalWeight
            );

        int accumulatedWeight = 0;

        for (int i = 0;
             i < candidates.Count;
             i++)
        {
            T candidate =
                candidates[i];

            if (!IsValidCandidate(
                    candidate
                ))
            {
                continue;
            }

            accumulatedWeight +=
                candidate.SelectionWeight;

            if (randomValue <
                accumulatedWeight)
            {
                return candidate;
            }
        }

        return null;
    }

    private static T
        SelectWeightedExcluding<T>(
            List<T> candidates,
            T excludedCandidate)
        where T : RewardDefinition
    {
        if (candidates == null ||
            candidates.Count == 0)
        {
            return null;
        }

        int totalWeight = 0;

        for (int i = 0;
             i < candidates.Count;
             i++)
        {
            T candidate =
                candidates[i];

            if (candidate ==
                    excludedCandidate ||
                !IsValidCandidate(
                    candidate
                ))
            {
                continue;
            }

            totalWeight +=
                candidate.SelectionWeight;
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        int randomValue =
            Random.Range(
                0,
                totalWeight
            );

        int accumulatedWeight = 0;

        for (int i = 0;
             i < candidates.Count;
             i++)
        {
            T candidate =
                candidates[i];

            if (candidate ==
                    excludedCandidate ||
                !IsValidCandidate(
                    candidate
                ))
            {
                continue;
            }

            accumulatedWeight +=
                candidate.SelectionWeight;

            if (randomValue <
                accumulatedWeight)
            {
                return candidate;
            }
        }

        return null;
    }
}