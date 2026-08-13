using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Augment_MultiDirectionLaunch",
    menuName =
        "Ballchemy/Augments/Multi Direction Launch Augment"
)]
public sealed class
    MultiDirectionLaunchAugmentDefinition :
        AugmentDefinition
{
    [Header("Branch Count By Level")]

    [Tooltip(
        "각 레벨에서 사용하는 발사 갈래 수입니다.\n" +
        "Element 0은 Lv.1입니다."
    )]
    [SerializeField]
    private List<int> branchCountByLevel =
        new List<int>
        {
            2,
            3
        };

    [Header("Spread Angle By Level")]

    [Tooltip(
        "기준 발사 방향에서 가장 바깥 갈래까지의 " +
        "각도 차이입니다.\n" +
        "Element 0은 Lv.1입니다."
    )]
    [SerializeField]
    private List<float> spreadAngleByLevel =
        new List<float>
        {
            6f,
            8f
        };

    public int GetBranchCount(
        int level)
    {
        if (level <= 0 ||
            branchCountByLevel == null ||
            branchCountByLevel.Count == 0)
        {
            return 1;
        }

        int index =
            Mathf.Clamp(
                level - 1,
                0,
                branchCountByLevel.Count - 1
            );

        return Mathf.Max(
            branchCountByLevel[index],
            1
        );
    }

    public float GetSpreadAngle(
        int level)
    {
        if (level <= 0 ||
            spreadAngleByLevel == null ||
            spreadAngleByLevel.Count == 0)
        {
            return 0f;
        }

        int index =
            Mathf.Clamp(
                level - 1,
                0,
                spreadAngleByLevel.Count - 1
            );

        return Mathf.Clamp(
            spreadAngleByLevel[index],
            0f,
            45f
        );
    }

    public override bool ApplyLevel(
        RunAugmentState runState,
        int previousLevel,
        int newLevel)
    {
        /*
         * 획득 순간 공이나 발사기를 직접 변경하지 않습니다.
         *
         * 실제 발사 직전에
         * MultiDirectionLaunchAugmentSystem이
         * 현재 레벨과 발사 방향을 계산합니다.
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

        int branchCount =
            GetBranchCount(
                level
            );

        float spreadAngle =
            GetSpreadAngle(
                level
            );

        return
            $"보유 공을 {branchCount}갈래로 나누어 " +
            $"기준에서 좌우 {spreadAngle:0.#}도 방향으로 " +
            "동시에 발사합니다.";
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        if (branchCountByLevel == null)
        {
            branchCountByLevel =
                new List<int>();
        }

        if (spreadAngleByLevel == null)
        {
            spreadAngleByLevel =
                new List<float>();
        }

        EnsureIntListSize(
            branchCountByLevel,
            MaxLevel,
            1
        );

        EnsureFloatListSize(
            spreadAngleByLevel,
            MaxLevel,
            0f
        );

        for (int i = 0;
             i < branchCountByLevel.Count;
             i++)
        {
            branchCountByLevel[i] =
                Mathf.Clamp(
                    branchCountByLevel[i],
                    1,
                    3
                );
        }

        for (int i = 0;
             i < spreadAngleByLevel.Count;
             i++)
        {
            spreadAngleByLevel[i] =
                Mathf.Clamp(
                    spreadAngleByLevel[i],
                    0f,
                    45f
                );
        }
    }

    private static void EnsureIntListSize(
        List<int> values,
        int requiredSize,
        int defaultValue)
    {
        requiredSize =
            Mathf.Max(
                requiredSize,
                1
            );

        while (values.Count < requiredSize)
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

    private static void EnsureFloatListSize(
        List<float> values,
        int requiredSize,
        float defaultValue)
    {
        requiredSize =
            Mathf.Max(
                requiredSize,
                1
            );

        while (values.Count < requiredSize)
        {
            float nextValue =
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
