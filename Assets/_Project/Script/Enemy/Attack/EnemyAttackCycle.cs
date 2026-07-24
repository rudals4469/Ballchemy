using System;
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

    private int turnsUntilAttack;

    public int TurnsUntilAttack =>
        turnsUntilAttack;

    public event Action<int> TurnsUntilAttackChanged;

    private void OnValidate()
    {
        attackIntervalTurns =
            Mathf.Max(
                1,
                attackIntervalTurns
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

    private void NotifyTurnsUntilAttackChanged()
    {
        TurnsUntilAttackChanged?.Invoke(
            turnsUntilAttack
        );
    }
}