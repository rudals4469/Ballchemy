using UnityEngine;

[CreateAssetMenu(
    fileName = "UnknownEvent_RemoveRandomBalls",
    menuName =
        "Ballchemy/Events/Unknown/Negative/Remove Random Balls"
)]
public sealed class
    RemoveRandomBallsUnknownEventDefinition :
        UnknownEventDefinition
{
    [Header("Remove Random Balls")]

    [Tooltip(
        "무작위로 제거할 공 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int removeCount = 6;

    [Tooltip(
        "결과 적용 후 반드시 남겨둘 최소 공 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int minimumRemainingBalls = 1;

    [Tooltip(
        "관통 공을 제거 대상에서 제외할지 결정합니다."
    )]
    [SerializeField]
    private bool excludePiercingBalls = true;

    public int RemoveCount =>
        removeCount;

    public int MinimumRemainingBalls =>
        minimumRemainingBalls;

    public bool ExcludePiercingBalls =>
        excludePiercingBalls;

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

        int removableCount =
            ballCollection.CountMatchingBalls(
                IsRemovableBall
            );

        if (removableCount <
            removeCount)
        {
            return false;
        }

        int remainingBallCount =
            ballCollection.Count -
            removeCount;

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
                "무작위 공 제거를 적용할 수 없습니다."
            );
        }

        BallCollection ballCollection =
            context.BallCollection;

        int removedCount =
            ballCollection.RemoveRandomBalls(
                removeCount,
                IsRemovableBall,
                minimumRemainingBalls
            );

        if (removedCount !=
            removeCount)
        {
            Debug.LogError(
                "RemoveRandomBallsUnknownEventDefinition: " +
                "공 제거 결과가 예상과 다릅니다. " +
                $"예상={removeCount}, 실제={removedCount}",
                this
            );

            return new UnknownEventResult(
                this,
                false,
                $"무작위 공 {removedCount}개만 제거되었습니다."
            );
        }

        string resultText =
            $"무작위 공 {removedCount}개를 잃었습니다.";

        Debug.Log(
            "RemoveRandomBallsUnknownEventDefinition: " +
            resultText,
            this
        );

        return new UnknownEventResult(
            this,
            true,
            resultText
        );
    }

    private bool IsRemovableBall(
        Ball ball)
    {
        if (ball == null ||
            ball.Definition == null)
        {
            return false;
        }

        if (excludePiercingBalls &&
            ball.TraitType ==
                BallTraitType.Piercing)
        {
            return false;
        }

        return true;
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        removeCount =
            Mathf.Max(
                removeCount,
                1
            );

        minimumRemainingBalls =
            Mathf.Max(
                minimumRemainingBalls,
                1
            );
    }
}