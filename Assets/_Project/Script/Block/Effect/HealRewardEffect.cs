using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
public sealed class HealRewardEffect :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerHealth playerHealth;

    [Header("Healing Reward")]
    [SerializeField, Min(1)]
    private int minimumHealingAmount = 3;

    [SerializeField, Min(1)]
    private int maximumHealingAmount = 5;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog = true;

    private Block block;

    private bool hasGrantedReward;

    public event Action<int>
        HealingGranted;

    public event Action
        HealingBlockedByFullHealth;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
        ValidateReferences();
    }

    private void OnEnable()
    {
        hasGrantedReward = false;

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
        minimumHealingAmount =
            Mathf.Max(
                minimumHealingAmount,
                1
            );

        maximumHealingAmount =
            Mathf.Max(
                maximumHealingAmount,
                minimumHealingAmount
            );
    }

    private void ValidateReferences()
    {
        if (block == null)
        {
            Debug.LogError(
                "HealRewardEffect: " +
                "Block 컴포넌트를 찾지 못했습니다.",
                this
            );
        }

        if (playerHealth == null)
        {
            Debug.LogError(
                "HealRewardEffect: " +
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
        if (hasGrantedReward ||
            destroyedBlock == null ||
            destroyedBlock != block)
        {
            return;
        }

        /*
         * 회복 보상은 일반 스테이지의
         * Special 블록에서만 작동한다.
         */
        if (destroyedBlock.BlockType !=
            BlockType.Special)
        {
            if (showDebugLog)
            {
                Debug.LogWarning(
                    "HealRewardEffect: " +
                    $"{destroyedBlock.name}의 타입이 " +
                    $"{destroyedBlock.BlockType}이므로 " +
                    "회복 보상을 실행하지 않습니다.",
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

        hasGrantedReward = true;

        if (playerHealth.CurrentHealth >=
            playerHealth.MaxHealth)
        {
            if (showDebugLog)
            {
                Debug.Log(
                    "HealRewardEffect: " +
                    "플레이어의 체력이 가득 차 있어 " +
                    "회복되지 않았습니다.",
                    this
                );
            }

            HealingBlockedByFullHealth
                ?.Invoke();

            return;
        }

        int requestedHealingAmount =
            UnityEngine.Random.Range(
                minimumHealingAmount,
                maximumHealingAmount + 1
            );

        int previousHealth =
            playerHealth.CurrentHealth;

        playerHealth.Heal(
            requestedHealingAmount
        );

        int appliedHealingAmount =
            playerHealth.CurrentHealth -
            previousHealth;

        if (appliedHealingAmount <= 0)
        {
            HealingBlockedByFullHealth
                ?.Invoke();

            return;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "HealRewardEffect: " +
                $"체력 {appliedHealingAmount} 회복, " +
                $"요청량={requestedHealingAmount}, " +
                $"현재 체력=" +
                $"{playerHealth.CurrentHealth}/" +
                $"{playerHealth.MaxHealth}",
                this
            );
        }

        HealingGranted?.Invoke(
            appliedHealingAmount
        );
    }
}