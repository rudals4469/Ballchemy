using System;
using UnityEngine;

public sealed class EnemyAttackCycle :
    MonoBehaviour
{
    [Header("Attack Cycle")]

    [Tooltip(
        "적 전체가 몇 턴마다 한 번씩 " +
        "단체 공격하는지 결정합니다."
    )]
    [SerializeField, Min(1)]
    private int attackIntervalTurns =
        3;

    [Header("Stage Modifier")]

    [SerializeField]
    private StageModifierState stageModifierState;

    private int turnsUntilAttack;

    public int BaseAttackIntervalTurns =>
        attackIntervalTurns;

    public int CurrentAttackIntervalTurns =>
        CalculateCurrentAttackInterval();

    public int TurnsUntilAttack =>
        turnsUntilAttack;

    public event Action<int>
        TurnsUntilAttackChanged;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void OnValidate()
    {
        attackIntervalTurns =
            Mathf.Max(
                attackIntervalTurns,
                1
            );

        if (Application.isPlaying)
        {
            FindReferences();
        }
    }

    private void FindReferences()
    {
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
        if (stageModifierState == null)
        {
            Debug.LogWarning(
                "EnemyAttackCycle: " +
                "StageModifierState가 연결되지 않았습니다. " +
                "적 공격 주기 증가 버프가 적용되지 않습니다.",
                this
            );
        }
    }

    public void InitializeCycle()
    {
        turnsUntilAttack =
            CalculateCurrentAttackInterval();

        NotifyTurnsUntilAttackChanged();
    }

    public bool AdvanceTurn()
    {
        turnsUntilAttack =
            Mathf.Max(
                turnsUntilAttack - 1,
                0
            );

        NotifyTurnsUntilAttackChanged();

        return turnsUntilAttack <= 0;
    }

    public void ResetCycle()
    {
        turnsUntilAttack =
            CalculateCurrentAttackInterval();

        NotifyTurnsUntilAttackChanged();
    }

    private int CalculateCurrentAttackInterval()
    {
        int baseInterval =
            Mathf.Max(
                attackIntervalTurns,
                1
            );

        if (stageModifierState == null)
        {
            stageModifierState =
                FindFirstObjectByType<
                    StageModifierState
                >();
        }

        if (stageModifierState == null)
        {
            return baseInterval;
        }

        return stageModifierState
            .ApplyEnemyAttackIntervalModifier(
                baseInterval
            );
    }

    private void NotifyTurnsUntilAttackChanged()
    {
        TurnsUntilAttackChanged?.Invoke(
            turnsUntilAttack
        );
    }
}