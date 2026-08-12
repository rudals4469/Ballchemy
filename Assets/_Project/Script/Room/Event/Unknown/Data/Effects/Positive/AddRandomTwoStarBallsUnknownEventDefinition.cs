using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "UnknownEvent_AddRandomTwoStarBalls",
    menuName =
        "Ballchemy/Events/Unknown/Positive/Add Random Two Star Balls"
)]
public sealed class
    AddRandomTwoStarBallsUnknownEventDefinition :
        UnknownEventDefinition
{
    [Header("Add Random Two Star Balls")]

    [Tooltip(
        "이 결과로 지급할 2성 공 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int amount = 2;

    [Tooltip(
        "지급 후보로 사용할 2성 공 Definition 목록입니다.\n" +
        "2성이 아닌 공과 관통 공은 등록하지 않습니다."
    )]
    [SerializeField]
    private List<BallDefinition>
        twoStarBallDefinitions =
            new List<BallDefinition>();

    private readonly List<BallDefinition>
        validTwoStarDefinitions =
            new List<BallDefinition>();

    public int Amount =>
        amount;

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

        if (!ballCollection.IsInitialized)
        {
            return false;
        }

        CollectValidTwoStarDefinitions();

        return validTwoStarDefinitions.Count > 0;
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
                "무작위 2성 공을 지급할 수 없습니다."
            );
        }

        BallCollection ballCollection =
            context.BallCollection;

        int addedCount = 0;

        for (int i = 0;
             i < amount;
             i++)
        {
            BallDefinition selectedDefinition =
                SelectRandomTwoStarDefinition();

            if (selectedDefinition == null)
            {
                Debug.LogError(
                    "AddRandomTwoStarBallsUnknownEventDefinition: " +
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

        if (addedCount != amount)
        {
            Debug.LogError(
                "AddRandomTwoStarBallsUnknownEventDefinition: " +
                "2성 공 지급 결과가 예상과 다릅니다. " +
                $"예상={amount}, 실제={addedCount}",
                this
            );

            return new UnknownEventResult(
                this,
                false,
                $"무작위 2성 공 {addedCount}개만 지급되었습니다."
            );
        }

        string resultText =
            $"무작위 2성 공을 {addedCount}개 획득했습니다.";

        Debug.Log(
            "AddRandomTwoStarBallsUnknownEventDefinition: " +
            resultText,
            this
        );

        return new UnknownEventResult(
            this,
            true,
            resultText
        );
    }

    private void CollectValidTwoStarDefinitions()
    {
        validTwoStarDefinitions.Clear();

        if (twoStarBallDefinitions == null)
        {
            return;
        }

        HashSet<BallDefinition> uniqueDefinitions =
            new HashSet<BallDefinition>();

        for (int i = 0;
             i < twoStarBallDefinitions.Count;
             i++)
        {
            BallDefinition definition =
                twoStarBallDefinitions[i];

            if (!IsValidTwoStarDefinition(
                    definition
                ))
            {
                continue;
            }

            if (!uniqueDefinitions.Add(
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

        if (BallPoolPolicy
            .IsRemovedFromPlayerPool(
                definition
            ))
        {
            return false;
        }

        return definition.SelectionWeight > 0;
    }

    private BallDefinition
        SelectRandomTwoStarDefinition()
    {
        if (validTwoStarDefinitions.Count == 0)
        {
            return null;
        }

        int randomIndex =
            Random.Range(
                0,
                validTwoStarDefinitions.Count
            );

        return validTwoStarDefinitions[
            randomIndex
        ];
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        amount =
            Mathf.Max(
                amount,
                1
            );

        if (twoStarBallDefinitions == null)
        {
            twoStarBallDefinitions =
                new List<BallDefinition>();

            return;
        }

        HashSet<BallDefinition> uniqueDefinitions =
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

            if (!uniqueDefinitions.Add(
                    definition
                ))
            {
                Debug.LogWarning(
                    "AddRandomTwoStarBallsUnknownEventDefinition: " +
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
                "AddRandomTwoStarBallsUnknownEventDefinition: " +
                $"{definition.name}은 유효한 2성 공이 아닙니다.",
                this
            );
        }
    }
}
