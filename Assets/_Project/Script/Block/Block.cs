using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class Block : MonoBehaviour
{
    [Header("Definition")]
    [SerializeField]
    private BlockDefinition definition;

    [SerializeField]
    private SpriteRenderer visualRenderer;

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

    private void Awake()
    {
        FindReferences();

        currentHealth =
            maxHealth;

        ApplyDefinitionVisual();
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

        FindReferences();
        ApplyDefinitionVisual();
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

        gameObject.SetActive(
            true
        );
    }

    public void Initialize(
        BlockDefinition blockDefinition,
        int health,
        int attack)
    {
        ApplyDefinition(
            blockDefinition
        );

        SetRuntimeStats(
            health,
            attack
        );

        gameObject.SetActive(
            true
        );
    }

    public void ApplyDefinition(
        BlockDefinition blockDefinition)
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

        ApplyDefinitionVisual();

        DefinitionChanged?.Invoke(
            definition
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
        if (definition == null ||
            visualRenderer == null)
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

        visualRenderer.transform.localScale =
            definition.VisualScale;
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