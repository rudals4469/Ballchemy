using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class Block : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Min(1)]
    private int maxHealth = 3;

    private int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void Initialize(int health)
    {
        maxHealth = Mathf.Max(1, health);
        currentHealth = maxHealth;

        gameObject.SetActive(true);
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || currentHealth <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(
            currentHealth - damage,
            0
        );

        Debug.Log(
            $"{name} 피격: {currentHealth}/{maxHealth}",
            this
        );

        if (currentHealth == 0)
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