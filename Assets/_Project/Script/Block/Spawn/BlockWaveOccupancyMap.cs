using UnityEngine;

public sealed class BlockWaveOccupancyMap
{
    private readonly bool[,] occupiedCells;

    public int ColumnCount { get; }

    public int RowCount { get; }

    public BlockWaveOccupancyMap(
        int columnCount,
        int rowCount)
    {
        ColumnCount =
            Mathf.Max(
                columnCount,
                1
            );

        RowCount =
            Mathf.Max(
                rowCount,
                1
            );

        occupiedCells =
            new bool[
                ColumnCount,
                RowCount
            ];
    }

    public bool IsInside(
        int column,
        int row)
    {
        return column >= 0 &&
               column < ColumnCount &&
               row >= 0 &&
               row < RowCount;
    }

    public bool CanOccupy(
        int startColumn,
        int startRow,
        Vector2Int gridSize)
    {
        gridSize =
            NormalizeGridSize(
                gridSize
            );

        if (startColumn < 0 ||
            startRow < 0)
        {
            return false;
        }

        int endColumnExclusive =
            startColumn +
            gridSize.x;

        int endRowExclusive =
            startRow +
            gridSize.y;

        if (endColumnExclusive >
            ColumnCount)
        {
            return false;
        }

        if (endRowExclusive >
            RowCount)
        {
            return false;
        }

        for (int row = startRow;
             row < endRowExclusive;
             row++)
        {
            for (int column = startColumn;
                 column < endColumnExclusive;
                 column++)
            {
                if (occupiedCells[
                        column,
                        row])
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool TryOccupy(
        int startColumn,
        int startRow,
        Vector2Int gridSize)
    {
        if (!CanOccupy(
                startColumn,
                startRow,
                gridSize))
        {
            return false;
        }

        Occupy(
            startColumn,
            startRow,
            gridSize
        );

        return true;
    }

    public void Occupy(
        int startColumn,
        int startRow,
        Vector2Int gridSize)
    {
        gridSize =
            NormalizeGridSize(
                gridSize
            );

        int endColumnExclusive =
            startColumn +
            gridSize.x;

        int endRowExclusive =
            startRow +
            gridSize.y;

        for (int row = startRow;
             row < endRowExclusive;
             row++)
        {
            for (int column = startColumn;
                 column < endColumnExclusive;
                 column++)
            {
                if (!IsInside(
                        column,
                        row))
                {
                    continue;
                }

                occupiedCells[
                    column,
                    row
                ] = true;
            }
        }
    }

    public bool IsOccupied(
        int column,
        int row)
    {
        if (!IsInside(
                column,
                row))
        {
            return false;
        }

        return occupiedCells[
            column,
            row
        ];
    }

    private Vector2Int NormalizeGridSize(
        Vector2Int gridSize)
    {
        return new Vector2Int(
            Mathf.Max(
                gridSize.x,
                1
            ),
            Mathf.Max(
                gridSize.y,
                1
            )
        );
    }
}