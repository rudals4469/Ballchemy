using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public sealed class BlockWavePatternBuilder
{
    private readonly List<Vector2Int> selectedTeleportSlots =
        new List<Vector2Int>();
    private MapTeleportMode selectedTeleportMode;
    [Header("Block Data")]

    [SerializeField]
    private BlockCatalog blockCatalog;

    [Header("Special Blocks")]

    [SerializeField]
    private BlockWaveSpecialInjector specialInjector =
        new BlockWaveSpecialInjector();

    public int MinimumGuardianTargets =>
        specialInjector.MinimumGuardianTargets;

    public int MaximumGuardianTargets =>
        specialInjector.MaximumGuardianTargets;

    [Header("Wave Row Settings")]

    [SerializeField, Min(1)]
    private int minimumRowsPerWave = 3;

    [SerializeField, Min(1)]
    private int maximumRowsPerWave = 3;

    [Header("Compact Row Spacing")]

    [Tooltip(
        "활성화하면 행 수가 적은 패턴의 논리 행 사이를 띄워 " +
        "보드 세로 공간을 더 넓게 사용합니다."
    )]
    [SerializeField]
    private bool enableCompactRowSpacing = true;

    [Tooltip(
        "이 값 이하의 행 수에 Compact Row Spacing을 적용합니다."
    )]
    [SerializeField, Min(1)]
    private int maximumRowsForCompactSpacing = 4;

    [Tooltip(
        "작은 패턴에서 논리 행 사이에 적용할 실제 보드 행 간격입니다.\n" +
        "2이면 0, 2, 4, 6행처럼 배치합니다."
    )]
    [SerializeField, Min(1)]
    private int compactRowSpacing = 2;

    [Header("Blocks Per Row")]

    [SerializeField, Min(1)]
    private int minimumBlocksPerRow = 4;

    [SerializeField, Min(1)]
    private int maximumBlocksPerRow = 5;

    [Header("Ricochet Pattern")]

    [Tooltip(
        "Inspector에서 테스트할 방 배치 패턴을 선택합니다.\n" +
        "Automatic은 기존 Wall Pocket 확률 규칙을 사용합니다."
    )]
    [SerializeField]
    private BlockWavePatternType patternType =
        BlockWavePatternType.Automatic;

    [Tooltip(
        "Wall Pocket 패턴이 선택될 확률입니다.\n" +
        "현재 테스트 단계에서는 1로 두는 것을 권장합니다."
    )]
    [SerializeField, Range(0f, 1f)]
    private float wallPocketChance = 1f;

    [Tooltip(
        "Wall Pocket이 사용할 최대 가로 폭입니다.\n" +
        "벽 근처에 블록을 밀집시켜 공이 벽과 블록 사이에서 " +
        "여러 번 반사될 가능성을 높입니다."
    )]
    [SerializeField, Min(3)]
    private int wallPocketWidth = 5;

    [Tooltip(
        "Wall Pocket의 주 블록 라인이 실제 벽에서 " +
        "몇 칸 떨어질지 결정합니다.\n" +
        "1이면 벽과 블록 사이에 1칸짜리 통로를 만듭니다."
    )]
    [SerializeField, Min(1)]
    private int wallPocketCorridorOffset = 1;

    [Header("Twin Pocket")]

    [SerializeField]
    private TwinPocketPatternBuilder twinPocket =
        new TwinPocketPatternBuilder();

    [Header("Zigzag Corridor")]

    [SerializeField]
    private ZigzagCorridorPatternBuilder zigzagCorridor =
        new ZigzagCorridorPatternBuilder();

    [Header("Center Gate")]

    [SerializeField]
    private CenterGatePatternBuilder centerGate =
        new CenterGatePatternBuilder();

    [Header("Ricochet Pocket")]

    [SerializeField]
    private RicochetPocketPatternBuilder ricochetPocket =
        new RicochetPocketPatternBuilder();

    [Tooltip(
        "일반 지그재그 패턴에서 이전 줄 기준으로 " +
        "좌우 몇 칸 이동시킬지 결정합니다."
    )]
    [SerializeField, Min(1)]
    private int horizontalStaggerDistance = 1;

    public BlockCatalog BlockCatalog =>
        blockCatalog;

    public void Normalize(
        int availableColumns,
        int availableRows)
    {
        EnsureHelpers();

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

        wallPocketChance =
            Mathf.Clamp01(
                wallPocketChance
            );

        wallPocketWidth =
            Mathf.Clamp(
                wallPocketWidth,
                3,
                availableColumns
            );

        wallPocketCorridorOffset =
            Mathf.Clamp(
                wallPocketCorridorOffset,
                1,
                Mathf.Max(
                    availableColumns - 1,
                    1
                )
            );

        maximumRowsForCompactSpacing =
            Mathf.Clamp(
                maximumRowsForCompactSpacing,
                1,
                availableRows
            );

        compactRowSpacing =
            Mathf.Clamp(
                compactRowSpacing,
                1,
                availableRows
            );

        twinPocket.Normalize(
            availableColumns
        );

        zigzagCorridor.Normalize(
            availableColumns
        );

        centerGate.Normalize(
            availableColumns
        );

        ricochetPocket.Normalize(
            availableColumns,
            availableRows
        );

        horizontalStaggerDistance =
            Mathf.Clamp(
                horizontalStaggerDistance,
                1,
                Mathf.Max(
                    availableColumns - 1,
                    1
                )
            );

        specialInjector.Normalize();
    }

    public void Validate(
        UnityEngine.Object context)
    {
        EnsureHelpers();

        if (blockCatalog == null)
        {
            Debug.LogWarning(
                "BlockWavePatternBuilder: " +
                "Block Catalog가 연결되지 않았습니다. " +
                "Normal 타입은 기본 1x1 블록으로 대체됩니다.",
                context
            );
        }

        specialInjector.Validate(
            context
        );
    }

    private void EnsureHelpers()
    {
        if (specialInjector == null)
        {
            specialInjector =
                new BlockWaveSpecialInjector();
        }

        if (twinPocket == null)
        {
            twinPocket =
                new TwinPocketPatternBuilder();
        }

        if (zigzagCorridor == null)
        {
            zigzagCorridor =
                new ZigzagCorridorPatternBuilder();
        }

        if (centerGate == null)
        {
            centerGate =
                new CenterGatePatternBuilder();
        }

        if (ricochetPocket == null)
        {
            ricochetPocket =
                new RicochetPocketPatternBuilder();
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
                Mathf.Max(featuredDefinition.GridSize.y, 1) + 1
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
        EnsureHelpers();

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

        int logicalRowCount =
            Mathf.Clamp(
                requestedRowCount,
                1,
                boardRowCount
            );

        int appliedRowSpacing =
            GetAppliedRowSpacing(
                logicalRowCount,
                boardRowCount
            );

        int layoutRowCount =
            GetLayoutRowCount(
                logicalRowCount,
                appliedRowSpacing
            );

        waveIndex =
            Mathf.Max(
                waveIndex,
                0
            );

        int fixedMapRows = ricochetPocket.PrepareFixedLayout(
            Mathf.Max(columnCount - 2, 0),
            Mathf.Max(boardRowCount - 1, 0),
            waveIndex);
        if (fixedMapRows > 0)
        {
            layoutRowCount = Mathf.Min(
                Mathf.Max(layoutRowCount, fixedMapRows + 1),
                boardRowCount);
        }

        List<BlockSpawnRequest> requests =
            new List<BlockSpawnRequest>();

        BlockWaveOccupancyMap occupancyMap =
            new BlockWaveOccupancyMap(
                columnCount,
                boardRowCount
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

        BlockWavePatternType selectedPattern =
            ricochetPocket.HasPreparedFixedLayout
                ? BlockWavePatternType.RicochetPocket
                : SelectPattern(columnCount, layoutRowCount);

        if (selectedPattern == BlockWavePatternType.RicochetPocket)
        {
            BuildRicochetPocketRequests(
                columnCount,
                layoutRowCount,
                waveIndex,
                fillBlockType,
                baseHealth,
                baseAttack,
                occupancyMap,
                requests);
            return requests;
        }

        bool useWallPocket =
            selectedPattern ==
            BlockWavePatternType.WallPocket;

        bool useTwinPocket =
            selectedPattern ==
            BlockWavePatternType.TwinPocket;

        bool useZigzagCorridor =
            selectedPattern ==
            BlockWavePatternType.ZigzagCorridor;

        bool useCenterGate =
            selectedPattern ==
            BlockWavePatternType.CenterGate;

        bool pocketOnLeft =
            Random.value < 0.5f;

        bool firstCorridorOpeningOnLeft =
            Random.value < 0.5f;

        bool firstGateShoulderOnLeft =
            Random.value < 0.5f;

        int leftTwinPocketEntryRow = -1;
        int rightTwinPocketEntryRow = -1;

        if (useTwinPocket)
        {
            twinPocket.SelectEntryRows(
                logicalRowCount,
                out leftTwinPocketEntryRow,
                out rightTwinPocketEntryRow
            );
        }

        HashSet<int> usedColumns =
            new HashSet<int>();

        List<int> previousRowColumns =
            new List<int>();

        int initialStaggerDirection =
            Random.value < 0.5f
                ? -1
                : 1;

        for (int row = 0;
             row < logicalRowCount;
             row++)
        {
            int boardRow =
                row *
                appliedRowSpacing;

            int targetBlockCount =
                GetRandomBlockCountPerRow(
                    columnCount
                );

            if (useTwinPocket)
            {
                targetBlockCount =
                    twinPocket
                        .GetTargetBlockCountPerRow(
                            columnCount
                        );
            }
            else if (useZigzagCorridor)
            {
                targetBlockCount =
                    zigzagCorridor
                        .GetTargetBlockCountPerRow(
                            row,
                            logicalRowCount,
                            columnCount
                        );
            }
            else if (useCenterGate)
            {
                targetBlockCount =
                    centerGate
                        .GetTargetBlockCountPerRow(
                            row,
                            logicalRowCount,
                            columnCount
                        );
            }

            List<int> preferredColumns;

            if (useWallPocket)
            {
                preferredColumns =
                    CreateWallPocketColumnPriority(
                        row,
                        targetBlockCount,
                        columnCount,
                        pocketOnLeft
                    );
            }
            else if (useTwinPocket)
            {
                preferredColumns =
                    twinPocket.CreateColumnPriority(
                        row,
                        columnCount,
                        leftTwinPocketEntryRow,
                        rightTwinPocketEntryRow
                    );
            }
            else if (useZigzagCorridor)
            {
                preferredColumns =
                    zigzagCorridor.CreateColumnPriority(
                        row,
                        logicalRowCount,
                        targetBlockCount,
                        columnCount,
                        firstCorridorOpeningOnLeft
                    );
            }
            else if (useCenterGate)
            {
                preferredColumns =
                    centerGate.CreateColumnPriority(
                        row,
                        logicalRowCount,
                        columnCount,
                        firstGateShoulderOnLeft
                    );
            }
            else if (row == 0 ||
                     previousRowColumns.Count == 0)
            {
                preferredColumns =
                    CreateFirstRowColumnPriority(
                        targetBlockCount,
                        columnCount
                    );
            }
            else
            {
                int rowDirection =
                    row % 2 == 1
                        ? initialStaggerDirection
                        : -initialStaggerDirection;

                preferredColumns =
                    CreateStaggeredColumnPriority(
                        previousRowColumns,
                        usedColumns,
                        columnCount,
                        rowDirection
                    );
            }

            List<int> spawnedColumns =
                FillRow(
                    boardRow,
                    waveIndex,
                    targetBlockCount,
                    preferredColumns,
                    fillBlockType,
                    baseHealth,
                    baseAttack,
                    occupancyMap,
                    requests,
                    useTwinPocket ||
                    useZigzagCorridor ||
                    useCenterGate
                );

            for (int i = 0;
                 i < spawnedColumns.Count;
                 i++)
            {
                usedColumns.Add(
                    spawnedColumns[i]
                );
            }

            previousRowColumns =
                spawnedColumns.Count > 0
                    ? spawnedColumns
                    : previousRowColumns;
        }

        return requests;
    }

    public void InjectScaledSpecialBlocks(
        List<BlockSpawnRequest> requests,
        int columnCount,
        int rowCount,
        int waveIndex,
        bool isNamedRoom,
        int baseHealth,
        TeleportPairSpawnSettings teleportSettings)
    {
        TryInjectSelectedMapTeleportPair(
            requests,
            teleportSettings,
            waveIndex,
            isNamedRoom);
        specialInjector.InjectScaledSpecialBlocks(
            requests,
            blockCatalog,
            columnCount,
            rowCount,
            waveIndex,
            isNamedRoom,
            baseHealth,
            teleportSettings);
    }

    private void TryInjectSelectedMapTeleportPair(
        List<BlockSpawnRequest> requests,
        TeleportPairSpawnSettings settings,
        int waveIndex,
        bool isNamedRoom)
    {
        if (selectedTeleportMode == MapTeleportMode.None ||
            selectedTeleportSlots.Count != 2 || settings == null)
            return;

        bool shouldSpawn = selectedTeleportMode == MapTeleportMode.Required
            ? settings.CanSpawn(isNamedRoom)
            : settings.ShouldSpawn(isNamedRoom);
        if (!shouldSpawn) return;

        TeleportPairInjector.TryInjectAtPositions(
            requests, settings, waveIndex,
            selectedTeleportSlots[0], selectedTeleportSlots[1]);
    }

    public int CalculateLayoutRowCount(
        int requestedRowCount,
        int boardRowCount)
    {
        int logicalRowCount = Mathf.Clamp(
            requestedRowCount,
            1,
            Mathf.Max(boardRowCount, 1)
        );

        int rowSpacing = GetAppliedRowSpacing(
            logicalRowCount,
            Mathf.Max(boardRowCount, 1)
        );

        return GetLayoutRowCount(
            logicalRowCount,
            rowSpacing
        );
    }

    private int GetAppliedRowSpacing(
        int logicalRowCount,
        int boardRowCount)
    {
        if (!enableCompactRowSpacing ||
            logicalRowCount <= 1 ||
            logicalRowCount >
            maximumRowsForCompactSpacing)
        {
            return 1;
        }

        int maximumFittingSpacing =
            Mathf.Max(
                (boardRowCount - 1) /
                (logicalRowCount - 1),
                1
            );

        return Mathf.Clamp(
            compactRowSpacing,
            1,
            maximumFittingSpacing
        );
    }

    private int GetLayoutRowCount(
        int logicalRowCount,
        int rowSpacing)
    {
        return Mathf.Max(
            (logicalRowCount - 1) *
            Mathf.Max(
                rowSpacing,
                1
            ) +
            1,
            1
        );
    }

    private BlockWavePatternType SelectPattern(
        int columnCount,
        int rowCount)
    {
        if ((patternType == BlockWavePatternType.Automatic ||
             patternType == BlockWavePatternType.RicochetPocket) &&
            ricochetPocket.CanBuild(columnCount - 2, rowCount))
        {
            return BlockWavePatternType.RicochetPocket;
        }

        if (patternType ==
                BlockWavePatternType.CenterGate &&
            centerGate.CanBuild(
                columnCount
            ))
        {
            return BlockWavePatternType.CenterGate;
        }

        if (patternType ==
                BlockWavePatternType.ZigzagCorridor &&
            zigzagCorridor.CanBuild(
                columnCount
            ))
        {
            return BlockWavePatternType.ZigzagCorridor;
        }

        if (patternType ==
                BlockWavePatternType.TwinPocket &&
            twinPocket.CanBuild(
                columnCount
            ))
        {
            return BlockWavePatternType.TwinPocket;
        }

        if (patternType ==
            BlockWavePatternType.WallPocket)
        {
            return BlockWavePatternType.WallPocket;
        }

        if (patternType ==
            BlockWavePatternType.Legacy)
        {
            return BlockWavePatternType.Legacy;
        }

        return columnCount >= 4 &&
               Random.value <= wallPocketChance
            ? BlockWavePatternType.WallPocket
            : BlockWavePatternType.Legacy;
    }

    private void BuildRicochetPocketRequests(
        int columnCount,
        int rowCount,
        int waveIndex,
        BlockType fillBlockType,
        int baseHealth,
        int baseAttack,
        BlockWaveOccupancyMap occupancyMap,
        List<BlockSpawnRequest> requests)
    {
        selectedTeleportSlots.Clear();
        selectedTeleportMode = MapTeleportMode.None;
        int pocketColumnCount = Mathf.Max(columnCount - 2, 0);
        int pocketRowCount = Mathf.Min(
            rowCount,
            Mathf.Max(occupancyMap.RowCount - 1, 0));
        List<RicochetPocketPatternBuilder.Cell> cells =
            ricochetPocket.Build(
                pocketColumnCount,
                pocketRowCount,
                waveIndex);
        BlockDefinition indestructibleDefinition =
            blockCatalog != null
                ? blockCatalog.GetById("boss_pattern_wall")
                : null;
        int pocketGroupId = waveIndex;

        for (int i = 0; i < cells.Count; i++)
        {
            RicochetPocketPatternBuilder.Cell cell = cells[i];
            Vector2Int position = cell.Position + Vector2Int.one;
            if (cell.CellType == PocketLayoutCellType.TeleportSlot)
            {
                selectedTeleportSlots.Add(position);
                selectedTeleportMode = cell.TeleportMode;
                continue;
            }
            if (cell.CellType == PocketLayoutCellType.SpecialSlot ||
                cell.CellType == PocketLayoutCellType.NamedSlot)
                continue;
            BlockDefinition definition = cell.Indestructible
                ? indestructibleDefinition
                : GetRandomFittingDefinition(
                    fillBlockType,
                    position.x,
                    position.y,
                    occupancyMap,
                    true);
            BlockType requestedType = definition != null
                ? definition.BlockType
                : fillBlockType;

            if (definition == null && requestedType != BlockType.Normal)
                continue;
            if (!occupancyMap.TryOccupy(
                    position.x,
                    position.y,
                    Vector2Int.one))
                continue;

            BlockSpawnRequest request = new BlockSpawnRequest(
                position.x,
                position.y,
                waveIndex,
                definition,
                requestedType,
                Vector2Int.one,
                baseHealth,
                cell.Role == BlockSpawnRequest.CombatRole.Attacker
                    ? baseAttack
                    : 0,
                null,
                -1,
                default,
                cell.Indestructible
                    ? BlockSpawnRequest.CombatRole.Unspecified
                    : cell.Role,
                cell.Indestructible ? -1 : pocketGroupId);
            requests.Add(request);
        }
    }

    private List<int> CreateWallPocketColumnPriority(
        int row,
        int targetBlockCount,
        int columnCount,
        bool pocketOnLeft)
    {
        List<int> result =
            new List<int>();

        targetBlockCount =
            Mathf.Clamp(
                targetBlockCount,
                1,
                columnCount
            );

        int direction =
            pocketOnLeft
                ? 1
                : -1;

        int wallColumn =
            pocketOnLeft
                ? 0
                : columnCount - 1;

        int corridorColumn =
            wallColumn +
            direction *
            wallPocketCorridorOffset;

        corridorColumn =
            Mathf.Clamp(
                corridorColumn,
                0,
                columnCount - 1
            );

        /*
         * 매 줄마다 주 라인을 살짝 안쪽/바깥쪽으로
         * 움직여 완전한 직선 벽이 되는 것을 피한다.
         *
         * row 0 : 벽에서 1칸
         * row 1 : 벽에서 2칸
         * row 2 : 다시 벽에서 1칸
         */
        int rowStagger =
            row % 2;

        int anchorColumn =
            corridorColumn +
            direction *
            rowStagger;

        anchorColumn =
            Mathf.Clamp(
                anchorColumn,
                0,
                columnCount - 1
            );

        AddColumnIfValid(
            result,
            anchorColumn,
            columnCount
        );

        /*
         * Anchor보다 안쪽으로 먼저 확장한다.
         *
         * 벽 바로 옆을 완전히 채우기보다는
         * 벽과 블록 사이의 좁은 통로를 유지하면서
         * 그 안쪽에 밀집 구역을 만든다.
         */
        for (int distance = 2;
             distance < wallPocketWidth;
             distance += 2)
        {
            AddColumnIfValid(
                result,
                anchorColumn +
                direction *
                distance,
                columnCount
            );
        }

        for (int distance = 1;
             distance < wallPocketWidth;
             distance += 2)
        {
            AddColumnIfValid(
                result,
                anchorColumn +
                direction *
                distance,
                columnCount
            );
        }

        /*
         * 일부 행에서는 실제 벽쪽 칸도 후순위 후보로 둔다.
         *
         * 벽에 딱 붙은 블록과
         * 한 칸 안쪽 블록이 섞이면
         * 단순한 평행 통로만 반복되는 것을 줄일 수 있다.
         */
        if (row % 2 == 1)
        {
            AddColumnIfValid(
                result,
                wallColumn,
                columnCount
            );
        }

        /*
         * Pocket 반대쪽으로도 몇 개의 블록이
         * 존재할 수 있게 나머지 열을 추가한다.
         *
         * 앞쪽 후보가 목표 개수를 이미 채우면
         * 실제 생성에는 사용되지 않는다.
         */
        List<int> remainingColumns =
            CreateAllColumns(
                columnCount
            );

        remainingColumns.RemoveAll(
            column =>
                result.Contains(
                    column
                )
        );

        /*
         * 완전 무작위보다는 Pocket 가까운 열부터
         * 뒤에 배치한다.
         */
        remainingColumns.Sort(
            (a, b) =>
            {
                int distanceA =
                    Mathf.Abs(
                        a -
                        corridorColumn
                    );

                int distanceB =
                    Mathf.Abs(
                        b -
                        corridorColumn
                    );

                if (distanceA ==
                    distanceB)
                {
                    return Random.value < 0.5f
                        ? -1
                        : 1;
                }

                return distanceA.CompareTo(
                    distanceB
                );
            }
        );

        AddColumnsIfMissing(
            result,
            remainingColumns
        );

        return result;
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

        int maximumStartRow =
            occupancyMap.RowCount -
            gridSize.y;

        // 네임드 블럭은 최상단 행을 비워 두고 그 아래에 배치한다.
        // 공이 블럭 위를 한 번 스치고 끝나는 대신 주변 구조물 사이에서
        // 여러 차례 반사될 여지를 만들기 위한 규칙이다.
        if (maximumStartRow < 1)
        {
            return null;
        }

        int startRow = Random.Range(1, maximumStartRow + 1);

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
        List<BlockSpawnRequest> requests,
        bool requireUnitSize = false)
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
                    requireUnitSize,
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
                    requireUnitSize,
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
        bool requireUnitSize,
        out BlockSpawnRequest request)
    {
        request = null;

        BlockDefinition definition =
            GetRandomFittingDefinition(
                blockType,
                startColumn,
                startRow,
                occupancyMap,
                requireUnitSize
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
        BlockWaveOccupancyMap occupancyMap,
        bool requireUnitSize)
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

            if (requireUnitSize &&
                NormalizeGridSize(
                    definition.GridSize) !=
                Vector2Int.one)
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

    private List<int> CreateFirstRowColumnPriority(
        int targetBlockCount,
        int columnCount)
    {
        targetBlockCount =
            Mathf.Clamp(
                targetBlockCount,
                1,
                columnCount
            );

        List<int> selectedColumns =
            new List<int>();

        int firstColumn =
            Random.Range(
                0,
                columnCount
            );

        selectedColumns.Add(
            firstColumn
        );

        while (selectedColumns.Count <
               targetBlockCount)
        {
            int nextColumn =
                FindMostSeparatedColumn(
                    selectedColumns,
                    columnCount
                );

            if (nextColumn < 0)
            {
                break;
            }

            selectedColumns.Add(
                nextColumn
            );
        }

        List<int> remainingColumns =
            CreateAllColumns(
                columnCount
            );

        remainingColumns.RemoveAll(
            column =>
                selectedColumns.Contains(
                    column
                )
        );

        ShuffleColumns(
            remainingColumns
        );

        selectedColumns.AddRange(
            remainingColumns
        );

        return selectedColumns;
    }

    private int FindMostSeparatedColumn(
        IReadOnlyList<int> selectedColumns,
        int columnCount)
    {
        int bestDistance = -1;

        List<int> bestCandidates =
            new List<int>();

        for (int candidate = 0;
             candidate < columnCount;
             candidate++)
        {
            if (ContainsColumn(
                    selectedColumns,
                    candidate))
            {
                continue;
            }

            int minimumDistance =
                int.MaxValue;

            for (int i = 0;
                 i < selectedColumns.Count;
                 i++)
            {
                int distance =
                    Mathf.Abs(
                        candidate -
                        selectedColumns[i]
                    );

                minimumDistance =
                    Mathf.Min(
                        minimumDistance,
                        distance
                    );
            }

            if (minimumDistance >
                bestDistance)
            {
                bestDistance =
                    minimumDistance;

                bestCandidates.Clear();

                bestCandidates.Add(
                    candidate
                );

                continue;
            }

            if (minimumDistance ==
                bestDistance)
            {
                bestCandidates.Add(
                    candidate
                );
            }
        }

        if (bestCandidates.Count == 0)
        {
            return -1;
        }

        return bestCandidates[
            Random.Range(
                0,
                bestCandidates.Count
            )
        ];
    }

    private List<int> CreateStaggeredColumnPriority(
        IReadOnlyList<int> previousRowColumns,
        HashSet<int> usedColumns,
        int columnCount,
        int staggerDirection)
    {
        List<int> result =
            new List<int>();

        HashSet<int> previousColumnSet =
            new HashSet<int>(
                previousRowColumns
            );

        List<int> shuffledPreviousColumns =
            new List<int>(
                previousRowColumns
            );

        ShuffleColumns(
            shuffledPreviousColumns
        );

        int normalizedDirection =
            staggerDirection < 0
                ? -1
                : 1;

        for (int i = 0;
             i < shuffledPreviousColumns.Count;
             i++)
        {
            int sourceColumn =
                shuffledPreviousColumns[i];

            int preferredColumn =
                sourceColumn +
                normalizedDirection *
                horizontalStaggerDistance;

            if (!IsValidColumn(
                    preferredColumn,
                    columnCount) ||
                previousColumnSet.Contains(
                    preferredColumn))
            {
                preferredColumn =
                    sourceColumn -
                    normalizedDirection *
                    horizontalStaggerDistance;
            }

            if (!IsValidColumn(
                    preferredColumn,
                    columnCount))
            {
                continue;
            }

            if (previousColumnSet.Contains(
                    preferredColumn))
            {
                continue;
            }

            AddColumnIfMissing(
                result,
                preferredColumn
            );
        }

        List<int> unusedFreshColumns =
            new List<int>();

        for (int column = 0;
             column < columnCount;
             column++)
        {
            if (previousColumnSet.Contains(
                    column))
            {
                continue;
            }

            if (usedColumns != null &&
                usedColumns.Contains(
                    column))
            {
                continue;
            }

            unusedFreshColumns.Add(
                column
            );
        }

        ShuffleColumns(
            unusedFreshColumns
        );

        AddColumnsIfMissing(
            result,
            unusedFreshColumns
        );

        List<int> nonVerticalColumns =
            new List<int>();

        for (int column = 0;
             column < columnCount;
             column++)
        {
            if (previousColumnSet.Contains(
                    column))
            {
                continue;
            }

            nonVerticalColumns.Add(
                column
            );
        }

        ShuffleColumns(
            nonVerticalColumns
        );

        AddColumnsIfMissing(
            result,
            nonVerticalColumns
        );

        List<int> repeatedColumns =
            new List<int>(
                previousRowColumns
            );

        ShuffleColumns(
            repeatedColumns
        );

        AddColumnsIfMissing(
            result,
            repeatedColumns
        );

        List<int> allColumns =
            CreateAllColumns(
                columnCount
            );

        ShuffleColumns(
            allColumns
        );

        AddColumnsIfMissing(
            result,
            allColumns
        );

        return result;
    }

    private void AddColumnIfValid(
        List<int> columns,
        int column,
        int columnCount)
    {
        if (!IsValidColumn(
                column,
                columnCount))
        {
            return;
        }

        AddColumnIfMissing(
            columns,
            column
        );
    }

    private void AddColumnsIfMissing(
        List<int> destination,
        IReadOnlyList<int> source)
    {
        if (destination == null ||
            source == null)
        {
            return;
        }

        for (int i = 0;
             i < source.Count;
             i++)
        {
            AddColumnIfMissing(
                destination,
                source[i]
            );
        }
    }

    private void AddColumnIfMissing(
        List<int> columns,
        int column)
    {
        if (columns == null)
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

    private bool ContainsColumn(
        IReadOnlyList<int> columns,
        int column)
    {
        if (columns == null)
        {
            return false;
        }

        for (int i = 0;
             i < columns.Count;
             i++)
        {
            if (columns[i] ==
                column)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsValidColumn(
        int column,
        int columnCount)
    {
        return column >= 0 &&
               column < columnCount;
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
        if (columns == null)
        {
            return;
        }

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
