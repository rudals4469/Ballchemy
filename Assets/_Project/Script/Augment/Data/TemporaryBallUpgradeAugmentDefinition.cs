using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Augment_TemporaryBallUpgrade",
    menuName =
        "Ballchemy/Augments/Temporary Ball Upgrade Augment"
)]
public sealed class
    TemporaryBallUpgradeAugmentDefinition :
        AugmentDefinition
{
    [Header("Activation Chance By Level")]

    [Tooltip(
        "각 레벨에서 공 하나마다 임시 강화가 " +
        "발동할 확률입니다.\n" +
        "Element 0은 Lv.1이며, 0.15는 15%입니다."
    )]
    [SerializeField]
    private List<float> activationChanceByLevel =
        new List<float>
        {
            0.15f,
            0.30f,
            0.30f
        };

    [Header("Upgrade Steps By Level")]

    [Tooltip(
        "강화가 발동했을 때 임시로 상승시키는 " +
        "등급 단계 수입니다.\n" +
        "Element 0은 Lv.1입니다."
    )]
    [SerializeField]
    private List<int> upgradeStepsByLevel =
        new List<int>
        {
            1,
            1,
            2
        };

    [Header("Maximum Grade Bonus")]

    [Tooltip(
        "이미 최종 등급인 공에서 강화가 발동했을 때 " +
        "해당 비행 동안 추가되는 직접 피해입니다."
    )]
    [SerializeField, Min(0)]
    private int maximumGradeDamageBonus = 2;

    public int MaximumGradeDamageBonus =>
        maximumGradeDamageBonus;

    public float GetActivationChance(
        int level)
    {
        if (level <= 0 ||
            activationChanceByLevel == null ||
            activationChanceByLevel.Count == 0)
        {
            return 0f;
        }

        int index =
            Mathf.Clamp(
                level - 1,
                0,
                activationChanceByLevel.Count - 1
            );

        return Mathf.Clamp01(
            activationChanceByLevel[index]
        );
    }

    public int GetUpgradeStepCount(
        int level)
    {
        if (level <= 0 ||
            upgradeStepsByLevel == null ||
            upgradeStepsByLevel.Count == 0)
        {
            return 0;
        }

        int index =
            Mathf.Clamp(
                level - 1,
                0,
                upgradeStepsByLevel.Count - 1
            );

        return Mathf.Max(
            upgradeStepsByLevel[index],
            0
        );
    }

    public override bool ApplyLevel(
        RunAugmentState runState,
        int previousLevel,
        int newLevel)
    {
        /*
         * 이 증강은 획득 즉시 공 Definition을 변경하지 않습니다.
         *
         * 실제 효과는 각 공을 발사하기 직전에
         * TemporaryBallUpgradeAugmentSystem이 처리합니다.
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

        float chance =
            GetActivationChance(
                level
            );

        int upgradeSteps =
            GetUpgradeStepCount(
                level
            );

        return
            $"{chance * 100f:0.#}% 확률로 " +
            $"해당 비행 동안 공의 등급이 " +
            $"{upgradeSteps}단계 상승합니다.";
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        if (activationChanceByLevel == null)
        {
            activationChanceByLevel =
                new List<float>();
        }

        if (upgradeStepsByLevel == null)
        {
            upgradeStepsByLevel =
                new List<int>();
        }

        EnsureFloatListSize(
            activationChanceByLevel,
            MaxLevel,
            0f
        );

        EnsureIntListSize(
            upgradeStepsByLevel,
            MaxLevel,
            1
        );

        for (int i = 0;
             i < activationChanceByLevel.Count;
             i++)
        {
            activationChanceByLevel[i] =
                Mathf.Clamp01(
                    activationChanceByLevel[i]
                );
        }

        for (int i = 0;
             i < upgradeStepsByLevel.Count;
             i++)
        {
            upgradeStepsByLevel[i] =
                Mathf.Max(
                    upgradeStepsByLevel[i],
                    0
                );
        }

        maximumGradeDamageBonus =
            Mathf.Max(
                maximumGradeDamageBonus,
                0
            );
    }

    private static void EnsureFloatListSize(
        List<float> values,
        int requiredSize,
        float defaultValue)
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

        while (values.Count < requiredSize)
        {
            float nextValue =
                values.Count > 0
                    ? values[values.Count - 1]
                    : defaultValue;

            values.Add(
                nextValue
            );
        }
    }

    private static void EnsureIntListSize(
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

        while (values.Count < requiredSize)
        {
            int nextValue =
                values.Count > 0
                    ? values[values.Count - 1]
                    : defaultValue;

            values.Add(
                nextValue
            );
        }
    }
}