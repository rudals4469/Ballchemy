using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
public sealed class PlayerDamageOnDestroyedEffect :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerHealth playerHealth;

    [Header("Curse Damage")]
    [SerializeField, Min(1)]
    private int minimumDamage = 2;

    [SerializeField, Min(1)]
    private int maximumDamage = 4;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog = true;

    private Block block;

    private bool hasAppliedDamage;

    public PlayerHealth TargetPlayerHealth =>
        playerHealth;

    public event Action<int>
        DamageApplied;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
        ValidateReferences();
    }

    private void OnEnable()
    {
        hasAppliedDamage = false;

        FindReferences();
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        NormalizeSettings();
    }

    private void FindReferences()
    {
        if (block == null)
        {
            block =
                GetComponent<Block>();
        }

        if (playerHealth == null &&
            Application.isPlaying)
        {
            playerHealth =
                FindFirstObjectByType<
                    PlayerHealth
                >();
        }
    }

    private void NormalizeSettings()
    {
        minimumDamage =
            Mathf.Max(
                minimumDamage,
                1
            );

        maximumDamage =
            Mathf.Max(
                maximumDamage,
                minimumDamage
            );
    }

    private void ValidateReferences()
    {
        if (block == null)
        {
            Debug.LogError(
                "PlayerDamageOnDestroyedEffect: " +
                "Block 컴포넌트를 찾지 못했습니다.",
                this
            );
        }

        if (playerHealth == null)
        {
            Debug.LogError(
                "PlayerDamageOnDestroyedEffect: " +
                "PlayerHealth를 찾지 못했습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (block == null)
        {
            return;
        }

        block.Destroyed -=
            HandleBlockDestroyed;

        block.Destroyed +=
            HandleBlockDestroyed;
    }

    private void UnsubscribeEvents()
    {
        if (block == null)
        {
            return;
        }

        block.Destroyed -=
            HandleBlockDestroyed;
    }

    private void HandleBlockDestroyed(
        Block destroyedBlock)
    {
        if (hasAppliedDamage ||
            destroyedBlock == null ||
            destroyedBlock != block)
        {
            return;
        }

        /*
         * 일반 스테이지의 Special 블록에서만
         * 저주 피해를 발생시킨다.
         */
        if (destroyedBlock.BlockType !=
            BlockType.Special)
        {
            if (showDebugLog)
            {
                Debug.LogWarning(
                    "PlayerDamageOnDestroyedEffect: " +
                    $"{destroyedBlock.name}의 타입이 " +
                    $"{destroyedBlock.BlockType}이므로 " +
                    "저주 피해를 적용하지 않습니다.",
                    destroyedBlock
                );
            }

            return;
        }

        if (playerHealth == null)
        {
            FindReferences();

            if (playerHealth == null)
            {
                return;
            }
        }

        if (playerHealth.IsDead)
        {
            return;
        }

        hasAppliedDamage = true;

        int requestedDamage =
            UnityEngine.Random.Range(
                minimumDamage,
                maximumDamage + 1
            );

        int previousHealth =
            playerHealth.CurrentHealth;

        playerHealth.TakeDamage(
            requestedDamage
        );

        int appliedDamage =
            previousHealth -
            playerHealth.CurrentHealth;

        if (appliedDamage <= 0)
        {
            return;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "PlayerDamageOnDestroyedEffect: " +
                $"플레이어에게 {appliedDamage} 피해, " +
                $"요청량={requestedDamage}, " +
                $"현재 체력=" +
                $"{playerHealth.CurrentHealth}/" +
                $"{playerHealth.MaxHealth}",
                this
            );
        }

        DamageApplied?.Invoke(
            appliedDamage
        );
    }
}