using System;
using UnityEngine;

public enum BossEncounterArchetype
{
    Pattern = 0,
    ProliferatingColony = 1
}

[CreateAssetMenu(fileName = "BossDefinition", menuName = "Ballchemy/Boss/Boss Definition")]
public sealed class BossDefinition : ScriptableObject
{
    [SerializeField] private string bossId = "boss";
    [SerializeField] private string displayName = "Boss";
    [SerializeField] private BossPatternDefinition patternDefinition;
    [SerializeField] private BossEncounterArchetype encounterArchetype =
        BossEncounterArchetype.Pattern;
    [Tooltip("RequiredEnemy 보스 블록에 적용할 체력 배율입니다.")]
    [SerializeField, Min(0.01f)] private float requiredEnemyHealthMultiplier = 1f;
    [SerializeField, Min(1)] private int attackIntervalTurns = 2;
    [SerializeField] private BossAttackDefinition[] attacks = Array.Empty<BossAttackDefinition>();
    [SerializeField, Min(0)] private int spawnedBlockCountPerAttack = 2;
    [SerializeField, Min(0)] private int spawnedBlockIncreasePerStage = 2;
    [SerializeField, Min(1)] private int spawnedBlockHealth = 10;
    [SerializeField, Range(0f, 1f)] private float specialBlockSpawnChance = 0.35f;
    [SerializeField, Min(1)] private int specialBlockHealth = 5;
    [SerializeField] private BlockDefinition[] specialBlockDefinitions = Array.Empty<BlockDefinition>();

    [Header("Proliferating Colony")]
    [SerializeField, Min(1)] private int colonyMinimumCoreCount = 2;
    [SerializeField, Min(1)] private int colonyMaximumCoreCount = 4;
    [SerializeField, Min(0)] private int colonyInitialGrowthCount = 3;
    [SerializeField, Min(1)] private int colonyGrowthBlockHealth = 3;
    [SerializeField, Min(0)] private int colonyMinimumCoreDistance = 4;
    [SerializeField, Min(0f)] private float colonyInitialGrowthStepDelay = 0.22f;
    [SerializeField] private Color colonyGrowthOutlineColor =
        new Color(0.25f, 1f, 0.35f, 0.95f);

    public string BossId => bossId;
    public string DisplayName => displayName;
    public BossPatternDefinition PatternDefinition => patternDefinition;
    public BossEncounterArchetype EncounterArchetype => encounterArchetype;
    public bool IsProliferatingColony =>
        encounterArchetype == BossEncounterArchetype.ProliferatingColony;
    public float RequiredEnemyHealthMultiplier => Mathf.Max(requiredEnemyHealthMultiplier, 0.01f);
    public int AttackIntervalTurns => Mathf.Max(attackIntervalTurns, 1);
    public int AttackCount => attacks != null ? attacks.Length : 0;
    public int SpawnedBlockCountPerAttack => Mathf.Max(spawnedBlockCountPerAttack, 0);
    public int SpawnedBlockIncreasePerStage => Mathf.Max(spawnedBlockIncreasePerStage, 0);
    public int SpawnedBlockHealth => Mathf.Max(spawnedBlockHealth, 1);
    public float SpecialBlockSpawnChance => Mathf.Clamp01(specialBlockSpawnChance);
    public int SpecialBlockHealth => Mathf.Max(specialBlockHealth, 1);
    public int ColonyInitialGrowthCount => Mathf.Max(colonyInitialGrowthCount, 0);
    public int ColonyGrowthBlockHealth => Mathf.Max(colonyGrowthBlockHealth, 1);
    public int ColonyMinimumCoreDistance => Mathf.Max(colonyMinimumCoreDistance, 0);
    public float ColonyInitialGrowthStepDelay => Mathf.Max(colonyInitialGrowthStepDelay, 0f);
    public Color ColonyGrowthOutlineColor => colonyGrowthOutlineColor;

    public int CalculateColonyCoreCount(
        int stageNumber)
    {
        int minimum =
            Mathf.Max(
                colonyMinimumCoreCount,
                1
            );

        int maximum =
            Mathf.Max(
                colonyMaximumCoreCount,
                minimum
            );

        float stageProgress =
            Mathf.Clamp01(
                (Mathf.Max(stageNumber, 1) - 1) /
                5f
            );

        return Mathf.Clamp(
            Mathf.RoundToInt(
                Mathf.Lerp(
                    minimum,
                    maximum,
                    stageProgress
                )
            ),
            minimum,
            maximum
        );
    }

    public int CalculateSpawnedBlockCount(int stageNumber)
    {
        return SpawnedBlockCountPerAttack +
               Mathf.Max(stageNumber - 1, 0) * SpawnedBlockIncreasePerStage;
    }

    public BlockDefinition GetRandomSpecialBlockDefinition()
    {
        if (specialBlockDefinitions == null || specialBlockDefinitions.Length == 0)
        {
            return null;
        }

        int totalWeight = 0;
        for (int i = 0; i < specialBlockDefinitions.Length; i++)
        {
            BlockDefinition definition = specialBlockDefinitions[i];
            if (definition != null && definition.BlockType == BlockType.Special)
            {
                totalWeight += Mathf.Max(definition.SelectionWeight, 1);
            }
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        for (int i = 0; i < specialBlockDefinitions.Length; i++)
        {
            BlockDefinition definition = specialBlockDefinitions[i];
            if (definition == null || definition.BlockType != BlockType.Special)
            {
                continue;
            }

            randomValue -= Mathf.Max(definition.SelectionWeight, 1);
            if (randomValue < 0)
            {
                return definition;
            }
        }

        return null;
    }

    public BossAttackDefinition GetAttack(int index)
    {
        if (attacks == null || attacks.Length == 0)
        {
            return null;
        }

        index = Mathf.Abs(index) % attacks.Length;
        return attacks[index];
    }

    private void OnValidate()
    {
        requiredEnemyHealthMultiplier = Mathf.Max(requiredEnemyHealthMultiplier, 0.01f);
        attackIntervalTurns = Mathf.Max(attackIntervalTurns, 1);
        spawnedBlockCountPerAttack = Mathf.Max(spawnedBlockCountPerAttack, 0);
        spawnedBlockIncreasePerStage = Mathf.Max(spawnedBlockIncreasePerStage, 0);
        spawnedBlockHealth = Mathf.Max(spawnedBlockHealth, 1);
        specialBlockSpawnChance = Mathf.Clamp01(specialBlockSpawnChance);
        specialBlockHealth = Mathf.Max(specialBlockHealth, 1);
        colonyMinimumCoreCount = Mathf.Max(colonyMinimumCoreCount, 1);
        colonyMaximumCoreCount = Mathf.Max(colonyMaximumCoreCount, colonyMinimumCoreCount);
        colonyInitialGrowthCount = Mathf.Max(colonyInitialGrowthCount, 0);
        colonyGrowthBlockHealth = Mathf.Max(colonyGrowthBlockHealth, 1);
        colonyMinimumCoreDistance = Mathf.Max(colonyMinimumCoreDistance, 0);
        colonyInitialGrowthStepDelay = Mathf.Max(colonyInitialGrowthStepDelay, 0f);
    }
}
