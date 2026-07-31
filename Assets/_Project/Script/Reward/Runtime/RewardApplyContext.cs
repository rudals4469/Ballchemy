public sealed class RewardApplyContext
{
    public RewardApplyContext(
        BallCollection ballCollection)
    {
        BallCollection =
            ballCollection;
    }

    public BallCollection BallCollection
    {
        get;
    }

    public bool HasBallCollection =>
        BallCollection != null;
}