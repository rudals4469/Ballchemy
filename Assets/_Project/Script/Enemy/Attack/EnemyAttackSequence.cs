using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyAttackSequence :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private EnemyAttackLineEffect attackLineEffect;

    [SerializeField]
    private PlayerHealth playerHealth;

    [SerializeField]
    private BlockElementSystem blockElementSystem;

    [SerializeField]
    private StageModifierState stageModifierState;

    [Header("Sequence Timing")]

    [Tooltip(
        "적 공격 턴이 시작된 뒤 " +
        "첫 블록이 공격하기까지의 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float firstAttackDelay =
        0.15f;

    [Tooltip(
        "한 블록의 공격이 끝난 뒤 " +
        "다음 블록이 공격하기까지의 간격입니다."
    )]
    [SerializeField, Min(0f)]
    private float intervalBetweenAttacks =
        0.1f;

    public bool IsTargetDead =>
        playerHealth != null &&
        playerHealth.IsDead;

    public event Action<Block, int>
        BlockAttackTriggered;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void OnValidate()
    {
        firstAttackDelay =
            Mathf.Max(
                firstAttackDelay,
                0f
            );

        intervalBetweenAttacks =
            Mathf.Max(
                intervalBetweenAttacks,
                0f
            );

        if (Application.isPlaying)
        {
            FindReferences();
        }
    }

    private void FindReferences()
    {
        if (attackLineEffect == null)
        {
            attackLineEffect =
                GetComponentInChildren<
                    EnemyAttackLineEffect
                >(
                    true
                );
        }

        if (playerHealth == null)
        {
            playerHealth =
                FindFirstObjectByType<
                    PlayerHealth
                >();
        }

        if (blockElementSystem == null)
        {
            blockElementSystem =
                GetComponent<
                    BlockElementSystem
                >();
        }

        if (blockElementSystem == null)
        {
            blockElementSystem =
                GetComponentInParent<
                    BlockElementSystem
                >();
        }

        if (blockElementSystem == null)
        {
            blockElementSystem =
                FindFirstObjectByType<
                    BlockElementSystem
                >();
        }

        if (stageModifierState == null)
        {
            stageModifierState =
                FindFirstObjectByType<
                    StageModifierState
                >();
        }
    }

    private void ValidateReferences()
    {
        if (attackLineEffect == null)
        {
            Debug.LogWarning(
                "EnemyAttackSequence: " +
                "EnemyAttackLineEffect가 연결되지 않았습니다. " +
                "피해는 적용되지만 레이저 효과는 나오지 않습니다.",
                this
            );
        }

        if (playerHealth == null)
        {
            Debug.LogWarning(
                "EnemyAttackSequence: " +
                "PlayerHealth가 연결되지 않았습니다.",
                this
            );
        }

        if (blockElementSystem == null)
        {
            Debug.LogWarning(
                "EnemyAttackSequence: " +
                "BlockElementSystem을 찾지 못했습니다. " +
                "동결 공격 취소가 적용되지 않습니다.",
                this
            );
        }

        if (stageModifierState == null)
        {
            Debug.LogWarning(
                "EnemyAttackSequence: " +
                "StageModifierState를 찾지 못했습니다. " +
                "적 공격력 감소 버프가 적용되지 않습니다.",
                this
            );
        }
    }

    public IEnumerator ResolveAttackRoutine(
        IReadOnlyList<Block> blocks)
    {
        yield return ResolveAttackRoutine(
            blocks,
            0
        );
    }

    public IEnumerator ResolveAttackRoutine(
        IReadOnlyList<Block> blocks,
        int overrideAttackPower)
    {
        List<Block> attackers =
            CreateAttackSnapshot(
                blocks,
                overrideAttackPower > 0
            );

        Debug.Log(
            "EnemyAttackSequence: " +
            $"생존 블록 {attackers.Count}개의 " +
            "순차 공격을 시작합니다.",
            this
        );

        if (firstAttackDelay > 0f)
        {
            yield return new WaitForSeconds(
                firstAttackDelay
            );
        }

        foreach (Block attackingBlock
                 in attackers)
        {
            if (IsTargetDead)
            {
                Debug.Log(
                    "EnemyAttackSequence: " +
                    "플레이어가 사망하여 남은 공격을 중단합니다.",
                    this
                );

                yield break;
            }

            if (attackingBlock == null ||
                !attackingBlock.IsAlive)
            {
                continue;
            }

            int damage =
                CalculateModifiedAttackPower(
                    overrideAttackPower > 0
                        ? overrideAttackPower
                        : attackingBlock.AttackPower
                );

            if (damage <= 0)
            {
                continue;
            }

            /*
             * 실제 공격력을 가진 블록이 공격하려는 순간
             * 동결을 소비하고 해당 공격을 취소합니다.
             */
            if (blockElementSystem != null &&
                blockElementSystem
                    .TryConsumeFrozenAttack(
                        attackingBlock
                    ))
            {
                Debug.Log(
                    "EnemyAttackSequence: " +
                    $"{attackingBlock.name}이 동결되어 " +
                    "이번 공격을 건너뜁니다.",
                    attackingBlock
                );

                if (intervalBetweenAttacks > 0f)
                {
                    yield return new WaitForSeconds(
                        intervalBetweenAttacks
                    );
                }

                continue;
            }

            bool damageApplied =
                false;

            if (attackLineEffect != null)
            {
                yield return
                    attackLineEffect.PlayAttackRoutine(
                        attackingBlock.transform,
                        () =>
                        {
                            if (damageApplied)
                            {
                                return;
                            }

                            damageApplied =
                                true;

                            TriggerBlockAttack(
                                attackingBlock,
                                damage
                            );
                        }
                    );
            }

            if (!damageApplied)
            {
                damageApplied =
                    true;

                TriggerBlockAttack(
                    attackingBlock,
                    damage
                );
            }

            if (IsTargetDead)
            {
                yield break;
            }

            if (intervalBetweenAttacks > 0f)
            {
                yield return new WaitForSeconds(
                    intervalBetweenAttacks
                );
            }
        }

        Debug.Log(
            "EnemyAttackSequence: " +
            "모든 생존 블록의 공격이 완료됐습니다.",
            this
        );
    }

    public int CalculateTotalAttackPower(
        IEnumerable<Block> blocks)
    {
        if (blocks == null)
        {
            return 0;
        }

        int totalAttackPower =
            0;

        foreach (Block block in blocks)
        {
            if (block == null ||
                !block.IsAlive)
            {
                continue;
            }

            /*
             * 동결 블록은 다음 공격을 취소하므로
             * 예상 총 공격력에서도 제외합니다.
             */
            BlockElementStatus status =
                block.GetComponent<
                    BlockElementStatus
                >();

            if (status != null &&
                status.IsFrozen)
            {
                continue;
            }

            totalAttackPower +=
                CalculateModifiedAttackPower(
                    block.AttackPower
                );
        }

        return totalAttackPower;
    }

    private int CalculateModifiedAttackPower(
        int baseAttackPower)
    {
        baseAttackPower =
            Mathf.Max(
                baseAttackPower,
                0
            );

        if (baseAttackPower <= 0)
        {
            return 0;
        }

        if (stageModifierState == null)
        {
            stageModifierState =
                FindFirstObjectByType<
                    StageModifierState
                >();
        }

        if (stageModifierState == null)
        {
            return baseAttackPower;
        }

        return stageModifierState
            .ApplyEnemyAttackDamageModifier(
                baseAttackPower
            );
    }

    private List<Block> CreateAttackSnapshot(
        IReadOnlyList<Block> blocks,
        bool includeBlocksWithoutAttackPower)
    {
        List<Block> result =
            new List<Block>();

        if (blocks == null)
        {
            return result;
        }

        foreach (Block block in blocks)
        {
            if (block == null ||
                !block.IsAlive ||
                (!includeBlocksWithoutAttackPower &&
                 block.AttackPower <= 0))
            {
                continue;
            }

            /*
             * 동결 블록도 스냅샷에는 포함해야
             * 실제 차례에서 동결을 소비할 수 있습니다.
             */
            result.Add(
                block
            );
        }

        return result;
    }

    private void TriggerBlockAttack(
        Block attackingBlock,
        int damage)
    {
        Debug.Log(
            "EnemyAttackSequence: " +
            $"{attackingBlock.name} 공격, " +
            $"피해량 {damage}",
            attackingBlock
        );

        BlockAttackTriggered?.Invoke(
            attackingBlock,
            damage
        );
    }
}
