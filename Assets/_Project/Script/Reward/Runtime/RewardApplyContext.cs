public sealed class RewardApplyContext
{
    public RewardApplyContext(
        BallCollection ballCollection)
        : this(
            ballCollection,
            null
        )
    {
    }

    public RewardApplyContext(
        BallCollection ballCollection,
        RunAugmentState runAugmentState)
    {
        BallCollection =
            ballCollection;

        RunAugmentState =
            runAugmentState;
    }

    public BallCollection BallCollection
    {
        get;
    }

    public RunAugmentState RunAugmentState
    {
        get;
    }

    public bool HasBallCollection =>
        BallCollection != null;

    public bool HasRunAugmentState =>
        RunAugmentState != null;
}