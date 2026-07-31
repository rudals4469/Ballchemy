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
                ResolveFallbackDescription(
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
            BuildBallEffectText(
                ballDefinition,
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
            return ballDefinition.DisplayName;
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
            return rewardDefinition.DisplayName;
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
                ? ballDefinition.DisplayName
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

    private static string BuildBallEffectText(
        BallDefinition ballDefinition,
        RewardDefinition rewardDefinition)
    {
        if (ballDefinition == null)
        {
            return ResolveFallbackDescription(
                rewardDefinition
            );
        }

        switch (ballDefinition.TraitType)
        {
            case BallTraitType.Basic:
                return string.Empty;

            case BallTraitType.Explosion:
                return BuildExplosionEffectText(
                    ballDefinition,
                    rewardDefinition
                );

            case BallTraitType.Elemental:
                return BuildElementalEffectText(
                    ballDefinition,
                    rewardDefinition
                );

            /*
             * Critical과 Piercing은 현재 제공된 코드만으로
             * 실제 수치와 규칙을 정확하게 만들 수 없습니다.
             *
             * 임의의 설명을 생성하지 않고
             * RewardDefinition에 작성된 설명을 사용합니다.
             */
            case BallTraitType.Critical:
            case BallTraitType.Piercing:
                return ResolveFallbackDescription(
                    rewardDefinition
                );

            default:
                return ResolveFallbackDescription(
                    rewardDefinition
                );
        }
    }

    private static string BuildExplosionEffectText(
        BallDefinition ballDefinition,
        RewardDefinition rewardDefinition)
    {
        ExplosionBallTraitDefinition
            explosionDefinition =
                ballDefinition.TraitDefinition as
                    ExplosionBallTraitDefinition;

        if (explosionDefinition == null)
        {
            return ResolveFallbackDescription(
                rewardDefinition
            );
        }

        int range =
            explosionDefinition.GetRange(
                ballDefinition.StarGrade
            );

        range =
            Mathf.Max(
                range,
                1
            );

        string patternText =
            ResolveExplosionPatternText(
                explosionDefinition.PatternType
            );

        string damageText =
            BuildExplosionDamageText(
                explosionDefinition
            );

        return
            $"적중 지점을 중심으로 {patternText},\n" +
            $"각 방향으로 {range}칸까지 폭발합니다.\n" +
            damageText;
    }

    private static string
        ResolveExplosionPatternText(
            ExplosionPatternType patternType)
    {
        switch (patternType)
        {
            case ExplosionPatternType.Cross:
                return "상하좌우 4방향";

            case ExplosionPatternType.Diagonal:
                return "대각선 4방향";

            case ExplosionPatternType.AllDirections:
                return "상하좌우와 대각선 8방향";

            default:
                return "주변 방향";
        }
    }

    private static string BuildExplosionDamageText(
        ExplosionBallTraitDefinition definition)
    {
        if (definition == null)
        {
            return string.Empty;
        }

        string multiplierText =
            FormatPercentage(
                definition
                    .ExplosionDamageMultiplier
            );

        int flatBonus =
            Mathf.Max(
                definition
                    .FlatExplosionDamageBonus,
                0
            );

        if (flatBonus > 0)
        {
            return
                $"주변 피해: 직접 피해의 " +
                $"{multiplierText} + {flatBonus}";
        }

        return
            $"주변 피해: 직접 피해의 " +
            $"{multiplierText}";
    }

    private static string BuildElementalEffectText(
        BallDefinition ballDefinition,
        RewardDefinition rewardDefinition)
    {
        ElementalBallTraitDefinition
            elementalDefinition =
                ballDefinition.TraitDefinition as
                    ElementalBallTraitDefinition;

        if (elementalDefinition == null)
        {
            return ResolveFallbackDescription(
                rewardDefinition
            );
        }

        int stackAmount =
            elementalDefinition.GetStackAmount(
                ballDefinition.StarGrade
            );

        stackAmount =
            Mathf.Max(
                stackAmount,
                1
            );

        switch (elementalDefinition.ElementType)
        {
            case ElementType.Water:
                return
                    $"적중 시 젖음 {stackAmount}스택을 " +
                    "부여합니다.";

            case ElementType.Electric:
                return
                    $"적중 시 전하 {stackAmount}스택을 " +
                    "부여합니다.\n" +
                    "젖음과 만나면 감전을 일으킵니다.";

            case ElementType.Fire:
                return
                    $"적중 시 화상 {stackAmount}스택을 " +
                    "부여합니다.\n" +
                    "냉기와 만나면 열충격을 일으킵니다.";

            case ElementType.Ice:
                return
                    $"적중 시 냉기 {stackAmount}스택을 " +
                    "부여합니다.\n" +
                    "냉기가 최대치에 도달하면 " +
                    "동결시킵니다.";

            default:
                return ResolveFallbackDescription(
                    rewardDefinition
                );
        }
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

    private static string FormatPercentage(
        float multiplier)
    {
        multiplier =
            Mathf.Max(
                multiplier,
                0f
            );

        float percentage =
            multiplier *
            100f;

        if (Mathf.Approximately(
                percentage,
                Mathf.Round(
                    percentage
                )
            ))
        {
            return
                $"{Mathf.RoundToInt(percentage)}%";
        }

        return
            $"{percentage:0.#}%";
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
            ResolveFallbackDescription(
                rewardDefinition
            ),
            rewardDefinition != null
                ? rewardDefinition.Icon
                : null
        );
    }

    private static string
        ResolveFallbackDescription(
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