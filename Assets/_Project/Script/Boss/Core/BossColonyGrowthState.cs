using System.Collections.Generic;
using UnityEngine;

public sealed class BossColonyGrowthReservation
{
    public Block Parent
    {
        get;
    }

    public Vector2Int Cell
    {
        get;
    }

    public BossColonyGrowthReservation(
        Block parent,
        Vector2Int cell)
    {
        Parent = parent;
        Cell = cell;
    }
}

/*
 * 증식 가능한 군체 블록과 다음 증식 예약만 관리한다.
 * 블록 생성과 연출은 BossEncounterController가 담당한다.
 */
public sealed class BossColonyGrowthState
{
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private readonly List<Block> members =
        new List<Block>();

    private readonly List<Block> parentBuffer =
        new List<Block>();

    private readonly List<BossColonyGrowthReservation>
        reservations =
            new List<BossColonyGrowthReservation>();

    private readonly HashSet<Vector2Int> occupiedCells =
        new HashSet<Vector2Int>();

    private readonly HashSet<Vector2Int> reservedCells =
        new HashSet<Vector2Int>();

    private readonly List<Vector2Int> directionBuffer =
        new List<Vector2Int>(Directions.Length);

    private readonly List<Vector2Int> targetCellBuffer =
        new List<Vector2Int>();

    private Vector2 colonyCenter;

    public IReadOnlyList<BossColonyGrowthReservation> Reservations =>
        reservations;

    public void Clear()
    {
        members.Clear();
        parentBuffer.Clear();
        reservations.Clear();
        occupiedCells.Clear();
        reservedCells.Clear();
        directionBuffer.Clear();
        targetCellBuffer.Clear();
    }

    public void RegisterMember(
        Block block)
    {
        if (block == null ||
            members.Contains(block))
        {
            return;
        }

        members.Add(block);
    }

    public int PrepareReservations(
        BoardGrid boardGrid,
        IReadOnlyList<Block> blockers,
        int maximumSpawnRow)
    {
        reservations.Clear();
        occupiedCells.Clear();
        reservedCells.Clear();
        parentBuffer.Clear();

        if (boardGrid == null)
        {
            return 0;
        }

        maximumSpawnRow =
            Mathf.Clamp(
                maximumSpawnRow,
                0,
                boardGrid.RowCount - 1
            );

        CollectOccupiedCells(blockers);
        CollectLivingParents();
        CalculateColonyCenter();
        Shuffle(parentBuffer);

        for (int i = 0;
             i < parentBuffer.Count;
             i++)
        {
            Block parent =
                parentBuffer[i];

            CollectGrowthTargets(parent);
            Shuffle(targetCellBuffer);

            bool foundTarget = false;
            Vector2Int selectedTarget = default;
            float selectedDistance = float.MinValue;

            for (int directionIndex = 0;
                 directionIndex < targetCellBuffer.Count;
                 directionIndex++)
            {
                Vector2Int targetCell =
                    targetCellBuffer[directionIndex];

                if (targetCell.x < 0 ||
                    targetCell.x >= boardGrid.ColumnCount ||
                    targetCell.y < 0 ||
                    targetCell.y > maximumSpawnRow ||
                    occupiedCells.Contains(targetCell) ||
                    reservedCells.Contains(targetCell))
                {
                    continue;
                }

                // 이미 둘 이상의 블록과 맞닿는 칸은 내부를 메우는
                // 위치이므로 건너뛰어 군체가 가지처럼 바깥으로 퍼지게 한다.
                if (CountOccupiedNeighbors(targetCell) > 1)
                {
                    continue;
                }

                float outwardDistance =
                    (targetCell - colonyCenter).sqrMagnitude;

                if (foundTarget &&
                    outwardDistance <= selectedDistance)
                {
                    continue;
                }

                foundTarget = true;
                selectedTarget = targetCell;
                selectedDistance = outwardDistance;
            }

            if (!foundTarget)
            {
                continue;
            }

            reservations.Add(
                new BossColonyGrowthReservation(
                    parent,
                    selectedTarget
                )
            );

            reservedCells.Add(selectedTarget);
        }

        return reservations.Count;
    }

    private void CollectGrowthTargets(Block parent)
    {
        targetCellBuffer.Clear();

        if (parent == null || !parent.HasGridPosition)
        {
            return;
        }

        for (int row = parent.StartRow;
             row <= parent.EndRow;
             row++)
        {
            for (int column = parent.StartColumn;
                 column <= parent.EndColumn;
                 column++)
            {
                Vector2Int occupiedCell =
                    new Vector2Int(column, row);

                for (int i = 0; i < Directions.Length; i++)
                {
                    Vector2Int target =
                        occupiedCell + Directions[i];

                    if (!occupiedCells.Contains(target) &&
                        !targetCellBuffer.Contains(target))
                    {
                        targetCellBuffer.Add(target);
                    }
                }
            }
        }
    }

    private int CountOccupiedNeighbors(
        Vector2Int cell)
    {
        int count = 0;

        for (int i = 0; i < Directions.Length; i++)
        {
            Vector2Int neighbor = cell + Directions[i];

            if (occupiedCells.Contains(neighbor) ||
                reservedCells.Contains(neighbor))
            {
                count++;
            }
        }

        return count;
    }

    private void CalculateColonyCenter()
    {
        if (parentBuffer.Count <= 0)
        {
            colonyCenter = Vector2.zero;
            return;
        }

        Vector2 total = Vector2.zero;

        for (int i = 0; i < parentBuffer.Count; i++)
        {
            Block parent = parentBuffer[i];
            total += (Vector2)parent.GridPosition +
                     ((Vector2)parent.GridSize - Vector2.one) * 0.5f;
        }

        colonyCenter = total / parentBuffer.Count;
    }

    public bool CanResolve(
        BossColonyGrowthReservation reservation)
    {
        return reservation != null &&
               reservation.Parent != null &&
               reservation.Parent.IsAlive &&
               reservation.Parent.HasGridPosition;
    }

    public void ClearReservations()
    {
        reservations.Clear();
        reservedCells.Clear();
    }

    private void CollectLivingParents()
    {
        for (int i = members.Count - 1;
             i >= 0;
             i--)
        {
            Block member = members[i];

            if (member == null ||
                !member.IsAlive ||
                !member.HasGridPosition)
            {
                members.RemoveAt(i);
                continue;
            }

            parentBuffer.Add(member);
        }
    }

    private void CollectOccupiedCells(
        IReadOnlyList<Block> blockers)
    {
        if (blockers == null)
        {
            return;
        }

        for (int i = 0;
             i < blockers.Count;
             i++)
        {
            Block blocker = blockers[i];

            if (blocker == null ||
                !blocker.IsAlive ||
                !blocker.HasGridPosition)
            {
                continue;
            }

            for (int row = blocker.StartRow;
                 row <= blocker.EndRow;
                 row++)
            {
                for (int column = blocker.StartColumn;
                     column <= blocker.EndColumn;
                     column++)
                {
                    occupiedCells.Add(
                        new Vector2Int(
                            column,
                            row
                        )
                    );
                }
            }
        }
    }

    private static void Shuffle<T>(
        List<T> items)
    {
        for (int i = items.Count - 1;
             i > 0;
             i--)
        {
            int swapIndex =
                Random.Range(
                    0,
                    i + 1
                );

            T temporary = items[i];
            items[i] = items[swapIndex];
            items[swapIndex] = temporary;
        }
    }
}
