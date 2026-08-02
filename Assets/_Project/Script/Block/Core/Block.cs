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
    private int shieldHitCount;

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

    public int ShieldHitCount =>
        shieldHitCount;

    public bool HasShield =>
        shieldHitCount > 0;

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

    public event Action<Block, int>
        HealingReceived;

    public event Action<Block, int>
        ShieldChanged;

    public event Action<Block, int>
        ShieldGranted;

    public event Action<Block, int>
        ShieldConsumed;

    public event Action<Block, Vector2Int>
        GridPositionChanged;

    public event Action<Block>
        Destroyed;

    public event Action<Block>
        ExpiredWithoutReward;

    private void Awake()
    {
        EnsureHelperObjects();

        isDestructionStarted = false;

        currentHealth =
            maxHealth;

        shieldHitCount = 0;

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

        shieldHitCount = 0;

        HealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );

        ShieldChanged?.Invoke(
            this,
            shieldHitCount
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

        /*
         * 쉴드는 피해량과 관계없이
         * 피격 한 번을 완전히 방어한다.
         */
        if (shieldHitCount > 0)
        {
            shieldHitCount =
                Mathf.Max(
                    shieldHitCount - 1,
                    0
                );

            Debug.Log(
                $"{name} 쉴드로 피해 방어, " +
                $"남은 쉴드 {shieldHitCount}",
                this
            );

            ShieldConsumed?.Invoke(
                this,
                shieldHitCount
            );

            ShieldChanged?.Invoke(
                this,
                shieldHitCount
            );

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
    
    public int ReduceCurrentHealthByPercent(
        float reductionPercent,
        int minimumHealth = 1)
    {
        if (!IsAlive ||
            !IsBreakable ||
            reductionPercent <= 0f)
        {
            return 0;
        }

        reductionPercent =
            Mathf.Clamp01(
                reductionPercent
            );

        minimumHealth =
            Mathf.Clamp(
                minimumHealth,
                1,
                maxHealth
            );

        int reductionAmount =
            Mathf.CeilToInt(
                maxHealth *
                reductionPercent
            );

        reductionAmount =
            Mathf.Max(
                reductionAmount,
                1
            );

        int targetHealth =
            Mathf.Max(
                maxHealth -
                reductionAmount,
                minimumHealth
            );

        /*
         * 최대 체력을 기준으로 목표 체력을 계산하므로,
         * 같은 효과가 실수로 두 번 호출되어도
         * 체력이 계속 중첩 감소하지 않습니다.
         */
        if (currentHealth <= targetHealth)
        {
            return 0;
        }

        int previousHealth =
            currentHealth;

        currentHealth =
            targetHealth;

        int reducedHealth =
            previousHealth -
            currentHealth;

        HealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );

        return reducedHealth;
    }

    public int Heal(
        int amount)
    {
        if (amount <= 0 ||
            !IsAlive ||
            isDestructionStarted ||
            !IsBreakable)
        {
            return 0;
        }

        if (currentHealth >= maxHealth)
        {
            return 0;
        }

        int previousHealth =
            currentHealth;

        currentHealth =
            Mathf.Min(
                currentHealth + amount,
                maxHealth
            );

        int appliedHealing =
            currentHealth -
            previousHealth;

        if (appliedHealing <= 0)
        {
            return 0;
        }

        Debug.Log(
            $"{name} 회복: " +
            $"+{appliedHealing}, " +
            $"HP {currentHealth}/{maxHealth}",
            this
        );

        HealingReceived?.Invoke(
            this,
            appliedHealing
        );

        HealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );

        return appliedHealing;
    }

    public int AddShield(
        int amount,
        int maximumShieldHitCount = 1)
    {
        if (amount <= 0 ||
            maximumShieldHitCount <= 0 ||
            !IsAlive ||
            isDestructionStarted ||
            !IsBreakable)
        {
            return 0;
        }

        int previousShieldHitCount =
            shieldHitCount;

        shieldHitCount =
            Mathf.Min(
                shieldHitCount + amount,
                maximumShieldHitCount
            );

        int appliedShield =
            shieldHitCount -
            previousShieldHitCount;

        if (appliedShield <= 0)
        {
            return 0;
        }

        Debug.Log(
            $"{name} 쉴드 획득: " +
            $"+{appliedShield}, " +
            $"현재 쉴드 {shieldHitCount}",
            this
        );

        ShieldGranted?.Invoke(
            this,
            appliedShield
        );

        ShieldChanged?.Invoke(
            this,
            shieldHitCount
        );

        return appliedShield;
    }

    public void ClearShield()
    {
        if (shieldHitCount <= 0)
        {
            return;
        }

        shieldHitCount = 0;

        ShieldChanged?.Invoke(
            this,
            shieldHitCount
        );
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
        shieldHitCount = 0;

        ClearGridPosition();

        Debug.Log(
            $"{name} 특수 블록 시간 만료",
            this
        );

        ExpiredWithoutReward?.Invoke(
            this
        );

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