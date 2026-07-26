using System.Collections.Generic;
using UnityEngine;

public static class BlockNeighborhoodResolver
{
    /*
     * 기존 특수 폭발 블록에서 사용하는 메서드입니다.
     *
     * radius = 1
     * → 중심 블록 바깥으로 한 칸 확장된 사각형 범위
     *
     * radius = 2
     * → 중심 블록 바깥으로 두 칸 확장된 사각형 범위
     *
     * 기존 ExplosionOnDestroyedEffect의 동작이
     * 바뀌지 않도록 사각 범위 판정을 유지합니다.
     */
    public static List<Block>
        FindSurroundingBlocks(
            Block sourceBlock,
            IReadOnlyList<Block> activeBlocks,
            int radius = 1)
    {
        List<Block> result =
            new List<Block>();

        if (!CanResolve(
                sourceBlock,
                activeBlocks))
        {
            return result;
        }

        radius =
            Mathf.Max(
                radius,
                1
            );

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

        HashSet<Block> uniqueBlocks =
            new HashSet<Block>();

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

            if (!IsInsideBounds(
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

    /*
     * 폭발 공 전용 패턴 탐색입니다.
     *
     * AllDirections
     * → 십자 + 대각선, 총 8방향
     *
     * Cross
     * → 상하좌우 네 방향
     *
     * Diagonal
     * → 대각선 네 방향
     *
     * 범위 내부를 전부 채우는 사각형 폭발은 아닙니다.
     */
    public static List<Block>
        FindPatternBlocks(
            Block sourceBlock,
            IReadOnlyList<Block> activeBlocks,
            ExplosionPatternType patternType,
            int range)
    {
        List<Block> result =
            new List<Block>();

        if (!CanResolve(
                sourceBlock,
                activeBlocks))
        {
            return result;
        }

        range =
            Mathf.Max(
                range,
                1
            );

        HashSet<Block> uniqueBlocks =
            new HashSet<Block>();

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

            if (!MatchesPattern(
                    sourceBlock,
                    targetBlock,
                    patternType,
                    range))
            {
                continue;
            }

            /*
             * 2×2 이상의 블록이 여러 패턴 셀에
             * 걸쳐 있어도 피해 대상에는 한 번만 등록합니다.
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

    private static bool CanResolve(
        Block sourceBlock,
        IReadOnlyList<Block> activeBlocks)
    {
        return
            sourceBlock != null &&
            activeBlocks != null &&
            sourceBlock.HasGridPosition;
    }

    private static bool MatchesPattern(
        Block sourceBlock,
        Block targetBlock,
        ExplosionPatternType patternType,
        int range)
    {
        switch (patternType)
        {
            case ExplosionPatternType
                .AllDirections:

                return
                    IsInsideCrossPattern(
                        sourceBlock,
                        targetBlock,
                        range
                    ) ||
                    IsInsideDiagonalPattern(
                        sourceBlock,
                        targetBlock,
                        range
                    );

            case ExplosionPatternType.Cross:

                return IsInsideCrossPattern(
                    sourceBlock,
                    targetBlock,
                    range
                );

            case ExplosionPatternType.Diagonal:

                return IsInsideDiagonalPattern(
                    sourceBlock,
                    targetBlock,
                    range
                );

            default:

                return
                    IsInsideCrossPattern(
                        sourceBlock,
                        targetBlock,
                        range
                    ) ||
                    IsInsideDiagonalPattern(
                        sourceBlock,
                        targetBlock,
                        range
                    );
        }
    }

    /*
     * 중심 블록이 점유한 Row를 기준으로 좌우,
     * 중심 블록이 점유한 Column을 기준으로 상하를 판정합니다.
     *
     * 따라서 2×2 이상의 중심 블록도 정상적으로
     * 두께를 가진 십자 형태를 만들 수 있습니다.
     */
    private static bool IsInsideCrossPattern(
        Block sourceBlock,
        Block targetBlock,
        int range)
    {
        bool overlapsSourceRows =
            targetBlock.EndRow >=
            sourceBlock.StartRow &&
            targetBlock.StartRow <=
            sourceBlock.EndRow;

        bool isInsideHorizontalRange =
            targetBlock.EndColumn >=
            sourceBlock.StartColumn -
            range &&
            targetBlock.StartColumn <=
            sourceBlock.EndColumn +
            range;

        bool isHorizontalTarget =
            overlapsSourceRows &&
            isInsideHorizontalRange;

        bool overlapsSourceColumns =
            targetBlock.EndColumn >=
            sourceBlock.StartColumn &&
            targetBlock.StartColumn <=
            sourceBlock.EndColumn;

        bool isInsideVerticalRange =
            targetBlock.EndRow >=
            sourceBlock.StartRow -
            range &&
            targetBlock.StartRow <=
            sourceBlock.EndRow +
            range;

        bool isVerticalTarget =
            overlapsSourceColumns &&
            isInsideVerticalRange;

        return
            isHorizontalTarget ||
            isVerticalTarget;
    }

    /*
     * 중심 블록과 대상 블록이 점유한 셀을 비교합니다.
     *
     * 두 셀의 가로 거리와 세로 거리가 같으면
     * 대각선 위에 있는 것으로 판정합니다.
     */
    private static bool IsInsideDiagonalPattern(
        Block sourceBlock,
        Block targetBlock,
        int range)
    {
        for (int sourceColumn =
                 sourceBlock.StartColumn;
             sourceColumn <=
             sourceBlock.EndColumn;
             sourceColumn++)
        {
            for (int sourceRow =
                     sourceBlock.StartRow;
                 sourceRow <=
                 sourceBlock.EndRow;
                 sourceRow++)
            {
                for (int targetColumn =
                         targetBlock.StartColumn;
                     targetColumn <=
                     targetBlock.EndColumn;
                     targetColumn++)
                {
                    int columnDistance =
                        Mathf.Abs(
                            targetColumn -
                            sourceColumn
                        );

                    if (columnDistance <= 0 ||
                        columnDistance > range)
                    {
                        continue;
                    }

                    for (int targetRow =
                             targetBlock.StartRow;
                         targetRow <=
                         targetBlock.EndRow;
                         targetRow++)
                    {
                        int rowDistance =
                            Mathf.Abs(
                                targetRow -
                                sourceRow
                            );

                        if (rowDistance <= 0 ||
                            rowDistance > range)
                        {
                            continue;
                        }

                        if (columnDistance ==
                            rowDistance)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
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
         * 서로 다른 보드에서 같은 좌표를 사용하는 블록은
         * 폭발 대상으로 포함하지 않습니다.
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

    private static bool IsInsideBounds(
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

        return
            overlapsColumns &&
            overlapsRows;
    }
}