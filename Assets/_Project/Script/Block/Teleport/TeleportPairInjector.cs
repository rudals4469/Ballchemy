using System.Collections.Generic;
using UnityEngine;

public static class TeleportPairInjector
{
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

        for (int i = 0; i < candidates.Count; i++)
        {
            for (int j = i + 1; j < candidates.Count; j++)
            {
                Vector2Int a = candidates[i];
                Vector2Int b = candidates[j];
                int separation = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

                if (separation < minimumSeparation ||
                    !HasCardinalExit(a, occupancy, b) ||
                    !HasCardinalExit(b, occupancy, a))
                {
                    continue;
                }

                first = a;
                second = b;
                return true;
            }
        }

        return false;
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

    private static List<Vector2Int> CollectCandidates(BlockWaveOccupancyMap occupancy)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        for (int row = 0; row < occupancy.RowCount; row++)
        {
            for (int column = 0; column < occupancy.ColumnCount; column++)
            {
                if (!occupancy.IsOccupied(column, row))
                {
                    result.Add(new Vector2Int(column, row));
                }
            }
        }

        return result;
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
