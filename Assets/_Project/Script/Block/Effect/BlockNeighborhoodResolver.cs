using System.Collections.Generic;
using UnityEngine;

public static class BlockNeighborhoodResolver
{
    /*
     * radius = 1
     * → 중심 블록을 포함한 3×3 범위
     *
     * radius = 2
     * → 중심 블록을 포함한 5×5 범위
     *
     * radius = 3
     * → 중심 블록을 포함한 7×7 범위
     */
    public static List<Block>
        FindSurroundingBlocks(
            Block sourceBlock,
            IReadOnlyList<Block> activeBlocks,
            int radius = 1)
    {
        List<Block> result =
            new List<Block>();

        if (sourceBlock == null ||
            activeBlocks == null ||
            !sourceBlock.HasGridPosition)
        {
            return result;
        }

        radius =
            Mathf.Max(
                radius,
                1
            );

        HashSet<Block> uniqueBlocks =
            new HashSet<Block>();

        int minimumColumn =
            sourceBlock.StartColumn -
            radius;

        int maximumColumn =
            sourceBlock.EndColumn +
            radius;

        int minimumRow =
            sourceBlock.StartRow -
            radius;

        int maximumRow =
            sourceBlock.EndRow +
            radius;

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

            /*
             * 2×2 이상의 블록이 범위의 여러 칸에
             * 걸쳐 있어도 한 번만 등록한다.
             */
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
         * 서로 다른 보드에 있는 블록이
         * 같은 좌표를 가지고 있어도
         * 범위 대상으로 판정되지 않게 한다.
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