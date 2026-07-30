using UnityEngine;

public enum RoomDirection
{
    Up,
    Right,
    Down,
    Left
}

public static class RoomDirectionUtility
{
    public static Vector2Int ToOffset(
        RoomDirection direction)
    {
        switch (direction)
        {
            case RoomDirection.Up:
                return Vector2Int.up;

            case RoomDirection.Right:
                return Vector2Int.right;

            case RoomDirection.Down:
                return Vector2Int.down;

            case RoomDirection.Left:
                return Vector2Int.left;

            default:
                return Vector2Int.zero;
        }
    }

    public static RoomDirection GetOpposite(
        RoomDirection direction)
    {
        switch (direction)
        {
            case RoomDirection.Up:
                return RoomDirection.Down;

            case RoomDirection.Right:
                return RoomDirection.Left;

            case RoomDirection.Down:
                return RoomDirection.Up;

            case RoomDirection.Left:
                return RoomDirection.Right;

            default:
                return RoomDirection.Up;
        }
    }

    public static bool TryGetDirection(
        Vector2Int fromPosition,
        Vector2Int toPosition,
        out RoomDirection direction)
    {
        Vector2Int difference =
            toPosition -
            fromPosition;

        if (difference == Vector2Int.up)
        {
            direction =
                RoomDirection.Up;

            return true;
        }

        if (difference == Vector2Int.right)
        {
            direction =
                RoomDirection.Right;

            return true;
        }

        if (difference == Vector2Int.down)
        {
            direction =
                RoomDirection.Down;

            return true;
        }

        if (difference == Vector2Int.left)
        {
            direction =
                RoomDirection.Left;

            return true;
        }

        direction =
            RoomDirection.Up;

        return false;
    }
}