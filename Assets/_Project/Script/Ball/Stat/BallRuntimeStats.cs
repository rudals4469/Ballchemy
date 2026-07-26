using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BallRuntimeStats :
    MonoBehaviour
{
    [Header("Direct Damage")]

    [Tooltip(
        "모든 공이 공통으로 사용하는 " +
        "기본 직접 타격 피해입니다."
    )]
    [SerializeField, Min(1)]
    private int baseDirectDamage = 2;

    [Tooltip(
        "현재 런에서 획득한 " +
        "모든 공의 고정 직접 피해 증가량입니다."
    )]
    [SerializeField]
    private int runDirectDamageBonus;

    [Tooltip(
        "현재 런에서 획득한 " +
        "모든 공의 직접 피해 배율 증가량입니다. " +
        "0.2는 최종 피해 배율 +20%를 뜻합니다."
    )]
    [SerializeField, Min(0f)]
    private float runDirectDamageMultiplierBonus;

    [Header("Critical Damage")]

    [Tooltip(
        "현재 런에서 획득한 " +
        "치명타 피해 배율의 추가량입니다. " +
        "0.2는 치명타 배율에 +0.2를 더합니다."
    )]
    [SerializeField, Min(0f)]
    private float criticalDamageMultiplierBonus;

    public int BaseDirectDamage =>
        baseDirectDamage;

    public int RunDirectDamageBonus =>
        runDirectDamageBonus;

    public float RunDirectDamageMultiplierBonus =>
        runDirectDamageMultiplierBonus;

    public float DirectDamageMultiplier =>
        1f +
        runDirectDamageMultiplierBonus;

    public float CriticalDamageMultiplierBonus =>
        criticalDamageMultiplierBonus;

    public event Action StatsChanged;

    private void OnValidate()
    {
        baseDirectDamage =
            Mathf.Max(
                baseDirectDamage,
                1
            );

        runDirectDamageMultiplierBonus =
            Mathf.Max(
                runDirectDamageMultiplierBonus,
                0f
            );

        criticalDamageMultiplierBonus =
            Mathf.Max(
                criticalDamageMultiplierBonus,
                0f
            );
    }

    public int CalculateDirectDamage(
        int definitionDamageBonus,
        int individualDamageBonus = 0)
    {
        int flatDamage =
            baseDirectDamage +
            runDirectDamageBonus +
            definitionDamageBonus +
            individualDamageBonus;

        flatDamage =
            Mathf.Max(
                flatDamage,
                1
            );

        int finalDamage =
            Mathf.RoundToInt(
                flatDamage *
                DirectDamageMultiplier
            );

        return Mathf.Max(
            finalDamage,
            1
        );
    }

    public float CalculateCriticalMultiplier(
        float gradeMultiplier,
        float individualMultiplierBonus = 0f)
    {
        float finalMultiplier =
            gradeMultiplier +
            criticalDamageMultiplierBonus +
            individualMultiplierBonus;

        return Mathf.Max(
            finalMultiplier,
            1f
        );
    }

    public void SetBaseDirectDamage(
        int amount)
    {
        amount =
            Mathf.Max(
                amount,
                1
            );

        if (baseDirectDamage == amount)
        {
            return;
        }

        baseDirectDamage =
            amount;

        NotifyStatsChanged();
    }

    public void AddRunDirectDamageBonus(
        int amount)
    {
        if (amount == 0)
        {
            return;
        }

        runDirectDamageBonus +=
            amount;

        NotifyStatsChanged();
    }

    public void SetRunDirectDamageBonus(
        int amount)
    {
        if (runDirectDamageBonus == amount)
        {
            return;
        }

        runDirectDamageBonus =
            amount;

        NotifyStatsChanged();
    }

    public void AddRunDirectDamageMultiplierBonus(
        float amount)
    {
        if (Mathf.Approximately(
                amount,
                0f
            ))
        {
            return;
        }

        runDirectDamageMultiplierBonus =
            Mathf.Max(
                runDirectDamageMultiplierBonus +
                amount,
                0f
            );

        NotifyStatsChanged();
    }

    public void SetRunDirectDamageMultiplierBonus(
        float amount)
    {
        amount =
            Mathf.Max(
                amount,
                0f
            );

        if (Mathf.Approximately(
                runDirectDamageMultiplierBonus,
                amount
            ))
        {
            return;
        }

        runDirectDamageMultiplierBonus =
            amount;

        NotifyStatsChanged();
    }

    public void AddCriticalDamageMultiplierBonus(
        float amount)
    {
        if (Mathf.Approximately(
                amount,
                0f
            ))
        {
            return;
        }

        criticalDamageMultiplierBonus =
            Mathf.Max(
                criticalDamageMultiplierBonus +
                amount,
                0f
            );

        NotifyStatsChanged();
    }

    public void SetCriticalDamageMultiplierBonus(
        float amount)
    {
        amount =
            Mathf.Max(
                amount,
                0f
            );

        if (Mathf.Approximately(
                criticalDamageMultiplierBonus,
                amount
            ))
        {
            return;
        }

        criticalDamageMultiplierBonus =
            amount;

        NotifyStatsChanged();
    }

    public void ResetRunBonuses()
    {
        runDirectDamageBonus = 0;
        runDirectDamageMultiplierBonus = 0f;
        criticalDamageMultiplierBonus = 0f;

        NotifyStatsChanged();
    }

    private void NotifyStatsChanged()
    {
        StatsChanged?.Invoke();
    }
}