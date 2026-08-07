using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public sealed class ZigzagCorridorPatternBuilder
{
    [Tooltip(
        "일반 지그재그 장벽을 구성하는 블록 수입니다.\n" +
        "9열 보드에서는 5를 권장합니다."
    )]
    [SerializeField, Min(2)]
    private int barrierWidth = 5;

    [Tooltip(
        "발사 지점과 가장 가까운 첫 장벽의 블록 수입니다.\n" +
        "9열 보드에서는 중앙 열을 포함해 입장할 수 있도록 4를 권장합니다."
    )]
    [SerializeField, Min(1)]
    private int entranceBarrierWidth = 4;

    [Tooltip(
        "장벽 사이 완충 행에 둘 가장자리 가이드 블록 수입니다."
    )]
    [SerializeField, Min(0)]
    private int spacerBlockCount = 1;

    public void Normalize(
        int availableColumns)
    {
        availableColumns =
            Mathf.Max(
                availableColumns,
                1
            );

        int maximumAllowedBarrierWidth =
            Mathf.Max(
                availableColumns - 3,
                1
            );

        barrierWidth =
            Mathf.Clamp(
                barrierWidth,
                1,
                maximumAllowedBarrierWidth
            );

        entranceBarrierWidth =
            Mathf.Clamp(
                entranceBarrierWidth,
                1,
                Mathf.Max(
                    barrierWidth - 1,
                    1
                )
            );

        spacerBlockCount =
            Mathf.Clamp(
                spacerBlockCount,
                0,
                Mathf.Max(
                    availableColumns -
                    barrierWidth,
                    0
                )
            );
    }

    public bool CanBuild(
        int columnCount)
    {
        Normalize(
            columnCount
        );

        return columnCount >= 5;
    }

    public int GetTargetBlockCountPerRow(
        int row,
        int rowCount,
        int columnCount)
    {
        Normalize(
            columnCount
        );

        if (row <= 0)
        {
            return 0;
        }

        int distanceFromEntrance =
            GetDistanceFromEntrance(
                row,
                rowCount
            );

        if (distanceFromEntrance == 0)
        {
            return entranceBarrierWidth;
        }

        return distanceFromEntrance % 2 == 1
            ? spacerBlockCount
            : barrierWidth;
    }

    public List<int> CreateColumnPriority(
        int row,
        int rowCount,
        int targetBlockCount,
        int columnCount,
        bool firstOpeningOnLeft)
    {
        Normalize(
            columnCount
        );

        int distanceFromEntrance =
            GetDistanceFromEntrance(
                row,
                rowCount
            );

        bool isSpacerRow =
            distanceFromEntrance % 2 == 1;

        int corridorStep =
            distanceFromEntrance /
            2;

        bool openingOnLeft =
            corridorStep % 2 == 0
                ? firstOpeningOnLeft
                : !firstOpeningOnLeft;

        int maximumTargetBlockCount =
            isSpacerRow
                ? spacerBlockCount
                : distanceFromEntrance == 0
                    ? entranceBarrierWidth
                    : barrierWidth;

        targetBlockCount =
            Mathf.Clamp(
                targetBlockCount,
                0,
                maximumTargetBlockCount
            );

        if (targetBlockCount <= 0)
        {
            return new List<int>();
        }

        int barrierStartColumn =
            openingOnLeft
                ? columnCount -
                  targetBlockCount
                : 0;

        List<int> result =
            new List<int>();

        if (isSpacerRow)
        {
            AddSpacerColumns(
                result,
                targetBlockCount,
                columnCount,
                openingOnLeft
            );
        }
        else
        {
            for (int offset = 0;
                 offset < targetBlockCount;
                 offset++)
            {
                result.Add(
                    barrierStartColumn +
                    offset
                );
            }
        }

        List<int> remainingColumns =
            new List<int>();

        for (int column = 0;
             column < columnCount;
             column++)
        {
            if (!result.Contains(
                    column))
            {
                remainingColumns.Add(
                    column
                );
            }
        }

        if (openingOnLeft)
        {
            remainingColumns.Sort(
                (left, right) =>
                    right.CompareTo(
                        left
                    )
            );
        }
        else
        {
            remainingColumns.Sort();
        }

        result.AddRange(
            remainingColumns
        );

        return result;
    }

    private int GetDistanceFromEntrance(
        int row,
        int rowCount)
    {
        rowCount =
            Mathf.Max(
                rowCount,
                1
            );

        row =
            Mathf.Clamp(
                row,
                0,
                rowCount - 1
            );

        return rowCount - 1 - row;
    }

    private void AddSpacerColumns(
        List<int> columns,
        int targetBlockCount,
        int columnCount,
        bool openingOnLeft)
    {
        int firstColumn =
            openingOnLeft
                ? columnCount - 1
                : 0;

        int direction =
            openingOnLeft
                ? -1
                : 1;

        int noiseOffset =
            targetBlockCount == 1 &&
            columnCount >= 2
                ? Random.Range(
                    0,
                    2
                )
                : 0;

        firstColumn +=
            direction *
            noiseOffset;

        for (int i = 0;
             i < targetBlockCount;
             i++)
        {
            int column =
                firstColumn +
                direction * i;

            if (column < 0 ||
                column >= columnCount)
            {
                continue;
            }

            columns.Add(
                column
            );
        }
    }
}
