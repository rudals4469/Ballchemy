using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public sealed class BlockStatVariationSettings
{
    [Header("Health Variation")]
    [Tooltip(
        "파괴 가능한 블록 체력의 기준값 대비 랜덤 편차 비율입니다."
    )]
    [SerializeField, Range(0f, 1f)]
    private float healthVariationRatio = 0.15f;

    [Header("Attack Variation")]
    [Tooltip(
        "공격 가능한 블록 공격력의 기준값 대비 랜덤 편차 비율입니다."
    )]
    [SerializeField, Range(0f, 1f)]
    private float attackVariationRatio = 0.1f;

    [Tooltip(
        "낮은 공격력에서도 정수 반올림 후 편차가 보이도록 보장하는 최소 절대 편차입니다."
    )]
    [SerializeField, Min(0)]
    private int minimumAbsoluteAttackVariation = 1;

    public void Normalize()
    {
        healthVariationRatio =
            Mathf.Clamp01(healthVariationRatio);
        attackVariationRatio =
            Mathf.Clamp01(attackVariationRatio);
        minimumAbsoluteAttackVariation =
            Mathf.Max(
                minimumAbsoluteAttackVariation,
                0
            );
    }

    public List<BlockSpawnRequest> Apply(
        IReadOnlyList<BlockSpawnRequest> sourceRequests)
    {
        Normalize();

        List<BlockSpawnRequest> result =
            new List<BlockSpawnRequest>();

        if (sourceRequests == null)
        {
            return result;
        }

        for (int i = 0;
             i < sourceRequests.Count;
             i++)
        {
            BlockSpawnRequest source =
                sourceRequests[i];

            if (source == null)
            {
                continue;
            }

            bool usesHealth =
                source.Definition == null ||
                source.Definition.DestructionRule ==
                    BlockDestructionRule.Breakable;

            bool usesAttack =
                source.Attack > 0 &&
                source.RequestedBlockType !=
                    BlockType.Special;

            result.Add(
                source.CreateCopyWithStats(
                    usesHealth
                        ? RollHealth(source.Health)
                        : source.Health,
                    usesAttack
                        ? RollAttack(source.Attack)
                        : source.Attack)
            );
        }

        return result;
    }

    private int RollHealth(
        int baseHealth)
    {
        baseHealth = Mathf.Max(baseHealth, 1);

        return Mathf.Max(
            1,
            Mathf.RoundToInt(
                baseHealth *
                Random.Range(
                    1f - healthVariationRatio,
                    1f + healthVariationRatio
                )
            )
        );
    }

    private int RollAttack(
        int baseAttack)
    {
        baseAttack = Mathf.Max(baseAttack, 1);

        float variationAmount =
            Mathf.Max(
                baseAttack * attackVariationRatio,
                minimumAbsoluteAttackVariation
            );

        return Mathf.Max(
            1,
            Mathf.RoundToInt(
                baseAttack +
                Random.Range(
                    -variationAmount,
                    variationAmount
                )
            )
        );
    }
}
