using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
public sealed class ExplosionOnDestroyedEffect :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BlockGridManager blockGridManager;

    [Header("Explosion")]
    [SerializeField, Min(1)]
    private int explosionDamage = 2;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog = true;

    private Block block;
    private bool hasExploded;

    /*
     * 피해 대상의 위치와 실제 적용된 피해량을 전달한다.
     * 대상 블록이 즉시 파괴되어도 해당 위치에 팝업을 띄울 수 있다.
     */
    public event Action<Vector3, int>
        ExplosionDamageApplied;

    public event Action<int>
        ExplosionCompleted;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
        ValidateReferences();
    }

    private void OnEnable()
    {
        hasExploded = false;

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

        if (blockGridManager == null &&
            Application.isPlaying)
        {
            blockGridManager =
                FindFirstObjectByType<
                    BlockGridManager
                >();
        }
    }

    private void NormalizeSettings()
    {
        explosionDamage =
            Mathf.Max(
                explosionDamage,
                1
            );
    }

    private void ValidateReferences()
    {
        if (block == null)
        {
            Debug.LogError(
                "ExplosionOnDestroyedEffect: " +
                "Block 컴포넌트를 찾지 못했습니다.",
                this
            );
        }

        if (blockGridManager == null)
        {
            Debug.LogError(
                "ExplosionOnDestroyedEffect: " +
                "BlockGridManager를 찾지 못했습니다.",
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
        if (hasExploded ||
            destroyedBlock == null ||
            destroyedBlock != block)
        {
            return;
        }

        if (destroyedBlock.BlockType !=
            BlockType.Special)
        {
            if (showDebugLog)
            {
                Debug.LogWarning(
                    "ExplosionOnDestroyedEffect: " +
                    $"{destroyedBlock.name}의 타입이 " +
                    $"{destroyedBlock.BlockType}이므로 " +
                    "폭발을 실행하지 않습니다.",
                    destroyedBlock
                );
            }

            return;
        }

        if (blockGridManager == null)
        {
            FindReferences();

            if (blockGridManager == null)
            {
                return;
            }
        }

        /*
         * 연쇄 폭발 중 같은 폭발 블록이
         * 두 번 실행되지 않게 먼저 잠근다.
         */
        hasExploded = true;

        List<Block> surroundingBlocks =
            BlockNeighborhoodResolver
                .FindSurroundingBlocks(
                    destroyedBlock,
                    blockGridManager.ActiveBlocks
                );

        int damagedBlockCount = 0;

        for (int i = 0;
             i < surroundingBlocks.Count;
             i++)
        {
            Block targetBlock =
                surroundingBlocks[i];

            if (!CanDamageTarget(
                    targetBlock))
            {
                continue;
            }

            /*
             * 피해로 대상 블록이 파괴되기 전에
             * 팝업을 띄울 위치를 미리 저장한다.
             */
            Vector3 targetPosition =
                targetBlock.transform.position;

            int previousHealth =
                targetBlock.CurrentHealth;

            targetBlock.TakeDamage(
                explosionDamage
            );

            int appliedDamage =
                previousHealth -
                targetBlock.CurrentHealth;

            if (appliedDamage <= 0)
            {
                continue;
            }

            damagedBlockCount++;

            /*
             * 주변 피해 대상마다 한 번씩 이벤트를 발생시킨다.
             * 따라서 피해받은 모든 블록에 각각 숫자가 표시된다.
             */
            ExplosionDamageApplied?.Invoke(
                targetPosition,
                appliedDamage
            );

            if (showDebugLog)
            {
                Debug.Log(
                    "ExplosionOnDestroyedEffect: " +
                    $"{targetBlock.name}에게 " +
                    $"{appliedDamage} 폭발 피해, " +
                    $"팝업 위치={targetPosition}",
                    this
                );
            }
        }

        if (showDebugLog)
        {
            Debug.Log(
                "ExplosionOnDestroyedEffect: " +
                $"폭발 완료, 피해 및 팝업 대상 " +
                $"{damagedBlockCount}개",
                this
            );
        }

        ExplosionCompleted?.Invoke(
            damagedBlockCount
        );
    }

    private bool CanDamageTarget(
        Block targetBlock)
    {
        if (targetBlock == null ||
            targetBlock == block ||
            !targetBlock.IsAlive)
        {
            return false;
        }

        /*
         * 실제로 피해를 받을 수 있는 블록에만
         * 피해 숫자를 표시한다.
         */
        if (!targetBlock.IsBreakable)
        {
            return false;
        }

        return true;
    }
}