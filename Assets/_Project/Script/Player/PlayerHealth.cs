using System;
using UnityEngine;

public sealed class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Min(1)]
    private int maxHealth = 30;

    private int currentHealth;
    private bool isDead;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    public float HealthRatio =>
        maxHealth > 0
            ? (float)currentHealth / maxHealth
            : 0f;

    public event Action<int, int> HealthChanged;
    public event Action<int> Damaged;
    public event Action<int> Healed;
    public event Action Died;

    private void Awake()
    {
        InitializeHealth();
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(
            1,
            maxHealth
        );
    }

    private void InitializeHealth()
    {
        currentHealth = maxHealth;
        isDead = false;

        Debug.Log(
            $"PlayerHealth: 체력 초기화 " +
            $"{currentHealth}/{maxHealth}",
            this
        );
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 ||
            isDead)
        {
            return;
        }

        int previousHealth =
            currentHealth;

        currentHealth =
            Mathf.Max(
                currentHealth - damage,
                0
            );

        int appliedDamage =
            previousHealth - currentHealth;

        Debug.Log(
            $"PlayerHealth: 피해 {appliedDamage}, " +
            $"현재 체력 {currentHealth}/{maxHealth}",
            this
        );

        Damaged?.Invoke(
            appliedDamage
        );

        HealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0 ||
            isDead)
        {
            return;
        }

        int previousHealth =
            currentHealth;

        currentHealth =
            Mathf.Min(
                currentHealth + amount,
                maxHealth
            );

        int appliedHealing =
            currentHealth - previousHealth;

        if (appliedHealing <= 0)
        {
            return;
        }

        Debug.Log(
            $"PlayerHealth: 회복 {appliedHealing}, " +
            $"현재 체력 {currentHealth}/{maxHealth}",
            this
        );

        Healed?.Invoke(
            appliedHealing
        );

        HealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;

        Debug.Log(
            $"PlayerHealth: 체력 재설정 " +
            $"{currentHealth}/{maxHealth}",
            this
        );

        HealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        currentHealth = 0;

        Debug.Log(
            "PlayerHealth: 플레이어 사망",
            this
        );

        Died?.Invoke();
    }
}