using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class Block : MonoBehaviour
{
    [Header("Definition")]
    [SerializeField]
    private BlockDefinition definition;

    [SerializeField]
    private SpriteRenderer visualRenderer;

    [SerializeField]
    private BoxCollider2D boxCollider;

    [Header("Layout")]
    [Tooltip(
        "블록 외형과 셀 경계 사이의 간격입니다."
    )]
    [SerializeField, Min(0f)]
    private float visualPadding = 0.12f;

    [Tooltip(
        "콜라이더와 셀 경계 사이의 간격입니다."
    )]
    [SerializeField, Min(0f)]
    private float colliderInset = 0.02f;

    [SerializeField, Min(0.1f)]
    private float runtimeCellSize = 1f;

    [Header("Grid Runtime")]
    [Tooltip(
        "이 블록이 배치된 BoardGrid입니다. " +
        "BlockWaveGenerator가 생성할 때 자동으로 할당합니다."
    )]
    [SerializeField]
    private BoardGrid boardGrid;

    [Tooltip(
        "블록이 점유하는 영역의 왼쪽 위 시작 셀입니다."
    )]
    [SerializeField]
    private Vector2Int gridPosition =
        new Vector2Int(
            -1,
            -1
        );

    [SerializeField]
    private bool hasGridPosition;

    [Header("Runtime Stats")]
    [SerializeField, Min(1)]
    private int maxHealth = 8;

    [SerializeField, Min(0)]
    private int attackPower = 2;

    private int currentHealth;

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
            GridSize.x * runtimeCellSize,
            GridSize.y * runtimeCellSize
        );

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

    public int EndColumn =>
        StartColumn +
        GridSize.x -
        1;

    public int EndRow =>
        StartRow +
        GridSize.y -
        1;

    public int BottomRow =>
        EndRow;

    public int CurrentHealth =>
        currentHealth;

    public int MaxHealth =>
        maxHealth;

    public int AttackPower =>
        attackPower;

    public bool IsAlive =>
        currentHealth > 0;

    public bool IsBreakable =>
        DestructionRule ==
        BlockDestructionRule.Breakable;

    public bool IsIndestructible =>
        DestructionRule ==
        BlockDestructionRule.Indestructible;

    public bool IsInsideBoard
    {
        get
        {
            if (boardGrid == null ||
                !hasGridPosition)
            {
                return false;
            }

            return StartColumn >= 0 &&
                   StartRow >= 0 &&
                   EndColumn <
                   boardGrid.ColumnCount &&
                   EndRow <
                   boardGrid.RowCount;
        }
    }

    public bool IsTouchingBottomRow
    {
        get
        {
            if (boardGrid == null ||
                !hasGridPosition)
            {
                return false;
            }

            return BottomRow ==
                   boardGrid.RowCount - 1;
        }
    }

    public bool IsOutsideBottom
    {
        get
        {
            if (boardGrid == null ||
                !hasGridPosition)
            {
                return false;
            }

            return BottomRow >=
                   boardGrid.RowCount;
        }
    }

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

    private void Awake()
    {
        FindReferences();

        currentHealth =
            maxHealth;

        ApplyDefinitionVisual();
        ApplyLayout();

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

        visualPadding =
            Mathf.Max(
                visualPadding,
                0f
            );

        colliderInset =
            Mathf.Max(
                colliderInset,
                0f
            );

        FindReferences();
    }

    private void FindReferences()
    {
        if (visualRenderer == null)
        {
            visualRenderer =
                GetComponent<SpriteRenderer>();
        }

        if (visualRenderer == null)
        {
            visualRenderer =
                GetComponentInChildren<SpriteRenderer>(
                    true
                );
        }

        if (boxCollider == null)
        {
            boxCollider =
                GetComponent<BoxCollider2D>();
        }
    }

    public void Initialize(
        int health,
        int attack)
    {
        SetRuntimeStats(
            health,
            attack
        );

        ApplyDefinitionVisual();
        ApplyLayout();
        RefreshGridPlacement();

        gameObject.SetActive(
            true
        );

        LayoutChanged?.Invoke(
            GridSize,
            runtimeCellSize
        );
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
        definition =
            blockDefinition;

        runtimeCellSize =
            Mathf.Max(
                cellSize,
                0.1f
            );

        ApplyDefinitionVisual();
        ApplyLayout();

        SetRuntimeStats(
            health,
            attack
        );

        RefreshGridPlacement();

        gameObject.SetActive(
            true
        );

        DefinitionChanged?.Invoke(
            definition
        );

        LayoutChanged?.Invoke(
            GridSize,
            runtimeCellSize
        );
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

        definition =
            blockDefinition;

        runtimeCellSize =
            Mathf.Max(
                cellSize,
                0.1f
            );

        ApplyDefinitionVisual();
        ApplyLayout();
        RefreshGridPlacement();

        DefinitionChanged?.Invoke(
            definition
        );

        LayoutChanged?.Invoke(
            GridSize,
            runtimeCellSize
        );
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

        boardGrid =
            targetBoardGrid;

        gridPosition =
            new Vector2Int(
                startColumn,
                startRow
            );

        hasGridPosition =
            true;

        runtimeCellSize =
            Mathf.Max(
                boardGrid.CellSize,
                0.1f
            );

        ApplyLayout();

        if (snapToWorld)
        {
            SnapToGridPosition();
        }

        LayoutChanged?.Invoke(
            GridSize,
            runtimeCellSize
        );

        GridPositionChanged?.Invoke(
            this,
            gridPosition
        );
    }

    public void SetGridPosition(
        int startColumn,
        int startRow,
        bool snapToWorld = true)
    {
        if (boardGrid == null)
        {
            Debug.LogWarning(
                "Block: 기존 BoardGrid가 없어 " +
                "Grid Position을 변경할 수 없습니다.",
                this
            );

            return;
        }

        SetGridPosition(
            boardGrid,
            startColumn,
            startRow,
            snapToWorld
        );
    }

    public bool MoveGridRows(
        int rowAmount,
        bool snapToWorld = true)
    {
        if (boardGrid == null ||
            !hasGridPosition)
        {
            Debug.LogWarning(
                "Block: Grid Position이 없어 " +
                "행을 이동할 수 없습니다.",
                this
            );

            return false;
        }

        SetGridPosition(
            boardGrid,
            StartColumn,
            StartRow + rowAmount,
            snapToWorld
        );

        return true;
    }

    public void SnapToGridPosition()
    {
        if (boardGrid == null ||
            !hasGridPosition)
        {
            return;
        }

        transform.position =
            GetGridWorldPosition();

        transform.rotation =
            boardGrid.transform.rotation;
    }

    public Vector3 GetGridWorldPosition()
    {
        if (boardGrid == null ||
            !hasGridPosition)
        {
            return transform.position;
        }

        Vector3 startCellPosition =
            boardGrid.GetCellWorldPosition(
                StartColumn,
                StartRow
            );

        float horizontalDistance =
            (GridSize.x - 1) *
            boardGrid.CellSize *
            0.5f;

        float verticalDistance =
            (GridSize.y - 1) *
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

    public bool OccupiesCell(
        int column,
        int row)
    {
        if (!hasGridPosition)
        {
            return false;
        }

        return column >= StartColumn &&
               column <= EndColumn &&
               row >= StartRow &&
               row <= EndRow;
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

    private void RefreshGridPlacement()
    {
        if (boardGrid == null ||
            !hasGridPosition)
        {
            return;
        }

        runtimeCellSize =
            Mathf.Max(
                boardGrid.CellSize,
                0.1f
            );

        ApplyLayout();
        SnapToGridPosition();
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

    private void ApplyDefinitionVisual()
    {
        if (visualRenderer == null ||
            definition == null)
        {
            return;
        }

        if (definition.Sprite != null)
        {
            visualRenderer.sprite =
                definition.Sprite;
        }

        visualRenderer.color =
            definition.Color;
    }

    private void ApplyLayout()
    {
        Vector2 desiredWorldSize =
            WorldSize;

        ApplyVisualLayout(
            desiredWorldSize
        );

        ApplyColliderLayout(
            desiredWorldSize
        );
    }

    private void ApplyVisualLayout(
        Vector2 desiredWorldSize)
    {
        if (visualRenderer == null)
        {
            return;
        }

        Vector3 definitionScale =
            definition != null
                ? definition.VisualScale
                : Vector3.one;

        Vector2 desiredVisualWorldSize =
            new Vector2(
                Mathf.Max(
                    desiredWorldSize.x -
                    visualPadding,
                    0.05f
                ) *
                definitionScale.x,

                Mathf.Max(
                    desiredWorldSize.y -
                    visualPadding,
                    0.05f
                ) *
                definitionScale.y
            );

        Vector2 localRendererSize =
            ConvertWorldSizeToLocalSize(
                visualRenderer.transform,
                desiredVisualWorldSize
            );

        visualRenderer.drawMode =
            SpriteDrawMode.Sliced;

        visualRenderer.size =
            localRendererSize;

        if (visualRenderer.transform !=
            transform)
        {
            Vector3 localPosition =
                visualRenderer.transform.localPosition;

            localPosition.x = 0f;
            localPosition.y = 0f;

            visualRenderer.transform.localPosition =
                localPosition;

            visualRenderer.transform.localRotation =
                Quaternion.identity;
        }
    }

    private void ApplyColliderLayout(
        Vector2 desiredWorldSize)
    {
        if (boxCollider == null)
        {
            return;
        }

        Vector2 desiredColliderWorldSize =
            new Vector2(
                Mathf.Max(
                    desiredWorldSize.x -
                    (colliderInset * 2f),
                    0.05f
                ),

                Mathf.Max(
                    desiredWorldSize.y -
                    (colliderInset * 2f),
                    0.05f
                )
            );

        Vector2 localColliderSize =
            ConvertWorldSizeToLocalSize(
                boxCollider.transform,
                desiredColliderWorldSize
            );

        boxCollider.size =
            localColliderSize;

        boxCollider.offset =
            Vector2.zero;
    }

    private Vector2 ConvertWorldSizeToLocalSize(
        Transform targetTransform,
        Vector2 desiredWorldSize)
    {
        Vector3 lossyScale =
            targetTransform.lossyScale;

        float scaleX =
            Mathf.Max(
                Mathf.Abs(lossyScale.x),
                0.0001f
            );

        float scaleY =
            Mathf.Max(
                Mathf.Abs(lossyScale.y),
                0.0001f
            );

        return new Vector2(
            desiredWorldSize.x / scaleX,
            desiredWorldSize.y / scaleY
        );
    }

    [ContextMenu("Apply Layout Preview")]
    private void ApplyLayoutPreview()
    {
        FindReferences();
        ApplyDefinitionVisual();
        ApplyLayout();
        RefreshGridPlacement();
    }

    public void TakeDamage(
        int damage)
    {
        if (damage <= 0 ||
            !IsAlive)
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
                currentHealth - damage,
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

    private void DestroyBlock()
    {
        Debug.Log(
            $"{name} 파괴",
            this
        );

        Destroy(
            gameObject
        );
    }
}