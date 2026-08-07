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

    [Header("Special Blocks")]

    [SerializeField]
    private BlockWaveSpecialInjector specialInjector =
        new BlockWaveSpecialInjector();

    [Header("Wave Row Settings")]

    [SerializeField, Min(1)]
    private int minimumRowsPerWave = 3;

    [SerializeField, Min(1)]
    private int maximumRowsPerWave = 3;

    [Header("Blocks Per Row")]

    [SerializeField, Min(1)]
    private int minimumBlocksPerRow = 4;

    [SerializeField, Min(1)]
    private int maximumBlocksPerRow = 5;

    [Header("Ricochet Pattern")]

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

        bool useWallPocket =
            columnCount >= 4 &&
            Random.value <= wallPocketChance;

        bool pocketOnLeft =
            Random.value < 0.5f;

        HashSet<int> usedColumns =
            new HashSet<int>();

        List<int> previousRowColumns =
            new List<int>();

        int initialStaggerDirection =
            Random.value < 0.5f
                ? -1
                : 1;

        for (int row = 0;
             row < rowCount;
             row++)
        {
            int targetBlockCount =
                GetRandomBlockCountPerRow(
                    columnCount
                );

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

        specialInjector.InjectSpecialBlocks(
            requests,
            blockCatalog,
            columnCount,
            rowCount,
            waveIndex,
            featuredDefinition != null,
            baseHealth
        );

        return requests;
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