using System;
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
    [SerializeField] private BossDefinition fallbackBoss;

    public bool TryResolve(int stageNumber, out BossDefinition boss, out bool usedFallback)
    {
        stageNumber = Mathf.Max(stageNumber, 1);

        int entryCount = stageBosses != null ? stageBosses.Length : 0;

        for (int i = 0; i < entryCount; i++)
        {
            StageBossEntry entry = stageBosses[i];
            if (entry != null && entry.stageNumber == stageNumber && entry.bossDefinition != null)
            {
                boss = entry.bossDefinition;
                usedFallback = false;
                return true;
            }
        }

        boss = fallbackBoss;
        usedFallback = boss != null;
        return boss != null;
    }
}
