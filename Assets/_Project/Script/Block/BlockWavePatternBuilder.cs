using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public sealed class BlockWavePatternBuilder
{
    [Header("Block Data")]
    [SerializeField]
    private BlockCatalog blockCatalog;

    [Header("Wave Row Settings")]
    [SerializeField, Min(1)]
    private int minimumRowsPerWave = 2;

    [SerializeField, Min(1)]
    private int maximumRowsPerWave = 3;

    [Header("Blocks Per Row")]
    [SerializeField, Min(1)]
    private int minimumBlocksPerRow = 2;

    [SerializeField, Min(1)]
    private int maximumBlocksPerRow = 3;

    [Header("Vertical Pattern")]
    [Tooltip(
        "웨이브의 모든 줄에 공통으로 등장하는 " +
        "최소 세로 기둥 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int minimumPillarColumns = 2;

    [Tooltip(
        "웨이브의 모든 줄에 공통으로 등장하는 " +
        "최대 세로 기둥 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int maximumPillarColumns = 2;

    [Tooltip(
        "이전 줄의 추가 블록 열이 " +
        "다음 줄에도 이어질 확률입니다."
    )]
    [SerializeField, Range(0f, 1f)]
    private float extraColumnCarryChance = 0.65f;

    public BlockCatalog BlockCatalog =>
        blockCatalog;

    public void Normalize(
        int availableColumns,
        int availableRows)
    {
        availableColumns =
            Mathf.Max(
                availableColumns,
                1
            );

        availableRows =
            Mathf.Max(
                availableRows,
                1
            );

        minimumRowsPerWave =
            Mathf.Clamp(
                minimumRowsPerWave,
                1,
                availableRows
            );

        maximumRowsPerWave =
            Mathf.Clamp(
                maximumRowsPerWave,
                minimumRowsPerWave,
                availableRows
            );

        minimumBlocksPerRow =
            Mathf.Clamp(
                minimumBlocksPerRow,
                1,
                availableColumns
            );

        maximumBlocksPerRow =
            Mathf.Clamp(
                maximumBlocksPerRow,
                minimumBlocksPerRow,
                availableColumns
            );

        minimumPillarColumns =
            Mathf.Clamp(
                minimumPillarColumns,
                1,
                minimumBlocksPerRow
            );

        maximumPillarColumns =
            Mathf.Clamp(
                maximumPillarColumns,
                minimumPillarColumns,
                minimumBlocksPerRow
            );

        extraColumnCarryChance =
            Mathf.Clamp01(
                extraColumnCarryChance
            );
    }

    public void Validate(
        UnityEngine.Object context)
    {
        if (blockCatalog == null)
        {
            Debug.LogWarning(
                "BlockWavePatternBuilder: " +
                "Block Catalog가 연결되지 않았습니다. " +
                "Normal 타입은 기본 1x1 블록으로 대체됩니다.",
                context
            );
        }
    }

    public int GetRandomWaveRowCount(
        int availableRows)
    {
        availableRows =
            Mathf.Max(
                availableRows,
                1
            );

        int minimumRowCount =
            Mathf.Clamp(
                minimumRowsPerWave,
                1,
                availableRows
            );

        int maximumRowCount =
            Mathf.Clamp(
                maximumRowsPerWave,
                minimumRowCount,
                availableRows
            );

        return Random.Range(
            minimumRowCount,
            maximumRowCount + 1
        );
    }

    public BlockDefinition GetRandomDefinition(
        BlockType blockType)
    {
        if (blockCatalog == null)
        {
            return null;
        }

        return blockCatalog.GetRandom(
            blockType
        );
    }

    public int GetRequiredRowCount(
        int baseRowCount,
        BlockDefinition featuredDefinition,
        int availableRows)
    {
        availableRows =
            Mathf.Max(
                availableRows,
                1
            );

        baseRowCount =
            Mathf.Clamp(
                baseRowCount,
                1,
                availableRows
            );

        if (featuredDefinition == null)
        {
            return baseRowCount;
        }

        int requiredRowCount =
            Mathf.Max(
                baseRowCount,
                featuredDefinition.GridSize.y
            );

        return Mathf.Clamp(
            requiredRowCount,
            1,
            availableRows
        );
    }

    public List<BlockSpawnRequest> BuildWaveRequests(
        int columnCount,
        int boardRowCount,
        int requestedRowCount,
        int waveIndex,
        BlockType fillBlockType,
        BlockDefinition featuredDefinition,
        int baseHealth,
        int baseAttack,
        float featuredHealthMultiplier,
        float featuredAttackMultiplier)
    {
        columnCount =
            Mathf.Max(
                columnCount,
                1
            );

        boardRowCount =
            Mathf.Max(
                boardRowCount,
                1
            );

        Normalize(
            columnCount,
            boardRowCount
        );

        int rowCount =
            Mathf.Clamp(
                requestedRowCount,
                1,
                boardRowCount
            );

        waveIndex =
            Mathf.Max(
                waveIndex,
                0
            );

        List<BlockSpawnRequest> requests =
            new List<BlockSpawnRequest>();

        BlockWaveOccupancyMap occupancyMap =
            new BlockWaveOccupancyMap(
                columnCount,
                rowCount
            );

        if (featuredDefinition != null)
        {
            BlockSpawnRequest featuredRequest =
                CreateFeaturedRequest(
                    occupancyMap,
                    waveIndex,
                    featuredDefinition,
                    baseHealth,
                    baseAttack,
                    featuredHealthMultiplier,
                    featuredAttackMultiplier
                );

            if (featuredRequest != null)
            {
                requests.Add(
                    featuredRequest
                );
            }
        }

        int pillarCount =
            GetRandomPillarCount(
                columnCount
            );

        List<int> pillarColumns =
            CreateRandomUniqueColumns(
                pillarCount,
                columnCount
            );

        List<int> previousRowColumns =
            new List<int>(
                pillarColumns
            );

        for (int row = 0;
             row < rowCount;
             row++)
        {
            int targetBlockCount =
                GetRandomBlockCountPerRow(
                    columnCount
                );

            List<int> preferredColumns =
                CreateWaveRowColumns(
                    targetBlockCount,
                    pillarColumns,
                    previousRowColumns,
                    columnCount
                );

            List<int> spawnedColumns =
                FillRow(
                    row,
                    waveIndex,
                    targetBlockCount,
                    preferredColumns,
                    fillBlockType,
                    baseHealth,
                    baseAttack,
                    occupancyMap,
                    requests
                );

            previousRowColumns =
                spawnedColumns.Count > 0
                    ? spawnedColumns
                    : preferredColumns;
        }

        return requests;
    }

    private BlockSpawnRequest CreateFeaturedRequest(
        BlockWaveOccupancyMap occupancyMap,
        int waveIndex,
        BlockDefinition definition,
        int baseHealth,
        int baseAttack,
        float healthMultiplier,
        float attackMultiplier)
    {
        Vector2Int gridSize =
            NormalizeGridSize(
                definition.GridSize
            );

        if (gridSize.x >
            occupancyMap.ColumnCount ||
            gridSize.y >
            occupancyMap.RowCount)
        {
            return null;
        }

        int maximumStartColumn =
            occupancyMap.ColumnCount -
            gridSize.x;

        int startColumn =
            Random.Range(
                0,
                maximumStartColumn + 1
            );

        int startRow = 0;

        if (!occupancyMap.TryOccupy(
                startColumn,
                startRow,
                gridSize))
        {
            return null;
        }

        int featuredHealth =
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    baseHealth *
                    Mathf.Max(
                        healthMultiplier,
                        1f
                    )
                )
            );

        int featuredAttack =
            Mathf.Max(
                0,
                Mathf.RoundToInt(
                    baseAttack *
                    Mathf.Max(
                        attackMultiplier,
                        0f
                    )
                )
            );

        return new BlockSpawnRequest(
            startColumn,
            startRow,
            waveIndex,
            definition,
            definition.BlockType,
            gridSize,
            featuredHealth,
            featuredAttack
        );
    }

    private List<int> FillRow(
        int row,
        int waveIndex,
        int targetBlockCount,
        List<int> preferredColumns,
        BlockType blockType,
        int blockHealth,
        int blockAttack,
        BlockWaveOccupancyMap occupancyMap,
        List<BlockSpawnRequest> requests)
    {
        List<int> spawnedColumns =
            new List<int>();

        HashSet<int> attemptedColumns =
            new HashSet<int>();

        for (int i = 0;
             i < preferredColumns.Count;
             i++)
        {
            if (spawnedColumns.Count >=
                targetBlockCount)
            {
                break;
            }

            int column =
                preferredColumns[i];

            attemptedColumns.Add(
                column
            );

            if (!TryCreateTypeRequest(
                    column,
                    row,
                    waveIndex,
                    blockType,
                    blockHealth,
                    blockAttack,
                    occupancyMap,
                    out BlockSpawnRequest request))
            {
                continue;
            }

            requests.Add(
                request
            );

            spawnedColumns.Add(
                column
            );
        }

        List<int> remainingColumns =
            CreateAllColumns(
                occupancyMap.ColumnCount
            );

        ShuffleColumns(
            remainingColumns
        );

        for (int i = 0;
             i < remainingColumns.Count;
             i++)
        {
            if (spawnedColumns.Count >=
                targetBlockCount)
            {
                break;
            }

            int column =
                remainingColumns[i];

            if (!attemptedColumns.Add(
                    column))
            {
                continue;
            }

            if (!TryCreateTypeRequest(
                    column,
                    row,
                    waveIndex,
                    blockType,
                    blockHealth,
                    blockAttack,
                    occupancyMap,
                    out BlockSpawnRequest request))
            {
                continue;
            }

            requests.Add(
                request
            );

            spawnedColumns.Add(
                column
            );
        }

        return spawnedColumns;
    }

    private bool TryCreateTypeRequest(
        int startColumn,
        int startRow,
        int waveIndex,
        BlockType blockType,
        int blockHealth,
        int blockAttack,
        BlockWaveOccupancyMap occupancyMap,
        out BlockSpawnRequest request)
    {
        request = null;

        BlockDefinition definition =
            GetRandomFittingDefinition(
                blockType,
                startColumn,
                startRow,
                occupancyMap
            );

        Vector2Int gridSize;

        if (definition != null)
        {
            gridSize =
                NormalizeGridSize(
                    definition.GridSize
                );
        }
        else
        {
            if (blockType !=
                BlockType.Normal)
            {
                return false;
            }

            gridSize =
                Vector2Int.one;

            if (!occupancyMap.CanOccupy(
                    startColumn,
                    startRow,
                    gridSize))
            {
                return false;
            }
        }

        if (!occupancyMap.TryOccupy(
                startColumn,
                startRow,
                gridSize))
        {
            return false;
        }

        request =
            new BlockSpawnRequest(
                startColumn,
                startRow,
                waveIndex,
                definition,
                blockType,
                gridSize,
                blockHealth,
                blockAttack
            );

        return true;
    }

    private BlockDefinition GetRandomFittingDefinition(
        BlockType blockType,
        int startColumn,
        int startRow,
        BlockWaveOccupancyMap occupancyMap)
    {
        if (blockCatalog == null)
        {
            return null;
        }

        List<BlockDefinition> definitions =
            blockCatalog.GetAll(
                blockType
            );

        if (definitions == null ||
            definitions.Count == 0)
        {
            return null;
        }

        List<BlockDefinition> fittingDefinitions =
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

            if (!occupancyMap.CanOccupy(
                    startColumn,
                    startRow,
                    definition.GridSize))
            {
                continue;
            }

            fittingDefinitions.Add(
                definition
            );

            totalWeight +=
                definition.SelectionWeight;
        }

        if (fittingDefinitions.Count == 0 ||
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
             i < fittingDefinitions.Count;
             i++)
        {
            BlockDefinition definition =
                fittingDefinitions[i];

            accumulatedWeight +=
                definition.SelectionWeight;

            if (randomWeight <
                accumulatedWeight)
            {
                return definition;
            }
        }

        return fittingDefinitions[
            fittingDefinitions.Count - 1
        ];
    }

    private int GetRandomBlockCountPerRow(
        int columnCount)
    {
        int minimumBlockCount =
            Mathf.Clamp(
                minimumBlocksPerRow,
                1,
                columnCount
            );

        int maximumBlockCount =
            Mathf.Clamp(
                maximumBlocksPerRow,
                minimumBlockCount,
                columnCount
            );

        return Random.Range(
            minimumBlockCount,
            maximumBlockCount + 1
        );
    }

    private int GetRandomPillarCount(
        int columnCount)
    {
        int maximumAllowedPillars =
            Mathf.Clamp(
                minimumBlocksPerRow,
                1,
                columnCount
            );

        int minimumPillars =
            Mathf.Clamp(
                minimumPillarColumns,
                1,
                maximumAllowedPillars
            );

        int maximumPillars =
            Mathf.Clamp(
                maximumPillarColumns,
                minimumPillars,
                maximumAllowedPillars
            );

        return Random.Range(
            minimumPillars,
            maximumPillars + 1
        );
    }

    private List<int> CreateWaveRowColumns(
        int targetBlockCount,
        List<int> pillarColumns,
        List<int> previousRowColumns,
        int columnCount)
    {
        targetBlockCount =
            Mathf.Clamp(
                targetBlockCount,
                1,
                columnCount
            );

        List<int> result =
            new List<int>();

        for (int i = 0;
             i < pillarColumns.Count;
             i++)
        {
            AddColumnIfAvailable(
                result,
                pillarColumns[i],
                targetBlockCount
            );
        }

        for (int i = 0;
             i < previousRowColumns.Count;
             i++)
        {
            if (result.Count >=
                targetBlockCount)
            {
                break;
            }

            int previousColumn =
                previousRowColumns[i];

            if (result.Contains(
                    previousColumn))
            {
                continue;
            }

            if (Random.value >
                extraColumnCarryChance)
            {
                continue;
            }

            result.Add(
                previousColumn
            );
        }

        while (result.Count <
               targetBlockCount)
        {
            int randomColumn =
                Random.Range(
                    0,
                    columnCount
                );

            if (!result.Contains(
                    randomColumn))
            {
                result.Add(
                    randomColumn
                );
            }
        }

        ShuffleColumns(
            result
        );

        return result;
    }

    private void AddColumnIfAvailable(
        List<int> columns,
        int column,
        int maximumCount)
    {
        if (columns.Count >=
            maximumCount)
        {
            return;
        }

        if (columns.Contains(
                column))
        {
            return;
        }

        columns.Add(
            column
        );
    }

    private List<int> CreateRandomUniqueColumns(
        int count,
        int columnCount)
    {
        count =
            Mathf.Clamp(
                count,
                1,
                columnCount
            );

        List<int> columns =
            CreateAllColumns(
                columnCount
            );

        ShuffleColumns(
            columns
        );

        if (columns.Count > count)
        {
            columns.RemoveRange(
                count,
                columns.Count - count
            );
        }

        return columns;
    }

    private List<int> CreateAllColumns(
        int columnCount)
    {
        List<int> columns =
            new List<int>();

        for (int column = 0;
             column < columnCount;
             column++)
        {
            columns.Add(
                column
            );
        }

        return columns;
    }

    private void ShuffleColumns(
        List<int> columns)
    {
        for (int i = columns.Count - 1;
             i > 0;
             i--)
        {
            int randomIndex =
                Random.Range(
                    0,
                    i + 1
                );

            int temporary =
                columns[i];

            columns[i] =
                columns[randomIndex];

            columns[randomIndex] =
                temporary;
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