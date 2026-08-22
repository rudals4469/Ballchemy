using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public sealed class RicochetPocketPatternBuilder
{
    public sealed class Cell
    {
        public Vector2Int Position;
        public BlockSpawnRequest.CombatRole Role;
        public bool Indestructible;
    }

    [SerializeField, Range(2, 3)] private int minimumCorridorWidth = 2;
    [SerializeField, Range(2, 3)] private int maximumCorridorWidth = 3;
    [SerializeField, Range(1, 3)] private int minimumAttackerCount = 1;
    [SerializeField, Range(1, 4)] private int maximumAttackerCount = 2;
    [SerializeField, Range(0, 3)] private int maximumIndestructibleAnchors = 2;
    [SerializeField, Range(0f, 1f)] private float indestructibleAnchorChance = 0.65f;
    [SerializeField, Range(0f, 1f)] private float pathNoiseChance = 0.45f;

    public void Normalize(int columns, int rows)
    {
        minimumCorridorWidth = Mathf.Clamp(minimumCorridorWidth, 2, 3);
        maximumCorridorWidth = Mathf.Clamp(
            maximumCorridorWidth, minimumCorridorWidth, 3);
        minimumAttackerCount = Mathf.Max(minimumAttackerCount, 1);
        maximumAttackerCount = Mathf.Max(
            maximumAttackerCount, minimumAttackerCount);
        maximumIndestructibleAnchors = Mathf.Max(
            maximumIndestructibleAnchors, 0);
        indestructibleAnchorChance = Mathf.Clamp01(
            indestructibleAnchorChance);
        pathNoiseChance = Mathf.Clamp01(pathNoiseChance);
    }

    public bool CanBuild(int columns, int rows) =>
        columns >= 5 && rows >= 5;

    public List<Cell> Build(int columns, int rows)
    {
        Normalize(columns, rows);
        List<Cell> result = new List<Cell>();
        if (!CanBuild(columns, rows)) return result;

        bool transpose = Random.value < 0.5f;
        int width = transpose ? rows : columns;
        int height = transpose ? columns : rows;
        List<Vector2Int> path = BuildElbowPath(width, height);
        int corridorWidth = Random.Range(
            minimumCorridorWidth, maximumCorridorWidth + 1);
        HashSet<Vector2Int> corridor = BuildCorridor(
            path, width, height, corridorWidth);
        HashSet<Vector2Int> wall = BuildWall(corridor, width, height);

        OpenEntrance(wall, path, corridorWidth);
        HashSet<Vector2Int> anchors = SelectAnchors(
            wall, corridor, path, maximumIndestructibleAnchors);

        foreach (Vector2Int position in wall)
        {
            result.Add(new Cell
            {
                Position = Transform(position, transpose),
                Role = BlockSpawnRequest.CombatRole.Tank,
                Indestructible = anchors.Contains(position)
            });
        }

        List<Vector2Int> attackerPositions = SelectAttackers(
            path, corridor, wall);
        for (int i = 0; i < attackerPositions.Count; i++)
        {
            result.Add(new Cell
            {
                Position = Transform(attackerPositions[i], transpose),
                Role = BlockSpawnRequest.CombatRole.Attacker,
                Indestructible = false
            });
        }

        return result;
    }

    private List<Vector2Int> BuildElbowPath(int width, int height)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        int startX = Random.Range(1, width - 1);
        int bendY = Mathf.Clamp(
            Random.Range(2, Mathf.Max(3, height - 1)), 2, height - 2);
        int horizontalDirection = startX < width / 2 ? 1 : -1;
        if (Random.value < 0.35f) horizontalDirection *= -1;

        int x = startX;
        for (int y = 0; y <= bendY; y++)
        {
            if (y > 1 && y < bendY &&
                Random.value < pathNoiseChance)
            {
                int shift = Random.value < 0.5f ? -1 : 1;
                x = Mathf.Clamp(x + shift, 1, width - 2);
            }
            AddConnected(path, new Vector2Int(x, y));
        }

        int targetX = horizontalDirection > 0 ? width - 2 : 1;
        int step = targetX >= x ? 1 : -1;
        for (int currentX = x + step;
             currentX != targetX + step;
             currentX += step)
        {
            int noisyY = bendY;
            if (currentX != targetX && Random.value < pathNoiseChance * 0.5f)
                noisyY = Mathf.Clamp(bendY + (Random.value < 0.5f ? -1 : 1),
                    1, height - 2);
            AddConnected(path, new Vector2Int(currentX, noisyY));
        }
        return path;
    }

    private static void AddConnected(
        List<Vector2Int> path, Vector2Int destination)
    {
        if (path.Count == 0)
        {
            path.Add(destination);
            return;
        }
        Vector2Int current = path[path.Count - 1];
        while (current.x != destination.x)
        {
            current.x += Math.Sign(destination.x - current.x);
            if (path[path.Count - 1] != current) path.Add(current);
        }
        while (current.y != destination.y)
        {
            current.y += Math.Sign(destination.y - current.y);
            if (path[path.Count - 1] != current) path.Add(current);
        }
    }

    private static HashSet<Vector2Int> BuildCorridor(
        IReadOnlyList<Vector2Int> path, int width, int height,
        int corridorWidth)
    {
        HashSet<Vector2Int> result = new HashSet<Vector2Int>();
        int radius = corridorWidth >= 3 ? 1 : 0;
        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int center = path[i];
            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (Mathf.Abs(dx) + Mathf.Abs(dy) > radius) continue;
                AddInside(result, center + new Vector2Int(dx, dy), width, height);
            }
            if (corridorWidth == 2)
            {
                Vector2Int direction = i + 1 < path.Count
                    ? path[i + 1] - center
                    : center - path[Mathf.Max(i - 1, 0)];
                Vector2Int side = new Vector2Int(-direction.y, direction.x);
                AddInside(result, center + side, width, height);
            }
        }
        return result;
    }

    private static HashSet<Vector2Int> BuildWall(
        HashSet<Vector2Int> corridor, int width, int height)
    {
        HashSet<Vector2Int> wall = new HashSet<Vector2Int>();
        Vector2Int[] directions =
        {
            Vector2Int.left, Vector2Int.right,
            Vector2Int.up, Vector2Int.down
        };
        foreach (Vector2Int cell in corridor)
        {
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int candidate = cell + directions[i];
                if (IsInside(candidate, width, height) &&
                    !corridor.Contains(candidate))
                    wall.Add(candidate);
            }
        }
        return wall;
    }

    private static void OpenEntrance(
        HashSet<Vector2Int> wall, IReadOnlyList<Vector2Int> path, int width)
    {
        int count = Mathf.Min(width + 1, path.Count);
        for (int i = 0; i < count; i++)
        {
            Vector2Int center = path[i];
            wall.Remove(center + Vector2Int.left);
            wall.Remove(center + Vector2Int.right);
            if (center.y <= 1) wall.Remove(center + Vector2Int.down);
        }
    }

    private HashSet<Vector2Int> SelectAnchors(
        HashSet<Vector2Int> wall, HashSet<Vector2Int> corridor,
        IReadOnlyList<Vector2Int> path, int maximumCount)
    {
        HashSet<Vector2Int> result = new HashSet<Vector2Int>();
        if (maximumCount <= 0 || Random.value > indestructibleAnchorChance)
            return result;
        List<Vector2Int> candidates = new List<Vector2Int>();
        foreach (Vector2Int cell in wall)
        {
            if (Vector2Int.Distance(cell, path[0]) < 3f) continue;
            int touching = 0;
            if (corridor.Contains(cell + Vector2Int.left)) touching++;
            if (corridor.Contains(cell + Vector2Int.right)) touching++;
            if (corridor.Contains(cell + Vector2Int.up)) touching++;
            if (corridor.Contains(cell + Vector2Int.down)) touching++;
            if (touching >= 2) candidates.Add(cell);
        }
        Shuffle(candidates);
        int count = Mathf.Min(Random.Range(1, maximumCount + 1), candidates.Count);
        for (int i = 0; i < count; i++) result.Add(candidates[i]);
        return result;
    }

    private List<Vector2Int> SelectAttackers(
        IReadOnlyList<Vector2Int> path, HashSet<Vector2Int> corridor,
        HashSet<Vector2Int> wall)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        int desired = Random.Range(minimumAttackerCount, maximumAttackerCount + 1);
        int start = Mathf.Max(path.Count / 2, 2);
        for (int i = path.Count - 2; i >= start && result.Count < desired; i -= 2)
        {
            Vector2Int candidate = path[i];
            if (!corridor.Contains(candidate) || wall.Contains(candidate) ||
                result.Contains(candidate)) continue;
            result.Add(candidate);
        }
        if (result.Count == 0 && path.Count > 2)
            result.Add(path[path.Count - 2]);
        return result;
    }

    private static Vector2Int Transform(Vector2Int value, bool transpose) =>
        transpose ? new Vector2Int(value.y, value.x) : value;

    private static void AddInside(
        HashSet<Vector2Int> set, Vector2Int value, int width, int height)
    {
        if (IsInside(value, width, height)) set.Add(value);
    }

    private static bool IsInside(Vector2Int value, int width, int height) =>
        value.x >= 0 && value.x < width && value.y >= 0 && value.y < height;

    private static void Shuffle(List<Vector2Int> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int index = Random.Range(0, i + 1);
            Vector2Int temporary = values[i];
            values[i] = values[index];
            values[index] = temporary;
        }
    }
}
