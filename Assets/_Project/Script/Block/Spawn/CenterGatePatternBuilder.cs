using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CenterGatePatternBuilder
{
    [Tooltip(
        "중앙 성문 덩어리의 가로 폭입니다.\n" +
        "홀수만 사용하며 9열 보드에서는 3을 권장합니다."
    )]
    [SerializeField, Min(1)]
    private int centerMassWidth = 3;

    [Tooltip(
        "좌우에 항상 남겨 둘 최소 진입로 폭입니다.\n" +
        "9열 보드에서는 2를 권장합니다."
    )]
    [SerializeField, Min(1)]
    private int minimumSideLaneWidth = 2;

    [Tooltip(
        "발사 지점과 가장 가까운 성문 끝부분의 폭입니다.\n" +
        "중앙 시작점에서 좌우 선택이 가능하도록 1을 권장합니다."
    )]
    [SerializeField, Min(1)]
    private int entranceTipWidth = 1;

    public void Normalize(
        int availableColumns)
    {
        availableColumns =
            Mathf.Max(
                availableColumns,
                1
            );

        minimumSideLaneWidth =
            Mathf.Clamp(
                minimumSideLaneWidth,
                1,
                Mathf.Max(
                    (availableColumns - 3) /
                    2,
                    1
                )
            );

        int maximumMassWidth =
            Mathf.Max(
                availableColumns -
                minimumSideLaneWidth * 2 -
                2,
                1
            );

        centerMassWidth =
            NormalizeOddWidth(
                centerMassWidth,
                maximumMassWidth
            );

        entranceTipWidth =
            NormalizeOddWidth(
                entranceTipWidth,
                centerMassWidth
            );
    }

    public bool CanBuild(
        int columnCount)
    {
        Normalize(
            columnCount
        );

        return columnCount >= 7;
    }

    public int GetTargetBlockCountPerRow(
        int row,
        int rowCount,
        int columnCount)
    {
        Normalize(
            columnCount
        );

        int distanceFromEntrance =
            GetDistanceFromEntrance(
                row,
                rowCount
            );

        if (distanceFromEntrance == 0)
        {
            return entranceTipWidth;
        }

        return centerMassWidth +
               (HasShoulder(
                    distanceFromEntrance)
                   ? 1
                   : 0);
    }

    public List<int> CreateColumnPriority(
        int row,
        int rowCount,
        int columnCount,
        bool firstShoulderOnLeft)
    {
        Normalize(
            columnCount
        );

        int distanceFromEntrance =
            GetDistanceFromEntrance(
                row,
                rowCount
            );

        int rowMassWidth =
            distanceFromEntrance == 0
                ? entranceTipWidth
                : centerMassWidth;

        int centerStartColumn =
            (columnCount -
             rowMassWidth) /
            2;

        List<int> result =
            new List<int>();

        for (int offset = 0;
             offset < rowMassWidth;
             offset++)
        {
            result.Add(
                centerStartColumn +
                offset
            );
        }

        if (HasShoulder(
                distanceFromEntrance))
        {
            int shoulderIndex =
                (distanceFromEntrance - 2) /
                2;

            bool shoulderOnLeft =
                shoulderIndex % 2 == 0
                    ? firstShoulderOnLeft
                    : !firstShoulderOnLeft;

            int shoulderColumn =
                shoulderOnLeft
                    ? centerStartColumn - 1
                    : centerStartColumn +
                      rowMassWidth;

            if (shoulderColumn >= 0 &&
                shoulderColumn < columnCount)
            {
                result.Add(
                    shoulderColumn
                );
            }
        }

        List<int> remainingColumns =
            new List<int>();

        for (int distance = 1;
             distance < columnCount;
             distance++)
        {
            int leftColumn =
                centerStartColumn -
                distance;

            int rightColumn =
                centerStartColumn +
                rowMassWidth - 1 +
                distance;

            AddIfAvailable(
                remainingColumns,
                result,
                leftColumn,
                columnCount
            );

            AddIfAvailable(
                remainingColumns,
                result,
                rightColumn,
                columnCount
            );
        }

        result.AddRange(
            remainingColumns
        );

        return result;
    }

    private bool HasShoulder(
        int distanceFromEntrance)
    {
        return distanceFromEntrance >= 2 &&
               distanceFromEntrance % 2 == 0;
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

    private int NormalizeOddWidth(
        int width,
        int maximumWidth)
    {
        maximumWidth =
            Mathf.Max(
                maximumWidth,
                1
            );

        if (maximumWidth % 2 == 0)
        {
            maximumWidth--;
        }

        width =
            Mathf.Clamp(
                width,
                1,
                maximumWidth
            );

        if (width % 2 == 0)
        {
            width--;
        }

        return Mathf.Max(
            width,
            1
        );
    }

    private void AddIfAvailable(
        List<int> destination,
        List<int> occupied,
        int column,
        int columnCount)
    {
        if (column < 0 ||
            column >= columnCount ||
            occupied.Contains(
                column) ||
            destination.Contains(
                column))
        {
            return;
        }

        destination.Add(
            column
        );
    }
}
