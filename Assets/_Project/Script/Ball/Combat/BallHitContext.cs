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

    public int DirectDamage
    {
        get;
    }

    public float CriticalDamageMultiplierBonus
    {
        get;
    }

    public BallHitContext(
        Ball ball,
        Block block,
        BallDefinition definition,
        Vector2 hitPoint,
        Vector2 incomingVelocity,
        int directDamage,
        float criticalDamageMultiplierBonus)
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

        DirectDamage =
            Mathf.Max(
                directDamage,
                1
            );

        CriticalDamageMultiplierBonus =
            Mathf.Max(
                criticalDamageMultiplierBonus,
                0f
            );
    }
}