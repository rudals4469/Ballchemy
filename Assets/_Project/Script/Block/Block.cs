using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class Block : MonoBehaviour
{
    [SerializeField, Min(1)]
    private int maxHealth = 3;

    private int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
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
            $"{name} 피격: {currentHealth}/{maxHealth}"
        );

        if (currentHealth == 0)
        {
            Destroy(gameObject);
        }
    }
}