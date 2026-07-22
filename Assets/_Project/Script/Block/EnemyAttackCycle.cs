using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyAttackCycle : MonoBehaviour
{
    [Header("Attack Cycle")]
    [Tooltip(
        "적 전체가 몇 턴마다 한 번씩 " +
        "단체 공격하는지 결정합니다."
    )]
    [SerializeField, Min(1)]
    private int attackIntervalTurns = 3;

    [Tooltip(
        "공격 이벤트 발생 후 다음 처리까지 " +
        "기다리는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float attackResolveDelay = 0.25f;

    private int turnsUntilAttack;

    public int TurnsUntilAttack =>
        turnsUntilAttack;

    public event Action<int> TurnsUntilAttackChanged;
    public event Action<int> EnemyAttackTriggered;

    private void OnValidate()
    {
        attackIntervalTurns =
            Mathf.Max(
                1,
                attackIntervalTurns
            );

        attackResolveDelay =
            Mathf.Max(
                0f,
                attackResolveDelay
            );
    }

    public void InitializeCycle()
    {
        turnsUntilAttack =
            attackIntervalTurns;

        NotifyTurnsUntilAttackChanged();
    }

    public bool AdvanceTurn()
    {
        turnsUntilAttack =
            Mathf.Max(
                0,
                turnsUntilAttack - 1
            );

        NotifyTurnsUntilAttackChanged();

        return turnsUntilAttack <= 0;
    }

    public void ResetCycle()
    {
        turnsUntilAttack =
            attackIntervalTurns;

        NotifyTurnsUntilAttackChanged();
    }

    public IEnumerator ResolveAttackRoutine(
        IReadOnlyList<Block> blocks)
    {
        int totalAttackPower =
            CalculateTotalAttackPower(
                blocks
            );

        int aliveBlockCount =
            CountAliveBlocks(
                blocks
            );

        Debug.Log(
            "EnemyAttackCycle: 적 단체 공격, " +
            $"생존 블록 {aliveBlockCount}개, " +
            $"총 피해량 {totalAttackPower}",
            this
        );

        EnemyAttackTriggered?.Invoke(
            totalAttackPower
        );

        // PlayerHealth가 구현되면
        // 이 이벤트에 연결하여 실제 피해를 처리한다.
        if (attackResolveDelay > 0f)
        {
            yield return new WaitForSeconds(
                attackResolveDelay
            );
        }
    }

    public int CalculateTotalAttackPower(
        IEnumerable<Block> blocks)
    {
        if (blocks == null)
        {
            return 0;
        }

        int totalAttackPower = 0;

        foreach (Block block in blocks)
        {
            if (block == null ||
                !block.IsAlive)
            {
                continue;
            }

            totalAttackPower +=
                block.AttackPower;
        }

        return totalAttackPower;
    }

    private int CountAliveBlocks(
        IEnumerable<Block> blocks)
    {
        if (blocks == null)
        {
            return 0;
        }

        int aliveBlockCount = 0;

        foreach (Block block in blocks)
        {
            if (block == null ||
                !block.IsAlive)
            {
                continue;
            }

            aliveBlockCount++;
        }

        return aliveBlockCount;
    }

    private void NotifyTurnsUntilAttackChanged()
    {
        TurnsUntilAttackChanged?.Invoke(
            turnsUntilAttack
        );
    }
}