using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "UnknownEvent_HomogeneousConversion",
    menuName =
        "Ballchemy/Events/Unknown/Alchemy/Homogeneous Conversion"
)]
public sealed class
    HomogeneousConversionUnknownEventDefinition :
        UnknownEventDefinition
{
    [Header("Homogeneous Conversion")]

    [Tooltip(
        "동일한 특성 공으로 변환할 " +
        "1성 기본 공의 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int conversionCount = 8;

    [Tooltip(
        "변환 결과로 사용할 수 있는 " +
        "1성 특성 공 Definition 목록입니다.\n" +
        "기본 공과 관통 공은 등록하지 않습니다."
    )]
    [SerializeField]
    private List<BallDefinition>
        oneStarTraitBallDefinitions =
            new List<BallDefinition>();

    private readonly List<BallDefinition>
        validTargetDefinitions =
            new List<BallDefinition>();

    public int ConversionCount =>
        conversionCount;

    public IReadOnlyList<BallDefinition>
        OneStarTraitBallDefinitions =>
            oneStarTraitBallDefinitions;

    public override bool CanApply(
        UnknownEventApplyContext context)
    {
        if (context == null ||
            !context.HasBallCollection)
        {
            return false;
        }

        BallCollection ballCollection =
            context.BallCollection;

        if (!ballCollection.CanModifyBallComposition)
        {
            return false;
        }

        CollectValidTargetDefinitions();

        if (validTargetDefinitions.Count == 0)
        {
            return false;
        }

        int convertibleCount =
            ballCollection.CountMatchingBalls(
                IsConvertibleBasicBall
            );

        return convertibleCount >=
               conversionCount;
    }

    public override UnknownEventResult Apply(
        UnknownEventApplyContext context)
    {
        if (!CanApply(
                context
            ))
        {
            return new UnknownEventResult(
                this,
                false,
                "동질 변환을 적용할 수 없습니다."
            );
        }

        BallDefinition selectedDefinition =
            SelectWeightedTargetDefinition();

        if (selectedDefinition == null)
        {
            return new UnknownEventResult(
                this,
                false,
                "변환할 특성 공을 선택하지 못했습니다."
            );
        }

        BallCollection ballCollection =
            context.BallCollection;

        int convertedCount =
            ballCollection
                .ReplaceRandomBallDefinitions(
                    conversionCount,
                    selectedDefinition,
                    IsConvertibleBasicBall
                );

        if (convertedCount !=
            conversionCount)
        {
            Debug.LogError(
                "HomogeneousConversionUnknownEventDefinition: " +
                "공 변환 결과가 예상과 다릅니다. " +
                $"예상={conversionCount}, " +
                $"실제={convertedCount}",
                this
            );

            return new UnknownEventResult(
                this,
                false,
                $"기본 공 {convertedCount}개만 변환되어 " +
                "동질 변환을 완료하지 못했습니다."
            );
        }

        string targetName =
            !string.IsNullOrWhiteSpace(
                selectedDefinition.DisplayName
            )
                ? selectedDefinition.DisplayName
                : selectedDefinition.name;

        string resultText =
            $"1성 기본 공 {convertedCount}개가 " +
            $"{targetName}(으)로 동질 변환되었습니다.";

        Debug.Log(
            "HomogeneousConversionUnknownEventDefinition: " +
            resultText,
            this
        );

        return new UnknownEventResult(
            this,
            true,
            resultText
        );
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
            definition.StarGrade ==
                BallStarGrade.OneStar &&
            definition.TraitType ==
                BallTraitType.Basic;
    }

    private void CollectValidTargetDefinitions()
    {
        validTargetDefinitions.Clear();

        if (oneStarTraitBallDefinitions == null)
        {
            return;
        }

        HashSet<BallDefinition> unique =
            new HashSet<BallDefinition>();

        for (int i = 0;
             i < oneStarTraitBallDefinitions.Count;
             i++)
        {
            BallDefinition definition =
                oneStarTraitBallDefinitions[i];

            if (!IsValidTargetDefinition(
                    definition
                ) ||
                !unique.Add(
                    definition
                ))
            {
                continue;
            }

            validTargetDefinitions.Add(
                definition
            );
        }
    }

    private static bool IsValidTargetDefinition(
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

        return definition.SelectionWeight > 0;
    }

    private BallDefinition
        SelectWeightedTargetDefinition()
    {
        if (validTargetDefinitions.Count == 0)
        {
            return null;
        }

        int totalWeight = 0;

        for (int i = 0;
             i < validTargetDefinitions.Count;
             i++)
        {
            BallDefinition definition =
                validTargetDefinitions[i];

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
            return null;
        }

        int randomValue =
            Random.Range(
                0,
                totalWeight
            );

        int accumulatedWeight = 0;

        for (int i = 0;
             i < validTargetDefinitions.Count;
             i++)
        {
            BallDefinition definition =
                validTargetDefinitions[i];

            if (definition == null)
            {
                continue;
            }

            accumulatedWeight +=
                Mathf.Max(
                    definition.SelectionWeight,
                    0
                );

            if (randomValue >=
                accumulatedWeight)
            {
                continue;
            }

            return definition;
        }

        return validTargetDefinitions[
            validTargetDefinitions.Count - 1
        ];
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

            return;
        }

        HashSet<BallDefinition> unique =
            new HashSet<BallDefinition>();

        for (int i =
                 oneStarTraitBallDefinitions.Count - 1;
             i >= 0;
             i--)
        {
            BallDefinition definition =
                oneStarTraitBallDefinitions[i];

            if (definition == null)
            {
                oneStarTraitBallDefinitions.RemoveAt(
                    i
                );

                continue;
            }

            if (!unique.Add(
                    definition
                ))
            {
                Debug.LogWarning(
                    "HomogeneousConversionUnknownEventDefinition: " +
                    $"{definition.name}이 중복 등록되어 제거됩니다.",
                    this
                );

                oneStarTraitBallDefinitions.RemoveAt(
                    i
                );

                continue;
            }

            if (IsValidTargetDefinition(
                    definition
                ))
            {
                continue;
            }

            Debug.LogWarning(
                "HomogeneousConversionUnknownEventDefinition: " +
                $"{definition.name}은 선택 가능한 " +
                "1성 특성 공이 아닙니다.",
                this
            );
        }
    }
}