using System;
using UnityEngine;

public enum BossEncounterArchetype
{
    Pattern = 0,
    ProliferatingColony = 1,
    DescendingWave = 2,
    BombReactor = 3,
    TrapMaster = 4,
    TeleportCircuit = 5
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
    [Tooltip("보스가 배정된 스테이지가 하나 증가할 때 보스 핵심 체력에 복리로 적용되는 비율입니다.")]
    [SerializeField, Min(0f)] private float bossHealthIncreasePerStageRatio = 0.55f;
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
    [SerializeField, Range(0f, 1f)] private float colonyMaximumSpawnRowRatio = 0.5f;
    [SerializeField] private Vector2 colonyGrowthHealthRatioRange =
        new Vector2(0.4f, 0.6f);
    [SerializeField, Min(0)] private int colonyMinimumCoreDistance = 4;
    [SerializeField, Min(0f)] private float colonyInitialGrowthStepDelay = 0.22f;
    [SerializeField] private Color colonyGrowthOutlineColor =
        new Color(0.25f, 1f, 0.35f, 0.95f);

    [Header("Descending Wave")]
    [SerializeField, Min(1)] private int descendingWaveCount = 10;
    [SerializeField, Min(1)] private int descendingMinimumRowCount = 2;
    [SerializeField, Min(1)] private int descendingMaximumRowCount = 4;
    [SerializeField, Min(1)] private int descendingDamagePerEscapedBlock = 1;

    [Header("Bomb Reactor")]
    [SerializeField] private BlockDefinition reactorBombDefinition;
    [SerializeField, Min(1)] private int reactorBombsToDestroy = 5;
    [SerializeField, Min(1)] private int reactorBombHealth = 5;
    [SerializeField, Min(1)] private int reactorInitialBombCount = 2;
    [SerializeField, Min(1)] private int reactorBombsPerTurn = 1;
    [SerializeField, Min(1)] private int reactorDescentRowCount = 1;
    [SerializeField, Min(1)] private int reactorEscapedBombDamage = 2;
    [SerializeField, Min(1)] private int reactorBombDamage = 10;
    [SerializeField, Min(1)] private int reactorExplosionBlockDamage = 1;
    [SerializeField, Min(0)] private int reactorChainBossDamageBonus = 5;
    [SerializeField, Min(0)] private int reactorBaseAttackDamage = 2;
    [SerializeField, Min(0)] private int reactorCrossLockBonusDamage = 20;
    [SerializeField, Min(0)] private int reactorCrossLockAttackDelay = 1;
    [SerializeField] private BlockDefinition reactorBlockerDefinition;
    [SerializeField, Min(1)] private int reactorBlockerCount = 4;

    [Header("Trap Master")]
    [Tooltip("순서: 생명 공급, 공 정지, 전이, 증식, 방벽")]
    [SerializeField] private BlockDefinition[] trapDefinitions =
        Array.Empty<BlockDefinition>();
    [SerializeField] private BlockDefinition trapTerrainDefinition;
    [SerializeField] private BlockDefinition trapAmplifierDefinition;
    [SerializeField, Min(1)] private int trapInitialCount = 4;
    [SerializeField, Min(1)] private int trapSpawnCountPerTurn = 2;
    [SerializeField, Min(1)] private int trapMaximumCount = 10;
    [SerializeField, Min(1)] private int trapTerrainCount = 5;
    [SerializeField, Range(0.05f, 1f)] private float trapHealthBallRatio = 0.25f;
    [SerializeField, Range(0.05f, 2f)] private float trapAmplifierHealthBallRatio = 0.75f;
    [SerializeField, Range(0.05f, 0.4f)] private float trapBallSealRatio = 0.2f;
    [SerializeField, Min(1)] private int trapBossHealthIncrease = 20;
    [SerializeField, Min(1)] private int trapDisarmBossDamage = 8;
    [SerializeField, Min(0)] private int trapAmplifierBaseShield = 2;

    [Header("Boss Arena Blocks")]
    [SerializeField] private BlockDefinition arenaNormalBlockDefinition;
    [SerializeField, Min(0)] private int arenaNormalBlockCount = 24;
    [SerializeField, Min(1)] private int arenaNormalBlockHealth = 8;
    [SerializeField, Min(1)] private int arenaNormalRegenerationIntervalTurns = 3;
    [SerializeField, Min(1)] private int arenaNormalRegenerationCount = 6;

    [Header("Teleport Circuit")]
    [SerializeField] private BlockDefinition teleportCircuitPortalDefinition;
    [SerializeField, Min(3)] private int teleportCircuitPortalCount = 6;
    [SerializeField, Min(1)] private int teleportCircuitBonusThreshold = 3;
    [SerializeField, Min(1f)] private float teleportCircuitBossDamageMultiplier = 1.5f;

    [Header("Random Arena Obstacles")]
    [SerializeField] private bool randomizePatternObstacles;
    [SerializeField, Min(0)] private int randomBreakableObstacleCount = 20;
    [SerializeField, Min(0)] private int randomIndestructibleObstacleCount = 8;
    [SerializeField, Range(0, 3)] private int randomObstacleMaximumNeighborCount = 1;

    public string BossId => bossId;
    public string DisplayName => displayName;
    public BossPatternDefinition PatternDefinition => patternDefinition;
    public BossEncounterArchetype EncounterArchetype => encounterArchetype;
    public bool IsProliferatingColony =>
        encounterArchetype == BossEncounterArchetype.ProliferatingColony;
    public bool IsDescendingWave =>
        encounterArchetype == BossEncounterArchetype.DescendingWave;
    public bool IsBombReactor =>
        encounterArchetype == BossEncounterArchetype.BombReactor;
    public bool IsTrapMaster =>
        encounterArchetype == BossEncounterArchetype.TrapMaster;
    public bool IsTeleportCircuit =>
        encounterArchetype == BossEncounterArchetype.TeleportCircuit;
    public bool IsFrontlineCommander =>
        encounterArchetype == BossEncounterArchetype.Pattern &&
        string.Equals(
            bossId,
            "boss_frontline_commander_01",
            StringComparison.Ordinal);
    public float RequiredEnemyHealthMultiplier => Mathf.Max(requiredEnemyHealthMultiplier, 0.01f);
    public float BossHealthIncreasePerStageRatio =>
        Mathf.Max(bossHealthIncreasePerStageRatio, 0f);

    public int CalculateBossHealth(int baseHealth, int stageNumber)
    {
        int stageIndex = Mathf.Max(stageNumber - 1, 0);
        float stageMultiplier = Mathf.Pow(
            1f + BossHealthIncreasePerStageRatio,
            stageIndex);

        return Mathf.Max(
            Mathf.CeilToInt(
                Mathf.Max(baseHealth, 1) *
                stageMultiplier *
                RequiredEnemyHealthMultiplier),
            1);
    }
    public int AttackIntervalTurns => Mathf.Max(attackIntervalTurns, 1);
    public int AttackCount => attacks != null ? attacks.Length : 0;
    public int SpawnedBlockCountPerAttack => Mathf.Max(spawnedBlockCountPerAttack, 0);
    public int SpawnedBlockIncreasePerStage => Mathf.Max(spawnedBlockIncreasePerStage, 0);
    public int SpawnedBlockHealth => Mathf.Max(spawnedBlockHealth, 1);
    public float SpecialBlockSpawnChance => Mathf.Clamp01(specialBlockSpawnChance);
    public int SpecialBlockHealth => Mathf.Max(specialBlockHealth, 1);
    public int ColonyInitialGrowthCount => Mathf.Max(colonyInitialGrowthCount, 0);
    public float ColonyMaximumSpawnRowRatio =>
        Mathf.Clamp01(colonyMaximumSpawnRowRatio);
    public int ColonyMinimumCoreDistance => Mathf.Max(colonyMinimumCoreDistance, 0);
    public float ColonyInitialGrowthStepDelay => Mathf.Max(colonyInitialGrowthStepDelay, 0f);
    public Color ColonyGrowthOutlineColor => colonyGrowthOutlineColor;
    public int DescendingWaveCount => Mathf.Max(descendingWaveCount, 1);
    public int DescendingMinimumRowCount => Mathf.Max(descendingMinimumRowCount, 1);
    public int DescendingMaximumRowCount =>
        Mathf.Max(descendingMaximumRowCount, DescendingMinimumRowCount);
    public int DescendingDamagePerEscapedBlock =>
        Mathf.Max(descendingDamagePerEscapedBlock, 1);
    public BlockDefinition ReactorBombDefinition => reactorBombDefinition;
    public int ReactorBombsToDestroy => Mathf.Max(reactorBombsToDestroy, 1);
    public int ReactorBombHealth => Mathf.Max(reactorBombHealth, 1);
    public int ReactorInitialBombCount => Mathf.Max(reactorInitialBombCount, 1);
    public int ReactorBombsPerTurn => Mathf.Max(reactorBombsPerTurn, 1);
    public int ReactorDescentRowCount => Mathf.Max(reactorDescentRowCount, 1);
    public int ReactorEscapedBombDamage => Mathf.Max(reactorEscapedBombDamage, 1);
    public int ReactorBombDamage => Mathf.Max(reactorBombDamage, 1);
    public int ReactorExplosionBlockDamage => Mathf.Max(reactorExplosionBlockDamage, 1);
    public int ReactorChainBossDamageBonus => Mathf.Max(reactorChainBossDamageBonus, 0);
    public int ReactorBaseAttackDamage => Mathf.Max(reactorBaseAttackDamage, 0);
    public int ReactorCrossLockBonusDamage => Mathf.Max(reactorCrossLockBonusDamage, 0);
    public int ReactorCrossLockAttackDelay => Mathf.Max(reactorCrossLockAttackDelay, 0);
    public BlockDefinition ReactorBlockerDefinition => reactorBlockerDefinition;
    public int ReactorBlockerCount => Mathf.Max(reactorBlockerCount, 1);
    public BlockDefinition TrapTerrainDefinition => trapTerrainDefinition;
    public BlockDefinition TrapAmplifierDefinition => trapAmplifierDefinition;
    public int TrapInitialCount => Mathf.Max(trapInitialCount, 1);
    public int TrapSpawnCountPerTurn => Mathf.Max(trapSpawnCountPerTurn, 1);
    public int TrapMaximumCount => Mathf.Max(trapMaximumCount, 1);
    public int TrapTerrainCount => Mathf.Max(trapTerrainCount, 1);
    public float TrapHealthBallRatio => Mathf.Clamp(trapHealthBallRatio, 0.05f, 1f);
    public float TrapAmplifierHealthBallRatio =>
        Mathf.Clamp(trapAmplifierHealthBallRatio, 0.05f, 2f);
    public float TrapBallSealRatio => Mathf.Clamp(trapBallSealRatio, 0.05f, 0.4f);
    public int TrapBossHealthIncrease => Mathf.Max(trapBossHealthIncrease, 1);
    public int TrapDisarmBossDamage => Mathf.Max(trapDisarmBossDamage, 1);
    public int TrapAmplifierBaseShield => Mathf.Max(trapAmplifierBaseShield, 0);
    public BlockDefinition ArenaNormalBlockDefinition => arenaNormalBlockDefinition;
    public int ArenaNormalBlockCount => Mathf.Max(arenaNormalBlockCount, 0);
    public int ArenaNormalBlockHealth => Mathf.Max(arenaNormalBlockHealth, 1);
    public int ArenaNormalRegenerationIntervalTurns =>
        Mathf.Max(arenaNormalRegenerationIntervalTurns, 1);
    public int ArenaNormalRegenerationCount =>
        Mathf.Max(arenaNormalRegenerationCount, 1);
    public BlockDefinition TeleportCircuitPortalDefinition =>
        teleportCircuitPortalDefinition;
    public int TeleportCircuitPortalCount =>
        Mathf.Max(teleportCircuitPortalCount, 3);
    public int TeleportCircuitBonusThreshold =>
        Mathf.Max(teleportCircuitBonusThreshold, 1);
    public float TeleportCircuitBossDamageMultiplier =>
        Mathf.Max(teleportCircuitBossDamageMultiplier, 1f);
    public bool RandomizePatternObstacles => randomizePatternObstacles;
    public int RandomBreakableObstacleCount =>
        Mathf.Max(randomBreakableObstacleCount, 0);
    public int RandomIndestructibleObstacleCount =>
        Mathf.Max(randomIndestructibleObstacleCount, 0);
    public int RandomObstacleMaximumNeighborCount =>
        Mathf.Clamp(randomObstacleMaximumNeighborCount, 0, 3);

    public int TrapDefinitionCount =>
        trapDefinitions != null ? trapDefinitions.Length : 0;

    public BlockDefinition GetTrapDefinition(int index)
    {
        return trapDefinitions != null &&
               index >= 0 &&
               index < trapDefinitions.Length
            ? trapDefinitions[index]
            : null;
    }

    public int CalculateColonyGrowthBlockHealth(
        int bossHealth)
    {
        float minimumRatio = Mathf.Clamp01(
            Mathf.Min(
                colonyGrowthHealthRatioRange.x,
                colonyGrowthHealthRatioRange.y
            )
        );

        float maximumRatio = Mathf.Clamp01(
            Mathf.Max(
                colonyGrowthHealthRatioRange.x,
                colonyGrowthHealthRatioRange.y
            )
        );

        return Mathf.Max(
            Mathf.RoundToInt(
                Mathf.Max(bossHealth, 1) *
                UnityEngine.Random.Range(
                    minimumRatio,
                    maximumRatio
                )
            ),
            1
        );
    }

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
        colonyMaximumSpawnRowRatio = Mathf.Clamp01(colonyMaximumSpawnRowRatio);
        colonyGrowthHealthRatioRange.x = Mathf.Clamp01(colonyGrowthHealthRatioRange.x);
        colonyGrowthHealthRatioRange.y = Mathf.Clamp01(colonyGrowthHealthRatioRange.y);
        colonyMinimumCoreDistance = Mathf.Max(colonyMinimumCoreDistance, 0);
        colonyInitialGrowthStepDelay = Mathf.Max(colonyInitialGrowthStepDelay, 0f);
        descendingWaveCount = Mathf.Max(descendingWaveCount, 1);
        descendingMinimumRowCount = Mathf.Max(descendingMinimumRowCount, 1);
        descendingMaximumRowCount = Mathf.Max(
            descendingMaximumRowCount,
            descendingMinimumRowCount
        );
        descendingDamagePerEscapedBlock = Mathf.Max(
            descendingDamagePerEscapedBlock,
            1
        );
        reactorBombsToDestroy = Mathf.Max(reactorBombsToDestroy, 1);
        reactorBombHealth = Mathf.Max(reactorBombHealth, 1);
        reactorInitialBombCount = Mathf.Max(reactorInitialBombCount, 1);
        reactorBombsPerTurn = Mathf.Max(reactorBombsPerTurn, 1);
        reactorDescentRowCount = Mathf.Max(reactorDescentRowCount, 1);
        reactorEscapedBombDamage = Mathf.Max(reactorEscapedBombDamage, 1);
        reactorBombDamage = Mathf.Max(reactorBombDamage, 1);
        reactorExplosionBlockDamage = Mathf.Max(reactorExplosionBlockDamage, 1);
        reactorChainBossDamageBonus = Mathf.Max(reactorChainBossDamageBonus, 0);
        reactorBaseAttackDamage = Mathf.Max(reactorBaseAttackDamage, 0);
        reactorCrossLockBonusDamage = Mathf.Max(reactorCrossLockBonusDamage, 0);
        reactorCrossLockAttackDelay = Mathf.Max(reactorCrossLockAttackDelay, 0);
        reactorBlockerCount = Mathf.Max(reactorBlockerCount, 1);
        trapInitialCount = Mathf.Max(trapInitialCount, 1);
        trapSpawnCountPerTurn = Mathf.Max(trapSpawnCountPerTurn, 1);
        trapMaximumCount = Mathf.Max(trapMaximumCount, trapInitialCount);
        trapTerrainCount = Mathf.Max(trapTerrainCount, 1);
        trapHealthBallRatio = Mathf.Clamp(trapHealthBallRatio, 0.05f, 1f);
        trapAmplifierHealthBallRatio = Mathf.Clamp(trapAmplifierHealthBallRatio, 0.05f, 2f);
        trapBallSealRatio = Mathf.Clamp(trapBallSealRatio, 0.05f, 0.4f);
        trapBossHealthIncrease = Mathf.Max(trapBossHealthIncrease, 1);
        trapAmplifierBaseShield = Mathf.Max(trapAmplifierBaseShield, 0);
        arenaNormalBlockCount = Mathf.Max(arenaNormalBlockCount, 0);
        arenaNormalBlockHealth = Mathf.Max(arenaNormalBlockHealth, 1);
        arenaNormalRegenerationIntervalTurns = Mathf.Max(
            arenaNormalRegenerationIntervalTurns,
            1
        );
        arenaNormalRegenerationCount = Mathf.Max(
            arenaNormalRegenerationCount,
            1
        );
        teleportCircuitPortalCount = Mathf.Max(
            teleportCircuitPortalCount,
            3
        );
        teleportCircuitBonusThreshold = Mathf.Max(
            teleportCircuitBonusThreshold,
            1
        );
        teleportCircuitBossDamageMultiplier = Mathf.Max(
            teleportCircuitBossDamageMultiplier,
            1f
        );
        randomBreakableObstacleCount = Mathf.Max(
            randomBreakableObstacleCount,
            0
        );
        randomIndestructibleObstacleCount = Mathf.Max(
            randomIndestructibleObstacleCount,
            0
        );
        randomObstacleMaximumNeighborCount = Mathf.Clamp(
            randomObstacleMaximumNeighborCount,
            0,
            3
        );
    }
}
