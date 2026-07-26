public readonly struct BallHitResult
{
    public bool WasHandled
    {
        get;
    }

    public bool ShouldBounce
    {
        get;
    }

    public BallHitResult(
        bool wasHandled,
        bool shouldBounce)
    {
        WasHandled =
            wasHandled;

        ShouldBounce =
            shouldBounce;
    }

    public static BallHitResult HandledWithBounce()
    {
        return new BallHitResult(
            true,
            true
        );
    }

    public static BallHitResult HandledWithoutBounce()
    {
        return new BallHitResult(
            true,
            false
        );
    }

    public static BallHitResult NotHandled()
    {
        return new BallHitResult(
            false,
            true
        );
    }
}