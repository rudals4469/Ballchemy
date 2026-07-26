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
                "Reward Special Definitions가 비어 있습니다.",
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

        /*
         * 먼저 보상 슬롯을 채운다.
         */
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

        /*
         * 나머지 슬롯은 랜덤 특수 블록으로 채운다.
         */
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
            $"일반 블록 교체 없음"
        );
    }

    private void InjectTestBlocks(
        List<BlockSpawnRequest> requests,
        BlockWaveOccupancyMap occupancyMap,
        int waveIndex,
        int baseHealth,
        int waveNumber)
    {
        if (forcedSpecialDefinition == null ||
            forcedSpecialDefinition.BlockType !=
            BlockType.Special)
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
            if (forcedSpecialDefinition == null)
            {
                return false;
            }

            if (forcedSpecialDefinition.BlockType !=
                BlockType.Special)
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
        if (!TryGetRandomFittingPosition(
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
                    specialHealthMultiplier
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
                        gridSize))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryGetRandomFittingPosition(
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

                fittingPositions.Add(
                    new Vector2Int(
                        column,
                        row
                    )
                );
            }
        }

        if (fittingPositions.Count == 0)
        {
            return false;
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
               definition.SelectionWeight > 0;
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