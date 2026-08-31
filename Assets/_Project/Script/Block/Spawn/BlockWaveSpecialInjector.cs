using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public sealed class BlockWaveSpecialInjector
{
    [Header("Special Block Spawn")]

    [SerializeField]
    private bool enableSpecialBlocks = true;

    [Tooltip(
        "특수 블록이 처음 등장할 수 있는 " +
        "웨이브 번호입니다. 1부터 시작합니다."
    )]
    [SerializeField, Min(1)]
    private int firstSpecialWaveNumber = 1;

    [Tooltip(
        "각 웨이브에서 특수 블록 묶음이 " +
        "등장할 확률입니다."
    )]
    [SerializeField, Range(0f, 1f)]
    private float specialWaveChance = 1f;

    [Tooltip("Special 슬롯 1개가 증가하는 기본 배치 블록 수입니다.")]
    [SerializeField, Min(1)]
    private int blocksPerSpecialSlot = 10;

    [Tooltip(
        "한 웨이브에 추가되는 " +
        "최소 특수 블록 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int minimumSpecialBlocksPerWave = 2;

    [Tooltip(
        "한 웨이브에 추가되는 " +
        "최대 특수 블록 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int maximumSpecialBlocksPerWave = 3;

    [Tooltip(
        "네임드 블록이 포함된 웨이브에도 " +
        "특수 블록을 추가할지 결정합니다."
    )]
    [SerializeField]
    private bool allowInFeaturedWaves;

    [Header("Special Placement")]

    [Tooltip(
        "활성화하면 특수 블록을 가능한 한 " +
        "기존 블록과 붙어 있는 위치에 배치합니다.\n" +
        "주변에 블록이 많은 후보를 우선합니다."
    )]
    [SerializeField]
    private bool preferAdjacentPlacement = true;

    [Tooltip("특수 블럭이 포켓 내부 후보로 인정되기 위한 최소 인접 벽 수입니다.")]
    [SerializeField, Range(1, 4)]
    private int minimumPocketAdjacency = 2;

    [Header("Guaranteed Reward")]

    [Tooltip(
        "한 웨이브에서 반드시 먼저 생성할 " +
        "보상 특수 블록 개수입니다."
    )]
    [SerializeField, Min(0)]
    private int guaranteedRewardCount = 1;

    [Tooltip(
        "보장 슬롯에서 선택될 보상형 " +
        "Special Definition 목록입니다."
    )]
    [SerializeField]
    private List<BlockDefinition>
        rewardSpecialDefinitions =
            new List<BlockDefinition>();

    [Header("Random Special Pool")]

    [Tooltip(
        "보장 슬롯 이후 랜덤으로 선택될 " +
        "Special Definition 목록입니다. " +
        "비어 있으면 BlockCatalog의 모든 " +
        "Special Definition을 사용합니다."
    )]
    [SerializeField]
    private List<BlockDefinition>
        randomSpecialDefinitions =
            new List<BlockDefinition>();

    [Tooltip(
        "활성화하면 한 웨이브 안에서 같은 " +
        "Special Definition이 중복되지 않습니다."
    )]
    [SerializeField]
    private bool preventDuplicateDefinitionsInSameWave =
        true;

    [Header("Special Block Health")]

    [Tooltip(
        "일반 블록 체력을 기준으로 특수 블록에 " +
        "적용되는 공통 체력 배율입니다."
    )]
    [SerializeField, Min(0.1f)]
    private float specialHealthMultiplier = 1f;

    [Header("Guardian Special")]
    [SerializeField] private bool enableGuardianBlocks = true;
    [SerializeField] private BlockDefinition guardianDefinition;
    [SerializeField, Range(0f, 1f)] private float guardianSpawnChance = 0.35f;
    [SerializeField, Min(1)] private int maximumGuardiansInNormalWave = 1;
    [SerializeField, Min(1)] private int maximumGuardiansInFeaturedWave = 2;
    [SerializeField, Min(1)] private int minimumGuardianTargets = 1;
    [SerializeField, Min(1)] private int maximumGuardianTargets = 2;
    [SerializeField, Min(0.1f)] private float guardianHealthMultiplier = 1.2f;
    [SerializeField] private bool allowGuardianWithOtherSpecials = true;

    public int MinimumGuardianTargets => minimumGuardianTargets;
    public int MaximumGuardianTargets => maximumGuardianTargets;

    [Header("Special Block Test Mode")]

    [Tooltip(
        "활성화하면 일반 랜덤 선택을 무시하고 " +
        "지정한 특수 블록만 생성합니다."
    )]
    [SerializeField]
    private bool enableTestMode;

    [Tooltip(
        "테스트 중 강제로 생성할 " +
        "Special 타입 BlockDefinition입니다."
    )]
    [SerializeField]
    private BlockDefinition forcedSpecialDefinition;

    [Tooltip(
        "활성화하면 등장 확률과 첫 등장 웨이브를 무시하고 " +
        "일반 웨이브마다 테스트 블록을 생성합니다."
    )]
    [SerializeField]
    private bool forceSpawnEveryWaveInTestMode = true;

    [Tooltip(
        "테스트 모드에서 한 웨이브에 추가할 " +
        "특수 블록 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int forcedSpecialCount = 1;

    public void Normalize()
    {
        firstSpecialWaveNumber =
            Mathf.Max(
                firstSpecialWaveNumber,
                1
            );

        specialWaveChance =
            Mathf.Clamp01(
                specialWaveChance
            );

        blocksPerSpecialSlot = Mathf.Max(blocksPerSpecialSlot, 1);

        minimumSpecialBlocksPerWave =
            Mathf.Max(
                minimumSpecialBlocksPerWave,
                1
            );

        maximumSpecialBlocksPerWave =
            Mathf.Max(
                maximumSpecialBlocksPerWave,
                minimumSpecialBlocksPerWave
            );

        guaranteedRewardCount =
            Mathf.Clamp(
                guaranteedRewardCount,
                0,
                maximumSpecialBlocksPerWave
            );

        specialHealthMultiplier =
            Mathf.Max(
                specialHealthMultiplier,
                0.1f
            );

        minimumPocketAdjacency = Mathf.Clamp(minimumPocketAdjacency, 1, 4);

        guardianSpawnChance = Mathf.Clamp01(guardianSpawnChance);
        maximumGuardiansInNormalWave = Mathf.Max(maximumGuardiansInNormalWave, 1);
        maximumGuardiansInFeaturedWave = Mathf.Max(maximumGuardiansInFeaturedWave, 1);
        minimumGuardianTargets = Mathf.Max(minimumGuardianTargets, 1);
        maximumGuardianTargets = Mathf.Max(maximumGuardianTargets, minimumGuardianTargets);
        guardianHealthMultiplier = Mathf.Max(guardianHealthMultiplier, 0.1f);

        forcedSpecialCount =
            Mathf.Max(
                forcedSpecialCount,
                1
            );

        if (rewardSpecialDefinitions == null)
        {
            rewardSpecialDefinitions =
                new List<BlockDefinition>();
        }

        if (randomSpecialDefinitions == null)
        {
            randomSpecialDefinitions =
                new List<BlockDefinition>();
        }
    }

    public void Validate(
        UnityEngine.Object context)
    {
        Normalize();

        if (enableTestMode)
        {
            if (forcedSpecialDefinition == null)
            {
                Debug.LogWarning(
                    "BlockWaveSpecialInjector: " +
                    "Test Mode가 활성화됐지만 " +
                    "Forced Special Definition이 비어 있습니다.",
                    context
                );

                return;
            }

            if (forcedSpecialDefinition.BlockType !=
                BlockType.Special)
            {
                Debug.LogWarning(
                    "BlockWaveSpecialInjector: " +
                    "Forced Special Definition의 " +
                    "Block Type이 Special이 아닙니다.",
                    forcedSpecialDefinition
                );
            }

            if (IsLegacyAddBallDefinition(
                    forcedSpecialDefinition))
            {
                Debug.LogWarning(
                    "BlockWaveSpecialInjector: " +
                    "Forced Special Definition에 기존 공 추가 " +
                    "블록이 지정되어 있어 생성하지 않습니다.",
                    forcedSpecialDefinition
                );
            }

            return;
        }

        if (!enableSpecialBlocks)
        {
            return;
        }

        if (specialWaveChance <= 0f)
        {
            Debug.LogWarning(
                "BlockWaveSpecialInjector: " +
                "Special Wave Chance가 0이라 " +
                "특수 블록이 등장하지 않습니다.",
                context
            );
        }

        if (guaranteedRewardCount > 0 &&
            CountValidSpecialDefinitions(
                rewardSpecialDefinitions) == 0)
        {
            Debug.LogWarning(
                "BlockWaveSpecialInjector: " +
                "Guaranteed Reward Count가 1 이상이지만 " +
                "생성 가능한 Reward Special Definition이 없습니다.",
                context
            );
        }

        ValidateDefinitionList(
            rewardSpecialDefinitions,
            "Reward Special Definitions",
            context
        );

        ValidateDefinitionList(
            randomSpecialDefinitions,
            "Random Special Definitions",
            context
        );
    }

    public void InjectSpecialBlocks(
        List<BlockSpawnRequest> requests,
        BlockCatalog blockCatalog,
        int columnCount,
        int rowCount,
        int waveIndex,
        bool hasFeaturedDefinition,
        int baseHealth)
    {
        Normalize();

        if (requests == null)
        {
            return;
        }

        int guardianCount = InjectGuardianBlocks(
            requests,
            columnCount,
            rowCount,
            waveIndex,
            hasFeaturedDefinition,
            baseHealth
        );

        if (guardianCount > 0 && !allowGuardianWithOtherSpecials)
        {
            return;
        }

        if (!enableTestMode &&
            !enableSpecialBlocks)
        {
            return;
        }

        columnCount =
            Mathf.Max(
                columnCount,
                1
            );

        rowCount =
            Mathf.Max(
                rowCount,
                1
            );

        int waveNumber =
            waveIndex + 1;

        if (!ShouldInjectSpecialBlocks(
                waveNumber,
                hasFeaturedDefinition))
        {
            return;
        }

        BlockWaveOccupancyMap occupancyMap =
            CreateOccupancyMap(
                requests,
                columnCount,
                rowCount
            );

        if (enableTestMode)
        {
            InjectTestBlocks(
                requests,
                occupancyMap,
                waveIndex,
                baseHealth,
                waveNumber
            );

            return;
        }

        int requestedSpecialCount =
            Random.Range(
                minimumSpecialBlocksPerWave,
                maximumSpecialBlocksPerWave + 1
            );

        HashSet<BlockDefinition>
            usedDefinitions =
                new HashSet<BlockDefinition>();

        int injectedCount = 0;
        int rewardInjectedCount = 0;

        int requestedRewardCount =
            Mathf.Min(
                guaranteedRewardCount,
                requestedSpecialCount
            );

        for (int i = 0;
             i < requestedRewardCount;
             i++)
        {
            BlockDefinition rewardDefinition =
                SelectRandomFittingDefinition(
                    rewardSpecialDefinitions,
                    occupancyMap,
                    usedDefinitions
                );

            if (rewardDefinition == null)
            {
                break;
            }

            if (!TryAppendSpecialRequest(
                    requests,
                    occupancyMap,
                    rewardDefinition,
                    waveIndex,
                    baseHealth))
            {
                break;
            }

            usedDefinitions.Add(
                rewardDefinition
            );

            injectedCount++;
            rewardInjectedCount++;
        }

        List<BlockDefinition> randomPool =
            GetRandomSpecialPool(
                blockCatalog
            );

        while (injectedCount <
               requestedSpecialCount)
        {
            BlockDefinition randomDefinition =
                SelectRandomFittingDefinition(
                    randomPool,
                    occupancyMap,
                    usedDefinitions
                );

            if (randomDefinition == null)
            {
                break;
            }

            if (!TryAppendSpecialRequest(
                    requests,
                    occupancyMap,
                    randomDefinition,
                    waveIndex,
                    baseHealth))
            {
                break;
            }

            usedDefinitions.Add(
                randomDefinition
            );

            injectedCount++;
        }

        if (rewardInjectedCount <
            requestedRewardCount)
        {
            Debug.LogWarning(
                "BlockWaveSpecialInjector: " +
                $"웨이브 {waveNumber}에서 보상 블록을 " +
                $"{requestedRewardCount}개 보장하려 했지만 " +
                $"{rewardInjectedCount}개만 생성했습니다. " +
                "Reward 목록과 남은 빈칸을 확인하세요."
            );
        }

        Debug.Log(
            "BlockWaveSpecialInjector: " +
            $"웨이브 {waveNumber}에 " +
            $"특수 블록 {injectedCount}개 추가, " +
            $"보상 블록 {rewardInjectedCount}개, " +
            $"인접 배치 우선={preferAdjacentPlacement}, " +
            $"기존 공 추가 블록 제외, " +
            $"일반 블록 교체 없음"
        );
    }

    public void InjectScaledSpecialBlocks(
        List<BlockSpawnRequest> requests,
        BlockCatalog blockCatalog,
        int columnCount,
        int boardRowCount,
        int waveIndex,
        bool isNamedRoom,
        int baseHealth,
        TeleportPairSpawnSettings teleportSettings)
    {
        Normalize();

        if (requests == null || requests.Count == 0)
        {
            return;
        }

        int baseBlockCount = 0;
        for (int i = 0; i < requests.Count; i++)
        {
            if (requests[i] != null &&
                requests[i].RequestedBlockType != BlockType.Special)
            {
                baseBlockCount++;
            }
        }

        if (baseBlockCount <= 0)
        {
            return;
        }

        int waveNumber = waveIndex + 1;
        if (!ShouldInjectSpecialBlocks(waveNumber, isNamedRoom))
        {
            return;
        }

        int activeRowCount = ResolveActiveSpecialRowCount(requests, boardRowCount);
        BlockWaveOccupancyMap occupancyMap =
            CreateOccupancyMap(requests, columnCount, activeRowCount);

        if (enableTestMode)
        {
            InjectTestBlocks(
                requests,
                occupancyMap,
                waveIndex,
                baseHealth,
                waveNumber);
            return;
        }

        int requestedSlotCount = Mathf.CeilToInt(
            baseBlockCount / (float)blocksPerSpecialSlot);
        bool canSelectGuardian =
            enableGuardianBlocks &&
            guardianDefinition != null &&
            HasProtectableEnemy(requests);
        // 텔레포트는 맵 에셋에 지정된 쌍 슬롯에서만 생성합니다.
        bool canSelectTeleport = false;

        List<BlockDefinition> pool = blockCatalog != null
            ? blockCatalog.GetAll(BlockType.Special)
            : GetRandomSpecialPool(blockCatalog);
        HashSet<BlockDefinition> usedDefinitions = new HashSet<BlockDefinition>();
        int injectedSlotCount = 0;

        while (injectedSlotCount < requestedSlotCount)
        {
            BlockDefinition selected = SelectUnifiedDefinition(
                pool,
                occupancyMap,
                usedDefinitions,
                canSelectGuardian,
                canSelectTeleport,
                teleportSettings);

            if (selected == null)
            {
                break;
            }

            usedDefinitions.Add(selected);
            bool injected;

            if (selected.SpecialCategory == SpecialBlockCategory.Teleport)
            {
                injected = TeleportPairInjector.TryInjectSelectedPair(
                    requests,
                    teleportSettings,
                    columnCount,
                    activeRowCount,
                    waveIndex);
            }
            else if (selected.SpecialCategory == SpecialBlockCategory.Guardian)
            {
                injected = TryAppendGuardianRequest(
                    requests,
                    occupancyMap,
                    waveIndex,
                    baseHealth);
            }
            else
            {
                injected = TryAppendSpecialRequest(
                    requests,
                    occupancyMap,
                    selected,
                    waveIndex,
                    baseHealth);
            }

            if (injected)
            {
                injectedSlotCount++;

                if (selected.SpecialCategory == SpecialBlockCategory.Teleport)
                {
                    occupancyMap = CreateOccupancyMap(
                        requests,
                        columnCount,
                        activeRowCount);
                }
            }
        }

        Debug.Log(
            "BlockWaveSpecialInjector: " +
            $"base blocks={baseBlockCount}, " +
            $"requested special slots={requestedSlotCount}, " +
            $"injected unique slots={injectedSlotCount}");
    }

    public void InjectScaledSpecialBlocksAtSlots(
        List<BlockSpawnRequest> requests,
        BlockCatalog blockCatalog,
        IReadOnlyList<Vector2Int> authoredSlots,
        int columnCount,
        int boardRowCount,
        int waveIndex,
        bool isNamedRoom,
        int baseHealth)
    {
        Normalize();
        if (requests == null || authoredSlots == null ||
            authoredSlots.Count == 0)
            return;

        int baseBlockCount = 0;
        for (int i = 0; i < requests.Count; i++)
            if (requests[i] != null &&
                requests[i].RequestedBlockType != BlockType.Special)
                baseBlockCount++;

        int waveNumber = waveIndex + 1;
        if (baseBlockCount <= 0 ||
            !ShouldInjectSpecialBlocks(waveNumber, isNamedRoom))
            return;

        int requestedCount = Mathf.Min(
            Mathf.CeilToInt(baseBlockCount / (float)blocksPerSpecialSlot),
            authoredSlots.Count);
        BlockWaveOccupancyMap occupancy = CreateOccupancyMap(
            requests, columnCount, Mathf.Max(boardRowCount, 1));
        List<Vector2Int> slots = new List<Vector2Int>(authoredSlots);
        for (int i = slots.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (slots[i], slots[swapIndex]) = (slots[swapIndex], slots[i]);
        }

        List<BlockDefinition> pool = blockCatalog != null
            ? blockCatalog.GetAll(BlockType.Special)
            : GetRandomSpecialPool(blockCatalog);
        HashSet<BlockDefinition> used = new HashSet<BlockDefinition>();
        int injected = 0;
        for (int slotIndex = 0;
             slotIndex < slots.Count && injected < requestedCount;
             slotIndex++)
        {
            BlockDefinition selected = SelectSpecialForAuthoredSlot(
                pool, used, occupancy, slots[slotIndex], requests);
            if (selected == null) continue;
            if (!TryAppendSpecialRequestAtPosition(
                    requests, occupancy, selected, slots[slotIndex],
                    waveIndex, baseHealth))
                continue;
            used.Add(selected);
            injected++;
        }

        Debug.Log(
            $"BlockWaveSpecialInjector: 에셋 S 슬롯 {authoredSlots.Count}개 중 " +
            $"특수 블록 {injected}개 생성");
    }

    private BlockDefinition SelectSpecialForAuthoredSlot(
        IReadOnlyList<BlockDefinition> definitions,
        HashSet<BlockDefinition> used,
        BlockWaveOccupancyMap occupancy,
        Vector2Int position,
        IReadOnlyList<BlockSpawnRequest> requests)
    {
        List<BlockDefinition> candidates = new List<BlockDefinition>();
        int totalWeight = 0;
        if (definitions == null) return null;
        bool hasProtectableEnemy = HasProtectableEnemy(requests);
        for (int i = 0; i < definitions.Count; i++)
        {
            BlockDefinition definition = definitions[i];
            if (!IsUnifiedSpecialDefinition(definition) ||
                used.Contains(definition) ||
                definition.SpecialCategory == SpecialBlockCategory.Teleport ||
                (definition.SpecialCategory == SpecialBlockCategory.Guardian &&
                 (!enableGuardianBlocks || definition != guardianDefinition ||
                  !hasProtectableEnemy)))
                continue;
            Vector2Int size = NormalizeGridSize(definition.GridSize);
            if (!occupancy.CanOccupy(position.x, position.y, size))
                continue;
            candidates.Add(definition);
            totalWeight += definition.SelectionWeight;
        }
        if (candidates.Count == 0 || totalWeight <= 0) return null;
        int roll = Random.Range(0, totalWeight);
        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= candidates[i].SelectionWeight;
            if (roll < 0) return candidates[i];
        }
        return candidates[candidates.Count - 1];
    }

    private bool TryAppendSpecialRequestAtPosition(
        List<BlockSpawnRequest> requests,
        BlockWaveOccupancyMap occupancy,
        BlockDefinition definition,
        Vector2Int position,
        int waveIndex,
        int baseHealth)
    {
        Vector2Int size = NormalizeGridSize(definition.GridSize);
        if (!occupancy.TryOccupy(position.x, position.y, size)) return false;
        int health = Mathf.Max(1, Mathf.RoundToInt(
            Mathf.Max(baseHealth, 1) * specialHealthMultiplier *
            (definition.SpecialCategory == SpecialBlockCategory.Guardian
                ? guardianHealthMultiplier : 1f)));
        requests.Add(new BlockSpawnRequest(
            position.x, position.y, waveIndex, definition,
            BlockType.Special, size, health, 0));
        return true;
    }

    private BlockDefinition SelectUnifiedDefinition(
        IReadOnlyList<BlockDefinition> definitions,
        BlockWaveOccupancyMap occupancyMap,
        HashSet<BlockDefinition> usedDefinitions,
        bool canSelectGuardian,
        bool canSelectTeleport,
        TeleportPairSpawnSettings teleportSettings)
    {
        if (definitions == null)
        {
            return null;
        }

        List<BlockDefinition> candidates = new List<BlockDefinition>();
        int totalWeight = 0;

        for (int i = 0; i < definitions.Count; i++)
        {
            BlockDefinition definition = definitions[i];
            if (!IsUnifiedSpecialDefinition(definition) ||
                usedDefinitions.Contains(definition))
            {
                continue;
            }

            if (definition.SpecialCategory == SpecialBlockCategory.Guardian &&
                (!canSelectGuardian || definition != guardianDefinition))
            {
                continue;
            }

            if (definition.SpecialCategory == SpecialBlockCategory.Teleport)
            {
                if (!canSelectTeleport ||
                    teleportSettings == null ||
                    definition != teleportSettings.Definition)
                {
                    continue;
                }
            }
            else if (!HasFittingPosition(definition, occupancyMap))
            {
                continue;
            }

            candidates.Add(definition);
            totalWeight += definition.SelectionWeight;
        }

        if (candidates.Count == 0 || totalWeight <= 0)
        {
            return null;
        }

        int roll = Random.Range(0, totalWeight);
        int accumulated = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            accumulated += candidates[i].SelectionWeight;
            if (roll < accumulated)
            {
                return candidates[i];
            }
        }

        return candidates[candidates.Count - 1];
    }

    private bool IsUnifiedSpecialDefinition(BlockDefinition definition)
    {
        return definition != null &&
               definition.BlockType == BlockType.Special &&
               definition.SelectionWeight > 0 &&
               !IsLegacyAddBallDefinition(definition);
    }

    private bool HasProtectableEnemy(IReadOnlyList<BlockSpawnRequest> requests)
    {
        for (int i = 0; i < requests.Count; i++)
        {
            BlockSpawnRequest request = requests[i];
            if (request != null &&
                request.RequestedBlockType != BlockType.Special &&
                request.Definition != null &&
                request.Definition.DestructionRule == BlockDestructionRule.Breakable)
            {
                return true;
            }
        }

        return false;
    }

    private int ResolveActiveSpecialRowCount(
        IReadOnlyList<BlockSpawnRequest> requests,
        int boardRowCount)
    {
        int activeRows = 1;
        for (int i = 0; i < requests.Count; i++)
        {
            BlockSpawnRequest request = requests[i];
            if (request != null)
            {
                activeRows = Mathf.Max(
                    activeRows,
                    request.StartRow + Mathf.Max(request.GridSize.y, 1));
            }
        }

        return Mathf.Clamp(activeRows, 1, Mathf.Max(boardRowCount, 1));
    }

    private int InjectGuardianBlocks(
        List<BlockSpawnRequest> requests,
        int columnCount,
        int rowCount,
        int waveIndex,
        bool hasFeaturedDefinition,
        int baseHealth)
    {
        if (!enableGuardianBlocks || enableTestMode ||
            guardianDefinition == null ||
            guardianDefinition.BlockType != BlockType.Special ||
            guardianDefinition.SpecialCategory != SpecialBlockCategory.Guardian ||
            Random.value > guardianSpawnChance)
        {
            return 0;
        }

        bool hasProtectableEnemy = false;
        for (int i = 0; i < requests.Count; i++)
        {
            BlockSpawnRequest request = requests[i];
            if (request != null &&
                request.RequestedBlockType != BlockType.Special &&
                request.Definition != null &&
                request.Definition.DestructionRule == BlockDestructionRule.Breakable)
            {
                hasProtectableEnemy = true;
                break;
            }
        }

        if (!hasProtectableEnemy)
        {
            return 0;
        }

        BlockWaveOccupancyMap occupancyMap = CreateOccupancyMap(requests, columnCount, rowCount);
        int maximumCount = hasFeaturedDefinition
            ? maximumGuardiansInFeaturedWave
            : maximumGuardiansInNormalWave;
        int requestedCount = Random.Range(1, maximumCount + 1);
        int injectedCount = 0;

        for (int i = 0; i < requestedCount; i++)
        {
            if (!TryAppendGuardianRequest(requests, occupancyMap, waveIndex, baseHealth))
            {
                break;
            }

            injectedCount++;
        }

        return injectedCount;
    }

    private bool TryAppendGuardianRequest(
        List<BlockSpawnRequest> requests,
        BlockWaveOccupancyMap occupancyMap,
        int waveIndex,
        int baseHealth)
    {
        if (!TryGetPreferredFittingPosition(guardianDefinition, occupancyMap, out Vector2Int startPosition))
        {
            return false;
        }

        Vector2Int gridSize = NormalizeGridSize(guardianDefinition.GridSize);
        if (!occupancyMap.TryOccupy(startPosition.x, startPosition.y, gridSize))
        {
            return false;
        }

        int health = Mathf.Max(1, Mathf.RoundToInt(
            Mathf.Max(baseHealth, 1) * specialHealthMultiplier * guardianHealthMultiplier));

        requests.Add(new BlockSpawnRequest(
            startPosition.x,
            startPosition.y,
            waveIndex,
            guardianDefinition,
            BlockType.Special,
            gridSize,
            health,
            0));

        return true;
    }

    private void InjectTestBlocks(
        List<BlockSpawnRequest> requests,
        BlockWaveOccupancyMap occupancyMap,
        int waveIndex,
        int baseHealth,
        int waveNumber)
    {
        if (!IsValidSpecialDefinition(
                forcedSpecialDefinition))
        {
            return;
        }

        int injectedCount = 0;

        for (int i = 0;
             i < forcedSpecialCount;
             i++)
        {
            if (!TryAppendSpecialRequest(
                    requests,
                    occupancyMap,
                    forcedSpecialDefinition,
                    waveIndex,
                    baseHealth))
            {
                break;
            }

            injectedCount++;
        }

        Debug.Log(
            "BlockWaveSpecialInjector: " +
            $"테스트 모드 - 웨이브 {waveNumber}에 " +
            $"{forcedSpecialDefinition.name} " +
            $"{injectedCount}개 추가"
        );
    }

    private bool ShouldInjectSpecialBlocks(
        int waveNumber,
        bool hasFeaturedDefinition)
    {
        if (enableTestMode)
        {
            if (!IsValidSpecialDefinition(
                    forcedSpecialDefinition))
            {
                return false;
            }

            if (forceSpawnEveryWaveInTestMode)
            {
                return true;
            }

            return Random.value <=
                   specialWaveChance;
        }

        if (waveNumber <
            firstSpecialWaveNumber)
        {
            return false;
        }

        if (hasFeaturedDefinition &&
            !allowInFeaturedWaves)
        {
            return false;
        }

        return Random.value <=
               specialWaveChance;
    }

    private BlockWaveOccupancyMap CreateOccupancyMap(
        IReadOnlyList<BlockSpawnRequest> requests,
        int columnCount,
        int rowCount)
    {
        BlockWaveOccupancyMap occupancyMap =
            new BlockWaveOccupancyMap(
                columnCount,
                rowCount
            );

        for (int i = 0;
             i < requests.Count;
             i++)
        {
            BlockSpawnRequest request =
                requests[i];

            if (request == null)
            {
                continue;
            }

            occupancyMap.TryOccupy(
                request.StartColumn,
                request.StartRow,
                NormalizeGridSize(
                    request.GridSize
                )
            );
        }

        return occupancyMap;
    }

    private List<BlockDefinition>
        GetRandomSpecialPool(
            BlockCatalog blockCatalog)
    {
        List<BlockDefinition> configuredPool =
            FilterValidSpecialDefinitions(
                randomSpecialDefinitions
            );

        if (configuredPool.Count > 0)
        {
            return configuredPool;
        }

        if (blockCatalog == null)
        {
            Debug.LogWarning(
                "BlockWaveSpecialInjector: " +
                "Random Special Definitions가 비어 있고 " +
                "BlockCatalog도 연결되지 않았습니다."
            );

            return new List<BlockDefinition>();
        }

        List<BlockDefinition> catalogDefinitions =
            blockCatalog.GetAll(
                BlockType.Special
            );

        return FilterValidSpecialDefinitions(
            catalogDefinitions
        );
    }

    private BlockDefinition
        SelectRandomFittingDefinition(
        IReadOnlyList<BlockDefinition> definitions,
        BlockWaveOccupancyMap occupancyMap,
        HashSet<BlockDefinition> usedDefinitions)
    {
        if (definitions == null ||
            definitions.Count == 0)
        {
            return null;
        }

        List<BlockDefinition> candidates =
            new List<BlockDefinition>();

        int totalWeight = 0;

        for (int i = 0;
             i < definitions.Count;
             i++)
        {
            BlockDefinition definition =
                definitions[i];

            if (!IsValidSpecialDefinition(
                    definition))
            {
                continue;
            }

            if (definition.SpecialCategory == SpecialBlockCategory.Guardian)
            {
                continue;
            }

            if (definition.SpecialCategory == SpecialBlockCategory.Teleport)
            {
                continue;
            }

            if (preventDuplicateDefinitionsInSameWave &&
                usedDefinitions.Contains(
                    definition))
            {
                continue;
            }

            if (!HasFittingPosition(
                    definition,
                    occupancyMap))
            {
                continue;
            }

            candidates.Add(
                definition
            );

            totalWeight +=
                definition.SelectionWeight;
        }

        if (candidates.Count == 0 ||
            totalWeight <= 0)
        {
            return null;
        }

        int randomWeight =
            Random.Range(
                0,
                totalWeight
            );

        int accumulatedWeight = 0;

        for (int i = 0;
             i < candidates.Count;
             i++)
        {
            BlockDefinition candidate =
                candidates[i];

            accumulatedWeight +=
                candidate.SelectionWeight;

            if (randomWeight <
                accumulatedWeight)
            {
                return candidate;
            }
        }

        return candidates[
            candidates.Count - 1
        ];
    }

    private bool TryAppendSpecialRequest(
        List<BlockSpawnRequest> requests,
        BlockWaveOccupancyMap occupancyMap,
        BlockDefinition definition,
        int waveIndex,
        int baseHealth)
    {
        if (!IsValidSpecialDefinition(
                definition))
        {
            return false;
        }

        if (!TryGetPreferredFittingPosition(
                definition,
                occupancyMap,
                out Vector2Int startPosition))
        {
            return false;
        }

        Vector2Int gridSize =
            NormalizeGridSize(
                definition.GridSize
            );

        if (!occupancyMap.TryOccupy(
                startPosition.x,
                startPosition.y,
                gridSize))
        {
            return false;
        }

        int specialHealth =
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    Mathf.Max(
                        baseHealth,
                        1
                    ) *
                    specialHealthMultiplier *
                    (definition.SpecialCategory == SpecialBlockCategory.Guardian
                        ? guardianHealthMultiplier
                        : 1f)
                )
            );

        requests.Add(
            new BlockSpawnRequest(
                startPosition.x,
                startPosition.y,
                waveIndex,
                definition,
                BlockType.Special,
                gridSize,
                specialHealth,
                0
            )
        );

        return true;
    }

    private bool HasFittingPosition(
        BlockDefinition definition,
        BlockWaveOccupancyMap occupancyMap)
    {
        if (!IsValidSpecialDefinition(
                definition))
        {
            return false;
        }

        Vector2Int gridSize =
            NormalizeGridSize(
                definition.GridSize
            );

        int maximumStartColumn =
            occupancyMap.ColumnCount -
            gridSize.x;

        int maximumStartRow =
            occupancyMap.RowCount -
            gridSize.y;

        if (maximumStartColumn < 0 ||
            maximumStartRow < 0)
        {
            return false;
        }

        for (int row = 0;
             row <= maximumStartRow;
             row++)
        {
            for (int column = 0;
                 column <= maximumStartColumn;
                 column++)
            {
                if (occupancyMap.CanOccupy(
                        column,
                        row,
                        gridSize) &&
                    !WouldCreateFilledTwoByTwo(
                        column,
                        row,
                        gridSize,
                        occupancyMap) &&
                    CalculateAdjacencyScore(
                        column,
                        row,
                        gridSize,
                        occupancyMap) >= minimumPocketAdjacency)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryGetPreferredFittingPosition(
        BlockDefinition definition,
        BlockWaveOccupancyMap occupancyMap,
        out Vector2Int startPosition)
    {
        startPosition =
            new Vector2Int(
                -1,
                -1
            );

        if (!IsValidSpecialDefinition(
                definition))
        {
            return false;
        }

        Vector2Int gridSize =
            NormalizeGridSize(
                definition.GridSize
            );

        int maximumStartColumn =
            occupancyMap.ColumnCount -
            gridSize.x;

        int maximumStartRow =
            occupancyMap.RowCount -
            gridSize.y;

        if (maximumStartColumn < 0 ||
            maximumStartRow < 0)
        {
            return false;
        }

        List<Vector2Int> fittingPositions =
            new List<Vector2Int>();

        List<Vector2Int> bestPositions =
            new List<Vector2Int>();

        int bestAdjacencyScore =
            -1;

        for (int row = 0;
             row <= maximumStartRow;
             row++)
        {
            for (int column = 0;
                 column <= maximumStartColumn;
                 column++)
            {
                if (!occupancyMap.CanOccupy(
                        column,
                        row,
                        gridSize))
                {
                    continue;
                }

                if (WouldCreateFilledTwoByTwo(
                        column,
                        row,
                        gridSize,
                        occupancyMap))
                {
                    continue;
                }

                Vector2Int candidatePosition =
                    new Vector2Int(
                        column,
                        row
                    );

                int adjacencyScore =
                    CalculateAdjacencyScore(
                        column,
                        row,
                        gridSize,
                        occupancyMap
                    );

                if (adjacencyScore < minimumPocketAdjacency)
                {
                    continue;
                }

                fittingPositions.Add(
                    candidatePosition
                );

                if (!preferAdjacentPlacement)
                {
                    continue;
                }

                if (adjacencyScore >
                    bestAdjacencyScore)
                {
                    bestAdjacencyScore =
                        adjacencyScore;

                    bestPositions.Clear();

                    bestPositions.Add(
                        candidatePosition
                    );

                    continue;
                }

                if (adjacencyScore ==
                    bestAdjacencyScore)
                {
                    bestPositions.Add(
                        candidatePosition
                    );
                }
            }
        }

        if (fittingPositions.Count == 0)
        {
            return false;
        }

        /*
         * 인접 배치가 활성화되어 있고
         * 최소 하나 이상의 기존 블록과 붙을 수 있다면
         * 가장 높은 인접 점수를 가진 후보 중 하나를 고른다.
         *
         * 모든 빈칸이 완전히 고립되어 있다면
         * 기존 방식처럼 전체 후보에서 랜덤 선택한다.
         */
        if (preferAdjacentPlacement &&
            bestAdjacencyScore > 0 &&
            bestPositions.Count > 0)
        {
            startPosition =
                bestPositions[
                    Random.Range(
                        0,
                        bestPositions.Count
                    )
                ];

            return true;
        }

        startPosition =
            fittingPositions[
                Random.Range(
                    0,
                    fittingPositions.Count
                )
            ];

        return true;
    }

    private static bool WouldCreateFilledTwoByTwo(
        int startColumn,
        int startRow,
        Vector2Int gridSize,
        BlockWaveOccupancyMap occupancyMap)
    {
        int minimumWindowColumn = Mathf.Max(startColumn - 1, 0);
        int maximumWindowColumn = Mathf.Min(
            startColumn + gridSize.x - 1,
            occupancyMap.ColumnCount - 2);
        int minimumWindowRow = Mathf.Max(startRow - 1, 0);
        int maximumWindowRow = Mathf.Min(
            startRow + gridSize.y - 1,
            occupancyMap.RowCount - 2);

        for (int row = minimumWindowRow; row <= maximumWindowRow; row++)
        for (int column = minimumWindowColumn;
             column <= maximumWindowColumn;
             column++)
        {
            bool allOccupied = true;
            bool allBelongToProposedBlock = true;
            for (int offsetY = 0; offsetY < 2; offsetY++)
            for (int offsetX = 0; offsetX < 2; offsetX++)
            {
                int cellColumn = column + offsetX;
                int cellRow = row + offsetY;
                bool belongsToProposedBlock =
                    cellColumn >= startColumn &&
                    cellColumn < startColumn + gridSize.x &&
                    cellRow >= startRow &&
                    cellRow < startRow + gridSize.y;
                bool occupied = belongsToProposedBlock ||
                    occupancyMap.IsOccupied(cellColumn, cellRow);
                allOccupied &= occupied;
                allBelongToProposedBlock &= belongsToProposedBlock;
            }

            // 하나의 2x2 대형 블럭 자체는 네 개의 개별 블럭 군집으로
            // 취급하지 않고, 기존 블럭과 합쳐져 생기는 2x2만 제한한다.
            if (allOccupied && !allBelongToProposedBlock) return true;
        }

        return false;
    }

    private int CalculateAdjacencyScore(
        int startColumn,
        int startRow,
        Vector2Int gridSize,
        BlockWaveOccupancyMap occupancyMap)
    {
        int score = 0;

        int endColumnExclusive =
            startColumn +
            gridSize.x;

        int endRowExclusive =
            startRow +
            gridSize.y;

        /*
         * 위 / 아래 경계.
         *
         * 후보 블록 자체가 2x2 이상일 수도 있으므로
         * 블록의 외곽 전체를 검사한다.
         */
        int topRow =
            startRow - 1;

        int bottomRow =
            endRowExclusive;

        for (int column = startColumn;
             column < endColumnExclusive;
             column++)
        {
            if (occupancyMap.IsOccupied(
                    column,
                    topRow))
            {
                score++;
            }

            if (occupancyMap.IsOccupied(
                    column,
                    bottomRow))
            {
                score++;
            }
        }

        /*
         * 왼쪽 / 오른쪽 경계.
         */
        int leftColumn =
            startColumn - 1;

        int rightColumn =
            endColumnExclusive;

        for (int row = startRow;
             row < endRowExclusive;
             row++)
        {
            if (occupancyMap.IsOccupied(
                    leftColumn,
                    row))
            {
                score++;
            }

            if (occupancyMap.IsOccupied(
                    rightColumn,
                    row))
            {
                score++;
            }
        }

        return score;
    }

    private List<BlockDefinition>
        FilterValidSpecialDefinitions(
            IReadOnlyList<BlockDefinition> definitions)
    {
        List<BlockDefinition> result =
            new List<BlockDefinition>();

        if (definitions == null)
        {
            return result;
        }

        for (int i = 0;
             i < definitions.Count;
             i++)
        {
            BlockDefinition definition =
                definitions[i];

            if (!IsValidSpecialDefinition(
                    definition))
            {
                continue;
            }

            if (!result.Contains(
                    definition))
            {
                result.Add(
                    definition
                );
            }
        }

        return result;
    }

    private int CountValidSpecialDefinitions(
        IReadOnlyList<BlockDefinition> definitions)
    {
        return FilterValidSpecialDefinitions(
            definitions
        ).Count;
    }

    private bool IsValidSpecialDefinition(
        BlockDefinition definition)
    {
        return definition != null &&
               definition.BlockType ==
               BlockType.Special &&
               definition.SpecialCategory !=
               SpecialBlockCategory.Teleport &&
               definition.SelectionWeight > 0 &&
               !IsLegacyAddBallDefinition(
                   definition);
    }

    private bool IsLegacyAddBallDefinition(
        BlockDefinition definition)
    {
        return definition != null &&
               definition.SpecialCategory ==
               SpecialBlockCategory.LegacyAddBall;
    }

    private void ValidateDefinitionList(
        IReadOnlyList<BlockDefinition> definitions,
        string listName,
        UnityEngine.Object context)
    {
        if (definitions == null)
        {
            return;
        }

        for (int i = 0;
             i < definitions.Count;
             i++)
        {
            BlockDefinition definition =
                definitions[i];

            if (definition == null)
            {
                continue;
            }

            if (definition.BlockType !=
                BlockType.Special)
            {
                Debug.LogWarning(
                    "BlockWaveSpecialInjector: " +
                    $"{listName}의 {definition.name}은 " +
                    "Special 타입이 아닙니다.",
                    context
                );
            }

            if (definition.SelectionWeight <= 0)
            {
                Debug.LogWarning(
                    "BlockWaveSpecialInjector: " +
                    $"{listName}의 {definition.name}은 " +
                    "Selection Weight가 0 이하입니다.",
                    context
                );
            }

            if (IsLegacyAddBallDefinition(
                    definition))
            {
                Debug.LogWarning(
                    "BlockWaveSpecialInjector: " +
                    $"{listName}의 {definition.name}은 " +
                    "기존 공 추가 블록이므로 생성 대상에서 제외됩니다.",
                    context
                );
            }
        }
    }

    private Vector2Int NormalizeGridSize(
        Vector2Int gridSize)
    {
        return new Vector2Int(
            Mathf.Max(
                gridSize.x,
                1
            ),
            Mathf.Max(
                gridSize.y,
                1
            )
        );
    }
}
