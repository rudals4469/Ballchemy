using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "UnknownEvent_DisassembleReassemble",
    menuName =
        "Ballchemy/Events/Unknown/Alchemy/Disassemble And Reassemble"
)]
public sealed class
    DisassembleReassembleUnknownEventDefinition :
        UnknownEventDefinition
{
    [Header("Disassemble And Reassemble")]

    [Tooltip(
        "재조립 재료로 제거할 무작위 공 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int ballsToRemove = 12;

    [Tooltip(
        "재조립 결과로 지급할 고등급 공 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int highGradeBallsToAdd = 4;

    [Tooltip(
        "이벤트 적용 후 반드시 남겨둘 최소 공 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int minimumRemainingBalls = 1;

    [Tooltip(
        "재조립 결과로 지급할 수 있는 " +
        "2성 또는 3성 공 Definition 목록입니다.\n" +
        "관통 공은 등록하지 않습니다."
    )]
    [SerializeField]
    private List<BallDefinition>
        highGradeBallDefinitions =
            new List<BallDefinition>();

    private readonly List<BallDefinition>
        validHighGradeDefinitions =
            new List<BallDefinition>();

    public int BallsToRemove =>
        ballsToRemove;

    public int HighGradeBallsToAdd =>
        highGradeBallsToAdd;

    public int MinimumRemainingBalls =>
        minimumRemainingBalls;

    public IReadOnlyList<BallDefinition>
        HighGradeBallDefinitions =>
            highGradeBallDefinitions;

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

        CollectValidHighGradeDefinitions();

        if (validHighGradeDefinitions.Count == 0)
        {
            return false;
        }

        int removableBallCount =
            ballCollection.CountMatchingBalls(
                IsRemovableBall
            );

        if (removableBallCount <
            ballsToRemove)
        {
            return false;
        }

        int remainingBallCount =
            ballCollection.Count -
            ballsToRemove;

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
                "분해와 재조립을 적용할 수 없습니다."
            );
        }

        BallCollection ballCollection =
            context.BallCollection;

        int removedCount =
            ballCollection.RemoveRandomBalls(
                ballsToRemove,
                IsRemovableBall,
                minimumRemainingBalls
            );

        if (removedCount !=
            ballsToRemove)
        {
            Debug.LogError(
                "DisassembleReassembleUnknownEventDefinition: " +
                "공 제거 결과가 예상과 다릅니다. " +
                $"예상={ballsToRemove}, " +
                $"실제={removedCount}",
                this
            );

            return new UnknownEventResult(
                this,
                false,
                $"공 {removedCount}개만 분해되어 " +
                "재조립을 완료하지 못했습니다."
            );
        }

        int addedCount = 0;

        for (int i = 0;
             i < highGradeBallsToAdd;
             i++)
        {
            BallDefinition selectedDefinition =
                SelectRandomHighGradeDefinition();

            if (selectedDefinition == null)
            {
                Debug.LogError(
                    "DisassembleReassembleUnknownEventDefinition: " +
                    "지급할 고등급 공을 추첨하지 못했습니다.",
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
            highGradeBallsToAdd)
        {
            Debug.LogError(
                "DisassembleReassembleUnknownEventDefinition: " +
                "고등급 공 지급 결과가 예상과 다릅니다. " +
                $"예상={highGradeBallsToAdd}, " +
                $"실제={addedCount}",
                this
            );

            return new UnknownEventResult(
                this,
                false,
                $"고등급 공 {addedCount}개만 생성되어 " +
                "재조립을 완료하지 못했습니다."
            );
        }

        string resultText =
            $"공 {removedCount}개를 분해해 " +
            $"무작위 고등급 공 {addedCount}개로 " +
            "재조립했습니다.";

        Debug.Log(
            "DisassembleReassembleUnknownEventDefinition: " +
            resultText,
            this
        );

        return new UnknownEventResult(
            this,
            true,
            resultText
        );
    }

    private static bool IsRemovableBall(
        Ball ball)
    {
        if (ball == null ||
            ball.Definition == null)
        {
            return false;
        }

        /*
         * 관통 공은 현재 일반 공 구성 변경 풀에서
         * 제외하고 있으므로 분해 대상에서도 제외합니다.
         */
        return ball.TraitType !=
               BallTraitType.Piercing;
    }

    private void CollectValidHighGradeDefinitions()
    {
        validHighGradeDefinitions.Clear();

        if (highGradeBallDefinitions == null)
        {
            return;
        }

        HashSet<BallDefinition> unique =
            new HashSet<BallDefinition>();

        for (int i = 0;
             i < highGradeBallDefinitions.Count;
             i++)
        {
            BallDefinition definition =
                highGradeBallDefinitions[i];

            if (!IsValidHighGradeDefinition(
                    definition
                ) ||
                !unique.Add(
                    definition
                ))
            {
                continue;
            }

            validHighGradeDefinitions.Add(
                definition
            );
        }
    }

    private static bool IsValidHighGradeDefinition(
        BallDefinition definition)
    {
        if (definition == null)
        {
            return false;
        }

        bool isHighGrade =
            definition.StarGrade ==
                BallStarGrade.TwoStar ||
            definition.StarGrade ==
                BallStarGrade.ThreeStar;

        if (!isHighGrade)
        {
            return false;
        }

        if (definition.TraitType ==
            BallTraitType.Piercing)
        {
            return false;
        }

        /*
         * Selection Weight가 0인 공은
         * 일반 선택 풀과 동일하게 비활성 후보로 취급합니다.
         *
         * 후보들 사이의 실제 추첨 확률은 균등합니다.
         */
        return definition.SelectionWeight > 0;
    }

    private BallDefinition
        SelectRandomHighGradeDefinition()
    {
        if (validHighGradeDefinitions.Count == 0)
        {
            return null;
        }

        int randomIndex =
            Random.Range(
                0,
                validHighGradeDefinitions.Count
            );

        return validHighGradeDefinitions[
            randomIndex
        ];
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        ballsToRemove =
            Mathf.Max(
                ballsToRemove,
                1
            );

        highGradeBallsToAdd =
            Mathf.Max(
                highGradeBallsToAdd,
                1
            );

        minimumRemainingBalls =
            Mathf.Max(
                minimumRemainingBalls,
                1
            );

        if (highGradeBallDefinitions == null)
        {
            highGradeBallDefinitions =
                new List<BallDefinition>();

            return;
        }

        HashSet<BallDefinition> unique =
            new HashSet<BallDefinition>();

        for (int i =
                 highGradeBallDefinitions.Count - 1;
             i >= 0;
             i--)
        {
            BallDefinition definition =
                highGradeBallDefinitions[i];

            if (definition == null)
            {
                highGradeBallDefinitions.RemoveAt(
                    i
                );

                continue;
            }

            if (!unique.Add(
                    definition
                ))
            {
                Debug.LogWarning(
                    "DisassembleReassembleUnknownEventDefinition: " +
                    $"{definition.name}이 중복 등록되어 제거됩니다.",
                    this
                );

                highGradeBallDefinitions.RemoveAt(
                    i
                );

                continue;
            }

            if (IsValidHighGradeDefinition(
                    definition
                ))
            {
                continue;
            }

            Debug.LogWarning(
                "DisassembleReassembleUnknownEventDefinition: " +
                $"{definition.name}은 선택 가능한 " +
                "2성 또는 3성 공이 아닙니다.",
                this
            );
        }
    }
}