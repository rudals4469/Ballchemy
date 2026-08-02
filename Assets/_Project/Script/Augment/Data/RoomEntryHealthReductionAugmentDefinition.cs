using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Augment_RoomEntryHealthReduction",
    menuName =
        "Ballchemy/Augments/Room Entry Health Reduction Augment"
)]
public sealed class
    RoomEntryHealthReductionAugmentDefinition :
        AugmentDefinition
{
    [Header("Health Reduction By Level")]

    [Tooltip(
        "일반 전투방에서 각 레벨이 적용하는 " +
        "적 최대 체력 기준 감소 비율입니다.\n" +
        "Element 0은 Lv.1입니다.\n" +
        "0.1은 10% 감소를 의미합니다."
    )]
    [SerializeField]
    private List<float>
        normalRoomReductionByLevel =
            new List<float>
            {
                0.10f,
                0.15f,
                0.20f
            };

    [Tooltip(
        "네임드와 보스에게 적용되는 배율입니다.\n" +
        "0.5이면 일반방 감소율의 절반입니다."
    )]
    [SerializeField, Range(0f, 1f)]
    private float namedAndBossMultiplier =
        0.5f;

    public float NamedAndBossMultiplier =>
        namedAndBossMultiplier;

    public float GetReductionPercent(
        int level,
        RoomType roomType)
    {
        float normalPercent =
            GetNormalReductionPercent(
                level
            );

        switch (roomType)
        {
            case RoomType.NormalCombat:
                return normalPercent;

            case RoomType.NamedCombat:
            case RoomType.Boss:
                return
                    normalPercent *
                    namedAndBossMultiplier;

            default:
                return 0f;
        }
    }

    public float GetNormalReductionPercent(
        int level)
    {
        if (level <= 0 ||
            normalRoomReductionByLevel == null ||
            normalRoomReductionByLevel.Count == 0)
        {
            return 0f;
        }

        int index =
            Mathf.Clamp(
                level - 1,
                0,
                normalRoomReductionByLevel.Count - 1
            );

        return Mathf.Clamp01(
            normalRoomReductionByLevel[index]
        );
    }

    public override bool ApplyLevel(
        RunAugmentState runState,
        int previousLevel,
        int newLevel)
    {
        /*
         * 이 증강은 획득 순간 블록을 변경하지 않습니다.
         *
         * RunAugmentState에 레벨이 저장된 뒤,
         * 다음 전투방의 블록 생성이 끝났을 때
         * RoomEntryAugmentResolver가 실제 효과를 적용합니다.
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

        float normalPercent =
            GetReductionPercent(
                level,
                RoomType.NormalCombat
            );

        float namedPercent =
            GetReductionPercent(
                level,
                RoomType.NamedCombat
            );

        return
            "방 입장 시 일반 적의 체력을 " +
            $"{FormatPercent(normalPercent)} 감소시키고, " +
            "네임드와 보스에게는 " +
            $"{FormatPercent(namedPercent)}를 적용합니다.";
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        namedAndBossMultiplier =
            Mathf.Clamp01(
                namedAndBossMultiplier
            );

        if (normalRoomReductionByLevel == null)
        {
            normalRoomReductionByLevel =
                new List<float>();
        }

        while (
            normalRoomReductionByLevel.Count <
            MaxLevel
        )
        {
            float nextValue =
                normalRoomReductionByLevel.Count > 0
                    ? normalRoomReductionByLevel[
                        normalRoomReductionByLevel.Count - 1
                    ]
                    : 0f;

            normalRoomReductionByLevel.Add(
                nextValue
            );
        }

        float previousValue = 0f;

        for (int i = 0;
             i < normalRoomReductionByLevel.Count;
             i++)
        {
            float currentValue =
                Mathf.Clamp01(
                    normalRoomReductionByLevel[i]
                );

            currentValue =
                Mathf.Max(
                    currentValue,
                    previousValue
                );

            normalRoomReductionByLevel[i] =
                currentValue;

            previousValue =
                currentValue;
        }
    }

    private static string FormatPercent(
        float value)
    {
        float percent =
            Mathf.Clamp01(value) *
            100f;

        return
            $"{percent:0.#}%";
    }
}