using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Augment_DirectDamage",
    menuName =
        "Ballchemy/Augments/Direct Damage Augment"
)]
public sealed class DirectDamageAugmentDefinition :
    AugmentDefinition
{
    [Header("Direct Damage By Level")]

    [Tooltip(
        "각 레벨에서 제공하는 누적 직접 피해 증가량입니다.\n" +
        "Element 0은 Lv.1, Element 1은 Lv.2입니다.\n" +
        "예: 1, 2, 3이면 레벨별 총 증가량은 " +
        "+1, +2, +3입니다."
    )]
    [SerializeField]
    private List<int>
        totalDirectDamageBonusByLevel =
            new List<int>
            {
                1,
                2,
                3
            };

    public int GetTotalDirectDamageBonus(
        int level)
    {
        if (level <= 0)
        {
            return 0;
        }

        if (totalDirectDamageBonusByLevel ==
                null ||
            totalDirectDamageBonusByLevel.Count ==
                0)
        {
            return 0;
        }

        int index =
            Mathf.Clamp(
                level - 1,
                0,
                totalDirectDamageBonusByLevel.Count - 1
            );

        return Mathf.Max(
            totalDirectDamageBonusByLevel[index],
            0
        );
    }

    public override bool ApplyLevel(
        RunAugmentState runState,
        int previousLevel,
        int newLevel)
    {
        if (runState == null)
        {
            return false;
        }

        BallRuntimeStats runtimeStats =
            runState.BallRuntimeStats;

        if (runtimeStats == null)
        {
            Debug.LogError(
                "DirectDamageAugmentDefinition: " +
                "BallRuntimeStats가 없어 증강을 " +
                "적용할 수 없습니다.",
                runState
            );

            return false;
        }

        int previousTotalBonus =
            GetTotalDirectDamageBonus(
                previousLevel
            );

        int newTotalBonus =
            GetTotalDirectDamageBonus(
                newLevel
            );

        int additionalBonus =
            newTotalBonus -
            previousTotalBonus;

        if (additionalBonus < 0)
        {
            Debug.LogError(
                "DirectDamageAugmentDefinition: " +
                $"{name}의 Lv.{newLevel} 누적 피해가 " +
                "이전 레벨보다 낮습니다.",
                this
            );

            return false;
        }

        if (additionalBonus > 0)
        {
            runtimeStats.AddRunDirectDamageBonus(
                additionalBonus
            );
        }

        Debug.Log(
            "DirectDamageAugmentDefinition: " +
            $"{DisplayName} Lv.{newLevel} 적용, " +
            $"추가 직접 피해 +{additionalBonus}, " +
            $"현재 런 피해 보너스 " +
            $"+{runtimeStats.RunDirectDamageBonus}",
            this
        );

        return true;
    }

    public override string GetLevelDescription(
        int level)
    {
        if (!IsValidLevel(level))
        {
            return string.Empty;
        }

        int totalBonus =
            GetTotalDirectDamageBonus(
                level
            );

        return
            $"모든 공의 직접 피해가 " +
            $"{totalBonus} 증가합니다.";
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        if (totalDirectDamageBonusByLevel ==
            null)
        {
            totalDirectDamageBonusByLevel =
                new List<int>();
        }

        while (
            totalDirectDamageBonusByLevel.Count <
            MaxLevel
        )
        {
            int nextValue =
                totalDirectDamageBonusByLevel.Count > 0
                    ? totalDirectDamageBonusByLevel[
                        totalDirectDamageBonusByLevel.Count -
                        1
                    ]
                    : 0;

            totalDirectDamageBonusByLevel.Add(
                nextValue
            );
        }

        int previousValue = 0;

        for (int i = 0;
             i < totalDirectDamageBonusByLevel.Count;
             i++)
        {
            int currentValue =
                Mathf.Max(
                    totalDirectDamageBonusByLevel[i],
                    previousValue
                );

            totalDirectDamageBonusByLevel[i] =
                currentValue;

            previousValue =
                currentValue;
        }
    }
}