using UnityEngine;

public readonly struct BallDamageEvent
{
    public Ball SourceBall
    {
        get;
    }

    public BallDefinition SourceDefinition
    {
        get;
    }

    public Block Target
    {
        get;
    }

    public int Damage
    {
        get;
    }

    public Vector2 HitPoint
    {
        get;
    }

    public BallDamageTextStyleDefinition Style
    {
        get;
    }

    public BallDamageEvent(
        Ball sourceBall,
        BallDefinition sourceDefinition,
        Block target,
        int damage,
        Vector2 hitPoint,
        BallDamageTextStyleDefinition style)
    {
        SourceBall =
            sourceBall;

        SourceDefinition =
            sourceDefinition;

        Target =
            target;

        Damage =
            Mathf.Max(
                damage,
                0
            );

        HitPoint =
            hitPoint;

        Style =
            style;
    }
}