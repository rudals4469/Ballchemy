using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class BlockWaveGenerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Block blockPrefab;

    [SerializeField]
    private Transform blockContainer;

    [Header("Block Data")]
    [SerializeField]
    private BlockCatalog blockCatalog;

    [Tooltip(
        "기존 GenerateWave 호출에서 생성할 기본 블록 타입입니다."
    )]
    [SerializeField]
    private BlockType defaultWaveBlockType =
        BlockType.Normal;

    [Header("Grid Settings")]
    [SerializeField, Range(3, 15)]
    private int columnCount = 9;

    [SerializeField, Min(0.1f)]
    private float cellSize = 1f;

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

    [Header("Block Health")]
    [SerializeField, Min(1)]
    private int startingBlockHealth = 8;

    [SerializeField, Min(0)]
    private int healthIncreasePerWave = 2;

    [Header("Block Attack")]
    [SerializeField, Min(0)]
    private int startingBlockAttack = 2;

    [SerializeField, Min(0)]
    private int attackIncreasePerWave = 0;

    [Header("Debug")]
    [SerializeField]
    private bool showSpawnDebugLog;

    public float CellSize =>
        cellSize;

    public int ColumnCount =>
        columnCount;

    public bool IsReady =>
        blockPrefab != null;

    public BlockCatalog BlockCatalog =>
        blockCatalog;

    private void Awake()
    {
        if (blockContainer == null)
        {
            blockContainer = transform;
        }

        NormalizeSettings();
        ValidateReferences();
    }

    private void OnValidate()
    {
        NormalizeSettings();
    }

    private void NormalizeSettings()
    {
        columnCount =
            Mathf.Max(
                columnCount,
                1
            );

        cellSize =
            Mathf.Max(
                cellSize,
                0.1f
            );

        minimumRowsPerWave =
            Mathf.Max(
                minimumRowsPerWave,
                1
            );

        maximumRowsPerWave =
            Mathf.Max(
                maximumRowsPerWave,
                minimumRowsPerWave
            );

        minimumBlocksPerRow =
            Mathf.Clamp(
                minimumBlocksPerRow,
                1,
                columnCount
            );

        maximumBlocksPerRow =
            Mathf.Clamp(
                maximumBlocksPerRow,
                minimumBlocksPerRow,
                columnCount
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

        startingBlockHealth =
            Mathf.Max(
                startingBlockHealth,
                1
            );

        healthIncreasePerWave =
            Mathf.Max(
                healthIncreasePerWave,
                0
            );

        startingBlockAttack =
            Mathf.Max(
                startingBlockAttack,
                0
            );

        attackIncreasePerWave =
            Mathf.Max(
                attackIncreasePerWave,
                0
            );
    }

    private void ValidateReferences()
    {
        if (blockPrefab == null)
        {
            Debug.LogError(
                "BlockWaveGenerator: " +
                "Block Prefab이 연결되지 않았습니다.",
                this
            );
        }

        if (blockCatalog == null)
        {
            Debug.LogWarning(
                "BlockWaveGenerator: " +
                "Block Catalog가 연결되지 않았습니다.",
                this
            );
        }
    }

    public int GetRandomWaveRowCount()
    {
        return Random.Range(
            minimumRowsPerWave,
            maximumRowsPerWave + 1
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
        BlockDefinition featuredDefinition)
    {
        baseRowCount =
            Mathf.Max(
                baseRowCount,
                1
            );

        if (featuredDefinition == null)
        {
            return baseRowCount;
        }

        return Mathf.Max(
            baseRowCount,
            featuredDefinition.GridSize.y
        );
    }

    public List<Block> GenerateWave(
        int rowCount,
        int waveIndex)
    {
        return GenerateWave(
            rowCount,
            waveIndex,
            defaultWaveBlockType
        );
    }

    public List<Block> GenerateWave(
        int rowCount,
        int waveIndex,
        BlockType blockType)
    {
        return GenerateWaveInternal(
            rowCount,
            waveIndex,
            blockType,
            null,
            1f,
            1f
        );
    }

    /// <summary>
    /// 네임드나 보스 하나를 먼저 배치하고,
    /// 남은 공간에 일반 블록을 채웁니다.
    /// </summary>
    public List<Block> GenerateFeaturedWave(
        int rowCount,
        int waveIndex,
        BlockDefinition featuredDefinition,
        float featuredHealthMultiplier,
        float featuredAttackMultiplier)
    {
        if (featuredDefinition == null)
        {
            Debug.LogWarning(
                "BlockWaveGenerator: " +
                "Featured Definition이 없어 " +
                "일반 웨이브로 생성합니다.",
                this
            );

            return GenerateWave(
                rowCount,
                waveIndex,
                BlockType.Normal
            );
        }

        rowCount =
            GetRequiredRowCount(
                rowCount,
                featuredDefinition
            );

        return GenerateWaveInternal(
            rowCount,
            waveIndex,
            BlockType.Normal,
            featuredDefinition,
            featuredHealthMultiplier,
            featuredAttackMultiplier
        );
    }

    private List<Block> GenerateWaveInternal(
        int rowCount,
        int waveIndex,
        BlockType fillBlockType,
        BlockDefinition featuredDefinition,
        float featuredHealthMultiplier,
        float featuredAttackMultiplier)
    {
        List<Block> generatedBlocks =
            new List<Block>();

        if (blockPrefab == null)
        {
            Debug.LogError(
                "BlockWaveGenerator: " +
                "Block Prefab이 연결되지 않았습니다.",
                this
            );

            return generatedBlocks;
        }

        rowCount =
            Mathf.Max(
                rowCount,
                1
            );

        waveIndex =
            Mathf.Max(
                waveIndex,
                0
            );

        bool[,] occupiedCells =
            new bool[
                columnCount,
                rowCount
            ];

        int baseHealth =
            CalculateWaveHealth(
                waveIndex
            );

        int baseAttack =
            CalculateWaveAttack(
                waveIndex
            );

        if (featuredDefinition != null)
        {
            Block featuredBlock =
                SpawnFeaturedBlock(
                    featuredDefinition,
                    rowCount,
                    waveIndex,
                    baseHealth,
                    baseAttack,
                    featuredHealthMultiplier,
                    featuredAttackMultiplier,
                    occupiedCells
                );

            if (featuredBlock != null)
            {
                generatedBlocks.Add(
                    featuredBlock
                );
            }
        }

        int pillarCount =
            Random.Range(
                minimumPillarColumns,
                maximumPillarColumns + 1
            );

        List<int> pillarColumns =
            CreateRandomUniqueColumns(
                pillarCount
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
                Random.Range(
                    minimumBlocksPerRow,
                    maximumBlocksPerRow + 1
                );

            List<int> preferredColumns =
                CreateWaveRowColumns(
                    targetBlockCount,
                    pillarColumns,
                    previousRowColumns
                );

            List<int> spawnedColumns =
                FillRow(
                    row,
                    rowCount,
                    waveIndex,
                    targetBlockCount,
                    preferredColumns,
                    fillBlockType,
                    baseHealth,
                    baseAttack,
                    occupiedCells,
                    generatedBlocks
                );

            previousRowColumns =
                spawnedColumns.Count > 0
                    ? spawnedColumns
                    : preferredColumns;
        }

        string featuredTypeText =
            featuredDefinition != null
                ? featuredDefinition.BlockType.ToString()
                : "None";

        Debug.Log(
            "BlockWaveGenerator: " +
            $"웨이브 {waveIndex + 1} 생성 완료, " +
            $"주요 블록 {featuredTypeText}, " +
            $"{rowCount}줄, " +
            $"블록 {generatedBlocks.Count}개, " +
            $"기본 HP {baseHealth}, " +
            $"기본 공격력 {baseAttack}",
            this
        );

        return generatedBlocks;
    }

    private Block SpawnFeaturedBlock(
        BlockDefinition definition,
        int rowCount,
        int waveIndex,
        int baseHealth,
        int baseAttack,
        float healthMultiplier,
        float attackMultiplier,
        bool[,] occupiedCells)
    {
        Vector2Int gridSize =
            definition.GridSize;

        if (gridSize.x > columnCount ||
            gridSize.y > rowCount)
        {
            Debug.LogWarning(
                "BlockWaveGenerator: " +
                $"{definition.name}의 크기 " +
                $"{gridSize.x}x{gridSize.y}가 " +
                "현재 생성 영역보다 큽니다.",
                this
            );

            return null;
        }

        int maximumStartColumn =
            columnCount -
            gridSize.x;

        int startColumn =
            Random.Range(
                0,
                maximumStartColumn + 1
            );

        int startRow = 0;

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

        Block featuredBlock =
            SpawnBlock(
                startColumn,
                startRow,
                waveIndex,
                definition,
                definition.BlockType,
                gridSize,
                featuredHealth,
                featuredAttack
            );

        if (featuredBlock == null)
        {
            return null;
        }

        OccupyCells(
            startColumn,
            startRow,
            gridSize,
            occupiedCells
        );

        return featuredBlock;
    }

    private List<int> FillRow(
        int row,
        int rowCount,
        int waveIndex,
        int targetBlockCount,
        List<int> preferredColumns,
        BlockType blockType,
        int blockHealth,
        int blockAttack,
        bool[,] occupiedCells,
        List<Block> generatedBlocks)
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

            if (!TrySpawnTypeBlock(
                    column,
                    row,
                    rowCount,
                    waveIndex,
                    blockType,
                    blockHealth,
                    blockAttack,
                    occupiedCells,
                    out Block spawnedBlock))
            {
                continue;
            }

            generatedBlocks.Add(
                spawnedBlock
            );

            spawnedColumns.Add(
                column
            );
        }

        List<int> remainingColumns =
            CreateAllColumns();

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

            if (!TrySpawnTypeBlock(
                    column,
                    row,
                    rowCount,
                    waveIndex,
                    blockType,
                    blockHealth,
                    blockAttack,
                    occupiedCells,
                    out Block spawnedBlock))
            {
                continue;
            }

            generatedBlocks.Add(
                spawnedBlock
            );

            spawnedColumns.Add(
                column
            );
        }

        return spawnedColumns;
    }

    private bool TrySpawnTypeBlock(
        int startColumn,
        int startRow,
        int rowCount,
        int waveIndex,
        BlockType blockType,
        int blockHealth,
        int blockAttack,
        bool[,] occupiedCells,
        out Block spawnedBlock)
    {
        spawnedBlock = null;

        BlockDefinition definition =
            GetRandomFittingDefinition(
                blockType,
                startColumn,
                startRow,
                rowCount,
                occupiedCells
            );

        Vector2Int gridSize;

        if (definition != null)
        {
            gridSize =
                definition.GridSize;
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

            if (!CanOccupyCells(
                    startColumn,
                    startRow,
                    gridSize,
                    rowCount,
                    occupiedCells))
            {
                return false;
            }
        }

        spawnedBlock =
            SpawnBlock(
                startColumn,
                startRow,
                waveIndex,
                definition,
                blockType,
                gridSize,
                blockHealth,
                blockAttack
            );

        if (spawnedBlock == null)
        {
            return false;
        }

        OccupyCells(
            startColumn,
            startRow,
            gridSize,
            occupiedCells
        );

        return true;
    }

    private BlockDefinition GetRandomFittingDefinition(
        BlockType blockType,
        int startColumn,
        int startRow,
        int rowCount,
        bool[,] occupiedCells)
    {
        if (blockCatalog == null)
        {
            return null;
        }

        List<BlockDefinition> definitions =
            blockCatalog.GetAll(
                blockType
            );

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

            if (!CanOccupyCells(
                    startColumn,
                    startRow,
                    definition.GridSize,
                    rowCount,
                    occupiedCells))
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

    private bool CanOccupyCells(
        int startColumn,
        int startRow,
        Vector2Int gridSize,
        int rowCount,
        bool[,] occupiedCells)
    {
        if (startColumn < 0 ||
            startRow < 0)
        {
            return false;
        }

        int endColumn =
            startColumn +
            gridSize.x;

        int endRow =
            startRow +
            gridSize.y;

        if (endColumn > columnCount ||
            endRow > rowCount)
        {
            return false;
        }

        for (int row = startRow;
             row < endRow;
             row++)
        {
            for (int column = startColumn;
                 column < endColumn;
                 column++)
            {
                if (occupiedCells[
                        column,
                        row])
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void OccupyCells(
        int startColumn,
        int startRow,
        Vector2Int gridSize,
        bool[,] occupiedCells)
    {
        int endColumn =
            startColumn +
            gridSize.x;

        int endRow =
            startRow +
            gridSize.y;

        for (int row = startRow;
             row < endRow;
             row++)
        {
            for (int column = startColumn;
                 column < endColumn;
                 column++)
            {
                occupiedCells[
                    column,
                    row
                ] = true;
            }
        }
    }

    private Block SpawnBlock(
        int startColumn,
        int startRow,
        int waveIndex,
        BlockDefinition definition,
        BlockType requestedBlockType,
        Vector2Int gridSize,
        int blockHealth,
        int blockAttack)
    {
        Vector3 spawnPosition =
            GetBlockCenterWorldPosition(
                startColumn,
                startRow,
                gridSize
            );

        Block newBlock =
            Instantiate(
                blockPrefab,
                spawnPosition,
                Quaternion.identity,
                blockContainer
            );

        if (newBlock == null)
        {
            return null;
        }

        string definitionId =
            definition != null
                ? definition.BlockId
                : "prefab_default";

        newBlock.name =
            $"Block_{requestedBlockType}" +
            $"_{definitionId}" +
            $"_{gridSize.x}x{gridSize.y}" +
            $"_W{waveIndex + 1}" +
            $"_R{startRow}" +
            $"_C{startColumn}";

        if (definition != null)
        {
            newBlock.Initialize(
                definition,
                blockHealth,
                blockAttack,
                cellSize
            );
        }
        else
        {
            newBlock.Initialize(
                blockHealth,
                blockAttack
            );
        }

        if (showSpawnDebugLog)
        {
            Debug.Log(
                "BlockWaveGenerator: 블록 생성, " +
                $"타입={requestedBlockType}, " +
                $"데이터={definitionId}, " +
                $"크기={gridSize.x}x{gridSize.y}, " +
                $"시작 칸=({startColumn}, {startRow}), " +
                $"HP={blockHealth}, " +
                $"공격력={blockAttack}",
                newBlock
            );
        }

        return newBlock;
    }

    private Vector3 GetBlockCenterWorldPosition(
        int startColumn,
        int startRow,
        Vector2Int gridSize)
    {
        Vector3 startCellPosition =
            GetCellWorldPosition(
                startColumn,
                startRow
            );

        float horizontalOffset =
            (gridSize.x - 1) *
            cellSize *
            0.5f;

        float verticalOffset =
            (gridSize.y - 1) *
            cellSize *
            0.5f;

        return new Vector3(
            startCellPosition.x +
            horizontalOffset,

            startCellPosition.y -
            verticalOffset,

            startCellPosition.z
        );
    }

    private List<int> CreateWaveRowColumns(
        int targetBlockCount,
        List<int> pillarColumns,
        List<int> previousRowColumns)
    {
        List<int> result =
            new List<int>();

        foreach (int pillarColumn
                 in pillarColumns)
        {
            AddColumnIfAvailable(
                result,
                pillarColumn,
                targetBlockCount
            );
        }

        foreach (int previousColumn
                 in previousRowColumns)
        {
            if (result.Count >=
                targetBlockCount)
            {
                break;
            }

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
        int count)
    {
        count =
            Mathf.Clamp(
                count,
                1,
                columnCount
            );

        List<int> columns =
            CreateAllColumns();

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

    private List<int> CreateAllColumns()
    {
        List<int> columns =
            new List<int>();

        for (int i = 0;
             i < columnCount;
             i++)
        {
            columns.Add(i);
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

    private Vector3 GetCellWorldPosition(
        int column,
        int rowOffset)
    {
        float totalWidth =
            (columnCount - 1) *
            cellSize;

        float leftPosition =
            transform.position.x -
            (totalWidth * 0.5f);

        float xPosition =
            leftPosition +
            (column * cellSize);

        float yPosition =
            transform.position.y -
            (rowOffset * cellSize);

        return new Vector3(
            xPosition,
            yPosition,
            transform.position.z
        );
    }

    private int CalculateWaveHealth(
        int waveIndex)
    {
        return Mathf.Max(
            1,
            startingBlockHealth +
            (
                waveIndex *
                healthIncreasePerWave
            )
        );
    }

    private int CalculateWaveAttack(
        int waveIndex)
    {
        return Mathf.Max(
            0,
            startingBlockAttack +
            (
                waveIndex *
                attackIncreasePerWave
            )
        );
    }
}