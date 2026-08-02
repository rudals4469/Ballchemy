using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Reward_BasicBallConversion",
    menuName =
        "Ballchemy/Rewards/Basic Ball Conversion Reward"
)]
public sealed class BasicBallConversionRewardDefinition :
    RewardDefinition
{
    [Header("Basic Ball Conversion")]

    [Tooltip(
        "이 보상을 선택했을 때 무작위로 변환할 " +
        "최대 1성 기본 공 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int conversionCount = 3;

    [Tooltip(
        "기본 공이 변환될 수 있는 1성 특성 공 목록입니다.\n" +
        "기본 공과 관통 공은 등록하지 않습니다."
    )]
    [SerializeField]
    private List<BallDefinition>
        oneStarTraitBallDefinitions =
            new List<BallDefinition>();

    private readonly List<Ball>
        conversionCandidates =
            new List<Ball>();

    private readonly List<BallDefinition>
        validTraitDefinitions =
            new List<BallDefinition>();

    public override RewardType RewardType =>
        RewardType.Ball;

    public int ConversionCount =>
        conversionCount;

    public IReadOnlyList<BallDefinition>
        OneStarTraitBallDefinitions =>
            oneStarTraitBallDefinitions;

    public override bool CanApply(
        RewardApplyContext context)
    {
        if (context == null ||
            !context.HasBallCollection)
        {
            return false;
        }

        BallCollection ballCollection =
            context.BallCollection;

        if (!ballCollection.IsInitialized)
        {
            return false;
        }

        CollectValidTraitDefinitions();

        if (validTraitDefinitions.Count == 0)
        {
            return false;
        }

        return HasConvertibleBasicBall(
            ballCollection
        );
    }

    public override bool Apply(
        RewardApplyContext context)
    {
        if (context == null ||
            !context.HasBallCollection)
        {
            Debug.LogWarning(
                $"BasicBallConversionRewardDefinition: {name}에 " +
                "BallCollection이 전달되지 않았습니다.",
                this
            );

            return false;
        }

        BallCollection ballCollection =
            context.BallCollection;

        CollectValidTraitDefinitions();

        if (validTraitDefinitions.Count == 0)
        {
            Debug.LogWarning(
                $"BasicBallConversionRewardDefinition: {name}에 " +
                "유효한 1성 특성 공 Definition이 없습니다.",
                this
            );

            return false;
        }

        CollectConversionCandidates(
            ballCollection
        );

        if (conversionCandidates.Count == 0)
        {
            Debug.LogWarning(
                $"BasicBallConversionRewardDefinition: {name}을 " +
                "적용할 수 있는 1성 기본 공이 없습니다.",
                this
            );

            return false;
        }

        ShuffleConversionCandidates();

        int targetConversionCount =
            Mathf.Min(
                conversionCount,
                conversionCandidates.Count
            );

        int convertedCount = 0;

        for (int i = 0;
             i < targetConversionCount;
             i++)
        {
            Ball targetBall =
                conversionCandidates[i];

            BallDefinition targetDefinition =
                SelectWeightedTraitDefinition();

            if (TryConvertBall(
                    targetBall,
                    targetDefinition
                ))
            {
                convertedCount++;
            }
        }

        conversionCandidates.Clear();
        validTraitDefinitions.Clear();

        if (convertedCount <= 0)
        {
            Debug.LogWarning(
                $"BasicBallConversionRewardDefinition: {name}으로 " +
                "기본 공을 변환하지 못했습니다.",
                this
            );

            return false;
        }

        Debug.Log(
            "BasicBallConversionRewardDefinition: " +
            $"기본 공 {convertedCount}개 특성 변환 완료",
            this
        );

        return true;
    }

    private static bool HasConvertibleBasicBall(
        BallCollection ballCollection)
    {
        if (ballCollection == null)
        {
            return false;
        }

        IReadOnlyList<Ball> balls =
            ballCollection.Balls;

        if (balls == null)
        {
            return false;
        }

        for (int i = 0;
             i < balls.Count;
             i++)
        {
            if (IsConvertibleBasicBall(
                    balls[i]
                ))
            {
                return true;
            }
        }

        return false;
    }

    private void CollectConversionCandidates(
        BallCollection ballCollection)
    {
        conversionCandidates.Clear();

        if (ballCollection == null)
        {
            return;
        }

        IReadOnlyList<Ball> balls =
            ballCollection.Balls;

        if (balls == null)
        {
            return;
        }

        for (int i = 0;
             i < balls.Count;
             i++)
        {
            Ball ball =
                balls[i];

            if (!IsConvertibleBasicBall(
                    ball
                ))
            {
                continue;
            }

            conversionCandidates.Add(
                ball
            );
        }
    }

    private static bool IsConvertibleBasicBall(
        Ball ball)
    {
        if (ball == null)
        {
            return false;
        }

        BallDefinition definition =
            ball.Definition;

        if (definition == null)
        {
            return false;
        }

        return
            definition.TraitType ==
            BallTraitType.Basic &&
            definition.StarGrade ==
            BallStarGrade.OneStar;
    }

    private void CollectValidTraitDefinitions()
    {
        validTraitDefinitions.Clear();

        if (oneStarTraitBallDefinitions == null)
        {
            return;
        }

        for (int i = 0;
             i < oneStarTraitBallDefinitions.Count;
             i++)
        {
            BallDefinition definition =
                oneStarTraitBallDefinitions[i];

            if (!IsValidTraitDefinition(
                    definition
                ))
            {
                continue;
            }

            if (validTraitDefinitions.Contains(
                    definition
                ))
            {
                continue;
            }

            validTraitDefinitions.Add(
                definition
            );
        }
    }

    private static bool IsValidTraitDefinition(
        BallDefinition definition)
    {
        if (definition == null)
        {
            return false;
        }

        if (definition.StarGrade !=
            BallStarGrade.OneStar)
        {
            return false;
        }

        if (definition.TraitType ==
            BallTraitType.Basic)
        {
            return false;
        }

        if (definition.TraitType ==
            BallTraitType.Piercing)
        {
            return false;
        }

        return true;
    }

    private BallDefinition
        SelectWeightedTraitDefinition()
    {
        if (validTraitDefinitions.Count == 0)
        {
            return null;
        }

        int totalWeight = 0;

        for (int i = 0;
             i < validTraitDefinitions.Count;
             i++)
        {
            BallDefinition definition =
                validTraitDefinitions[i];

            if (definition == null)
            {
                continue;
            }

            totalWeight +=
                Mathf.Max(
                    definition.SelectionWeight,
                    0
                );
        }

        if (totalWeight <= 0)
        {
            int randomIndex =
                Random.Range(
                    0,
                    validTraitDefinitions.Count
                );

            return validTraitDefinitions[
                randomIndex
            ];
        }

        int randomValue =
            Random.Range(
                0,
                totalWeight
            );

        int accumulatedWeight = 0;

        for (int i = 0;
             i < validTraitDefinitions.Count;
             i++)
        {
            BallDefinition definition =
                validTraitDefinitions[i];

            if (definition == null)
            {
                continue;
            }

            accumulatedWeight +=
                Mathf.Max(
                    definition.SelectionWeight,
                    0
                );

            if (randomValue <
                accumulatedWeight)
            {
                return definition;
            }
        }

        return validTraitDefinitions[
            validTraitDefinitions.Count - 1
        ];
    }

    private static bool TryConvertBall(
        Ball ball,
        BallDefinition targetDefinition)
    {
        if (!IsConvertibleBasicBall(
                ball
            ) ||
            targetDefinition == null)
        {
            return false;
        }

        BallCombatController combatController =
            ball.CombatController;

        if (combatController == null)
        {
            combatController =
                ball.GetComponent<
                    BallCombatController
                >();
        }

        if (combatController == null)
        {
            return false;
        }

        BallDefinition previousDefinition =
            ball.Definition;

        combatController.ApplyDefinition(
            targetDefinition
        );

        bool converted =
            ball.Definition ==
            targetDefinition;

        if (!converted)
        {
            return false;
        }

        string previousName =
            previousDefinition != null
                ? previousDefinition.DisplayName
                : "기본 공";

        string targetName =
            targetDefinition.DisplayName;

        Debug.Log(
            "BasicBallConversionRewardDefinition: " +
            $"{ball.name} 변환, " +
            $"{previousName} → {targetName}",
            ball
        );

        return true;
    }

    private void ShuffleConversionCandidates()
    {
        for (int i =
                 conversionCandidates.Count - 1;
             i > 0;
             i--)
        {
            int randomIndex =
                Random.Range(
                    0,
                    i + 1
                );

            Ball temporary =
                conversionCandidates[i];

            conversionCandidates[i] =
                conversionCandidates[randomIndex];

            conversionCandidates[randomIndex] =
                temporary;
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        conversionCount =
            Mathf.Max(
                conversionCount,
                1
            );

        if (oneStarTraitBallDefinitions == null)
        {
            oneStarTraitBallDefinitions =
                new List<BallDefinition>();
        }

        for (int i =
                 oneStarTraitBallDefinitions.Count - 1;
             i >= 0;
             i--)
        {
            BallDefinition definition =
                oneStarTraitBallDefinitions[i];

            if (definition == null)
            {
                continue;
            }

            if (IsValidTraitDefinition(
                    definition
                ))
            {
                continue;
            }

            Debug.LogWarning(
                $"BasicBallConversionRewardDefinition: {name}의 " +
                $"{definition.name}은 유효한 1성 특성 공이 아닙니다.",
                this
            );
        }
    }
}