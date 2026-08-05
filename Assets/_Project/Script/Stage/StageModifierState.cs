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

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog =
        true;

    public float DirectDamageIncreaseRatio =>
        directDamageIncreaseRatio;

    public float EnemyMaxHealthReductionRatio =>
        enemyMaxHealthReductionRatio;

    public bool HasDirectDamageIncrease =>
        directDamageIncreaseRatio > 0f;

    public bool HasEnemyMaxHealthReduction =>
        enemyMaxHealthReductionRatio > 0f;

    public event Action
        StateChanged;

    public event Action<float>
        DirectDamageIncreaseRatioChanged;

    public event Action<float>
        EnemyMaxHealthReductionRatioChanged;

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

    public void ResetForNewStage()
    {
        directDamageIncreaseRatio =
            0f;

        enemyMaxHealthReductionRatio =
            0f;

        DirectDamageIncreaseRatioChanged
            ?.Invoke(
                directDamageIncreaseRatio
            );

        EnemyMaxHealthReductionRatioChanged
            ?.Invoke(
                enemyMaxHealthReductionRatio
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