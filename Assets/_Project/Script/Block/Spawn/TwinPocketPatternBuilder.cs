using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public sealed class TwinPocketPatternBuilder
{
    [Tooltip(
        "각 벽과 주 블록 라인 사이의 빈 칸 수입니다.\n" +
        "1이면 좌우에 1칸 폭 포켓을 만듭니다."
    )]
    [SerializeField, Min(1)]
    private int corridorOffset = 1;

    [Tooltip(
        "각 포켓 군집이 안쪽으로 사용하는 블록 열 수입니다.\n" +
        "9열 보드에서는 2를 권장합니다."
    )]
    [SerializeField, Min(1)]
    private int clusterDepth = 2;

    public void Normalize(
        int availableColumns)
    {
        availableColumns =
            Mathf.Max(
                availableColumns,
                1
            );

        corridorOffset =
            Mathf.Clamp(
                corridorOffset,
                1,
                Mathf.Max(
                    (availableColumns - 3) / 2,
                    1
                )
            );

        clusterDepth =
            Mathf.Clamp(
                clusterDepth,
                1,
                GetMaximumClusterDepth(
                    availableColumns
                )
            );
    }

    public bool CanBuild(
        int columnCount)
    {
        Normalize(
            columnCount
        );

        return columnCount >=
               corridorOffset * 2 +
               clusterDepth * 2 +
               3;
    }

    public int GetTargetBlockCountPerRow(
        int columnCount)
    {
        Normalize(
            columnCount
        );

        return Mathf.Min(
            clusterDepth * 2,
            columnCount
        );
    }

    public void SelectEntryRows(
        int rowCount,
        out int leftEntryRow,
        out int rightEntryRow)
    {
        rowCount =
            Mathf.Max(
                rowCount,
                1
            );

        int firstEntryRow =
            Mathf.Clamp(
                rowCount / 2,
                0,
                rowCount - 1
            );

        int primaryEntryRow =
            Random.Range(
                firstEntryRow,
                rowCount
            );

        bool primaryPocketOnLeft =
            Random.value < 0.5f;

        if (primaryPocketOnLeft)
        {
            leftEntryRow =
                primaryEntryRow;

            rightEntryRow =
                rowCount;

            return;
        }

        leftEntryRow =
            rowCount;

        rightEntryRow =
            primaryEntryRow;
    }

    public List<int> CreateColumnPriority(
        int row,
        int columnCount,
        int leftEntryRow,
        int rightEntryRow)
    {
        Normalize(
            columnCount
        );

        List<int> result =
            new List<int>();

        int zigzagEntryRow =
            Mathf.Min(
                leftEntryRow,
                rightEntryRow
            );

        bool primaryPocketOnLeft =
            leftEntryRow <
            rightEntryRow;

        bool isInsideZigzag =
            row >= zigzagEntryRow;

        bool usePrimarySide =
            (row - zigzagEntryRow) % 2 == 0;

        bool shiftLeft =
            isInsideZigzag &&
            (primaryPocketOnLeft ==
             usePrimarySide);

        bool shiftRight =
            isInsideZigzag &&
            !shiftLeft;

        int leftStartColumn =
            corridorOffset +
            (shiftLeft
                ? 1
                : 0);

        int rightStartColumn =
            columnCount - 1 -
            corridorOffset -
            (shiftRight
                ? 1
                : 0);

        bool canApplyUpperRowNoise =
            !isInsideZigzag &&
            clusterDepth >= 2;

        bool noiseLeftInnerEdge =
            canApplyUpperRowNoise &&
            Random.value < 0.5f;

        bool noiseRightInnerEdge =
            canApplyUpperRowNoise &&
            !noiseLeftInnerEdge;

        for (int depth = 0;
             depth < clusterDepth;
             depth++)
        {
            int leftDepthNoise =
                noiseLeftInnerEdge &&
                depth == clusterDepth - 1
                    ? 1
                    : 0;

            int rightDepthNoise =
                noiseRightInnerEdge &&
                depth == clusterDepth - 1
                    ? 1
                    : 0;

            AddIfValid(
                result,
                leftStartColumn +
                depth +
                leftDepthNoise,
                columnCount
            );

            AddIfValid(
                result,
                rightStartColumn -
                depth -
                rightDepthNoise,
                columnCount
            );
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

        float center =
            (columnCount - 1) *
            0.5f;

        remainingColumns.Sort(
            (left, right) =>
                Mathf.Abs(
                    right - center
                ).CompareTo(
                    Mathf.Abs(
                        left - center
                    )
                )
        );

        result.AddRange(
            remainingColumns
        );

        return result;
    }

    private int GetMaximumClusterDepth(
        int columnCount)
    {
        return Mathf.Max(
            (columnCount -
             corridorOffset * 2 -
             3) /
            2,
            1
        );
    }

    private void AddIfValid(
        List<int> columns,
        int column,
        int columnCount)
    {
        if (column < 0 ||
            column >= columnCount ||
            columns.Contains(
                column))
        {
            return;
        }

        columns.Add(
            column
        );
    }
}
