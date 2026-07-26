using UnityEngine;

public sealed class BallHitContext
{
    public Ball Ball
    {
        get;
    }

    public Block Block
    {
        get;
    }

    public BallDefinition Definition
    {
        get;
    }

    public Vector2 HitPoint
    {
        get;
    }

    public Vector2 IncomingVelocity
    {
        get;
    }

    public int BaseDamage
    {
        get;
    }

    public BallHitContext(
        Ball ball,
        Block block,
        BallDefinition definition,
        Vector2 hitPoint,
        Vector2 incomingVelocity,
        int baseDamage)
    {
        Ball =
            ball;

        Block =
            block;

        Definition =
            definition;

        HitPoint =
            hitPoint;

        IncomingVelocity =
            incomingVelocity;

        BaseDamage =
            Mathf.Max(
                baseDamage,
                1
            );
    }
}