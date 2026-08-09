using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BossCatalog", menuName = "Ballchemy/Boss/Boss Catalog")]
public sealed class BossCatalog : ScriptableObject
{
    [Serializable]
    private sealed class StageBossEntry
    {
        [Min(1)] public int stageNumber = 1;
        public BossDefinition bossDefinition;
    }

    [SerializeField] private StageBossEntry[] stageBosses = Array.Empty<StageBossEntry>();
    [Tooltip("런 시작 시 무작위 순서를 만들 보스 후보 풀입니다.")]
    [SerializeField] private BossDefinition[] bossPool = Array.Empty<BossDefinition>();
    [SerializeField] private BossDefinition fallbackBoss;

    [Header("Boss Test Override")]
    [Tooltip("활성화하면 모든 스테이지에서 아래 테스트 보스를 우선 사용합니다.")]
    [SerializeField] private bool useTestBossOverride;
    [SerializeField] private BossDefinition testBoss;

    public bool TryGetTestBoss(
        out BossDefinition boss)
    {
        boss = useTestBossOverride
            ? testBoss
            : null;

        return boss != null;
    }

    public int CopyUniqueBossesTo(
        List<BossDefinition> destination)
    {
        if (destination == null)
        {
            return 0;
        }

        destination.Clear();

        AddUniqueBosses(
            destination,
            bossPool
        );

        return destination.Count;
    }

    public bool TryGetStageBoss(
        int stageNumber,
        out BossDefinition boss)
    {
        boss = null;
        stageNumber = Mathf.Max(stageNumber, 1);

        int entryCount =
            stageBosses != null
                ? stageBosses.Length
                : 0;

        for (int i = 0; i < entryCount; i++)
        {
            StageBossEntry entry =
                stageBosses[i];

            if (entry == null ||
                entry.stageNumber != stageNumber ||
                entry.bossDefinition == null)
            {
                continue;
            }

            boss = entry.bossDefinition;
            return true;
        }

        return false;
    }

    public bool TryResolve(int stageNumber, out BossDefinition boss, out bool usedFallback)
    {
        stageNumber = Mathf.Max(stageNumber, 1);

        if (TryGetStageBoss(
                stageNumber,
                out boss))
        {
            usedFallback = false;
            return true;
        }

        boss = fallbackBoss;
        usedFallback = boss != null;
        return boss != null;
    }

    private static void AddUniqueBosses(
        List<BossDefinition> destination,
        BossDefinition[] definitions)
    {
        int definitionCount =
            definitions != null
                ? definitions.Length
                : 0;

        for (int i = 0; i < definitionCount; i++)
        {
            AddUniqueBoss(
                destination,
                definitions[i]
            );
        }
    }

    private static void AddUniqueBoss(
        List<BossDefinition> destination,
        BossDefinition definition)
    {
        if (definition == null ||
            destination.Contains(definition))
        {
            return;
        }

        destination.Add(definition);
    }
}
