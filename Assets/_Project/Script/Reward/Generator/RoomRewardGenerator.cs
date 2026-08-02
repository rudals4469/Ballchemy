using System.Collections.Generic;
using UnityEngine;

public sealed class RoomRewardGenerator :
    MonoBehaviour
{
    private const int DefaultChoiceCount = 3;

    private enum TierTwoCategory
    {
        None = 0,
        UpgradeRandomBalls = 1,
        ConvertBasicBalls = 2,
        TwoStarTraitBall = 3,
        OneStarTraitBalls = 4,
        BasicBalls = 5
    }

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

    /*
     * Tier 2 범주별 후보 목록입니다.
     *
     * 공 종류별 RewardDefinition이 여러 개 존재하더라도
     * 범주 자체는 하나의 선택지로만 취급합니다.
     */
    private readonly List<RewardDefinition>
        tierTwoUpgradeCandidates =
            new List<RewardDefinition>();

    private readonly List<RewardDefinition>
        tierTwoConversionCandidates =
            new List<RewardDefinition>();

    private readonly List<BallRewardDefinition>
        tierTwoTwoStarTraitCandidates =
            new List<BallRewardDefinition>();

    private readonly List<BallRewardDefinition>
        tierTwoOneStarTraitCandidates =
            new List<BallRewardDefinition>();

    private readonly List<BallRewardDefinition>
        tierTwoBasicCandidates =
            new List<BallRewardDefinition>();

    private readonly List<TierTwoCategory>
        availableTierTwoCategories =
            new List<TierTwoCategory>();

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

    /*
     * 기존 공개 API를 유지합니다.
     *
     * 기존 참조처의 컴파일 오류를 막기 위해 남겨두며,
     * 적용 조건 검사가 필요한 실제 방 보상 흐름에서는
     * RewardApplyContext를 받는 오버로드를 사용합니다.
     */
    public List<RewardDefinition>
        GenerateChoices(
            RewardTier rewardTier)
    {
        return GenerateChoicesInternal(
            rewardTier,
            choiceCount,
            null,
            false
        );
    }

    public List<RewardDefinition>
        GenerateChoices(
            RewardTier rewardTier,
            int requestedChoiceCount)
    {
        return GenerateChoicesInternal(
            rewardTier,
            requestedChoiceCount,
            null,
            false
        );
    }

    public List<RewardDefinition>
        GenerateChoices(
            RewardTier rewardTier,
            RewardApplyContext applyContext)
    {
        return GenerateChoicesInternal(
            rewardTier,
            choiceCount,
            applyContext,
            true
        );
    }

    public List<RewardDefinition>
        GenerateChoices(
            RewardTier rewardTier,
            int requestedChoiceCount,
            RewardApplyContext applyContext)
    {
        return GenerateChoicesInternal(
            rewardTier,
            requestedChoiceCount,
            applyContext,
            true
        );
    }

    private List<RewardDefinition>
        GenerateChoicesInternal(
            RewardTier rewardTier,
            int requestedChoiceCount,
            RewardApplyContext applyContext,
            bool requireCanApply)
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
            GenerateTierOneChoices(
                applyContext,
                requireCanApply
            );

            FillRemainingChoices(
                requestedChoiceCount,
                applyContext,
                requireCanApply
            );
        }
        else if (rewardTier ==
                 RewardTier.Tier2)
        {
            GenerateTierTwoChoices(
                requestedChoiceCount,
                applyContext,
                requireCanApply
            );
        }
        else
        {
            GenerateGenericChoices(
                requestedChoiceCount,
                applyContext,
                requireCanApply
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

    private void GenerateTierOneChoices(
        RewardApplyContext applyContext,
        bool requireCanApply)
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

            if (!IsAvailableCandidate(
                    ballReward,
                    applyContext,
                    requireCanApply
                ) ||
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
            basicReward,
            applyContext,
            requireCanApply
        );

        BallRewardDefinition firstTraitReward =
            SelectWeighted(
                traitOneStarCandidates
            );

        AddChoice(
            firstTraitReward,
            applyContext,
            requireCanApply
        );

        BallRewardDefinition secondTraitReward =
            SelectWeightedExcluding(
                traitOneStarCandidates,
                firstTraitReward
            );

        AddChoice(
            secondTraitReward,
            applyContext,
            requireCanApply
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

    private void GenerateTierTwoChoices(
        int requestedChoiceCount,
        RewardApplyContext applyContext,
        bool requireCanApply)
    {
        BuildTierTwoCategories(
            applyContext,
            requireCanApply
        );

        while (generatedChoices.Count <
                   requestedChoiceCount &&
               availableTierTwoCategories.Count > 0)
        {
            TierTwoCategory selectedCategory =
                SelectWeightedTierTwoCategory();

            if (selectedCategory ==
                TierTwoCategory.None)
            {
                break;
            }

            RewardDefinition selectedReward =
                SelectRewardFromTierTwoCategory(
                    selectedCategory
                );

            availableTierTwoCategories.Remove(
                selectedCategory
            );

            AddChoice(
                selectedReward,
                applyContext,
                requireCanApply
            );
        }
    }

    private void BuildTierTwoCategories(
        RewardApplyContext applyContext,
        bool requireCanApply)
    {
        tierTwoUpgradeCandidates.Clear();
        tierTwoConversionCandidates.Clear();
        tierTwoTwoStarTraitCandidates.Clear();
        tierTwoOneStarTraitCandidates.Clear();
        tierTwoBasicCandidates.Clear();
        availableTierTwoCategories.Clear();

        for (int i = 0;
             i < tierCandidates.Count;
             i++)
        {
            RewardDefinition candidate =
                tierCandidates[i];

            if (!IsAvailableCandidate(
                    candidate,
                    applyContext,
                    requireCanApply
                ))
            {
                continue;
            }

            if (candidate is
                BallUpgradeRewardDefinition)
            {
                tierTwoUpgradeCandidates.Add(
                    candidate
                );

                continue;
            }

            if (candidate is
                BasicBallConversionRewardDefinition)
            {
                tierTwoConversionCandidates.Add(
                    candidate
                );

                continue;
            }

            BallRewardDefinition ballReward =
                candidate as
                    BallRewardDefinition;

            if (ballReward == null ||
                ballReward.BallDefinition == null)
            {
                continue;
            }

            if (ballReward.IsTraitBallReward &&
                ballReward.IsTwoStarBallReward &&
                ballReward.Amount == 1)
            {
                tierTwoTwoStarTraitCandidates.Add(
                    ballReward
                );

                continue;
            }

            if (ballReward.IsTraitBallReward &&
                ballReward.IsOneStarBallReward &&
                ballReward.Amount == 2)
            {
                tierTwoOneStarTraitCandidates.Add(
                    ballReward
                );

                continue;
            }

            if (ballReward.IsBasicBallReward &&
                ballReward.IsOneStarBallReward &&
                ballReward.Amount == 5)
            {
                tierTwoBasicCandidates.Add(
                    ballReward
                );
            }
        }

        AddTierTwoCategoryIfAvailable(
            TierTwoCategory.UpgradeRandomBalls,
            tierTwoUpgradeCandidates.Count
        );

        AddTierTwoCategoryIfAvailable(
            TierTwoCategory.ConvertBasicBalls,
            tierTwoConversionCandidates.Count
        );

        AddTierTwoCategoryIfAvailable(
            TierTwoCategory.TwoStarTraitBall,
            tierTwoTwoStarTraitCandidates.Count
        );

        AddTierTwoCategoryIfAvailable(
            TierTwoCategory.OneStarTraitBalls,
            tierTwoOneStarTraitCandidates.Count
        );

        AddTierTwoCategoryIfAvailable(
            TierTwoCategory.BasicBalls,
            tierTwoBasicCandidates.Count
        );
    }

    private void AddTierTwoCategoryIfAvailable(
        TierTwoCategory category,
        int candidateCount)
    {
        if (candidateCount <= 0)
        {
            return;
        }

        availableTierTwoCategories.Add(
            category
        );
    }

    private TierTwoCategory
        SelectWeightedTierTwoCategory()
    {
        int totalWeight = 0;

        for (int i = 0;
             i < availableTierTwoCategories.Count;
             i++)
        {
            totalWeight +=
                GetTierTwoCategoryWeight(
                    availableTierTwoCategories[i]
                );
        }

        if (totalWeight <= 0)
        {
            return TierTwoCategory.None;
        }

        int randomValue =
            Random.Range(
                0,
                totalWeight
            );

        int accumulatedWeight = 0;

        for (int i = 0;
             i < availableTierTwoCategories.Count;
             i++)
        {
            TierTwoCategory category =
                availableTierTwoCategories[i];

            accumulatedWeight +=
                GetTierTwoCategoryWeight(
                    category
                );

            if (randomValue <
                accumulatedWeight)
            {
                return category;
            }
        }

        return TierTwoCategory.None;
    }

    private int GetTierTwoCategoryWeight(
        TierTwoCategory category)
    {
        switch (category)
        {
            case TierTwoCategory
                .UpgradeRandomBalls:
                return GetMaximumSelectionWeight(
                    tierTwoUpgradeCandidates
                );

            case TierTwoCategory
                .ConvertBasicBalls:
                return GetMaximumSelectionWeight(
                    tierTwoConversionCandidates
                );

            case TierTwoCategory
                .TwoStarTraitBall:
                return GetMaximumSelectionWeight(
                    tierTwoTwoStarTraitCandidates
                );

            case TierTwoCategory
                .OneStarTraitBalls:
                return GetMaximumSelectionWeight(
                    tierTwoOneStarTraitCandidates
                );

            case TierTwoCategory
                .BasicBalls:
                return GetMaximumSelectionWeight(
                    tierTwoBasicCandidates
                );

            default:
                return 0;
        }
    }

    private RewardDefinition
        SelectRewardFromTierTwoCategory(
            TierTwoCategory category)
    {
        switch (category)
        {
            case TierTwoCategory
                .UpgradeRandomBalls:
                return SelectWeighted(
                    tierTwoUpgradeCandidates
                );

            case TierTwoCategory
                .ConvertBasicBalls:
                return SelectWeighted(
                    tierTwoConversionCandidates
                );

            case TierTwoCategory
                .TwoStarTraitBall:
                return SelectWeighted(
                    tierTwoTwoStarTraitCandidates
                );

            case TierTwoCategory
                .OneStarTraitBalls:
                return SelectWeighted(
                    tierTwoOneStarTraitCandidates
                );

            case TierTwoCategory
                .BasicBalls:
                return SelectWeighted(
                    tierTwoBasicCandidates
                );

            default:
                return null;
        }
    }

    private static int GetMaximumSelectionWeight<T>(
        List<T> candidates)
        where T : RewardDefinition
    {
        if (candidates == null)
        {
            return 0;
        }

        int maximumWeight = 0;

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

            maximumWeight =
                Mathf.Max(
                    maximumWeight,
                    candidate.SelectionWeight
                );
        }

        return maximumWeight;
    }

    private void FillRemainingChoices(
        int requestedChoiceCount,
        RewardApplyContext applyContext,
        bool requireCanApply)
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

            if (!IsAvailableCandidate(
                    candidate,
                    applyContext,
                    requireCanApply
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
                selected,
                applyContext,
                requireCanApply
            );

            workingCandidates.Remove(
                selected
            );
        }
    }

    private void GenerateGenericChoices(
        int requestedChoiceCount,
        RewardApplyContext applyContext,
        bool requireCanApply)
    {
        workingCandidates.Clear();

        for (int i = 0;
             i < tierCandidates.Count;
             i++)
        {
            RewardDefinition candidate =
                tierCandidates[i];

            if (!IsAvailableCandidate(
                    candidate,
                    applyContext,
                    requireCanApply
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
                selected,
                applyContext,
                requireCanApply
            );

            workingCandidates.Remove(
                selected
            );
        }
    }

    private void AddChoice(
        RewardDefinition rewardDefinition,
        RewardApplyContext applyContext,
        bool requireCanApply)
    {
        if (!IsAvailableCandidate(
                rewardDefinition,
                applyContext,
                requireCanApply
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

    private static bool IsAvailableCandidate(
        RewardDefinition rewardDefinition,
        RewardApplyContext applyContext,
        bool requireCanApply)
    {
        if (!IsValidCandidate(
                rewardDefinition
            ))
        {
            return false;
        }

        if (!requireCanApply)
        {
            return true;
        }

        if (applyContext == null)
        {
            return false;
        }

        return rewardDefinition.CanApply(
            applyContext
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