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
        "각 웨이브에서 특수 블록이 " +
        "등장할 확률입니다."
    )]
    [SerializeField, Range(0f, 1f)]
    private float specialWaveChance = 0.3f;

    [Tooltip(
        "특수 블록이 등장하기로 결정됐을 때 " +
        "생성되는 최소 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int minimumSpecialBlocksPerWave = 1;

    [Tooltip(
        "특수 블록이 등장하기로 결정됐을 때 " +
        "생성되는 최대 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int maximumSpecialBlocksPerWave = 1;

    [Tooltip(
        "네임드 블록이 포함된 웨이브에도 " +
        "특수 블록을 추가할지 결정합니다."
    )]
    [SerializeField]
    private bool allowInFeaturedWaves;

    [Header("Special Block Health")]
    [Tooltip(
        "일반 블록 체력을 기준으로 특수 블록에 " +
        "적용되는 공통 체력 배율입니다."
    )]
    [SerializeField, Min(0.1f)]
    private float specialHealthMultiplier = 1f;

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

        specialHealthMultiplier =
            Mathf.Max(
                specialHealthMultiplier,
                0.1f
            );
    }

    public void Validate(
        UnityEngine.Object context)
    {
        Normalize();

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
    }

    public void InjectSpecialBlocks(
        List<BlockSpawnRequest> requests,
        BlockCatalog blockCatalog,
        int waveIndex,
        bool hasFeaturedDefinition)
    {
        Normalize();

        if (!enableSpecialBlocks ||
            requests == null ||
            requests.Count == 0 ||
            blockCatalog == null)
        {
            return;
        }

        int waveNumber =
            waveIndex + 1;

        if (waveNumber <
            firstSpecialWaveNumber)
        {
            return;
        }

        if (hasFeaturedDefinition &&
            !allowInFeaturedWaves)
        {
            return;
        }

        if (Random.value >
            specialWaveChance)
        {
            return;
        }

        List<BlockDefinition> specialDefinitions =
            blockCatalog.GetAll(
                BlockType.Special
            );

        if (specialDefinitions == null ||
            specialDefinitions.Count == 0)
        {
            Debug.LogWarning(
                "BlockWaveSpecialInjector: " +
                "BlockCatalog에 Special 타입 " +
                "Definition이 없습니다."
            );

            return;
        }

        List<int> availableRequestIndexes =
            CollectReplaceableRequestIndexes(
                requests
            );

        if (availableRequestIndexes.Count == 0)
        {
            return;
        }

        int requestedSpecialCount =
            Random.Range(
                minimumSpecialBlocksPerWave,
                maximumSpecialBlocksPerWave + 1
            );

        requestedSpecialCount =
            Mathf.Min(
                requestedSpecialCount,
                availableRequestIndexes.Count
            );

        int injectedCount = 0;

        for (int i = 0;
             i < requestedSpecialCount;
             i++)
        {
            BlockDefinition selectedDefinition =
                GetRandomFittingSpecialDefinition(
                    specialDefinitions,
                    requests,
                    availableRequestIndexes
                );

            if (selectedDefinition == null)
            {
                break;
            }

            int selectedRequestListIndex =
                GetRandomMatchingRequestListIndex(
                    selectedDefinition,
                    requests,
                    availableRequestIndexes
                );

            if (selectedRequestListIndex < 0)
            {
                break;
            }

            int requestIndex =
                availableRequestIndexes[
                    selectedRequestListIndex
                ];

            BlockSpawnRequest originalRequest =
                requests[
                    requestIndex
                ];

            int specialHealth =
                Mathf.Max(
                    1,
                    Mathf.RoundToInt(
                        originalRequest.Health *
                        specialHealthMultiplier
                    )
                );

            requests[requestIndex] =
                new BlockSpawnRequest(
                    originalRequest.StartColumn,
                    originalRequest.StartRow,
                    originalRequest.WaveIndex,
                    selectedDefinition,
                    BlockType.Special,
                    selectedDefinition.GridSize,
                    specialHealth,
                    0
                );

            availableRequestIndexes.RemoveAt(
                selectedRequestListIndex
            );

            injectedCount++;
        }

        if (injectedCount > 0)
        {
            Debug.Log(
                "BlockWaveSpecialInjector: " +
                $"웨이브 {waveNumber}에 " +
                $"특수 블록 {injectedCount}개 추가"
            );
        }
    }

    private List<int>
        CollectReplaceableRequestIndexes(
            IReadOnlyList<BlockSpawnRequest> requests)
    {
        List<int> indexes =
            new List<int>();

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

            if (request.RequestedBlockType !=
                BlockType.Normal)
            {
                continue;
            }

            indexes.Add(
                i
            );
        }

        return indexes;
    }

    private BlockDefinition
        GetRandomFittingSpecialDefinition(
            IReadOnlyList<BlockDefinition> definitions,
            IReadOnlyList<BlockSpawnRequest> requests,
            IReadOnlyList<int> availableRequestIndexes)
    {
        List<BlockDefinition> candidates =
            new List<BlockDefinition>();

        int totalWeight = 0;

        for (int i = 0;
             i < definitions.Count;
             i++)
        {
            BlockDefinition definition =
                definitions[i];

            if (definition == null ||
                definition.SelectionWeight <= 0)
            {
                continue;
            }

            if (!HasMatchingRequest(
                    definition,
                    requests,
                    availableRequestIndexes))
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

    private bool HasMatchingRequest(
        BlockDefinition definition,
        IReadOnlyList<BlockSpawnRequest> requests,
        IReadOnlyList<int> availableRequestIndexes)
    {
        Vector2Int requiredSize =
            NormalizeGridSize(
                definition.GridSize
            );

        for (int i = 0;
             i < availableRequestIndexes.Count;
             i++)
        {
            int requestIndex =
                availableRequestIndexes[i];

            if (requestIndex < 0 ||
                requestIndex >=
                requests.Count)
            {
                continue;
            }

            BlockSpawnRequest request =
                requests[
                    requestIndex
                ];

            if (request == null)
            {
                continue;
            }

            if (request.GridSize ==
                requiredSize)
            {
                return true;
            }
        }

        return false;
    }

    private int
        GetRandomMatchingRequestListIndex(
            BlockDefinition definition,
            IReadOnlyList<BlockSpawnRequest> requests,
            IReadOnlyList<int> availableRequestIndexes)
    {
        Vector2Int requiredSize =
            NormalizeGridSize(
                definition.GridSize
            );

        List<int> matchingListIndexes =
            new List<int>();

        for (int i = 0;
             i < availableRequestIndexes.Count;
             i++)
        {
            int requestIndex =
                availableRequestIndexes[i];

            if (requestIndex < 0 ||
                requestIndex >=
                requests.Count)
            {
                continue;
            }

            BlockSpawnRequest request =
                requests[
                    requestIndex
                ];

            if (request == null ||
                request.GridSize !=
                requiredSize)
            {
                continue;
            }

            matchingListIndexes.Add(
                i
            );
        }

        if (matchingListIndexes.Count == 0)
        {
            return -1;
        }

        int randomIndex =
            Random.Range(
                0,
                matchingListIndexes.Count
            );

        return matchingListIndexes[
            randomIndex
        ];
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