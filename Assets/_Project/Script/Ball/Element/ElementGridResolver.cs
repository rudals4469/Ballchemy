using System.Collections.Generic;
using UnityEngine;

public readonly struct ElementChainLink
{
    public Block Source { get; }
    public Block Target { get; }

    public ElementChainLink(Block source, Block target)
    {
        Source = source;
        Target = target;
    }
}

public static class ElementGridResolver
{
    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int(-1, -1), new Vector2Int(-1, 0),
        new Vector2Int(-1, 1), new Vector2Int(0, -1),
        new Vector2Int(0, 1), new Vector2Int(1, -1),
        new Vector2Int(1, 0), new Vector2Int(1, 1)
    };

    public static List<Block> SpreadWetThroughMaximumBlocks(
        Block source,
        IReadOnlyList<Block> activeBlocks,
        int amount,
        int maximumStack)
    {
        List<Block> targets = new List<Block>();
        if (source == null || activeBlocks == null || amount <= 0)
            return targets;

        Dictionary<Vector2Int, Block> lookup = BuildLookup(activeBlocks);
        HashSet<Block> unique = new HashSet<Block>();

        for (int i = 0; i < Directions.Length; i++)
        {
            Vector2Int position = new Vector2Int(
                source.StartColumn, source.StartRow) + Directions[i];

            while (lookup.TryGetValue(position, out Block target))
            {
                BlockElementStatus status = target != null
                    ? target.GetComponent<BlockElementStatus>()
                    : null;
                int wet = status != null ? status.WetStack : 0;

                if (wet < maximumStack)
                {
                    if (target.IsAlive && target.IsBreakable && unique.Add(target))
                        targets.Add(target);
                    break;
                }

                position += Directions[i];
            }
        }

        return targets;
    }

    public static List<ElementChainLink> FindConnectedWetChain(
        Block source,
        IReadOnlyList<Block> activeBlocks,
        int maximumTargets)
    {
        List<ElementChainLink> result = new List<ElementChainLink>();
        if (source == null || activeBlocks == null || maximumTargets <= 0)
            return result;

        Dictionary<Vector2Int, Block> lookup = BuildLookup(activeBlocks);
        Queue<Block> queue = new Queue<Block>();
        HashSet<Block> visited = new HashSet<Block> { source };
        queue.Enqueue(source);

        while (queue.Count > 0 && result.Count < maximumTargets)
        {
            Block parent = queue.Dequeue();
            Vector2Int origin = new Vector2Int(parent.StartColumn, parent.StartRow);

            for (int i = 0; i < Directions.Length && result.Count < maximumTargets; i++)
            {
                if (!lookup.TryGetValue(origin + Directions[i], out Block target) ||
                    target == null || !target.IsAlive || !target.IsBreakable ||
                    !visited.Add(target))
                {
                    continue;
                }

                BlockElementStatus status = target.GetComponent<BlockElementStatus>();
                if (status == null || !status.HasWet)
                    continue;

                result.Add(new ElementChainLink(parent, target));
                queue.Enqueue(target);
            }
        }

        return result;
    }

    public static List<Block> FindEightNeighbors(
        Block source,
        IReadOnlyList<Block> activeBlocks)
    {
        List<Block> result = new List<Block>();
        if (source == null || activeBlocks == null) return result;
        Dictionary<Vector2Int, Block> lookup = BuildLookup(activeBlocks);
        Vector2Int origin = new Vector2Int(source.StartColumn, source.StartRow);
        for (int i = 0; i < Directions.Length; i++)
            if (lookup.TryGetValue(origin + Directions[i], out Block target) &&
                target != null && target.IsAlive && target.IsBreakable)
                result.Add(target);
        return result;
    }

    private static Dictionary<Vector2Int, Block> BuildLookup(
        IReadOnlyList<Block> activeBlocks)
    {
        Dictionary<Vector2Int, Block> lookup = new Dictionary<Vector2Int, Block>();
        for (int i = 0; i < activeBlocks.Count; i++)
        {
            Block block = activeBlocks[i];
            if (block == null) continue;
            Vector2Int size = block.GridSize;
            for (int y = 0; y < Mathf.Max(size.y, 1); y++)
                for (int x = 0; x < Mathf.Max(size.x, 1); x++)
                    lookup[new Vector2Int(
                        block.StartColumn + x,
                        block.StartRow + y)] = block;
        }
        return lookup;
    }
}
