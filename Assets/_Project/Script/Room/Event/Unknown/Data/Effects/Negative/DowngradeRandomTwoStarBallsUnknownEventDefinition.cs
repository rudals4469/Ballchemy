using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName =
        "UnknownEvent_DowngradeRandomTwoStarBalls",
    menuName =
        "Ballchemy/Events/Unknown/Negative/" +
        "Downgrade Random Two Star Balls"
)]
public sealed class
    DowngradeRandomTwoStarBallsUnknownEventDefinition :
        UnknownEventDefinition
{
    [Header("Downgrade Random Two Star Balls")]

    [Tooltip(
        "무작위로 강등할 2성 공의 최대 개수입니다.\n" +
        "보유한 강등 가능 2성 공이 이보다 적으면 " +
        "실제 보유 수만큼 강등합니다."
    )]
    [SerializeField, Min(1)]
    private int downgradeCount = 4;

    private readonly List<Ball>
        validCandidates =
            new List<Ball>();

    public int DowngradeCount =>
        downgradeCount;

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

        CollectValidCandidates(
            ballCollection
        );

        return validCandidates.Count > 0;
    }

    public override UnknownEventResult Apply(
        UnknownEventApplyContext context)
    {
        if (!CanApply(
                context
            ))
        {
            validCandidates.Clear();

            return new UnknownEventResult(
                this,
                false,
                "강등할 수 있는 2성 공이 없습니다."
            );
        }

        BallCollection ballCollection =
            context.BallCollection;

        ShuffleCandidates();

        int targetCount =
            Mathf.Min(
                downgradeCount,
                validCandidates.Count
            );

        int downgradedCount = 0;

        for (int i = 0;
             i < targetCount;
             i++)
        {
            Ball targetBall =
                validCandidates[i];

            if (!IsValidCandidate(
                    targetBall
                ))
            {
                continue;
            }

            BallDefinition previousDefinition =
                targetBall
                    .Definition
                    .PreviousStarDefinition;

            if (previousDefinition == null)
            {
                continue;
            }

            /*
             * BallCollection의 기존 교체 API를 사용합니다.
             *
             * predicate에서 정확히 현재 targetBall만
             * 허용하므로, 무작위 재선택 없이 해당 공 하나만
             * 이전 등급 Definition으로 교체됩니다.
             */
            int currentReplacedCount =
                ballCollection
                    .ReplaceRandomBallDefinitions(
                        1,
                        previousDefinition,
                        ball =>
                            ball == targetBall
                    );

            if (currentReplacedCount <= 0)
            {
                continue;
            }

            downgradedCount +=
                currentReplacedCount;
        }

        validCandidates.Clear();

        if (downgradedCount <= 0)
        {
            return new UnknownEventResult(
                this,
                false,
                "2성 공을 강등하지 못했습니다."
            );
        }

        string resultText =
            downgradedCount == 1
                ? "무작위 2성 공 1개가 " +
                  "같은 종류의 1성 공으로 강등되었습니다."
                : $"무작위 2성 공 {downgradedCount}개가 " +
                  "같은 종류의 1성 공으로 강등되었습니다.";

        Debug.Log(
            "DowngradeRandomTwoStarBallsUnknownEventDefinition: " +
            resultText,
            this
        );

        return new UnknownEventResult(
            this,
            true,
            resultText
        );
    }

    private void CollectValidCandidates(
        BallCollection ballCollection)
    {
        validCandidates.Clear();

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

            if (!IsValidCandidate(
                    ball
                ))
            {
                continue;
            }

            validCandidates.Add(
                ball
            );
        }
    }

    private static bool IsValidCandidate(
        Ball ball)
    {
        if (ball == null ||
            ball.IsMoving ||
            ball.Definition == null)
        {
            return false;
        }

        BallDefinition definition =
            ball.Definition;

        if (definition.StarGrade !=
            BallStarGrade.TwoStar)
        {
            return false;
        }

        if (!definition.CanDowngrade ||
            definition.PreviousStarDefinition == null)
        {
            return false;
        }

        if (definition.PreviousStarDefinition.StarGrade !=
            BallStarGrade.OneStar)
        {
            return false;
        }

        /*
         * 같은 종류의 공으로 강등되는지 확인합니다.
         */
        return definition.TraitType ==
               definition
                   .PreviousStarDefinition
                   .TraitType;
    }

    private void ShuffleCandidates()
    {
        for (int i = validCandidates.Count - 1;
             i > 0;
             i--)
        {
            int randomIndex =
                Random.Range(
                    0,
                    i + 1
                );

            Ball temporary =
                validCandidates[i];

            validCandidates[i] =
                validCandidates[randomIndex];

            validCandidates[randomIndex] =
                temporary;
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        downgradeCount =
            Mathf.Max(
                downgradeCount,
                1
            );
    }
}