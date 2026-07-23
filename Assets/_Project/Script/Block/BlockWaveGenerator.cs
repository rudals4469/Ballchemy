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
                "Block Catalog가 연결되지 않았습니다. " +
                "노멀 블록은 프리팹 기본 외형으로 생성되지만, " +
                "네임드 블록은 생성할 수 없습니다.",
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

        int blockHealth =
            CalculateWaveHealth(
                waveIndex
            );

        int blockAttack =
            CalculateWaveAttack(
                waveIndex
            );

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

            List<int> rowColumns =
                CreateWaveRowColumns(
                    targetBlockCount,
                    pillarColumns,
                    previousRowColumns
                );

            int spawnedInCurrentRow = 0;

            for (int i = 0;
                 i < rowColumns.Count;
                 i++)
            {
                int column =
                    rowColumns[i];

                BlockDefinition definition =
                    GetRandomFittingDefinition(
                        blockType,
                        column,
                        row,
                        rowCount,
                        occupiedCells
                    );

                Vector2Int gridSize =
                    definition != null
                        ? definition.GridSize
                        : Vector2Int.one;

                if (definition == null)
                {
                    if (blockType !=
                        BlockType.Normal)
                    {
                        continue;
                    }

                    if (!CanOccupyCells(
                            column,
                            row,
                            gridSize,
                            rowCount,
                            occupiedCells))
                    {
                        continue;
                    }
                }

                Block newBlock =
                    SpawnBlock(
                        column,
                        row,
                        waveIndex,
                        definition,
                        blockType,
                        gridSize,
                        blockHealth,
                        blockAttack
                    );

                if (newBlock == null)
                {
                    continue;
                }

                OccupyCells(
                    column,
                    row,
                    gridSize,
                    occupiedCells
                );

                generatedBlocks.Add(
                    newBlock
                );

                spawnedInCurrentRow++;

                if (spawnedInCurrentRow >=
                    targetBlockCount)
                {
                    break;
                }
            }

            previousRowColumns =
                new List<int>(
                    rowColumns
                );
        }

        Debug.Log(
            "BlockWaveGenerator: " +
            $"웨이브 {waveIndex + 1} 생성 완료, " +
            $"타입 {blockType}, " +
            $"{rowCount}줄, " +
            $"블록 {generatedBlocks.Count}개, " +
            $"HP {blockHealth}, " +
            $"공격력 {blockAttack}",
            this
        );

        return generatedBlocks;
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

            if (result.Contains(
                    randomColumn))
            {
                continue;
            }

            result.Add(
                randomColumn
            );
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

        List<BlockDefinition> typeDefinitions =
            blockCatalog.GetAll(
                blockType
            );

        List<BlockDefinition> fittingDefinitions =
            new List<BlockDefinition>();

        int totalWeight = 0;

        for (int i = 0;
             i < typeDefinitions.Count;
             i++)
        {
            BlockDefinition definition =
                typeDefinitions[i];

            if (definition == null)
            {
                continue;
            }

            if (definition.SelectionWeight <= 0)
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

        if (fittingDefinitions.Count <= 0 ||
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

        if (endColumn >
            columnCount)
        {
            return false;
        }

        if (endRow >
            rowCount)
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
            (
                gridSize.x - 1
            ) *
            cellSize *
            0.5f;

        float verticalOffset =
            (
                gridSize.y - 1
            ) *
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
            new List<int>();

        for (int i = 0;
             i < columnCount;
             i++)
        {
            columns.Add(
                i
            );
        }

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