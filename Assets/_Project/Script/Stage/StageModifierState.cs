using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StageModifierState :
    MonoBehaviour
{
    [Header("Stage Modifiers")]

    [Tooltip(
        "현재 스테이지에 적용되는 공 직접 피해 증가율입니다.\n" +
        "0.2는 직접 피해 20% 증가를 의미합니다."
    )]
    [SerializeField, Min(0f)]
    private float directDamageIncreaseRatio;

    [Tooltip(
        "현재 스테이지에 적용되는 일반·네임드 적 " +
        "최대 체력 감소율입니다.\n" +
        "0.2는 최대 체력 20% 감소를 의미합니다.\n" +
        "보스에게는 적용되지 않습니다."
    )]
    [SerializeField, Min(0f)]
    private float enemyMaxHealthReductionRatio;

    [Tooltip(
        "현재 스테이지에 적용되는 적 공격력 감소율입니다.\n" +
        "0.2는 적 공격력 20% 감소를 의미합니다."
    )]
    [SerializeField, Min(0f)]
    private float enemyAttackDamageReductionRatio;

    [Tooltip(
        "현재 스테이지에 추가되는 적 공격 주기 턴 수입니다.\n" +
        "1이면 기존 공격 주기가 1턴 증가합니다."
    )]
    [SerializeField, Min(0)]
    private int enemyAttackIntervalBonusTurns;

    [Tooltip(
        "현재 스테이지에 적용되는 블록 파괴 골드 증가율입니다.\n" +
        "0.25는 골드 획득량 25% 증가를 의미합니다."
    )]
    [SerializeField, Min(0f)]
    private float goldGainIncreaseRatio;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog =
        true;

    public float DirectDamageIncreaseRatio =>
        directDamageIncreaseRatio;

    public float EnemyMaxHealthReductionRatio =>
        enemyMaxHealthReductionRatio;

    public float EnemyAttackDamageReductionRatio =>
        enemyAttackDamageReductionRatio;

    public int EnemyAttackIntervalBonusTurns =>
        enemyAttackIntervalBonusTurns;

    public float GoldGainIncreaseRatio =>
        goldGainIncreaseRatio;

    public bool HasDirectDamageIncrease =>
        directDamageIncreaseRatio > 0f;

    public bool HasEnemyMaxHealthReduction =>
        enemyMaxHealthReductionRatio > 0f;

    public bool HasEnemyAttackDamageReduction =>
        enemyAttackDamageReductionRatio > 0f;

    public bool HasEnemyAttackIntervalBonus =>
        enemyAttackIntervalBonusTurns > 0;

    public bool HasGoldGainIncrease =>
        goldGainIncreaseRatio > 0f;

    public event Action
        StateChanged;

    public event Action<float>
        DirectDamageIncreaseRatioChanged;

    public event Action<float>
        EnemyMaxHealthReductionRatioChanged;

    public event Action<float>
        EnemyAttackDamageReductionRatioChanged;

    public event Action<int>
        EnemyAttackIntervalBonusTurnsChanged;

    public event Action<float>
        GoldGainIncreaseRatioChanged;

    public event Action
        StageStateReset;

    private void OnValidate()
    {
        directDamageIncreaseRatio =
            Mathf.Max(
                directDamageIncreaseRatio,
                0f
            );

        enemyMaxHealthReductionRatio =
            Mathf.Max(
                enemyMaxHealthReductionRatio,
                0f
            );

        enemyAttackDamageReductionRatio =
            Mathf.Max(
                enemyAttackDamageReductionRatio,
                0f
            );

        enemyAttackIntervalBonusTurns =
            Mathf.Max(
                enemyAttackIntervalBonusTurns,
                0
            );

        goldGainIncreaseRatio =
            Mathf.Max(
                goldGainIncreaseRatio,
                0f
            );
    }

    public bool TryAddDirectDamageIncreaseRatio(
        float ratio)
    {
        if (ratio <= 0f)
        {
            return false;
        }

        directDamageIncreaseRatio +=
            ratio;

        directDamageIncreaseRatio =
            Mathf.Max(
                directDamageIncreaseRatio,
                0f
            );

        DirectDamageIncreaseRatioChanged
            ?.Invoke(
                directDamageIncreaseRatio
            );

        StateChanged?.Invoke();

        if (showDebugLog)
        {
            Debug.Log(
                "StageModifierState: " +
                "직접 피해 증가율을 추가했습니다. " +
                $"추가={ratio:P0}, " +
                $"현재={directDamageIncreaseRatio:P0}",
                this
            );
        }

        return true;
    }

    public bool TryRemoveDirectDamageIncreaseRatio(
        float ratio)
    {
        if (ratio <= 0f ||
            directDamageIncreaseRatio <= 0f)
        {
            return false;
        }

        float previousRatio =
            directDamageIncreaseRatio;

        directDamageIncreaseRatio =
            Mathf.Max(
                directDamageIncreaseRatio -
                ratio,
                0f
            );

        if (Mathf.Approximately(
                previousRatio,
                directDamageIncreaseRatio
            ))
        {
            return false;
        }

        DirectDamageIncreaseRatioChanged
            ?.Invoke(
                directDamageIncreaseRatio
            );

        StateChanged?.Invoke();

        if (showDebugLog)
        {
            Debug.Log(
                "StageModifierState: " +
                "직접 피해 증가율을 되돌렸습니다. " +
                $"감소={ratio:P0}, " +
                $"현재={directDamageIncreaseRatio:P0}",
                this
            );
        }

        return true;
    }

    public bool TryAddEnemyMaxHealthReductionRatio(
        float ratio)
    {
        if (ratio <= 0f)
        {
            return false;
        }

        enemyMaxHealthReductionRatio +=
            ratio;

        enemyMaxHealthReductionRatio =
            Mathf.Max(
                enemyMaxHealthReductionRatio,
                0f
            );

        EnemyMaxHealthReductionRatioChanged
            ?.Invoke(
                enemyMaxHealthReductionRatio
            );

        StateChanged?.Invoke();

        if (showDebugLog)
        {
            Debug.Log(
                "StageModifierState: " +
                "적 최대 체력 감소율을 추가했습니다. " +
                $"추가={ratio:P0}, " +
                $"현재={enemyMaxHealthReductionRatio:P0}",
                this
            );
        }

        return true;
    }

    public bool TryRemoveEnemyMaxHealthReductionRatio(
        float ratio)
    {
        if (ratio <= 0f ||
            enemyMaxHealthReductionRatio <= 0f)
        {
            return false;
        }

        float previousRatio =
            enemyMaxHealthReductionRatio;

        enemyMaxHealthReductionRatio =
            Mathf.Max(
                enemyMaxHealthReductionRatio -
                ratio,
                0f
            );

        if (Mathf.Approximately(
                previousRatio,
                enemyMaxHealthReductionRatio
            ))
        {
            return false;
        }

        EnemyMaxHealthReductionRatioChanged
            ?.Invoke(
                enemyMaxHealthReductionRatio
            );

        StateChanged?.Invoke();

        if (showDebugLog)
        {
            Debug.Log(
                "StageModifierState: " +
                "적 최대 체력 감소율을 되돌렸습니다. " +
                $"감소={ratio:P0}, " +
                $"현재={enemyMaxHealthReductionRatio:P0}",
                this
            );
        }

        return true;
    }

    public bool TryAddEnemyAttackDamageReductionRatio(
        float ratio)
    {
        if (ratio <= 0f)
        {
            return false;
        }

        enemyAttackDamageReductionRatio +=
            ratio;

        enemyAttackDamageReductionRatio =
            Mathf.Max(
                enemyAttackDamageReductionRatio,
                0f
            );

        EnemyAttackDamageReductionRatioChanged
            ?.Invoke(
                enemyAttackDamageReductionRatio
            );

        StateChanged?.Invoke();

        if (showDebugLog)
        {
            Debug.Log(
                "StageModifierState: " +
                "적 공격력 감소율을 추가했습니다. " +
                $"추가={ratio:P0}, " +
                $"현재={enemyAttackDamageReductionRatio:P0}",
                this
            );
        }

        return true;
    }

    public bool TryRemoveEnemyAttackDamageReductionRatio(
        float ratio)
    {
        if (ratio <= 0f ||
            enemyAttackDamageReductionRatio <= 0f)
        {
            return false;
        }

        float previousRatio =
            enemyAttackDamageReductionRatio;

        enemyAttackDamageReductionRatio =
            Mathf.Max(
                enemyAttackDamageReductionRatio -
                ratio,
                0f
            );

        if (Mathf.Approximately(
                previousRatio,
                enemyAttackDamageReductionRatio
            ))
        {
            return false;
        }

        EnemyAttackDamageReductionRatioChanged
            ?.Invoke(
                enemyAttackDamageReductionRatio
            );

        StateChanged?.Invoke();

        if (showDebugLog)
        {
            Debug.Log(
                "StageModifierState: " +
                "적 공격력 감소율을 되돌렸습니다. " +
                $"감소={ratio:P0}, " +
                $"현재={enemyAttackDamageReductionRatio:P0}",
                this
            );
        }

        return true;
    }

    public bool TryAddEnemyAttackIntervalBonusTurns(
        int turns)
    {
        if (turns <= 0)
        {
            return false;
        }

        enemyAttackIntervalBonusTurns +=
            turns;

        enemyAttackIntervalBonusTurns =
            Mathf.Max(
                enemyAttackIntervalBonusTurns,
                0
            );

        EnemyAttackIntervalBonusTurnsChanged
            ?.Invoke(
                enemyAttackIntervalBonusTurns
            );

        StateChanged?.Invoke();

        if (showDebugLog)
        {
            Debug.Log(
                "StageModifierState: " +
                "적 공격 주기 추가 턴을 등록했습니다. " +
                $"추가={turns}턴, " +
                $"현재={enemyAttackIntervalBonusTurns}턴",
                this
            );
        }

        return true;
    }

    public bool TryRemoveEnemyAttackIntervalBonusTurns(
        int turns)
    {
        if (turns <= 0 ||
            enemyAttackIntervalBonusTurns <= 0)
        {
            return false;
        }

        int previousTurns =
            enemyAttackIntervalBonusTurns;

        enemyAttackIntervalBonusTurns =
            Mathf.Max(
                enemyAttackIntervalBonusTurns -
                turns,
                0
            );

        if (previousTurns ==
            enemyAttackIntervalBonusTurns)
        {
            return false;
        }

        EnemyAttackIntervalBonusTurnsChanged
            ?.Invoke(
                enemyAttackIntervalBonusTurns
            );

        StateChanged?.Invoke();

        if (showDebugLog)
        {
            Debug.Log(
                "StageModifierState: " +
                "적 공격 주기 추가 턴을 되돌렸습니다. " +
                $"감소={turns}턴, " +
                $"현재={enemyAttackIntervalBonusTurns}턴",
                this
            );
        }

        return true;
    }

    public bool TryAddGoldGainIncreaseRatio(
        float ratio)
    {
        if (ratio <= 0f)
        {
            return false;
        }

        goldGainIncreaseRatio +=
            ratio;

        goldGainIncreaseRatio =
            Mathf.Max(
                goldGainIncreaseRatio,
                0f
            );

        GoldGainIncreaseRatioChanged
            ?.Invoke(
                goldGainIncreaseRatio
            );

        StateChanged?.Invoke();

        if (showDebugLog)
        {
            Debug.Log(
                "StageModifierState: " +
                "골드 획득 증가율을 추가했습니다. " +
                $"추가={ratio:P0}, " +
                $"현재={goldGainIncreaseRatio:P0}",
                this
            );
        }

        return true;
    }

    public bool TryRemoveGoldGainIncreaseRatio(
        float ratio)
    {
        if (ratio <= 0f ||
            goldGainIncreaseRatio <= 0f)
        {
            return false;
        }

        float previousRatio =
            goldGainIncreaseRatio;

        goldGainIncreaseRatio =
            Mathf.Max(
                goldGainIncreaseRatio -
                ratio,
                0f
            );

        if (Mathf.Approximately(
                previousRatio,
                goldGainIncreaseRatio
            ))
        {
            return false;
        }

        GoldGainIncreaseRatioChanged
            ?.Invoke(
                goldGainIncreaseRatio
            );

        StateChanged?.Invoke();

        if (showDebugLog)
        {
            Debug.Log(
                "StageModifierState: " +
                "골드 획득 증가율을 되돌렸습니다. " +
                $"감소={ratio:P0}, " +
                $"현재={goldGainIncreaseRatio:P0}",
                this
            );
        }

        return true;
    }

    public int ApplyDirectDamageModifier(
        int baseDamage)
    {
        baseDamage =
            Mathf.Max(
                baseDamage,
                1
            );

        if (directDamageIncreaseRatio <= 0f)
        {
            return baseDamage;
        }

        float modifiedDamage =
            baseDamage *
            (
                1f +
                directDamageIncreaseRatio
            );

        return Mathf.Max(
            Mathf.CeilToInt(
                modifiedDamage
            ),
            1
        );
    }

    public int ApplyEnemyAttackDamageModifier(
        int baseDamage)
    {
        baseDamage =
            Mathf.Max(
                baseDamage,
                0
            );

        if (baseDamage <= 0 ||
            enemyAttackDamageReductionRatio <= 0f)
        {
            return baseDamage;
        }

        float reductionRatio =
            Mathf.Clamp01(
                enemyAttackDamageReductionRatio
            );

        float modifiedDamage =
            baseDamage *
            (
                1f -
                reductionRatio
            );

        return Mathf.Max(
            Mathf.FloorToInt(
                modifiedDamage
            ),
            1
        );
    }

    public int ApplyEnemyAttackIntervalModifier(
        int baseIntervalTurns)
    {
        baseIntervalTurns =
            Mathf.Max(
                baseIntervalTurns,
                1
            );

        return Mathf.Max(
            baseIntervalTurns +
            enemyAttackIntervalBonusTurns,
            1
        );
    }

    public int ApplyGoldGainModifier(
        int baseGold)
    {
        baseGold =
            Mathf.Max(
                baseGold,
                0
            );

        if (baseGold <= 0 ||
            goldGainIncreaseRatio <= 0f)
        {
            return baseGold;
        }

        float modifiedGold =
            baseGold *
            (
                1f +
                goldGainIncreaseRatio
            );

        return Mathf.Max(
            Mathf.CeilToInt(
                modifiedGold
            ),
            1
        );
    }

    public void ResetForNewStage()
    {
        directDamageIncreaseRatio =
            0f;

        enemyMaxHealthReductionRatio =
            0f;

        enemyAttackDamageReductionRatio =
            0f;

        enemyAttackIntervalBonusTurns =
            0;

        goldGainIncreaseRatio =
            0f;

        DirectDamageIncreaseRatioChanged
            ?.Invoke(
                directDamageIncreaseRatio
            );

        EnemyMaxHealthReductionRatioChanged
            ?.Invoke(
                enemyMaxHealthReductionRatio
            );

        EnemyAttackDamageReductionRatioChanged
            ?.Invoke(
                enemyAttackDamageReductionRatio
            );

        EnemyAttackIntervalBonusTurnsChanged
            ?.Invoke(
                enemyAttackIntervalBonusTurns
            );

        GoldGainIncreaseRatioChanged
            ?.Invoke(
                goldGainIncreaseRatio
            );

        StageStateReset?.Invoke();
        StateChanged?.Invoke();

        if (showDebugLog)
        {
            Debug.Log(
                "StageModifierState: " +
                "새 스테이지 버프 상태를 초기화했습니다.",
                this
            );
        }
    }
}