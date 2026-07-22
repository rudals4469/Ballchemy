using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class Block : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField, Min(1)]
    private int maxHealth = 8;

    [SerializeField, Min(0)]
    private int attackPower = 2;

    private int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int AttackPower => attackPower;

    public bool IsAlive =>
        currentHealth > 0;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void Initialize(
        int health,
        int attack)
    {
        maxHealth = Mathf.Max(1, health);
        currentHealth = maxHealth;

        attackPower = Mathf.Max(0, attack);

        gameObject.SetActive(true);
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || !IsAlive)
        {
            return;
        }

        currentHealth = Mathf.Max(
            currentHealth - damage,
            0
        );

        Debug.Log(
            $"{name} 피격: " +
            $"HP {currentHealth}/{maxHealth}",
            this
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

        Destroy(gameObject);
    }
}