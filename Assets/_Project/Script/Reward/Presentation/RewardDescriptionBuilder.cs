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
                ResolveDescription(
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

        /*
         * 모든 공 보상의 효과 설명은
         * BallRewardDefinition이 상속받은
         * RewardDefinition.Description을 사용합니다.
         *
         * 실제 지급 수량은 Description과 분리하여
         * BallRewardDefinition.Amount로 관리합니다.
         */
        string effectText =
            ResolveDescription(
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

    private static string ResolveBallTitle(
        BallDefinition ballDefinition,
        RewardDefinition rewardDefinition)
    {
        if (ballDefinition != null &&
            !string.IsNullOrWhiteSpace(
                ballDefinition.DisplayName
            ))
        {
            return ballDefinition.DisplayName.Trim();
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
            return rewardDefinition.DisplayName.Trim();
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
                ? ballDefinition.DisplayName.Trim()
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
            ResolveDescription(
                rewardDefinition
            ),
            rewardDefinition != null
                ? rewardDefinition.Icon
                : null
        );
    }

    private static string ResolveDescription(
        RewardDefinition rewardDefinition)
    {
        if (rewardDefinition == null ||
            string.IsNullOrWhiteSpace(
                rewardDefinition.Description
            ))
        {
            return string.Empty;
        }

        return rewardDefinition.Description.Trim();
    }
}