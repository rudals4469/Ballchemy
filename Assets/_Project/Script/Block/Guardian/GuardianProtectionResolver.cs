using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class GuardianProtectionResolver
{
    public static List<BlockSpawnRequest> AssignTargets(
        IReadOnlyList<BlockSpawnRequest> source,
        int minimumTargets,
        int maximumTargets)
    {
        List<BlockSpawnRequest> result = new List<BlockSpawnRequest>();

        if (source == null)
        {
            return result;
        }

        for (int i = 0; i < source.Count; i++)
        {
            result.Add(source[i]?.CreateCopy());
        }

        minimumTargets = Mathf.Max(minimumTargets, 1);
        maximumTargets = Mathf.Max(maximumTargets, minimumTargets);
        HashSet<Vector2Int> assignedPositions = new HashSet<Vector2Int>();

        for (int guardianIndex = result.Count - 1; guardianIndex >= 0; guardianIndex--)
        {
            BlockSpawnRequest guardian = result[guardianIndex];

            if (!IsGuardian(guardian))
            {
                continue;
            }

            List<BlockSpawnRequest> candidates = GetCandidates(result, assignedPositions);
            int targetCount = Mathf.Min(Random.Range(minimumTargets, maximumTargets + 1), candidates.Count);

            if (targetCount < minimumTargets)
            {
                result.RemoveAt(guardianIndex);
                continue;
            }

            List<Vector2Int> targetPositions = new List<Vector2Int>();

            for (int i = 0; i < targetCount; i++)
            {
                int highestPriority = GetPriority(candidates[0]);
                int samePriorityCount = 1;

                while (samePriorityCount < candidates.Count &&
                       GetPriority(candidates[samePriorityCount]) == highestPriority)
                {
                    samePriorityCount++;
                }

                int selectedIndex = Random.Range(0, samePriorityCount);
                BlockSpawnRequest selected = candidates[selectedIndex];
                Vector2Int position = new Vector2Int(selected.StartColumn, selected.StartRow);
                targetPositions.Add(position);
                assignedPositions.Add(position);
                candidates.RemoveAt(selectedIndex);

                if (candidates.Count == 0)
                {
                    break;
                }
            }

            result[guardianIndex] = new BlockSpawnRequest(
                guardian.StartColumn,
                guardian.StartRow,
                guardian.WaveIndex,
                guardian.Definition,
                guardian.RequestedBlockType,
                guardian.GridSize,
                guardian.Health,
                0,
                targetPositions
            );
        }

        return result;
    }

    public static void ConnectRuntime(
        IReadOnlyList<BlockSpawnRequest> requests,
        IReadOnlyList<Block> blocks)
    {
        if (requests == null || blocks == null)
        {
            return;
        }

        Dictionary<Vector2Int, Block> blocksByPosition = new Dictionary<Vector2Int, Block>();

        for (int i = 0; i < blocks.Count; i++)
        {
            Block block = blocks[i];
            if (block != null)
            {
                blocksByPosition[new Vector2Int(block.StartColumn, block.StartRow)] = block;
            }
        }

        for (int i = 0; i < requests.Count; i++)
        {
            BlockSpawnRequest request = requests[i];
            if (!IsGuardian(request) ||
                !blocksByPosition.TryGetValue(new Vector2Int(request.StartColumn, request.StartRow), out Block guardian))
            {
                continue;
            }

            List<Block> targets = new List<Block>();
            for (int j = 0; j < request.GuardianTargetPositions.Count; j++)
            {
                if (blocksByPosition.TryGetValue(request.GuardianTargetPositions[j], out Block target))
                {
                    targets.Add(target);
                }
            }

            GuardianBlockController controller = guardian.GetComponent<GuardianBlockController>();
            if (controller == null)
            {
                controller = guardian.gameObject.AddComponent<GuardianBlockController>();
            }

            if (guardian.GetComponent<GuardianLinkPresenter>() == null)
            {
                guardian.gameObject.AddComponent<GuardianLinkPresenter>();
            }

            controller.Configure(targets);
        }
    }

    private static List<BlockSpawnRequest> GetCandidates(
        IReadOnlyList<BlockSpawnRequest> requests,
        HashSet<Vector2Int> assignedPositions)
    {
        List<BlockSpawnRequest> candidates = new List<BlockSpawnRequest>();

        for (int i = 0; i < requests.Count; i++)
        {
            BlockSpawnRequest request = requests[i];
            Vector2Int position = request != null
                ? new Vector2Int(request.StartColumn, request.StartRow)
                : default;

            if (!IsProtectable(request) || assignedPositions.Contains(position))
            {
                continue;
            }

            candidates.Add(request);
        }

        candidates.Sort((left, right) => GetPriority(right).CompareTo(GetPriority(left)));
        return candidates;
    }

    private static bool IsGuardian(BlockSpawnRequest request) =>
        request?.Definition != null &&
        request.Definition.SpecialCategory == SpecialBlockCategory.Guardian;

    private static bool IsProtectable(BlockSpawnRequest request)
    {
        if (request == null || request.Definition == null ||
            request.Definition.DestructionRule != BlockDestructionRule.Breakable)
        {
            return false;
        }

        switch (request.RequestedBlockType)
        {
            case BlockType.Normal:
            case BlockType.Elite:
            case BlockType.Named:
            case BlockType.Boss:
                return true;
            default:
                return false;
        }
    }

    private static int GetPriority(BlockSpawnRequest request)
    {
        switch (request.RequestedBlockType)
        {
            case BlockType.Boss: return 4;
            case BlockType.Named: return 3;
            case BlockType.Elite: return 2;
            case BlockType.Normal: return 1;
            default: return 0;
        }
    }
}
