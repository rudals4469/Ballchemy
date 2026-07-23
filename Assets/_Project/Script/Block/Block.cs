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
        "블록 콜라이더 가장자리에서 " +
        "안쪽으로 줄일 크기입니다."
    )]
    [SerializeField, Min(0f)]
    private float colliderInset = 0.02f;

    [SerializeField, Min(0.1f)]
    private float runtimeCellSize = 1f;

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

    public int CurrentHealth =>
        currentHealth;

    public int MaxHealth =>
        maxHealth;

    public int AttackPower =>
        attackPower;

    public bool IsAlive =>
        currentHealth > 0;

    public event Action<int, int>
        HealthChanged;

    public event Action<BlockDefinition>
        DefinitionChanged;

    public event Action<Vector2Int, float>
        LayoutChanged;

    private void Awake()
    {
        FindReferences();

        currentHealth =
            maxHealth;

        ApplyDefinitionVisual();
        ApplyLayout();
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

        colliderInset =
            Mathf.Max(
                colliderInset,
                0f
            );

        FindReferences();
        ApplyDefinitionVisual();
        ApplyLayout();
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
                GetComponentInChildren<
                    SpriteRenderer
                >();
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

        gameObject.SetActive(
            true
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

        DefinitionChanged?.Invoke(
            definition
        );

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

    private void ApplyDefinitionVisual()
    {
        if (visualRenderer == null)
        {
            return;
        }

        if (definition != null)
        {
            if (definition.Sprite != null)
            {
                visualRenderer.sprite =
                    definition.Sprite;
            }

            visualRenderer.color =
                definition.Color;
        }
    }

    private void ApplyLayout()
    {
        Vector2 calculatedWorldSize =
            WorldSize;

        Vector3 visualScale =
            definition != null
                ? definition.VisualScale
                : Vector3.one;

        if (visualRenderer != null)
        {
            visualRenderer.drawMode =
                SpriteDrawMode.Sliced;

            visualRenderer.size =
                new Vector2(
                    calculatedWorldSize.x *
                    visualScale.x,

                    calculatedWorldSize.y *
                    visualScale.y
                );
        }

        if (boxCollider != null)
        {
            float colliderWidth =
                Mathf.Max(
                    calculatedWorldSize.x -
                    (colliderInset * 2f),
                    0.05f
                );

            float colliderHeight =
                Mathf.Max(
                    calculatedWorldSize.y -
                    (colliderInset * 2f),
                    0.05f
                );

            boxCollider.size =
                new Vector2(
                    colliderWidth,
                    colliderHeight
                );

            boxCollider.offset =
                Vector2.zero;
        }
    }

    public void TakeDamage(
        int damage)
    {
        if (damage <= 0 ||
            !IsAlive)
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