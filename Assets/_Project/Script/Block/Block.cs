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
        "블록 외형과 셀 경계 사이의 간격입니다. " +
        "값이 클수록 블록 사이가 넓게 보입니다."
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

        /*
         * SpriteRenderer가 별도의 자식 오브젝트일 때만
         * 자식을 블록 중심으로 정렬한다.
         *
         * 루트 Block에 SpriteRenderer가 붙어 있을 경우
         * 루트 위치를 변경하면 모든 블록이 부모 원점으로
         * 이동하므로 절대 위치를 건드리지 않는다.
         */
        if (visualRenderer.transform != transform)
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