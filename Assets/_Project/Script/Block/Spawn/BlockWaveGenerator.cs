using System.Collections.Generic;
using UnityEngine;

public sealed class BlockWaveGenerator :
    MonoBehaviour
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

    [Header("Combat Roles")]

    [SerializeField]
    private BlockWaveCombatRoleAssigner
        combatRoleAssigner =
            new BlockWaveCombatRoleAssigner();

    [Header("Teleport Pair")]

    [SerializeField]
    private TeleportPairSpawnSettings teleportPairSettings =
        new TeleportPairSpawnSettings();

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

    [Tooltip(
        "일반 블록은 공격하지 않습니다.\n" +
        "이 값은 Elite 및 공격 가능한 주요 블록의 " +
        "기준 공격력으로 사용됩니다."
    )]
    [SerializeField, Min(0)]
    private int startingBlockAttack = 2;

    [Tooltip(
        "웨이브 인덱스가 증가할 때 " +
        "기준 공격력에 추가되는 값입니다."
    )]
    [SerializeField, Min(0)]
    private int attackIncreasePerWave;

    [Header("Block Stat Variation")]

    [SerializeField]
    private BlockStatVariationSettings
        statVariationSettings =
            new BlockStatVariationSettings();

    private readonly List<BlockSpawnRequest>
        lastGeneratedRequests =
            new List<BlockSpawnRequest>();

    private readonly Dictionary<
        int,
        List<BlockSpawnRequest>
    > generatedRequestsByRoomId =
        new Dictionary<
            int,
            List<BlockSpawnRequest>
        >();

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

    public bool HasLastGeneratedWave =>
        lastGeneratedRequests.Count > 0;

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

        if (combatRoleAssigner == null)
        {
            combatRoleAssigner =
                new BlockWaveCombatRoleAssigner();
        }

        if (statVariationSettings == null)
        {
            statVariationSettings =
                new BlockStatVariationSettings();
        }

        if (teleportPairSettings == null)
        {
            teleportPairSettings = new TeleportPairSpawnSettings();
        }
    }

    private void FindReferences()
    {
        if (boardGrid == null)
        {
            boardGrid =
                FindFirstObjectByType<
                    BoardGrid
                >();
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

        combatRoleAssigner.Normalize();
        statVariationSettings.Normalize();
        teleportPairSettings.Normalize();
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

        combatRoleAssigner.Validate(
            this,
            BlockCatalog
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

    public List<Block> RegenerateLastWave()
    {
        List<Block> generatedBlocks =
            new List<Block>();

        if (!IsReady)
        {
            Debug.LogError(
                "BlockWaveGenerator: " +
                "필수 참조가 없어 마지막 배치를 " +
                "다시 생성할 수 없습니다.",
                this
            );

            return generatedBlocks;
        }

        if (!HasLastGeneratedWave)
        {
            Debug.LogWarning(
                "BlockWaveGenerator: " +
                "저장된 최초 블록 배치가 없습니다.",
                this
            );

            return generatedBlocks;
        }

        List<BlockSpawnRequest> requests =
            CreateRequestCopies(
                lastGeneratedRequests
            );

        generatedBlocks =
            SpawnRequests(
                requests
            );

        Debug.Log(
            "BlockWaveGenerator: " +
            $"저장된 최초 배치로 블록 " +
            $"{generatedBlocks.Count}개를 복원했습니다.",
            this
        );

        return generatedBlocks;
    }

    public bool HasGeneratedRoomWave(
        int roomId)
    {
        return roomId >= 0 &&
               generatedRequestsByRoomId
                   .ContainsKey(roomId);
    }

    public void SaveLastWaveForRoom(
        int roomId)
    {
        if (roomId < 0 ||
            !HasLastGeneratedWave)
        {
            return;
        }

        generatedRequestsByRoomId[roomId] =
            CreateRequestCopies(
                lastGeneratedRequests
            );
    }

    public List<Block> RegenerateRoomWave(
        int roomId)
    {
        if (!generatedRequestsByRoomId.TryGetValue(
                roomId,
                out List<BlockSpawnRequest> savedRequests))
        {
            return new List<Block>();
        }

        List<BlockSpawnRequest> requests =
            CreateRequestCopies(savedRequests);

        SaveLastGeneratedRequests(requests);

        return SpawnRequests(requests);
    }

    public void ClearRoomWaveSnapshots()
    {
        generatedRequestsByRoomId.Clear();
        lastGeneratedRequests.Clear();
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

        patternBuilder.InjectScaledSpecialBlocks(
            requests,
            ColumnCount,
            RowCount,
            waveIndex,
            featuredDefinition != null,
            baseHealth,
            teleportPairSettings);

        /*
         * 배치 자체가 모두 끝난 뒤
         * 블록의 전투 역할만 결정합니다.
         *
         * Normal:
         * Attack = 0
         *
         * Elite:
         * Normal 자리 일부를 교체하고
         * 공격력을 부여합니다.
         *
         * Named / Special 등은
         * 기존 요청을 유지합니다.
         */
        requests =
            statVariationSettings.Apply(
                requests
            );

        requests =
            combatRoleAssigner
                .ApplyCombatRoles(
                    requests,
                    BlockCatalog,
                    featuredDefinition != null
                );

        requests = GuardianProtectionResolver.AssignTargets(
            requests,
            patternBuilder.MinimumGuardianTargets,
            patternBuilder.MaximumGuardianTargets
        );

        SaveLastGeneratedRequests(
            requests
        );

        generatedBlocks =
            SpawnRequests(
                requests
            );

        string featuredTypeText =
            featuredDefinition != null
                ? featuredDefinition
                    .BlockType
                    .ToString()
                : "None";

        int normalCount =
            CountRequestType(
                requests,
                BlockType.Normal
            );

        int eliteCount =
            CountRequestType(
                requests,
                BlockType.Elite
            );

        Debug.Log(
            "BlockWaveGenerator: " +
            $"웨이브 {waveIndex + 1} 생성 완료, " +
            $"보드 {ColumnCount}x{RowCount}, " +
            $"주요 블록 {featuredTypeText}, " +
            $"생성 줄 {rowCount}, " +
            $"전체 블록 {generatedBlocks.Count}개, " +
            $"Normal {normalCount}개, " +
            $"Elite {eliteCount}개, " +
            $"기본 HP {baseHealth}, " +
            $"Elite 기준 공격력 {baseAttack}",
            this
        );

        return generatedBlocks;
    }

    private int CountRequestType(
        IReadOnlyList<BlockSpawnRequest> requests,
        BlockType blockType)
    {
        if (requests == null)
        {
            return 0;
        }

        int count = 0;

        for (int i = 0;
             i < requests.Count;
             i++)
        {
            BlockSpawnRequest request =
                requests[i];

            if (request == null ||
                request.RequestedBlockType !=
                blockType)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private List<Block> SpawnRequests(
        IReadOnlyList<BlockSpawnRequest> requests)
    {
        List<Block> generatedBlocks =
            new List<Block>();

        if (requests == null)
        {
            return generatedBlocks;
        }

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

            Block generatedBlock =
                blockSpawner.Spawn(
                    boardGrid,
                    request
                );

            if (generatedBlock == null)
            {
                continue;
            }

            generatedBlocks.Add(
                generatedBlock
            );
        }

        GuardianProtectionResolver.ConnectRuntime(
            requests,
            generatedBlocks
        );

        TeleportPairRuntimeResolver.Connect(
            requests,
            generatedBlocks
        );

        return generatedBlocks;
    }

    private void SaveLastGeneratedRequests(
        IReadOnlyList<BlockSpawnRequest> requests)
    {
        lastGeneratedRequests.Clear();

        if (requests == null)
        {
            return;
        }

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

            lastGeneratedRequests.Add(
                request.CreateCopy()
            );
        }
    }

    private List<BlockSpawnRequest>
        CreateRequestCopies(
            IReadOnlyList<BlockSpawnRequest> requests)
    {
        List<BlockSpawnRequest> copies =
            new List<BlockSpawnRequest>();

        if (requests == null)
        {
            return copies;
        }

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

            copies.Add(
                request.CreateCopy()
            );
        }

        return copies;
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
