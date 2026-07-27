using System.Collections.Generic;
using UnityEngine;

public static class ElementConductionResolver
{
    public static List<Block>
        FindWetConductionTargets(
            Block sourceBlock,
            IReadOnlyList<Block> activeBlocks,
            int maximumTargetCount,
            int minimumWetStack)
    {
        List<Block> result =
            new List<Block>();

        if (sourceBlock == null ||
            activeBlocks == null ||
            maximumTargetCount <= 0)
        {
            return result;
        }

        maximumTargetCount =
            Mathf.Max(
                maximumTargetCount,
                1
            );

        minimumWetStack =
            Mathf.Max(
                minimumWetStack,
                1
            );

        Queue<Block> searchQueue =
            new Queue<Block>();

        HashSet<Block> visitedBlocks =
            new HashSet<Block>();

        searchQueue.Enqueue(
            sourceBlock
        );

        visitedBlocks.Add(
            sourceBlock
        );

        while (searchQueue.Count > 0 &&
               result.Count <
               maximumTargetCount)
        {
            Block currentBlock =
                searchQueue.Dequeue();

            /*
             * 기존 폭발 패턴 탐색의 Cross 1칸을 이용하면
             * 현재 블록과 상하좌우로 직접 맞닿은 블록을
             * 가져올 수 있습니다.
             */
            List<Block> adjacentBlocks =
                BlockNeighborhoodResolver
                    .FindPatternBlocks(
                        currentBlock,
                        activeBlocks,
                        ExplosionPatternType.Cross,
                        1
                    );

            SortByGridPosition(
                adjacentBlocks
            );

            for (int i = 0;
                 i < adjacentBlocks.Count;
                 i++)
            {
                Block adjacentBlock =
                    adjacentBlocks[i];

                if (adjacentBlock == null ||
                    visitedBlocks.Contains(
                        adjacentBlock))
                {
                    continue;
                }

                visitedBlocks.Add(
                    adjacentBlock
                );

                if (!CanConductThrough(
                        adjacentBlock,
                        minimumWetStack))
                {
                    continue;
                }

                result.Add(
                    adjacentBlock
                );

                /*
                 * 젖은 블록을 다시 탐색 큐에 넣어
                 * 그 너머에 이어진 젖은 블록도 찾습니다.
                 */
                searchQueue.Enqueue(
                    adjacentBlock
                );

                if (result.Count >=
                    maximumTargetCount)
                {
                    break;
                }
            }
        }

        return result;
    }

    private static bool CanConductThrough(
        Block targetBlock,
        int minimumWetStack)
    {
        if (targetBlock == null ||
            !targetBlock.IsAlive ||
            !targetBlock.IsBreakable)
        {
            return false;
        }

        BlockElementStatus elementStatus =
            targetBlock.GetComponent<
                BlockElementStatus
            >();

        if (elementStatus == null)
        {
            return false;
        }

        return
            elementStatus.WetStack >=
            minimumWetStack;
    }

    private static void SortByGridPosition(
        List<Block> blocks)
    {
        if (blocks == null ||
            blocks.Count <= 1)
        {
            return;
        }

        blocks.Sort(
            CompareGridPosition
        );
    }

    private static int CompareGridPosition(
        Block left,
        Block right)
    {
        if (left == right)
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        int rowCompare =
            left.StartRow.CompareTo(
                right.StartRow
            );

        if (rowCompare != 0)
        {
            return rowCompare;
        }

        return left.StartColumn.CompareTo(
            right.StartColumn
        );
    }
}