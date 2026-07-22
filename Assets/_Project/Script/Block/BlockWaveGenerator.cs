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

    public float CellSize =>
        cellSize;

    public bool IsReady =>
        blockPrefab != null;

    private void Awake()
    {
        if (blockContainer == null)
        {
            blockContainer = transform;
        }

        NormalizeSettings();
    }

    private void OnValidate()
    {
        NormalizeSettings();
    }

    private void NormalizeSettings()
    {
        columnCount =
            Mathf.Max(
                1,
                columnCount
            );

        cellSize =
            Mathf.Max(
                0.1f,
                cellSize
            );

        minimumRowsPerWave =
            Mathf.Max(
                1,
                minimumRowsPerWave
            );

        maximumRowsPerWave =
            Mathf.Max(
                minimumRowsPerWave,
                maximumRowsPerWave
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
                1,
                startingBlockHealth
            );

        healthIncreasePerWave =
            Mathf.Max(
                0,
                healthIncreasePerWave
            );

        startingBlockAttack =
            Mathf.Max(
                0,
                startingBlockAttack
            );

        attackIncreasePerWave =
            Mathf.Max(
                0,
                attackIncreasePerWave
            );
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
                1,
                rowCount
            );

        waveIndex =
            Mathf.Max(
                0,
                waveIndex
            );

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

            foreach (int column in rowColumns)
            {
                Block newBlock =
                    SpawnBlock(
                        column,
                        row,
                        waveIndex,
                        blockHealth,
                        blockAttack
                    );

                if (newBlock != null)
                {
                    generatedBlocks.Add(
                        newBlock
                    );
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

    private Block SpawnBlock(
        int column,
        int rowOffset,
        int waveIndex,
        int blockHealth,
        int blockAttack)
    {
        Vector3 spawnPosition =
            GetCellWorldPosition(
                column,
                rowOffset
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

        newBlock.name =
            $"Block_W{waveIndex + 1}" +
            $"_R{rowOffset}" +
            $"_C{column}";

        newBlock.Initialize(
            blockHealth,
            blockAttack
        );

        return newBlock;
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
            columns.Add(i);
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