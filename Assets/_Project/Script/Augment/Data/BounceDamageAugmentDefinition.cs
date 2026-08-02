using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Augment_BounceDamage",
    menuName =
        "Ballchemy/Augments/Bounce Damage Augment"
)]
public sealed class BounceDamageAugmentDefinition :
    AugmentDefinition
{
    [Header("Bounce Damage By Level")]

    [Tooltip(
        "피해가 1회 증가하는 데 필요한 반사 횟수입니다.\n" +
        "Element 0은 Lv.1입니다."
    )]
    [SerializeField]
    private List<int> requiredBouncesByLevel =
        new List<int>
        {
            3,
            2,
            1
        };

    [Tooltip(
        "조건을 한 번 달성할 때 증가하는 직접 피해입니다.\n" +
        "Element 0은 Lv.1입니다."
    )]
    [SerializeField]
    private List<int> damagePerStepByLevel =
        new List<int>
        {
            1,
            1,
            1
        };

    public int GetRequiredBounceCount(
        int level)
    {
        if (level <= 0 ||
            requiredBouncesByLevel == null ||
            requiredBouncesByLevel.Count == 0)
        {
            return 0;
        }

        int index =
            Mathf.Clamp(
                level - 1,
                0,
                requiredBouncesByLevel.Count - 1
            );

        return Mathf.Max(
            requiredBouncesByLevel[index],
            1
        );
    }

    public int GetDamagePerStep(
        int level)
    {
        if (level <= 0 ||
            damagePerStepByLevel == null ||
            damagePerStepByLevel.Count == 0)
        {
            return 0;
        }

        int index =
            Mathf.Clamp(
                level - 1,
                0,
                damagePerStepByLevel.Count - 1
            );

        return Mathf.Max(
            damagePerStepByLevel[index],
            0
        );
    }

    public int CalculateDamageBonus(
        int level,
        int bounceCount)
    {
        if (!IsValidLevel(level) ||
            bounceCount <= 0)
        {
            return 0;
        }

        int requiredBounceCount =
            GetRequiredBounceCount(
                level
            );

        int damagePerStep =
            GetDamagePerStep(
                level
            );

        if (requiredBounceCount <= 0 ||
            damagePerStep <= 0)
        {
            return 0;
        }

        int completedSteps =
            bounceCount /
            requiredBounceCount;

        return Mathf.Max(
            completedSteps *
            damagePerStep,
            0
        );
    }

    public override bool ApplyLevel(
        RunAugmentState runState,
        int previousLevel,
        int newLevel)
    {
        /*
         * 획득 즉시 기존 공의 영구 스탯을 변경하지 않습니다.
         *
         * 공이 비행하며 실제로 반사될 때
         * BounceDamageAugmentSystem이 현재 레벨을 읽어
         * 비행 중 임시 피해 보너스를 계산합니다.
         */
        return
            runState != null &&
            IsValidLevel(newLevel);
    }

    public override string GetLevelDescription(
        int level)
    {
        if (!IsValidLevel(level))
        {
            return string.Empty;
        }

        int requiredBounceCount =
            GetRequiredBounceCount(
                level
            );

        int damagePerStep =
            GetDamagePerStep(
                level
            );

        return
            $"공이 {requiredBounceCount}회 반사될 때마다 " +
            $"직접 피해가 {damagePerStep} 증가합니다.";
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        if (requiredBouncesByLevel == null)
        {
            requiredBouncesByLevel =
                new List<int>();
        }

        if (damagePerStepByLevel == null)
        {
            damagePerStepByLevel =
                new List<int>();
        }

        EnsureListSize(
            requiredBouncesByLevel,
            MaxLevel,
            1
        );

        EnsureListSize(
            damagePerStepByLevel,
            MaxLevel,
            1
        );

        for (int i = 0;
             i < requiredBouncesByLevel.Count;
             i++)
        {
            requiredBouncesByLevel[i] =
                Mathf.Max(
                    requiredBouncesByLevel[i],
                    1
                );
        }

        for (int i = 0;
             i < damagePerStepByLevel.Count;
             i++)
        {
            damagePerStepByLevel[i] =
                Mathf.Max(
                    damagePerStepByLevel[i],
                    0
                );
        }
    }

    private static void EnsureListSize(
        List<int> values,
        int requiredSize,
        int defaultValue)
    {
        if (values == null)
        {
            return;
        }

        requiredSize =
            Mathf.Max(
                requiredSize,
                1
            );

        while (values.Count <
               requiredSize)
        {
            int nextValue =
                values.Count > 0
                    ? values[
                        values.Count - 1
                    ]
                    : defaultValue;

            values.Add(
                nextValue
            );
        }
    }
}