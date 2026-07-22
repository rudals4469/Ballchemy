using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    [SerializeField, Range(1, 10)]
    private int initialRowCount = 3;

    [Header("Row Generation")]
    [SerializeField, Min(1)]
    private int minimumBlocksPerRow = 3;

    [SerializeField, Min(1)]
    private int maximumBlocksPerRow = 5;

    [Header("Block Health")]
    [SerializeField, Min(1)]
    private int startingBlockHealth = 3;

    [SerializeField, Min(0)]
    private int healthIncreasePerTurn = 1;

    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float moveDuration = 0.25f;

    private readonly List<Block> activeBlocks =
        new List<Block>();

    private int currentTurn;

    public int CurrentTurn => currentTurn;
    public int ActiveBlockCount => activeBlocks.Count;

    private void Awake()
    {
        if (blockContainer == null)
        {
            blockContainer = transform;
        }

        ValidateSettings();
    }

    private void Start()
    {
        GenerateInitialRows();
    }

    private void ValidateSettings()
    {
        columnCount = Mathf.Max(1, columnCount);

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

        initialRowCount = Mathf.Max(
            1,
            initialRowCount
        );
    }

    private void GenerateInitialRows()
    {
        if (blockPrefab == null)
        {
            Debug.LogError(
                "BlockGridManager: Block Prefab이 연결되지 않았습니다.",
                this
            );

            return;
        }

        for (int row = 0; row < initialRowCount; row++)
        {
            GenerateRow(
                row,
                startingBlockHealth
            );
        }

        Debug.Log(
            $"BlockGridManager: 초기 블록 " +
            $"{initialRowCount}줄 생성 완료",
            this
        );
    }

    public IEnumerator AdvanceTurnRoutine()
    {
        RemoveDestroyedBlocks();

        yield return MoveAllBlocksDownRoutine();

        currentTurn++;

        int newRowHealth = CalculateNewRowHealth();

        GenerateRow(
            0,
            newRowHealth
        );

        RemoveDestroyedBlocks();

        Debug.Log(
            $"BlockGridManager: 턴 {currentTurn} 진행 완료, " +
            $"현재 블록 수 = {activeBlocks.Count}",
            this
        );
    }

    private IEnumerator MoveAllBlocksDownRoutine()
    {
        RemoveDestroyedBlocks();

        if (activeBlocks.Count == 0)
        {
            yield break;
        }

        Vector3 movement =
            Vector3.down * cellSize;

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

            // 움직임이 처음과 끝에서 부드럽게 보이도록 보정
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

    private void GenerateRow(
        int rowOffset,
        int blockHealth)
    {
        if (blockPrefab == null)
        {
            return;
        }

        int spawnCount = Random.Range(
            minimumBlocksPerRow,
            maximumBlocksPerRow + 1
        );

        List<int> availableColumns =
            CreateShuffledColumnList();

        for (int i = 0; i < spawnCount; i++)
        {
            int column = availableColumns[i];

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
                $"Block_T{currentTurn}_C{column}";

            newBlock.Initialize(blockHealth);

            activeBlocks.Add(newBlock);
        }
    }

    private List<int> CreateShuffledColumnList()
    {
        List<int> columns = new List<int>();

        for (int i = 0; i < columnCount; i++)
        {
            columns.Add(i);
        }

        for (int i = columns.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(
                0,
                i + 1
            );

            int temporary = columns[i];
            columns[i] = columns[randomIndex];
            columns[randomIndex] = temporary;
        }

        return columns;
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

    private int CalculateNewRowHealth()
    {
        return Mathf.Max(
            1,
            startingBlockHealth +
            (currentTurn * healthIncreasePerTurn)
        );
    }

    private void RemoveDestroyedBlocks()
    {
        activeBlocks.RemoveAll(
            block => block == null
        );
    }
}