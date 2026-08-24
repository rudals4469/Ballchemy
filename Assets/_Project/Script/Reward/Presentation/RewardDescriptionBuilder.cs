using UnityEngine;

public readonly struct RewardCardContent
{
    public string Title
    {
        get;
    }

    public string GrantText
    {
        get;
    }

    public string EffectText
    {
        get;
    }

    public Sprite Icon
    {
        get;
    }

    public bool HasEffectText =>
        !string.IsNullOrWhiteSpace(
            EffectText
        );

    public RewardCardContent(
        string title,
        string grantText,
        string effectText,
        Sprite icon)
    {
        Title =
            title;

        GrantText =
            grantText;

        EffectText =
            effectText;

        Icon =
            icon;
    }
}

public static class RewardDescriptionBuilder
{
    public static RewardCardContent Build(
        RewardDefinition rewardDefinition)
    {
        return Build(
            rewardDefinition,
            null
        );
    }

    public static RewardCardContent Build(
        RewardDefinition rewardDefinition,
        RunAugmentState runAugmentState)
    {
        if (rewardDefinition == null)
        {
            return new RewardCardContent(
                "알 수 없는 보상",
                string.Empty,
                string.Empty,
                null
            );
        }

        BallRewardDefinition ballReward =
            rewardDefinition as
                BallRewardDefinition;

        if (ballReward != null)
        {
            return BuildBallReward(
                ballReward
            );
        }

        AugmentRewardDefinition augmentReward =
            rewardDefinition as
                AugmentRewardDefinition;

        if (augmentReward != null)
        {
            return BuildAugmentReward(
                augmentReward,
                runAugmentState
            );
        }

        return BuildFallbackReward(
            rewardDefinition
        );
    }

    private static RewardCardContent
        BuildBallReward(
            BallRewardDefinition ballReward)
    {
        BallDefinition ballDefinition =
            ballReward.BallDefinition;

        if (ballDefinition == null)
        {
            return new RewardCardContent(
                ResolveRewardTitle(
                    ballReward
                ),
                string.Empty,
                ResolveRewardDescription(
                    ballReward
                ),
                ballReward.Icon
            );
        }

        string title =
            ResolveBallTitle(
                ballDefinition,
                ballReward
            );

        string grantText =
            BuildBallGrantText(
                ballDefinition,
                ballReward.Amount
            );

        string effectText =
            ResolveRewardDescription(
                ballReward
            );

        Sprite icon =
            ballDefinition.Sprite != null
                ? ballDefinition.Sprite
                : ballReward.Icon;

        return new RewardCardContent(
            title,
            grantText,
            effectText,
            icon
        );
    }

    private static RewardCardContent
        BuildAugmentReward(
            AugmentRewardDefinition augmentReward,
            RunAugmentState runAugmentState)
    {
        AugmentDefinition augmentDefinition =
            augmentReward.AugmentDefinition;

        if (augmentDefinition == null)
        {
            return BuildFallbackReward(
                augmentReward
            );
        }

        int currentLevel =
            runAugmentState != null
                ? runAugmentState.GetLevel(
                    augmentDefinition
                )
                : 0;

        int nextLevel =
            Mathf.Clamp(
                currentLevel + 1,
                1,
                augmentDefinition.MaxLevel
            );

        /*
         * 증강 카드 제목은
         * AugmentDefinition의 Display Name을 우선 사용합니다.
         */
        string title =
            !string.IsNullOrWhiteSpace(
                augmentDefinition.DisplayName
            )
                ? augmentDefinition
                    .DisplayName
                    .Trim()
                : ResolveRewardTitle(
                    augmentReward
                );

        /*
         * 카드에는 선택 후 도달할 레벨을 표시합니다.
         */
        string grantText =
            currentLevel <= 0
                ? $"신규 획득: Lv.{nextLevel}"
                : $"Lv.{currentLevel} > Lv.{nextLevel}";

        /*
         * Tier 3 증강 카드의 짧은 설명은
         * AugmentDefinition 에셋의 Description을 사용합니다.
         *
         * 레벨별 상세 수치는
         * AugmentDefinition.GetLevelDescription()에 남겨두고,
         * 이후 마우스 오버 상세 툴팁에서 사용합니다.
         */
        string effectText =
            ResolveAugmentDescription(
                augmentDefinition
            );

        if (string.IsNullOrWhiteSpace(effectText))
        {
            effectText = augmentDefinition
                .GetLevelDescription(nextLevel);
        }

        /*
         * AugmentDefinition에 아이콘이 있으면 우선 사용하고,
         * 없으면 RewardDefinition의 아이콘을 사용합니다.
         */
        Sprite icon =
            augmentDefinition.Icon != null
                ? augmentDefinition.Icon
                : augmentReward.Icon;

        return new RewardCardContent(
            title,
            grantText,
            effectText,
            icon
        );
    }

    private static string ResolveBallTitle(
        BallDefinition ballDefinition,
        RewardDefinition rewardDefinition)
    {
        if (ballDefinition != null &&
            !string.IsNullOrWhiteSpace(
                ballDefinition.DisplayName
            ))
        {
            return ballDefinition
                .DisplayName
                .Trim();
        }

        return ResolveRewardTitle(
            rewardDefinition
        );
    }

    private static string ResolveRewardTitle(
        RewardDefinition rewardDefinition)
    {
        if (rewardDefinition != null &&
            !string.IsNullOrWhiteSpace(
                rewardDefinition.DisplayName
            ))
        {
            return rewardDefinition
                .DisplayName
                .Trim();
        }

        return "이름 없는 보상";
    }

    private static string BuildBallGrantText(
        BallDefinition ballDefinition,
        int amount)
    {
        amount =
            Mathf.Max(
                amount,
                1
            );

        string gradeText =
            ResolveStarGradeText(
                ballDefinition != null
                    ? ballDefinition.StarGrade
                    : BallStarGrade.None
            );

        string ballName =
            ballDefinition != null &&
            !string.IsNullOrWhiteSpace(
                ballDefinition.DisplayName
            )
                ? ballDefinition
                    .DisplayName
                    .Trim()
                : "공";

        if (string.IsNullOrWhiteSpace(
                gradeText
            ))
        {
            return
                $"{ballName} {amount}개를 획득합니다.";
        }

        return
            $"{gradeText} {ballName} " +
            $"{amount}개를 획득합니다.";
    }

    private static string ResolveStarGradeText(
        BallStarGrade starGrade)
    {
        switch (starGrade)
        {
            case BallStarGrade.OneStar:
                return "1성";

            case BallStarGrade.TwoStar:
                return "2성";

            case BallStarGrade.ThreeStar:
                return "3성";

            case BallStarGrade.None:
            default:
                return string.Empty;
        }
    }

    private static RewardCardContent
        BuildFallbackReward(
            RewardDefinition rewardDefinition)
    {
        return new RewardCardContent(
            ResolveRewardTitle(
                rewardDefinition
            ),
            string.Empty,
            ResolveRewardDescription(
                rewardDefinition
            ),
            rewardDefinition != null
                ? rewardDefinition.Icon
                : null
        );
    }

    private static string ResolveRewardDescription(
        RewardDefinition rewardDefinition)
    {
        if (rewardDefinition == null ||
            string.IsNullOrWhiteSpace(
                rewardDefinition.Description
            ))
        {
            return string.Empty;
        }

        return rewardDefinition
            .Description
            .Trim();
    }

    private static string ResolveAugmentDescription(
        AugmentDefinition augmentDefinition)
    {
        if (augmentDefinition == null ||
            string.IsNullOrWhiteSpace(
                augmentDefinition.Description
            ))
        {
            return string.Empty;
        }

        return augmentDefinition
            .Description
            .Trim();
    }
}
