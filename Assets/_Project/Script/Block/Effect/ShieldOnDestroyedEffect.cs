using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
public sealed class ShieldOnDestroyedEffect :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BlockGridManager blockGridManager;

    [Header("Target Count")]
    [SerializeField, Min(1)]
    private int minimumTargetCount = 2;

    [SerializeField, Min(1)]
    private int maximumTargetCount = 3;

    [Header("Shield")]
    [Tooltip(
        "선택된 블록마다 추가할 " +
        "피격 방어 횟수입니다."
    )]
    [SerializeField, Min(1)]
    private int shieldAmountPerTarget = 1;

    [Header("Target Rules")]
    [Tooltip(
        "활성화하면 다른 Special 블록도 " +
        "쉴드 대상으로 선택될 수 있습니다."
    )]
    [SerializeField]
    private bool includeSpecialBlocks;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog = true;

    private readonly List<Block> candidateBlocks =
        new List<Block>();

    private Block block;
    private bool hasGrantedShield;

    public event Action<Vector3, int>
        ShieldApplied;

    public event Action<int>
        ShieldGrantCompleted;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
        ValidateReferences();
    }

    private void OnEnable()
    {
        hasGrantedShield = false;

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
        minimumTargetCount =
            Mathf.Max(
                minimumTargetCount,
                1
            );

        maximumTargetCount =
            Mathf.Max(
                maximumTargetCount,
                minimumTargetCount
            );

        shieldAmountPerTarget =
            Mathf.Max(
                shieldAmountPerTarget,
                1
            );
    }

    private void ValidateReferences()
    {
        if (block == null)
        {
            Debug.LogError(
                "ShieldOnDestroyedEffect: " +
                "Block 컴포넌트를 찾지 못했습니다.",
                this
            );
        }

        if (blockGridManager == null)
        {
            Debug.LogError(
                "ShieldOnDestroyedEffect: " +
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
        if (hasGrantedShield ||
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
                    "ShieldOnDestroyedEffect: " +
                    $"{destroyedBlock.name}의 타입이 " +
                    $"{destroyedBlock.BlockType}이므로 " +
                    "쉴드를 부여하지 않습니다.",
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

        hasGrantedShield = true;

        CollectCandidates(
            destroyedBlock
        );

        if (candidateBlocks.Count == 0)
        {
            if (showDebugLog)
            {
                Debug.Log(
                    "ShieldOnDestroyedEffect: " +
                    "쉴드를 부여할 수 있는 블록이 없습니다.",
                    this
                );
            }

            ShieldGrantCompleted?.Invoke(
                0
            );

            return;
        }

        ShuffleCandidates();

        int requestedTargetCount =
            UnityEngine.Random.Range(
                minimumTargetCount,
                maximumTargetCount + 1
            );

        int targetCount =
            Mathf.Min(
                requestedTargetCount,
                candidateBlocks.Count
            );

        int shieldedBlockCount = 0;

        for (int i = 0;
             i < targetCount;
             i++)
        {
            Block targetBlock =
                candidateBlocks[i];

            if (!CanShieldTarget(
                    targetBlock,
                    destroyedBlock))
            {
                continue;
            }

            EnsureShieldView(
                targetBlock
            );

            /*
             * 최대치를 int.MaxValue로 전달해
             * 기존 쉴드가 있더라도 계속 중첩한다.
             */
            int appliedShield =
                targetBlock.AddShield(
                    shieldAmountPerTarget,
                    int.MaxValue
                );

            if (appliedShield <= 0)
            {
                continue;
            }

            shieldedBlockCount++;

            ShieldApplied?.Invoke(
                targetBlock.transform.position,
                appliedShield
            );

            if (showDebugLog)
            {
                Debug.Log(
                    "ShieldOnDestroyedEffect: " +
                    $"{targetBlock.name}에 " +
                    $"쉴드 {appliedShield}회 추가, " +
                    $"현재 쉴드=" +
                    $"{targetBlock.ShieldHitCount}",
                    targetBlock
                );
            }
        }

        if (showDebugLog)
        {
            Debug.Log(
                "ShieldOnDestroyedEffect: " +
                $"쉴드 부여 완료, " +
                $"대상 {shieldedBlockCount}개",
                this
            );
        }

        ShieldGrantCompleted?.Invoke(
            shieldedBlockCount
        );
    }

    private void CollectCandidates(
        Block destroyedBlock)
    {
        candidateBlocks.Clear();

        IReadOnlyList<Block> activeBlocks =
            blockGridManager.ActiveBlocks;

        if (activeBlocks == null)
        {
            return;
        }

        for (int i = 0;
             i < activeBlocks.Count;
             i++)
        {
            Block targetBlock =
                activeBlocks[i];

            if (!CanShieldTarget(
                    targetBlock,
                    destroyedBlock))
            {
                continue;
            }

            candidateBlocks.Add(
                targetBlock
            );
        }
    }

    private bool CanShieldTarget(
        Block targetBlock,
        Block destroyedBlock)
    {
        if (targetBlock == null ||
            targetBlock == destroyedBlock ||
            !targetBlock.IsAlive ||
            !targetBlock.IsBreakable)
        {
            return false;
        }

        /*
         * 기존 쉴드 보유 여부는 검사하지 않는다.
         * 이미 쉴드가 있는 블록도 다시 대상으로
         * 선택되어 쉴드가 중첩될 수 있다.
         */

        if (!includeSpecialBlocks &&
            targetBlock.BlockType ==
            BlockType.Special)
        {
            return false;
        }

        return true;
    }

    private void ShuffleCandidates()
    {
        for (int i =
                 candidateBlocks.Count - 1;
             i > 0;
             i--)
        {
            int randomIndex =
                UnityEngine.Random.Range(
                    0,
                    i + 1
                );

            Block temporary =
                candidateBlocks[i];

            candidateBlocks[i] =
                candidateBlocks[randomIndex];

            candidateBlocks[randomIndex] =
                temporary;
        }
    }

    private void EnsureShieldView(
        Block targetBlock)
    {
        if (targetBlock == null)
        {
            return;
        }

        BlockShieldView shieldView =
            targetBlock.GetComponent<
                BlockShieldView
            >();

        if (shieldView != null)
        {
            return;
        }

        targetBlock.gameObject
            .AddComponent<BlockShieldView>();
    }
}