using System.Collections.Generic;
using UnityEngine;

public sealed class BlockWaveGenerator : MonoBehaviour
{
    [Header("Board")]
    [SerializeField]
    private BoardGrid boardGrid;

    [Header("Spawn")]
    [SerializeField]
    private BlockSpawner blockSpawner =
        new BlockSpawner();

    [Header("Pattern")]
    [SerializeField]
    private BlockWavePatternBuilder patternBuilder =
        new BlockWavePatternBuilder();

    [Header("Default Wave")]
    [SerializeField]
    private BlockType defaultWaveBlockType =
        BlockType.Normal;

    [Header("Block Health")]
    [SerializeField, Min(1)]
    private int startingBlockHealth = 8;

    [SerializeField, Min(0)]
    private int healthIncreasePerWave = 2;

    [Header("Block Attack")]
    [SerializeField, Min(0)]
    private int startingBlockAttack = 2;

    [SerializeField, Min(0)]
    private int attackIncreasePerWave;

    public BoardGrid BoardGrid =>
        boardGrid;

    public float CellSize =>
        boardGrid != null
            ? boardGrid.CellSize
            : 1f;

    public int ColumnCount =>
        boardGrid != null
            ? boardGrid.ColumnCount
            : 0;

    public int RowCount =>
        boardGrid != null
            ? boardGrid.RowCount
            : 0;

    public bool IsReady =>
        boardGrid != null &&
        blockSpawner != null &&
        blockSpawner.IsReady;

    public BlockCatalog BlockCatalog =>
        patternBuilder != null
            ? patternBuilder.BlockCatalog
            : null;

    private void Awake()
    {
        EnsureHelpers();
        FindReferences();
        NormalizeSettings();
        ValidateReferences();
    }

    private void OnValidate()
    {
        EnsureHelpers();
        FindReferences();
        NormalizeSettings();
    }

    private void EnsureHelpers()
    {
        if (blockSpawner == null)
        {
            blockSpawner =
                new BlockSpawner();
        }

        if (patternBuilder == null)
        {
            patternBuilder =
                new BlockWavePatternBuilder();
        }
    }

    private void FindReferences()
    {
        if (boardGrid == null)
        {
            boardGrid =
                FindFirstObjectByType<BoardGrid>();
        }

        blockSpawner.Prepare(
            transform
        );
    }

    private void NormalizeSettings()
    {
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

        int availableColumns =
            boardGrid != null
                ? boardGrid.ColumnCount
                : 9;

        int availableRows =
            boardGrid != null
                ? boardGrid.RowCount
                : 15;

        patternBuilder.Normalize(
            availableColumns,
            availableRows
        );
    }

    private void ValidateReferences()
    {
        if (boardGrid == null)
        {
            Debug.LogError(
                "BlockWaveGenerator: " +
                "BoardGrid가 연결되지 않았습니다.",
                this
            );
        }

        blockSpawner.Validate(
            this
        );

        patternBuilder.Validate(
            this
        );
    }

    public int GetRandomWaveRowCount()
    {
        EnsureHelpers();

        return patternBuilder
            .GetRandomWaveRowCount(
                Mathf.Max(
                    RowCount,
                    1
                )
            );
    }

    public BlockDefinition GetRandomDefinition(
        BlockType blockType)
    {
        EnsureHelpers();

        return patternBuilder
            .GetRandomDefinition(
                blockType
            );
    }

    public int GetRequiredRowCount(
        int baseRowCount,
        BlockDefinition featuredDefinition)
    {
        EnsureHelpers();

        return patternBuilder
            .GetRequiredRowCount(
                baseRowCount,
                featuredDefinition,
                Mathf.Max(
                    RowCount,
                    1
                )
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
                "일반 웨이브를 생성합니다.",
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

        if (!IsReady)
        {
            Debug.LogError(
                "BlockWaveGenerator: " +
                "필수 참조가 없어 웨이브를 생성할 수 없습니다.",
                this
            );

            return generatedBlocks;
        }

        EnsureHelpers();
        NormalizeSettings();

        rowCount =
            Mathf.Clamp(
                rowCount,
                1,
                RowCount
            );

        waveIndex =
            Mathf.Max(
                waveIndex,
                0
            );

        int baseHealth =
            CalculateWaveHealth(
                waveIndex
            );

        int baseAttack =
            CalculateWaveAttack(
                waveIndex
            );

        List<BlockSpawnRequest> requests =
            patternBuilder.BuildWaveRequests(
                ColumnCount,
                RowCount,
                rowCount,
                waveIndex,
                fillBlockType,
                featuredDefinition,
                baseHealth,
                baseAttack,
                featuredHealthMultiplier,
                featuredAttackMultiplier
            );

        for (int i = 0;
             i < requests.Count;
             i++)
        {
            Block generatedBlock =
                blockSpawner.Spawn(
                    boardGrid,
                    requests[i]
                );

            if (generatedBlock == null)
            {
                continue;
            }

            generatedBlocks.Add(
                generatedBlock
            );
        }

        string featuredTypeText =
            featuredDefinition != null
                ? featuredDefinition
                    .BlockType
                    .ToString()
                : "None";

        Debug.Log(
            "BlockWaveGenerator: " +
            $"웨이브 {waveIndex + 1} 생성 완료, " +
            $"보드 {ColumnCount}x{RowCount}, " +
            $"주요 블록 {featuredTypeText}, " +
            $"생성 줄 {rowCount}, " +
            $"블록 {generatedBlocks.Count}개, " +
            $"기본 HP {baseHealth}, " +
            $"기본 공격력 {baseAttack}",
            this
        );

        return generatedBlocks;
    }

    private int CalculateWaveHealth(
        int waveIndex)
    {
        return Mathf.Max(
            1,
            startingBlockHealth +
            waveIndex *
            healthIncreasePerWave
        );
    }

    private int CalculateWaveAttack(
        int waveIndex)
    {
        return Mathf.Max(
            0,
            startingBlockAttack +
            waveIndex *
            attackIncreasePerWave
        );
    }
}