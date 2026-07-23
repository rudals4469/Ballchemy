using UnityEngine;

public sealed class BlockGridMoveTarget
{
    public Block Block { get; }

    public Vector3 StartPosition { get; }

    public Vector3 TargetPosition { get; }

    public BlockGridMoveTarget(
        Block block,
        Vector3 startPosition,
        Vector3 targetPosition)
    {
        Block =
            block;

        StartPosition =
            startPosition;

        TargetPosition =
            targetPosition;
    }
}