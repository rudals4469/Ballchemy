using System.Collections.Generic;

public static class BlockNeighborhoodResolver
{
    public static List<Block>
        FindSurroundingBlocks(
            Block sourceBlock,
            IReadOnlyList<Block> activeBlocks)
    {
        List<Block> result =
            new List<Block>();

        if (sourceBlock == null ||
            activeBlocks == null ||
            !sourceBlock.HasGridPosition)
        {
            return result;
        }

        HashSet<Block> uniqueBlocks =
            new HashSet<Block>();

        int minimumColumn =
            sourceBlock.StartColumn - 1;

        int maximumColumn =
            sourceBlock.EndColumn + 1;

        int minimumRow =
            sourceBlock.StartRow - 1;

        int maximumRow =
            sourceBlock.EndRow + 1;

        for (int i = 0;
             i < activeBlocks.Count;
             i++)
        {
            Block targetBlock =
                activeBlocks[i];

            if (!IsValidTarget(
                    sourceBlock,
                    targetBlock))
            {
                continue;
            }

            if (!IsInsideExpandedBounds(
                    targetBlock,
                    minimumColumn,
                    maximumColumn,
                    minimumRow,
                    maximumRow))
            {
                continue;
            }

            if (uniqueBlocks.Add(
                    targetBlock))
            {
                result.Add(
                    targetBlock
                );
            }
        }

        return result;
    }

    private static bool IsValidTarget(
        Block sourceBlock,
        Block targetBlock)
    {
        if (targetBlock == null ||
            targetBlock == sourceBlock ||
            !targetBlock.IsAlive ||
            !targetBlock.HasGridPosition)
        {
            return false;
        }

        /*
         * 서로 다른 보드에 속한 블록이
         * 같은 좌표를 가지고 있어도
         * 주변 대상으로 판정되지 않게 한다.
         */
        if (sourceBlock.BoardGrid != null &&
            targetBlock.BoardGrid != null &&
            sourceBlock.BoardGrid !=
            targetBlock.BoardGrid)
        {
            return false;
        }

        return true;
    }

    private static bool IsInsideExpandedBounds(
        Block targetBlock,
        int minimumColumn,
        int maximumColumn,
        int minimumRow,
        int maximumRow)
    {
        bool overlapsColumns =
            targetBlock.EndColumn >=
            minimumColumn &&
            targetBlock.StartColumn <=
            maximumColumn;

        bool overlapsRows =
            targetBlock.EndRow >=
            minimumRow &&
            targetBlock.StartRow <=
            maximumRow;

        return overlapsColumns &&
               overlapsRows;
    }
}