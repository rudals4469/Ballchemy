using System;
using UnityEngine;

[Serializable]
public sealed class BlockGridState
{
    [Header("Runtime Grid")]
    [SerializeField]
    private BoardGrid boardGrid;

    [Tooltip(
        "블록 점유 영역의 왼쪽 위 시작 셀입니다."
    )]
    [SerializeField]
    private Vector2Int gridPosition =
        new Vector2Int(
            -1,
            -1
        );

    [SerializeField]
    private bool hasGridPosition;

    public BoardGrid BoardGrid =>
        boardGrid;

    public bool HasGridPosition =>
        hasGridPosition;

    public Vector2Int GridPosition =>
        gridPosition;

    public int StartColumn =>
        gridPosition.x;

    public int StartRow =>
        gridPosition.y;

    public void Set(
        BoardGrid targetBoardGrid,
        int startColumn,
        int startRow)
    {
        boardGrid =
            targetBoardGrid;

        gridPosition =
            new Vector2Int(
                startColumn,
                startRow
            );

        hasGridPosition =
            targetBoardGrid != null;
    }

    public bool MoveRows(
        int rowAmount)
    {
        if (boardGrid == null ||
            !hasGridPosition)
        {
            return false;
        }

        gridPosition =
            new Vector2Int(
                gridPosition.x,
                gridPosition.y +
                rowAmount
            );

        return true;
    }

    public int GetEndColumn(
        Vector2Int gridSize)
    {
        if (!hasGridPosition)
        {
            return -1;
        }

        return StartColumn +
               Mathf.Max(
                   gridSize.x,
                   1
               ) -
               1;
    }

    public int GetEndRow(
        Vector2Int gridSize)
    {
        if (!hasGridPosition)
        {
            return -1;
        }

        return StartRow +
               Mathf.Max(
                   gridSize.y,
                   1
               ) -
               1;
    }

    public int GetBottomRow(
        Vector2Int gridSize)
    {
        return GetEndRow(
            gridSize
        );
    }

    public bool IsInsideBoard(
        Vector2Int gridSize)
    {
        if (boardGrid == null ||
            !hasGridPosition)
        {
            return false;
        }

        return StartColumn >= 0 &&
               StartRow >= 0 &&
               GetEndColumn(
                   gridSize
               ) <
               boardGrid.ColumnCount &&
               GetEndRow(
                   gridSize
               ) <
               boardGrid.RowCount;
    }

    public bool IsTouchingBottomRow(
        Vector2Int gridSize)
    {
        if (boardGrid == null ||
            !hasGridPosition)
        {
            return false;
        }

        return GetBottomRow(
                   gridSize
               ) ==
               boardGrid.RowCount - 1;
    }

    public bool IsOutsideBottom(
        Vector2Int gridSize)
    {
        if (boardGrid == null ||
            !hasGridPosition)
        {
            return false;
        }

        return GetBottomRow(
                   gridSize
               ) >=
               boardGrid.RowCount;
    }

    public bool OccupiesCell(
        int column,
        int row,
        Vector2Int gridSize)
    {
        if (!hasGridPosition)
        {
            return false;
        }

        return column >= StartColumn &&
               column <=
               GetEndColumn(
                   gridSize
               ) &&
               row >= StartRow &&
               row <=
               GetEndRow(
                   gridSize
               );
    }

    public Vector3 GetWorldPosition(
        Transform fallbackTransform,
        Vector2Int gridSize)
    {
        if (boardGrid == null ||
            !hasGridPosition)
        {
            return fallbackTransform != null
                ? fallbackTransform.position
                : Vector3.zero;
        }

        Vector3 startCellPosition =
            boardGrid.GetCellWorldPosition(
                StartColumn,
                StartRow
            );

        float horizontalDistance =
            (
                Mathf.Max(
                    gridSize.x,
                    1
                ) -
                1
            ) *
            boardGrid.CellSize *
            0.5f;

        float verticalDistance =
            (
                Mathf.Max(
                    gridSize.y,
                    1
                ) -
                1
            ) *
            boardGrid.CellSize *
            0.5f;

        Vector3 horizontalOffset =
            boardGrid.transform.right *
            horizontalDistance;

        Vector3 verticalOffset =
            -boardGrid.transform.up *
            verticalDistance;

        return startCellPosition +
               horizontalOffset +
               verticalOffset;
    }

    public void Snap(
        Transform targetTransform,
        Vector2Int gridSize)
    {
        if (targetTransform == null ||
            boardGrid == null ||
            !hasGridPosition)
        {
            return;
        }

        targetTransform.position =
            GetWorldPosition(
                targetTransform,
                gridSize
            );

        targetTransform.rotation =
            boardGrid.transform.rotation;
    }

    public void Clear()
    {
        boardGrid =
            null;

        gridPosition =
            new Vector2Int(
                -1,
                -1
            );

        hasGridPosition =
            false;
    }
}