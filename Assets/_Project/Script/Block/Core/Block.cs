using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class Block : MonoBehaviour
{
    [Header("Definition")]
    [SerializeField]
    private BlockDefinition definition;

    [Header("Presentation")]
    [SerializeField]
    private BlockLayout layout =
        new BlockLayout();

    [Header("Grid State")]
    [SerializeField]
    private BlockGridState gridState =
        new BlockGridState();

    [Header("Runtime Layout")]
    [SerializeField, Min(0.1f)]
    private float runtimeCellSize = 1f;

    [Header("Runtime Stats")]
    [SerializeField, Min(1)]
    private int maxHealth = 8;

    [SerializeField, Min(0)]
    private int attackPower = 2;

    private int currentHealth;

    private bool isDestructionStarted;

    public BlockDefinition Definition =>
        definition;

    public string BlockId =>
        definition != null
            ? definition.BlockId
            : string.Empty;

    public BlockType BlockType =>
        definition != null
            ? definition.BlockType
            : BlockType.Normal;

    public BlockDestructionRule DestructionRule =>
        definition != null
            ? definition.DestructionRule
            : BlockDestructionRule.Breakable;

    public Vector2Int GridSize =>
        definition != null
            ? definition.GridSize
            : Vector2Int.one;

    public float CellSize =>
        runtimeCellSize;

    public Vector2 WorldSize =>
        new Vector2(
            GridSize.x *
            runtimeCellSize,

            GridSize.y *
            runtimeCellSize
        );

    public BoardGrid BoardGrid =>
        gridState != null
            ? gridState.BoardGrid
            : null;

    public bool HasGridPosition =>
        gridState != null &&
        gridState.HasGridPosition;

    public Vector2Int GridPosition =>
        gridState != null
            ? gridState.GridPosition
            : new Vector2Int(
                -1,
                -1
            );

    public int StartColumn =>
        HasGridPosition
            ? gridState.StartColumn
            : -1;

    public int StartRow =>
        HasGridPosition
            ? gridState.StartRow
            : -1;

    public int EndColumn =>
        HasGridPosition
            ? gridState.GetEndColumn(
                GridSize
            )
            : -1;

    public int EndRow =>
        HasGridPosition
            ? gridState.GetEndRow(
                GridSize
            )
            : -1;

    public int BottomRow =>
        HasGridPosition
            ? gridState.GetBottomRow(
                GridSize
            )
            : -1;

    public int CurrentHealth =>
        currentHealth;

    public int MaxHealth =>
        maxHealth;

    public int AttackPower =>
        attackPower;

    public bool IsAlive =>
        currentHealth > 0 &&
        !isDestructionStarted;

    public bool IsDestructionStarted =>
        isDestructionStarted;

    public bool IsBreakable =>
        DestructionRule ==
        BlockDestructionRule.Breakable;

    public bool IsIndestructible =>
        DestructionRule ==
        BlockDestructionRule.Indestructible;

    public bool IsInsideBoard =>
        gridState != null &&
        gridState.IsInsideBoard(
            GridSize
        );

    public bool IsTouchingBottomRow =>
        gridState != null &&
        gridState.IsTouchingBottomRow(
            GridSize
        );

    public bool IsOutsideBottom =>
        gridState != null &&
        gridState.IsOutsideBottom(
            GridSize
        );

    public event Action<int, int>
        HealthChanged;

    public event Action<BlockDefinition>
        DefinitionChanged;

    public event Action<Vector2Int, float>
        LayoutChanged;

    public event Action<Block, int>
        HitReceived;

    public event Action<Block, Vector2Int>
        GridPositionChanged;

    public event Action<Block>
        Destroyed;

    private void Awake()
    {
        EnsureHelperObjects();

        isDestructionStarted = false;

        currentHealth =
            maxHealth;

        ApplyPresentation();
        RefreshGridPlacement();
    }

    private void OnValidate()
    {
        maxHealth =
            Mathf.Max(
                maxHealth,
                1
            );

        attackPower =
            Mathf.Max(
                attackPower,
                0
            );

        runtimeCellSize =
            Mathf.Max(
                runtimeCellSize,
                0.1f
            );

        EnsureHelperObjects();

        layout.Validate(
            gameObject
        );
    }

    private void EnsureHelperObjects()
    {
        if (layout == null)
        {
            layout =
                new BlockLayout();
        }

        if (gridState == null)
        {
            gridState =
                new BlockGridState();
        }
    }

    public void Initialize(
        int health,
        int attack)
    {
        EnsureHelperObjects();

        isDestructionStarted = false;

        SetRuntimeStats(
            health,
            attack
        );

        ApplyPresentation();
        RefreshGridPlacement();

        gameObject.SetActive(
            true
        );

        NotifyLayoutChanged();
    }

    public void Initialize(
        BlockDefinition blockDefinition,
        int health,
        int attack)
    {
        Initialize(
            blockDefinition,
            health,
            attack,
            runtimeCellSize
        );
    }

    public void Initialize(
        BlockDefinition blockDefinition,
        int health,
        int attack,
        float cellSize)
    {
        EnsureHelperObjects();

        isDestructionStarted = false;

        definition =
            blockDefinition;

        runtimeCellSize =
            Mathf.Max(
                cellSize,
                0.1f
            );

        SetRuntimeStats(
            health,
            attack
        );

        ApplyPresentation();
        RefreshGridPlacement();

        gameObject.SetActive(
            true
        );

        DefinitionChanged?.Invoke(
            definition
        );

        NotifyLayoutChanged();
    }

    public void ApplyDefinition(
        BlockDefinition blockDefinition)
    {
        ApplyDefinition(
            blockDefinition,
            runtimeCellSize
        );
    }

    public void ApplyDefinition(
        BlockDefinition blockDefinition,
        float cellSize)
    {
        if (blockDefinition == null)
        {
            Debug.LogWarning(
                "Block: 적용하려는 " +
                "BlockDefinition이 비어 있습니다.",
                this
            );

            return;
        }

        EnsureHelperObjects();

        definition =
            blockDefinition;

        runtimeCellSize =
            Mathf.Max(
                cellSize,
                0.1f
            );

        ApplyPresentation();
        RefreshGridPlacement();

        DefinitionChanged?.Invoke(
            definition
        );

        NotifyLayoutChanged();
    }

    public void SetGridPosition(
        BoardGrid targetBoardGrid,
        int startColumn,
        int startRow,
        bool snapToWorld = true)
    {
        if (targetBoardGrid == null)
        {
            Debug.LogWarning(
                "Block: Grid Position을 설정하려 했지만 " +
                "BoardGrid가 비어 있습니다.",
                this
            );

            return;
        }

        EnsureHelperObjects();

        runtimeCellSize =
            Mathf.Max(
                targetBoardGrid.CellSize,
                0.1f
            );

        gridState.Set(
            targetBoardGrid,
            startColumn,
            startRow
        );

        ApplyPresentation();

        if (snapToWorld)
        {
            gridState.Snap(
                transform,
                GridSize
            );
        }

        NotifyLayoutChanged();

        GridPositionChanged?.Invoke(
            this,
            GridPosition
        );
    }

    public void SetGridPosition(
        int startColumn,
        int startRow,
        bool snapToWorld = true)
    {
        if (BoardGrid == null)
        {
            Debug.LogWarning(
                "Block: 기존 BoardGrid가 없어 " +
                "Grid Position을 변경할 수 없습니다.",
                this
            );

            return;
        }

        SetGridPosition(
            BoardGrid,
            startColumn,
            startRow,
            snapToWorld
        );
    }

    public bool MoveGridRows(
        int rowAmount,
        bool snapToWorld = true)
    {
        EnsureHelperObjects();

        if (!gridState.MoveRows(
                rowAmount))
        {
            Debug.LogWarning(
                "Block: Grid Position이 없어 " +
                "행을 이동할 수 없습니다.",
                this
            );

            return false;
        }

        if (snapToWorld)
        {
            gridState.Snap(
                transform,
                GridSize
            );
        }

        GridPositionChanged?.Invoke(
            this,
            GridPosition
        );

        return true;
    }

    public void SnapToGridPosition()
    {
        EnsureHelperObjects();

        gridState.Snap(
            transform,
            GridSize
        );
    }

    public Vector3 GetGridWorldPosition()
    {
        EnsureHelperObjects();

        return gridState.GetWorldPosition(
            transform,
            GridSize
        );
    }

    public bool OccupiesCell(
        int column,
        int row)
    {
        EnsureHelperObjects();

        return gridState.OccupiesCell(
            column,
            row,
            GridSize
        );
    }

    public bool OccupiesCell(
        Vector2Int cell)
    {
        return OccupiesCell(
            cell.x,
            cell.y
        );
    }

    public void ClearGridPosition()
    {
        EnsureHelperObjects();

        gridState.Clear();
    }

    private void RefreshGridPlacement()
    {
        if (!HasGridPosition ||
            BoardGrid == null)
        {
            return;
        }

        runtimeCellSize =
            Mathf.Max(
                BoardGrid.CellSize,
                0.1f
            );

        ApplyPresentation();

        gridState.Snap(
            transform,
            GridSize
        );
    }

    private void ApplyPresentation()
    {
        EnsureHelperObjects();

        layout.Apply(
            gameObject,
            definition,
            GridSize,
            runtimeCellSize
        );
    }

    private void NotifyLayoutChanged()
    {
        LayoutChanged?.Invoke(
            GridSize,
            runtimeCellSize
        );
    }

    private void SetRuntimeStats(
        int health,
        int attack)
    {
        maxHealth =
            Mathf.Max(
                health,
                1
            );

        currentHealth =
            maxHealth;

        attackPower =
            Mathf.Max(
                attack,
                0
            );

        HealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );
    }

    [ContextMenu("Apply Layout Preview")]
    private void ApplyLayoutPreview()
    {
        EnsureHelperObjects();

        layout.Validate(
            gameObject
        );

        ApplyPresentation();
        RefreshGridPlacement();
    }

    public void TakeDamage(
        int damage)
    {
        if (damage <= 0 ||
            !IsAlive ||
            isDestructionStarted)
        {
            return;
        }

        HitReceived?.Invoke(
            this,
            damage
        );

        if (DestructionRule ==
            BlockDestructionRule.Indestructible)
        {
            return;
        }

        if (DestructionRule ==
            BlockDestructionRule.TriggerOnly)
        {
            return;
        }

        currentHealth =
            Mathf.Max(
                currentHealth -
                damage,
                0
            );

        Debug.Log(
            $"{name} 피격: " +
            $"HP {currentHealth}/{maxHealth}",
            this
        );

        HealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );

        if (currentHealth <= 0)
        {
            DestroyBlock();
        }
    }

    public bool ExpireWithoutReward()
    {
        if (isDestructionStarted ||
            !gameObject.activeSelf)
        {
            return false;
        }

        isDestructionStarted = true;
        currentHealth = 0;

        ClearGridPosition();

        Debug.Log(
            $"{name} 특수 블록 시간 만료",
            this
        );

        /*
         * Destroyed 이벤트를 호출하지 않는다.
         * 따라서 공 추가, 회복 등의
         * 파괴 보상이 실행되지 않는다.
         */
        gameObject.SetActive(
            false
        );

        Destroy(
            gameObject
        );

        return true;
    }

    private void DestroyBlock()
    {
        if (isDestructionStarted)
        {
            return;
        }

        isDestructionStarted = true;

        Debug.Log(
            $"{name} 파괴",
            this
        );

        Destroyed?.Invoke(
            this
        );

        Destroy(
            gameObject
        );
    }
}