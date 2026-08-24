using System.Collections.Generic;
using UnityEngine;

public static class TeleportPairInjector
{
    public static bool TryInjectAtPositions(
        List<BlockSpawnRequest> requests,
        TeleportPairSpawnSettings settings,
        int waveIndex,
        Vector2Int first,
        Vector2Int second)
    {
        if (requests == null || settings == null ||
            settings.Definition == null || first == second ||
            IsOccupied(requests, first) || IsOccupied(requests, second))
            return false;

        int pairId = ResolveNextPairId(requests);
        AddPortal(requests, settings.Definition, waveIndex,
            first, second, pairId);
        AddPortal(requests, settings.Definition, waveIndex,
            second, first, pairId);
        return true;
    }

    private static bool IsOccupied(
        IReadOnlyList<BlockSpawnRequest> requests,
        Vector2Int cell)
    {
        for (int i = 0; i < requests.Count; i++)
        {
            BlockSpawnRequest request = requests[i];
            if (request == null) continue;
            if (cell.x >= request.StartColumn &&
                cell.x < request.StartColumn + request.GridSize.x &&
                cell.y >= request.StartRow &&
                cell.y < request.StartRow + request.GridSize.y)
                return true;
        }
        return false;
    }
    public static bool TryInjectPocketBridge(
        List<BlockSpawnRequest> requests,
        TeleportPairSpawnSettings settings,
        int columnCount,
        int boardRowCount,
        int waveIndex,
        bool isNamedRoom)
    {
        if (requests == null || settings == null ||
            !settings.CanSpawn(isNamedRoom) || HasTeleportPair(requests))
            return false;

        int activeRowCount = ResolveActiveRowCount(requests, boardRowCount);
        BlockWaveOccupancyMap occupancy =
            new BlockWaveOccupancyMap(columnCount, activeRowCount);
        OccupyExisting(requests, occupancy);

        HashSet<Vector2Int> reachable = CollectReachableEmptyCells(occupancy);
        List<Vector2Int> outsideCandidates = new List<Vector2Int>();
        List<Vector2Int> insideCandidates = new List<Vector2Int>();
        for (int row = 0; row < occupancy.RowCount; row++)
        for (int column = 0; column < occupancy.ColumnCount; column++)
        {
            Vector2Int cell = new Vector2Int(column, row);
            if (IsReservedPerimeter(cell, occupancy) ||
                occupancy.IsOccupied(column, row))
                continue;
            if (reachable.Contains(cell)) outsideCandidates.Add(cell);
            else insideCandidates.Add(cell);
        }

        if (outsideCandidates.Count == 0 || insideCandidates.Count == 0)
            return false;

        Vector2Int inside = SelectMostEnclosed(insideCandidates, occupancy);
        Vector2Int outside = SelectFarthest(outsideCandidates, inside);
        if (!HasCardinalExit(inside, occupancy, outside) ||
            !HasCardinalExit(outside, occupancy, inside))
            return false;

        int pairId = ResolveNextPairId(requests);
        AddPortal(requests, settings.Definition, waveIndex,
            outside, inside, pairId);
        AddPortal(requests, settings.Definition, waveIndex,
            inside, outside, pairId);
        return true;
    }

    public static bool TryInjectSelectedPair(
        List<BlockSpawnRequest> requests,
        TeleportPairSpawnSettings settings,
        int columnCount,
        int boardRowCount,
        int waveIndex)
    {
        if (requests == null || settings == null || settings.Definition == null)
        {
            return false;
        }

        int activeRowCount = ResolveActiveRowCount(requests, boardRowCount);
        BlockWaveOccupancyMap occupancy =
            new BlockWaveOccupancyMap(columnCount, activeRowCount);
        OccupyExisting(requests, occupancy);

        List<Vector2Int> candidates = CollectCandidates(occupancy);
        Shuffle(candidates);

        if (!TrySelectPair(
                candidates,
                occupancy,
                settings.MinimumPortalSeparationCells,
                out Vector2Int first,
                out Vector2Int second))
        {
            return false;
        }

        int pairId = ResolveNextPairId(requests);
        AddPortal(requests, settings.Definition, waveIndex, first, second, pairId);
        AddPortal(requests, settings.Definition, waveIndex, second, first, pairId);
        return true;
    }

    public static List<BlockSpawnRequest> Inject(
        IReadOnlyList<BlockSpawnRequest> source,
        TeleportPairSpawnSettings settings,
        int columnCount,
        int boardRowCount,
        int waveIndex,
        bool isNamedRoom)
    {
        List<BlockSpawnRequest> result = Copy(source);

        if (settings == null || !settings.ShouldSpawn(isNamedRoom))
        {
            return result;
        }

        int activeRowCount = ResolveActiveRowCount(result, boardRowCount);
        BlockWaveOccupancyMap occupancy = new BlockWaveOccupancyMap(columnCount, activeRowCount);
        OccupyExisting(result, occupancy);

        List<Vector2Int> candidates = CollectCandidates(occupancy);
        Shuffle(candidates);

        int createdPairs = 0;

        while (createdPairs < settings.MaximumPairsPerRoom && candidates.Count >= 2)
        {
            if (!TrySelectPair(candidates, occupancy, settings.MinimumPortalSeparationCells,
                    out Vector2Int first, out Vector2Int second))
            {
                break;
            }

            int pairId = createdPairs;
            AddPortal(result, settings.Definition, waveIndex, first, second, pairId);
            AddPortal(result, settings.Definition, waveIndex, second, first, pairId);
            occupancy.TryOccupy(first.x, first.y, Vector2Int.one);
            occupancy.TryOccupy(second.x, second.y, Vector2Int.one);
            createdPairs++;

            candidates.Remove(first);
            candidates.Remove(second);
        }

        return result;
    }

    private static bool TrySelectPair(
        IReadOnlyList<Vector2Int> candidates,
        BlockWaveOccupancyMap occupancy,
        int minimumSeparation,
        out Vector2Int first,
        out Vector2Int second)
    {
        first = default;
        second = default;
        int bestSeparation = -1;

        for (int i = 0; i < candidates.Count; i++)
        {
            for (int j = i + 1; j < candidates.Count; j++)
            {
                Vector2Int a = candidates[i];
                Vector2Int b = candidates[j];
                int separation = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

                if (separation < minimumSeparation ||
                    !HasCardinalExit(a, occupancy, b) ||
                    !HasCardinalExit(b, occupancy, a) ||
                    separation <= bestSeparation)
                {
                    continue;
                }

                first = a;
                second = b;
                bestSeparation = separation;
            }
        }

        return bestSeparation >= minimumSeparation;
    }

    private static bool HasCardinalExit(
        Vector2Int position,
        BlockWaveOccupancyMap occupancy,
        Vector2Int otherPortal)
    {
        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int neighbor = position + directions[i];
            if (neighbor == otherPortal)
            {
                continue;
            }

            if (occupancy.IsInside(neighbor.x, neighbor.y) &&
                !occupancy.IsOccupied(neighbor.x, neighbor.y))
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<Vector2Int> CollectReachableEmptyCells(
        BlockWaveOccupancyMap occupancy)
    {
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        for (int column = 0; column < occupancy.ColumnCount; column++)
        {
            if (occupancy.IsOccupied(column, 0)) continue;
            Vector2Int start = new Vector2Int(column, 0);
            visited.Add(start);
            queue.Enqueue(start);
        }

        Vector2Int[] directions =
        {
            Vector2Int.left, Vector2Int.right,
            Vector2Int.up, Vector2Int.down
        };
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int next = current + directions[i];
                if (!occupancy.IsInside(next.x, next.y) ||
                    occupancy.IsOccupied(next.x, next.y) ||
                    !visited.Add(next))
                    continue;
                queue.Enqueue(next);
            }
        }
        return visited;
    }

    private static Vector2Int SelectMostEnclosed(
        IReadOnlyList<Vector2Int> candidates,
        BlockWaveOccupancyMap occupancy)
    {
        Vector2Int best = default;
        int bestScore = -1;
        Vector2Int[] directions =
        {
            Vector2Int.left, Vector2Int.right,
            Vector2Int.up, Vector2Int.down
        };
        for (int i = 0; i < candidates.Count; i++)
        {
            int score = 0;
            for (int j = 0; j < directions.Length; j++)
            {
                Vector2Int neighbor = candidates[i] + directions[j];
                if (!occupancy.IsInside(neighbor.x, neighbor.y) ||
                    occupancy.IsOccupied(neighbor.x, neighbor.y))
                    score++;
            }
            if (score < 4 && score > bestScore)
            {
                best = candidates[i];
                bestScore = score;
            }
        }
        return best;
    }

    private static Vector2Int SelectFarthest(
        IReadOnlyList<Vector2Int> candidates,
        Vector2Int from)
    {
        Vector2Int best = candidates[0];
        int bestDistance = -1;
        for (int i = 0; i < candidates.Count; i++)
        {
            int distance = Mathf.Abs(candidates[i].x - from.x) +
                           Mathf.Abs(candidates[i].y - from.y);
            if (distance > bestDistance)
            {
                best = candidates[i];
                bestDistance = distance;
            }
        }
        return best;
    }

    private static bool HasTeleportPair(
        IReadOnlyList<BlockSpawnRequest> requests)
    {
        for (int i = 0; i < requests.Count; i++)
        {
            if (requests[i] != null && requests[i].HasTeleportPair)
                return true;
        }
        return false;
    }

    private static List<Vector2Int> CollectCandidates(BlockWaveOccupancyMap occupancy)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        for (int row = 0; row < occupancy.RowCount; row++)
        {
            for (int column = 0; column < occupancy.ColumnCount; column++)
            {
                if (!occupancy.IsOccupied(column, row))
                {
                    Vector2Int cell = new Vector2Int(column, row);
                    if (!IsReservedPerimeter(cell, occupancy))
                        result.Add(cell);
                }
            }
        }

        return result;
    }

    private static bool IsReservedPerimeter(
        Vector2Int cell,
        BlockWaveOccupancyMap occupancy)
    {
        return cell.y == 0 ||
               cell.x == 0 ||
               cell.x == occupancy.ColumnCount - 1;
    }

    private static void OccupyExisting(
        IReadOnlyList<BlockSpawnRequest> requests,
        BlockWaveOccupancyMap occupancy)
    {
        for (int i = 0; i < requests.Count; i++)
        {
            BlockSpawnRequest request = requests[i];
            if (request != null)
            {
                occupancy.TryOccupy(request.StartColumn, request.StartRow, request.GridSize);
            }
        }
    }

    private static int ResolveActiveRowCount(
        IReadOnlyList<BlockSpawnRequest> requests,
        int boardRowCount)
    {
        int activeRows = 1;

        for (int i = 0; i < requests.Count; i++)
        {
            BlockSpawnRequest request = requests[i];
            if (request != null)
            {
                activeRows = Mathf.Max(activeRows, request.StartRow + request.GridSize.y);
            }
        }

        return Mathf.Clamp(activeRows, 1, Mathf.Max(boardRowCount, 1));
    }

    private static void AddPortal(
        List<BlockSpawnRequest> requests,
        BlockDefinition definition,
        int waveIndex,
        Vector2Int position,
        Vector2Int partner,
        int pairId)
    {
        requests.Add(new BlockSpawnRequest(
            position.x, position.y, waveIndex, definition, BlockType.Special,
            Vector2Int.one, 1, 0, null, pairId, partner));
    }

    private static int ResolveNextPairId(IReadOnlyList<BlockSpawnRequest> requests)
    {
        int nextPairId = 0;
        for (int i = 0; i < requests.Count; i++)
        {
            BlockSpawnRequest request = requests[i];
            if (request != null && request.HasTeleportPair)
            {
                nextPairId = Mathf.Max(nextPairId, request.TeleportPairId + 1);
            }
        }

        return nextPairId;
    }

    private static List<BlockSpawnRequest> Copy(IReadOnlyList<BlockSpawnRequest> source)
    {
        List<BlockSpawnRequest> result = new List<BlockSpawnRequest>();
        if (source == null) return result;
        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] != null) result.Add(source[i].CreateCopy());
        }
        return result;
    }

    private static void Shuffle(List<Vector2Int> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            Vector2Int temp = values[i];
            values[i] = values[swapIndex];
            values[swapIndex] = temp;
        }
    }
}
