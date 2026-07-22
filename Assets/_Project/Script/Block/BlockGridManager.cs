using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class BlockGridManager : MonoBehaviour
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
        "모든 줄에 공통으로 유지되는 " +
        "최소 세로 기둥 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int minimumPillarColumns = 2;

    [Tooltip(
        "모든 줄에 공통으로 유지되는 " +
        "최대 세로 기둥 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int maximumPillarColumns = 2;

    [Tooltip(
        "이전 줄의 추가 블록이 " +
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

    [Header("Global Attack Cycle")]
    [SerializeField, Min(1)]
    private int attackIntervalTurns = 3;

    [SerializeField, Min(0f)]
    private float attackResolveDelay = 0.25f;

    [Header("Wave Movement")]
    [SerializeField, Min(0f)]
    private float moveDuration = 0.35f;

    private readonly List<Block> activeBlocks =
        new List<Block>();

    private int currentTurn;
    private int currentWaveIndex;
    private int turnsUntilAttack;

    public int CurrentTurn => currentTurn;

    public int CurrentWaveNumber =>
        currentWaveIndex + 1;

    public int TurnsUntilAttack =>
        turnsUntilAttack;

    public int ActiveBlockCount =>
        activeBlocks.Count;

    public int CurrentTotalAttackPower =>
        CalculateTotalAttackPower();

    public event Action<int> TurnsUntilAttackChanged;
    public event Action<int> EnemyAttackTriggered;
    public event Action<int> WaveGenerated;

    private void Awake()
    {
        if (blockContainer == null)
        {
            blockContainer = transform;
        }

        NormalizeSettings();
    }

    private void Start()
    {
        if (blockPrefab == null)
        {
            Debug.LogError(
                "BlockGridManager: " +
                "Block Prefab이 연결되지 않았습니다.",
                this
            );

            return;
        }

        currentTurn = 0;
        currentWaveIndex = 0;
        turnsUntilAttack = attackIntervalTurns;

        int initialWaveRowCount =
            GetRandomWaveRowCount();

        GenerateWave(
            initialWaveRowCount,
            CalculateWaveHealth(),
            CalculateWaveAttack()
        );

        TurnsUntilAttackChanged?.Invoke(
            turnsUntilAttack
        );

        Debug.Log(
            $"BlockGridManager: " +
            $"초기 웨이브 {initialWaveRowCount}줄 생성, " +
            $"적 공격까지 {turnsUntilAttack}턴",
            this
        );
    }

    private void OnValidate()
    {
        NormalizeSettings();
    }

    private void NormalizeSettings()
    {
        columnCount = Mathf.Max(
            1,
            columnCount
        );

        minimumRowsPerWave = Mathf.Max(
            1,
            minimumRowsPerWave
        );

        maximumRowsPerWave = Mathf.Max(
            minimumRowsPerWave,
            maximumRowsPerWave
        );

        minimumBlocksPerRow = Mathf.Clamp(
            minimumBlocksPerRow,
            1,
            columnCount
        );

        maximumBlocksPerRow = Mathf.Clamp(
            maximumBlocksPerRow,
            minimumBlocksPerRow,
            columnCount
        );

        minimumPillarColumns = Mathf.Clamp(
            minimumPillarColumns,
            1,
            minimumBlocksPerRow
        );

        maximumPillarColumns = Mathf.Clamp(
            maximumPillarColumns,
            minimumPillarColumns,
            minimumBlocksPerRow
        );

        attackIntervalTurns = Mathf.Max(
            1,
            attackIntervalTurns
        );
    }

    public IEnumerator AdvanceTurnRoutine()
    {
        RemoveDestroyedBlocks();

        currentTurn++;

        turnsUntilAttack = Mathf.Max(
            0,
            turnsUntilAttack - 1
        );

        TurnsUntilAttackChanged?.Invoke(
            turnsUntilAttack
        );

        if (turnsUntilAttack > 0)
        {
            Debug.Log(
                $"BlockGridManager: 턴 {currentTurn} 종료, " +
                $"적 공격까지 {turnsUntilAttack}턴",
                this
            );

            yield break;
        }

        yield return ResolveEnemyAttackRoutine();
        yield return SpawnNextWaveRoutine();
    }

    private IEnumerator ResolveEnemyAttackRoutine()
    {
        RemoveDestroyedBlocks();

        int totalAttackPower =
            CalculateTotalAttackPower();

        Debug.Log(
            $"BlockGridManager: 적 단체 공격, " +
            $"생존 블록 {activeBlocks.Count}개, " +
            $"총 피해량 {totalAttackPower}",
            this
        );

        EnemyAttackTriggered?.Invoke(
            totalAttackPower
        );

        // 현재는 플레이어 HP 시스템이 없기 때문에
        // 실제 피해 대신 로그와 이벤트만 발생한다.
        if (attackResolveDelay > 0f)
        {
            yield return new WaitForSeconds(
                attackResolveDelay
            );
        }
    }

    private IEnumerator SpawnNextWaveRoutine()
    {
        int newWaveRowCount =
            GetRandomWaveRowCount();

        yield return MoveAllBlocksDownRoutine(
            newWaveRowCount
        );

        currentWaveIndex++;

        int newWaveHealth =
            CalculateWaveHealth();

        int newWaveAttack =
            CalculateWaveAttack();

        GenerateWave(
            newWaveRowCount,
            newWaveHealth,
            newWaveAttack
        );

        turnsUntilAttack =
            attackIntervalTurns;

        TurnsUntilAttackChanged?.Invoke(
            turnsUntilAttack
        );

        Debug.Log(
            $"BlockGridManager: " +
            $"웨이브 {CurrentWaveNumber} 생성, " +
            $"{newWaveRowCount}줄, " +
            $"HP {newWaveHealth}, " +
            $"공격력 {newWaveAttack}, " +
            $"다음 공격까지 {turnsUntilAttack}턴",
            this
        );
    }

    private IEnumerator MoveAllBlocksDownRoutine(
        int rowCount)
    {
        RemoveDestroyedBlocks();

        if (activeBlocks.Count == 0)
        {
            yield break;
        }

        float movementDistance =
            rowCount * cellSize;

        Vector3 movement =
            Vector3.down * movementDistance;

        if (moveDuration <= 0f)
        {
            foreach (Block block in activeBlocks)
            {
                if (block == null)
                {
                    continue;
                }

                block.transform.position += movement;
            }

            yield break;
        }

        Dictionary<Block, Vector3> startPositions =
            new Dictionary<Block, Vector3>();

        foreach (Block block in activeBlocks)
        {
            if (block == null)
            {
                continue;
            }

            startPositions.Add(
                block,
                block.transform.position
            );
        }

        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / moveDuration
            );

            float smoothProgress =
                progress *
                progress *
                (3f - (2f * progress));

            foreach (
                KeyValuePair<Block, Vector3> pair
                in startPositions)
            {
                Block block = pair.Key;

                if (block == null)
                {
                    continue;
                }

                Vector3 targetPosition =
                    pair.Value + movement;

                block.transform.position =
                    Vector3.Lerp(
                        pair.Value,
                        targetPosition,
                        smoothProgress
                    );
            }

            yield return null;
        }

        foreach (
            KeyValuePair<Block, Vector3> pair
            in startPositions)
        {
            Block block = pair.Key;

            if (block == null)
            {
                continue;
            }

            block.transform.position =
                pair.Value + movement;
        }
    }

    private void GenerateWave(
        int rowCount,
        int blockHealth,
        int blockAttack)
    {
        int pillarCount = Random.Range(
            minimumPillarColumns,
            maximumPillarColumns + 1
        );

        List<int> pillarColumns =
            CreateRandomUniqueColumns(
                pillarCount
            );

        List<int> previousRowColumns =
            new List<int>(pillarColumns);

        for (int row = 0; row < rowCount; row++)
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
                SpawnBlock(
                    column,
                    row,
                    blockHealth,
                    blockAttack
                );
            }

            previousRowColumns =
                new List<int>(rowColumns);
        }

        WaveGenerated?.Invoke(
            currentWaveIndex + 1
        );
    }

    private List<int> CreateWaveRowColumns(
        int targetBlockCount,
        List<int> pillarColumns,
        List<int> previousRowColumns)
    {
        List<int> result =
            new List<int>();

        foreach (int pillarColumn in pillarColumns)
        {
            AddColumnIfAvailable(
                result,
                pillarColumn,
                targetBlockCount
            );
        }

        foreach (int previousColumn in previousRowColumns)
        {
            if (result.Count >= targetBlockCount)
            {
                break;
            }

            if (result.Contains(previousColumn))
            {
                continue;
            }

            if (Random.value >
                extraColumnCarryChance)
            {
                continue;
            }

            result.Add(previousColumn);
        }

        while (result.Count < targetBlockCount)
        {
            int randomColumn = Random.Range(
                0,
                columnCount
            );

            if (!result.Contains(randomColumn))
            {
                result.Add(randomColumn);
            }
        }

        ShuffleColumns(result);

        return result;
    }

    private void AddColumnIfAvailable(
        List<int> columns,
        int column,
        int maximumCount)
    {
        if (columns.Count >= maximumCount)
        {
            return;
        }

        if (!columns.Contains(column))
        {
            columns.Add(column);
        }
    }

    private void SpawnBlock(
        int column,
        int rowOffset,
        int blockHealth,
        int blockAttack)
    {
        Vector3 spawnPosition =
            GetCellWorldPosition(
                column,
                rowOffset
            );

        Block newBlock = Instantiate(
            blockPrefab,
            spawnPosition,
            Quaternion.identity,
            blockContainer
        );

        newBlock.name =
            $"Block_W{CurrentWaveNumber}" +
            $"_R{rowOffset}" +
            $"_C{column}";

        newBlock.Initialize(
            blockHealth,
            blockAttack
        );

        activeBlocks.Add(newBlock);
    }

    private List<int> CreateRandomUniqueColumns(
        int count)
    {
        List<int> columns =
            new List<int>();

        for (int i = 0; i < columnCount; i++)
        {
            columns.Add(i);
        }

        ShuffleColumns(columns);

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
            int randomIndex = Random.Range(
                0,
                i + 1
            );

            int temporary = columns[i];
            columns[i] = columns[randomIndex];
            columns[randomIndex] = temporary;
        }
    }

    private Vector3 GetCellWorldPosition(
        int column,
        int rowOffset)
    {
        float totalWidth =
            (columnCount - 1) * cellSize;

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

    private int GetRandomWaveRowCount()
    {
        return Random.Range(
            minimumRowsPerWave,
            maximumRowsPerWave + 1
        );
    }

    private int CalculateWaveHealth()
    {
        return Mathf.Max(
            1,
            startingBlockHealth +
            (
                currentWaveIndex *
                healthIncreasePerWave
            )
        );
    }

    private int CalculateWaveAttack()
    {
        return Mathf.Max(
            0,
            startingBlockAttack +
            (
                currentWaveIndex *
                attackIncreasePerWave
            )
        );
    }

    private int CalculateTotalAttackPower()
    {
        int totalAttackPower = 0;

        foreach (Block block in activeBlocks)
        {
            if (block == null ||
                !block.IsAlive)
            {
                continue;
            }

            totalAttackPower +=
                block.AttackPower;
        }

        return totalAttackPower;
    }

    private void RemoveDestroyedBlocks()
    {
        activeBlocks.RemoveAll(
            block =>
                block == null ||
                !block.IsAlive
        );
    }
}