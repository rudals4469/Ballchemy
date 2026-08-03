using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "UnknownEvent_Concentration",
    menuName =
        "Ballchemy/Events/Unknown/Alchemy/Concentration"
)]
public sealed class
    ConcentrationUnknownEventDefinition :
        UnknownEventDefinition
{
    [Header("Concentration")]

    [Tooltip(
        "농축 재료로 제거할 무작위 1성 공 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int oneStarBallsToRemove = 10;

    [Tooltip(
        "농축 결과로 지급할 2성 공 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int twoStarBallsToAdd = 5;

    [Tooltip(
        "이벤트 적용 후 반드시 남겨둘 최소 공 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int minimumRemainingBalls = 1;

    [Tooltip(
        "농축 결과로 지급할 수 있는 " +
        "2성 공 Definition 목록입니다.\n" +
        "2성이 아닌 공과 관통 공은 등록하지 않습니다."
    )]
    [SerializeField]
    private List<BallDefinition>
        twoStarBallDefinitions =
            new List<BallDefinition>();

    private readonly List<BallDefinition>
        validTwoStarDefinitions =
            new List<BallDefinition>();

    public int OneStarBallsToRemove =>
        oneStarBallsToRemove;

    public int TwoStarBallsToAdd =>
        twoStarBallsToAdd;

    public int MinimumRemainingBalls =>
        minimumRemainingBalls;

    public IReadOnlyList<BallDefinition>
        TwoStarBallDefinitions =>
            twoStarBallDefinitions;

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

        CollectValidTwoStarDefinitions();

        if (validTwoStarDefinitions.Count == 0)
        {
            return false;
        }

        int oneStarBallCount =
            ballCollection.CountMatchingBalls(
                IsRemovableOneStarBall
            );

        if (oneStarBallCount <
            oneStarBallsToRemove)
        {
            return false;
        }

        int remainingBallCount =
            ballCollection.Count -
            oneStarBallsToRemove;

        return remainingBallCount >=
               minimumRemainingBalls;
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
                "농축을 적용할 수 없습니다."
            );
        }

        BallCollection ballCollection =
            context.BallCollection;

        int removedCount =
            ballCollection.RemoveRandomBalls(
                oneStarBallsToRemove,
                IsRemovableOneStarBall,
                minimumRemainingBalls
            );

        if (removedCount !=
            oneStarBallsToRemove)
        {
            Debug.LogError(
                "ConcentrationUnknownEventDefinition: " +
                "1성 공 제거 결과가 예상과 다릅니다. " +
                $"예상={oneStarBallsToRemove}, " +
                $"실제={removedCount}",
                this
            );

            return new UnknownEventResult(
                this,
                false,
                $"1성 공 {removedCount}개만 제거되어 " +
                "농축을 완료하지 못했습니다."
            );
        }

        int addedCount = 0;

        for (int i = 0;
             i < twoStarBallsToAdd;
             i++)
        {
            BallDefinition selectedDefinition =
                SelectWeightedTwoStarDefinition();

            if (selectedDefinition == null)
            {
                Debug.LogError(
                    "ConcentrationUnknownEventDefinition: " +
                    "지급할 2성 공을 추첨하지 못했습니다.",
                    this
                );

                break;
            }

            int currentAddedCount =
                ballCollection.AddBalls(
                    1,
                    selectedDefinition
                );

            addedCount +=
                currentAddedCount;
        }

        if (addedCount !=
            twoStarBallsToAdd)
        {
            Debug.LogError(
                "ConcentrationUnknownEventDefinition: " +
                "2성 공 지급 결과가 예상과 다릅니다. " +
                $"예상={twoStarBallsToAdd}, " +
                $"실제={addedCount}",
                this
            );

            return new UnknownEventResult(
                this,
                false,
                $"2성 공 {addedCount}개만 생성되어 " +
                "농축을 완료하지 못했습니다."
            );
        }

        string resultText =
            $"1성 공 {removedCount}개를 농축해 " +
            $"무작위 2성 공 {addedCount}개를 획득했습니다.";

        Debug.Log(
            "ConcentrationUnknownEventDefinition: " +
            resultText,
            this
        );

        return new UnknownEventResult(
            this,
            true,
            resultText
        );
    }

    private static bool IsRemovableOneStarBall(
        Ball ball)
    {
        if (ball == null ||
            ball.Definition == null)
        {
            return false;
        }

        return
            ball.StarGrade ==
                BallStarGrade.OneStar &&
            ball.TraitType !=
                BallTraitType.Piercing;
    }

    private void CollectValidTwoStarDefinitions()
    {
        validTwoStarDefinitions.Clear();

        if (twoStarBallDefinitions == null)
        {
            return;
        }

        HashSet<BallDefinition> unique =
            new HashSet<BallDefinition>();

        for (int i = 0;
             i < twoStarBallDefinitions.Count;
             i++)
        {
            BallDefinition definition =
                twoStarBallDefinitions[i];

            if (!IsValidTwoStarDefinition(
                    definition
                ) ||
                !unique.Add(
                    definition
                ))
            {
                continue;
            }

            validTwoStarDefinitions.Add(
                definition
            );
        }
    }

    private static bool IsValidTwoStarDefinition(
        BallDefinition definition)
    {
        if (definition == null)
        {
            return false;
        }

        if (definition.StarGrade !=
            BallStarGrade.TwoStar)
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
        SelectWeightedTwoStarDefinition()
    {
        if (validTwoStarDefinitions.Count == 0)
        {
            return null;
        }

        int totalWeight = 0;

        for (int i = 0;
             i < validTwoStarDefinitions.Count;
             i++)
        {
            BallDefinition definition =
                validTwoStarDefinitions[i];

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
             i < validTwoStarDefinitions.Count;
             i++)
        {
            BallDefinition definition =
                validTwoStarDefinitions[i];

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

        return validTwoStarDefinitions[
            validTwoStarDefinitions.Count - 1
        ];
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        oneStarBallsToRemove =
            Mathf.Max(
                oneStarBallsToRemove,
                1
            );

        twoStarBallsToAdd =
            Mathf.Max(
                twoStarBallsToAdd,
                1
            );

        minimumRemainingBalls =
            Mathf.Max(
                minimumRemainingBalls,
                1
            );

        if (twoStarBallDefinitions == null)
        {
            twoStarBallDefinitions =
                new List<BallDefinition>();

            return;
        }

        HashSet<BallDefinition> unique =
            new HashSet<BallDefinition>();

        for (int i =
                 twoStarBallDefinitions.Count - 1;
             i >= 0;
             i--)
        {
            BallDefinition definition =
                twoStarBallDefinitions[i];

            if (definition == null)
            {
                twoStarBallDefinitions.RemoveAt(
                    i
                );

                continue;
            }

            if (!unique.Add(
                    definition
                ))
            {
                Debug.LogWarning(
                    "ConcentrationUnknownEventDefinition: " +
                    $"{definition.name}이 중복 등록되어 제거됩니다.",
                    this
                );

                twoStarBallDefinitions.RemoveAt(
                    i
                );

                continue;
            }

            if (IsValidTwoStarDefinition(
                    definition
                ))
            {
                continue;
            }

            Debug.LogWarning(
                "ConcentrationUnknownEventDefinition: " +
                $"{definition.name}은 선택 가능한 " +
                "2성 공이 아닙니다.",
                this
            );
        }
    }
}