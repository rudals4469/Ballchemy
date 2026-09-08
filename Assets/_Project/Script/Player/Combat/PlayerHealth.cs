using System;
using UnityEngine;

public sealed class PlayerHealth :
    MonoBehaviour
{
    [Header("Health")]

    [SerializeField, Min(1)]
    private int maxHealth = 30;

    private int currentHealth;
    private bool isDead;

    public int MaxHealth =>
        maxHealth;

    public int CurrentHealth =>
        currentHealth;

    public bool IsDead =>
        isDead;

    public float HealthRatio =>
        maxHealth > 0
            ? (float)currentHealth / maxHealth
            : 0f;

    public event Action<int, int>
        HealthChanged;

    public event Action<int>
        Damaged;

    public event Action<int>
        Healed;

    public event Action<int>
        MaxHealthIncreased;

    public event Action<int>
        MaxHealthDecreased;

    public event Action
        Died;

    private void Awake()
    {
        InitializeHealth();
    }

    private void OnValidate()
    {
        maxHealth =
            Mathf.Max(
                1,
                maxHealth
            );
    }

    private void InitializeHealth()
    {
        currentHealth =
            maxHealth;

        isDead =
            false;

        Debug.Log(
            $"PlayerHealth: 체력 초기화 " +
            $"{currentHealth}/{maxHealth}",
            this
        );
    }

    public void TakeDamage(
        int damage)
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
            previousHealth -
            currentHealth;

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

    public void Heal(
        int amount)
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
            currentHealth -
            previousHealth;

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

    public void RestoreState(int savedMaxHealth, int savedCurrentHealth)
    {
        maxHealth = Mathf.Max(1, savedMaxHealth);
        currentHealth = Mathf.Clamp(savedCurrentHealth, 1, maxHealth);
        isDead = false;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public bool TryIncreaseMaxHealth(
        int amount)
    {
        if (amount <= 0 ||
            isDead)
        {
            return false;
        }

        maxHealth +=
            amount;

        /*
         * 의도적으로 currentHealth는 변경하지 않습니다.
         *
         * 예:
         * 20 / 30
         * → 최대 체력 +5
         * → 20 / 35
         */
        Debug.Log(
            $"PlayerHealth: 최대 체력 증가 {amount}, " +
            $"현재 체력 {currentHealth}/{maxHealth}",
            this
        );

        MaxHealthIncreased?.Invoke(
            amount
        );

        HealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );

        return true;
    }

    public bool TryDecreaseMaxHealth(
        int amount,
        int minimumMaxHealth = 1)
    {
        if (amount <= 0 ||
            isDead)
        {
            return false;
        }

        minimumMaxHealth =
            Mathf.Max(
                minimumMaxHealth,
                1
            );

        if (maxHealth <=
            minimumMaxHealth)
        {
            return false;
        }

        int previousMaxHealth =
            maxHealth;

        int targetMaxHealth =
            Mathf.Max(
                maxHealth - amount,
                minimumMaxHealth
            );

        int appliedDecrease =
            previousMaxHealth -
            targetMaxHealth;

        if (appliedDecrease <= 0)
        {
            return false;
        }

        maxHealth =
            targetMaxHealth;

        /*
         * 현재 체력이 새 최대 체력보다 높을 때만
         * 새 최대 체력에 맞춰 Clamp합니다.
         *
         * 이 감소는 일반 피해가 아니므로
         * Damaged 이벤트를 발생시키지 않습니다.
         */
        currentHealth =
            Mathf.Clamp(
                currentHealth,
                1,
                maxHealth
            );

        /*
         * 이 메서드는 사망하지 않는 최대 체력 감소입니다.
         * 현재 체력은 항상 최소 1을 유지합니다.
         */
        isDead =
            false;

        Debug.Log(
            $"PlayerHealth: 최대 체력 감소 " +
            $"{appliedDecrease}, " +
            $"현재 체력 {currentHealth}/{maxHealth}",
            this
        );

        MaxHealthDecreased?.Invoke(
            appliedDecrease
        );

        HealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );

        return true;
    }

    public void ResetHealth()
    {
        currentHealth =
            maxHealth;

        isDead =
            false;

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

        isDead =
            true;

        currentHealth =
            0;

        Debug.Log(
            "PlayerHealth: 플레이어 사망",
            this
        );

        Died?.Invoke();
    }
}
