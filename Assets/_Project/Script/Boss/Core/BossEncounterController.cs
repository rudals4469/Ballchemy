using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class BossEncounterController :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BlockGridManager blockGridManager;

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private BallLauncher ballLauncher;

    [SerializeField]
    private StageRoomNavigator roomNavigator;

    [SerializeField]
    private BallCollection ballCollection;

    [SerializeField]
    private BoardGrid boardGrid;

    [SerializeField]
    private Block blockPrefab;

    [SerializeField]
    private BlockPrefabCatalog blockPrefabCatalog;

    [SerializeField]
    private Transform bossBlockContainer;

    [SerializeField]
    private EnemyAttackSequence enemyAttackSequence;

    [SerializeField]
    private BossCatalog bossCatalog;

    [Tooltip("기존 Scene 직렬화 보존 및 Catalog 누락 시 fallback용입니다.")]
    [SerializeField]
    private BossPatternDefinition testPattern;

    [Header("Entrance")]
    [SerializeField]
    private BossPatternEntranceAnimator
        entranceAnimator =
            new BossPatternEntranceAnimator();

    [Header("Transition")]
    [SerializeField, Min(0f)]
    private float boardClearDelay = 0.15f;

    [Header("Debug")]
    [SerializeField]
    private bool enableBossTestKey;

    private readonly List<Block>
        encounterBlocks =
            new List<Block>();

    private readonly List<
        BossPatternEntranceItem
    > entranceItems =
        new List<
            BossPatternEntranceItem
        >();

    private Block currentBossBlock;
    private BossDefinition activeBossDefinition;
    private BossPatternDefinition activePattern;
    private BossRunSequence bossRunSequence;

    private Coroutine startCoroutine;
    private Coroutine completeCoroutine;

    private bool isEncounterActive;
    private bool isTransitioning;
    private bool isBossDefeatPending;
    private int activeBossRoomId = -1;
    private int nextAttackIndex;
    private int turnsUntilBossAttack;

    public bool IsEncounterActive =>
        isEncounterActive;

    public bool IsTransitioning =>
        isTransitioning;

    public Block CurrentBossBlock =>
        currentBossBlock;

    public int TurnsUntilBossAttack =>
        turnsUntilBossAttack;

    public event Action
        BossEncounterStarted;

    public event Action
        BossEncounterCompleted;

    public event Action<int>
        TurnsUntilBossAttackChanged;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
        ValidateReferences();
        BeginNewBossRun();
    }

    public bool BeginNewBossRun()
    {
        if (bossRunSequence == null)
        {
            bossRunSequence =
                new BossRunSequence();
        }

        return bossRunSequence.Initialize(
            bossCatalog,
            this
        );
    }

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void OnValidate()
    {
        boardClearDelay =
            Mathf.Max(
                boardClearDelay,
                0f
            );

        FindReferences();
        NormalizeSettings();
    }

    private void Update()
    {
        /*
         * B키 테스트 진입도 일반 보스전 진입과
         * 동일한 StartBossEncounter()를 사용한다.
         */
        if (enableBossTestKey &&
            WasBossTestKeyPressed())
        {
            StartBossEncounter();
        }

        if (!isEncounterActive ||
            isTransitioning ||
            isBossDefeatPending)
        {
            return;
        }

        if (blockGridManager != null &&
            blockGridManager.RequiredEnemyCount > 0)
        {
            return;
        }

        isBossDefeatPending =
            true;

        Debug.Log(
            "BossEncounterController: " +
            "보스 처치 확인, " +
            "현재 공격 종료를 기다립니다.",
            this
        );

        TryCompletePendingEncounter();
    }

    private void FindReferences()
    {
        if (blockGridManager == null)
        {
            blockGridManager =
                FindFirstObjectByType<
                    BlockGridManager
                >();
        }

        if (turnManager == null)
        {
            turnManager =
                FindFirstObjectByType<
                    TurnManager
                >();
        }

        if (ballLauncher == null)
        {
            ballLauncher =
                FindFirstObjectByType<
                    BallLauncher
                >();
        }

        if (boardGrid == null)
        {
            boardGrid =
                FindFirstObjectByType<
                    BoardGrid
                >();
        }

        if (bossBlockContainer == null)
        {
            bossBlockContainer =
                transform;
        }

        if (enemyAttackSequence == null)
        {
            enemyAttackSequence =
                FindFirstObjectByType<EnemyAttackSequence>();
        }

        if (roomNavigator == null)
        {
            roomNavigator =
                FindFirstObjectByType<
                    StageRoomNavigator
                >();
        }

        if (ballCollection == null)
        {
            ballCollection =
                FindFirstObjectByType<
                    BallCollection
                >();
        }
    }

    private void NormalizeSettings()
    {
        if (entranceAnimator == null)
        {
            entranceAnimator =
                new BossPatternEntranceAnimator();
        }

        entranceAnimator.Normalize();
    }

    private void ValidateReferences()
    {
        if (blockGridManager == null)
        {
            Debug.LogError(
                "BossEncounterController: " +
                "BlockGridManager가 연결되지 않았습니다.",
                this
            );
        }

        if (turnManager == null)
        {
            Debug.LogError(
                "BossEncounterController: " +
                "TurnManager가 연결되지 않았습니다.",
                this
            );
        }

        if (ballLauncher == null)
        {
            Debug.LogError(
                "BossEncounterController: " +
                "BallLauncher가 연결되지 않았습니다.",
                this
            );
        }

        if (boardGrid == null)
        {
            Debug.LogError(
                "BossEncounterController: " +
                "BoardGrid가 연결되지 않았습니다.",
                this
            );
        }

        if (blockPrefab == null)
        {
            Debug.LogError(
                "BossEncounterController: " +
                "Block Prefab이 연결되지 않았습니다.",
                this
            );
        }

        if (bossCatalog == null)
        {
            Debug.LogWarning(
                "BossEncounterController: " +
                "Boss Catalog가 연결되지 않아 레거시 Test Pattern fallback을 확인합니다.",
                this
            );
        }

        if (blockPrefabCatalog == null)
        {
            Debug.LogWarning(
                "BossEncounterController: BlockPrefabCatalog가 없어 " +
                "특수 블록도 기본 Prefab으로 생성됩니다.",
                this
            );
        }

        if (enemyAttackSequence == null)
        {
            Debug.LogError(
                "BossEncounterController: EnemyAttackSequence가 연결되지 않았습니다.",
                this
            );
        }

        if (bossCatalog == null && testPattern == null)
        {
            Debug.LogError(
                "BossEncounterController: 보스 데이터가 연결되지 않았습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (turnManager != null)
        {
            turnManager.StateChanged -=
                HandleTurnStateChanged;

            turnManager.StateChanged +=
                HandleTurnStateChanged;
        }

        Ball.MovingBallCountChanged -=
            HandleMovingBallCountChanged;

        Ball.MovingBallCountChanged +=
            HandleMovingBallCountChanged;
    }

    private void UnsubscribeEvents()
    {
        if (turnManager != null)
        {
            turnManager.StateChanged -=
                HandleTurnStateChanged;
        }

        Ball.MovingBallCountChanged -=
            HandleMovingBallCountChanged;
    }

    private bool WasBossTestKeyPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            Keyboard.current.bKey
                .wasPressedThisFrame)
        {
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(
                KeyCode.B))
        {
            return true;
        }
#endif

        return false;
    }

    private void HandleBossEncounterRequested()
    {
        StartBossEncounter();
    }

    private void HandleTurnStateChanged(
        TurnState turnState)
    {
        if (turnState !=
            TurnState.Aiming)
        {
            return;
        }

        TryCompletePendingEncounter();
    }

    private void HandleMovingBallCountChanged(
        int movingBallCount)
    {
        if (movingBallCount > 0)
        {
            return;
        }

        TryCompletePendingEncounter();
    }

    public bool StartBossEncounter()
    {
        RoomNode currentRoom =
            roomNavigator != null
                ? roomNavigator.CurrentRoom
                : null;

        if (currentRoom == null ||
            currentRoom.RoomType != RoomType.Boss ||
            roomNavigator.IsRoomCleared(
                currentRoom.RoomId))
        {
            return false;
        }

        return StartBossRoomEncounter(
            currentRoom.RoomId
        );
    }

    public bool StartBossRoomEncounter(
        int roomId)
    {
        if (isEncounterActive ||
            isTransitioning)
        {
            return false;
        }

        if (Ball.ActiveMovingBallCount > 0)
        {
            Debug.LogWarning(
                "BossEncounterController: " +
                "공이 이동 중이어서 " +
                "보스전을 시작할 수 없습니다.",
                this
            );

            return false;
        }

        if (turnManager != null &&
            turnManager.IsGameOver)
        {
            return false;
        }

        if (!HasRequiredReferences())
        {
            Debug.LogError(
                "BossEncounterController: " +
                "보스전 설정이 완료되지 않았습니다.",
                this
            );

            return false;
        }

        if (!TrySelectBossDefinition())
        {
            return false;
        }

        if (!activePattern.TryValidatePattern(
                out string validationMessage))
        {
            Debug.LogError(
                "BossEncounterController: " +
                "보스 패턴이 올바르지 않습니다.\n" +
                validationMessage,
                activePattern
            );

            return false;
        }

        /*
         * 일반 보스 진입과 B키 테스트 진입이
         * 반드시 이 지점을 함께 통과한다.
         *
         * 보스 패턴을 생성하기 전에
         * Launcher와 모든 대기 공을 중앙으로 정렬한다.
         */
        bool launchPositionReset =
            ballLauncher
                .TryResetLaunchPositionToCenter();

        if (!launchPositionReset)
        {
            Debug.LogWarning(
                "BossEncounterController: " +
                "공 발사 위치를 중앙으로 " +
                "초기화하지 못해 보스전을 시작하지 않습니다.",
                this
            );

            return false;
        }

        isTransitioning = true;
        isBossDefeatPending = false;
        nextAttackIndex = 0;
        turnsUntilBossAttack = 0;
        activeBossRoomId = roomId;

        turnManager?.SetInputLocked(
            true
        );

        roomNavigator?.SetNavigationLocked(
            true
        );

        bool bossModeStarted =
            blockGridManager
                .BeginBossRoomEncounterMode(
                    roomId
                );

        if (!bossModeStarted)
        {
            isTransitioning = false;

            turnManager?.SetInputLocked(
                false
            );

            roomNavigator?.SetNavigationLocked(
                false
            );

            activeBossRoomId = -1;

            return false;
        }

        startCoroutine =
            StartCoroutine(
                StartBossEncounterRoutine()
            );

        return true;
    }

    private IEnumerator
        StartBossEncounterRoutine()
    {
        ClearEncounterObjects();

        if (boardClearDelay > 0f)
        {
            yield return
                new WaitForSeconds(
                    boardClearDelay
                );
        }
        else
        {
            yield return null;
        }

        bool spawnedPattern =
            TrySpawnPattern(
                activePattern
            );

        if (!spawnedPattern)
        {
            Debug.LogError(
                "BossEncounterController: " +
                "패턴 생성에 실패했습니다.",
                this
            );

            RecoverFromFailedStart();

            yield break;
        }

        yield return entranceAnimator
            .PlayRoutine(
                entranceItems
            );

        entranceItems.Clear();

        nextAttackIndex = 0;
        turnsUntilBossAttack =
            activeBossDefinition != null
                ? activeBossDefinition.AttackIntervalTurns
                : 1;

        NotifyBossAttackTurnsChanged();

        isEncounterActive =
            blockGridManager.RequiredEnemyCount > 0;

        isTransitioning = false;
        startCoroutine = null;

        turnManager?.SetInputLocked(
            false
        );

        roomNavigator?.SetNavigationLocked(
            false
        );

        if (!isEncounterActive)
        {
            RecoverFromFailedStart();

            yield break;
        }

        BossEncounterStarted?.Invoke();

        Debug.Log(
            "BossEncounterController: " +
            $"보스전 시작 - " +
            $"{GetActiveBossDisplayName()}",
            this
        );
    }

    private bool TrySpawnPattern(
        BossPatternDefinition pattern)
    {
        if (pattern == null ||
            boardGrid == null)
        {
            return false;
        }

        if (pattern.Width !=
                boardGrid.ColumnCount ||
            pattern.Height !=
                boardGrid.RowCount)
        {
            Debug.LogError(
                "BossEncounterController: " +
                $"보스 패턴 크기는 " +
                $"{boardGrid.ColumnCount}×" +
                $"{boardGrid.RowCount}이어야 합니다. " +
                $"현재 패턴은 " +
                $"{pattern.Width}×" +
                $"{pattern.Height}입니다.",
                pattern
            );

            return false;
        }

        if (!TryFindBossAnchor(
                pattern,
                out Vector2Int bossAnchor))
        {
            return false;
        }

        BlockDefinition bossDefinition =
            pattern.BossDefinition;

        if (bossDefinition == null)
        {
            Debug.LogError(
                "BossEncounterController: " +
                "Boss Definition이 비어 있습니다.",
                pattern
            );

            return false;
        }

        Vector2Int bossGridSize =
            new Vector2Int(
                Mathf.Max(
                    bossDefinition.GridSize.x,
                    1
                ),
                Mathf.Max(
                    bossDefinition.GridSize.y,
                    1
                )
            );

        if (bossGridSize.x % 2 == 0 ||
            bossGridSize.y % 2 == 0)
        {
            Debug.LogError(
                "BossEncounterController: " +
                "현재 B 중심 배치는 홀수 크기의 " +
                "보스만 지원합니다. " +
                $"현재 크기: " +
                $"{bossGridSize.x}×" +
                $"{bossGridSize.y}",
                bossDefinition
            );

            return false;
        }

        Vector2Int bossStartCell =
            new Vector2Int(
                bossAnchor.x -
                bossGridSize.x / 2,
                bossAnchor.y -
                bossGridSize.y / 2
            );

        if (!IsFootprintInsideBoard(
                bossStartCell,
                bossGridSize))
        {
            Debug.LogError(
                "BossEncounterController: " +
                "보스 점유 영역이 보드를 벗어납니다.",
                pattern
            );

            return false;
        }

        for (int row = 0;
             row < pattern.Height;
             row++)
        {
            for (int column = 0;
                 column < pattern.Width;
                 column++)
            {
                char symbol =
                    pattern.GetSymbol(
                        column,
                        row
                    );

                if (symbol == '.')
                {
                    continue;
                }

                if (symbol != 'B' &&
                    IsInsideFootprint(
                        column,
                        row,
                        bossStartCell,
                        bossGridSize))
                {
                    continue;
                }

                int startColumn =
                    symbol == 'B'
                        ? bossStartCell.x
                        : column;

                int startRow =
                    symbol == 'B'
                        ? bossStartCell.y
                        : row;

                BlockDefinition definition =
                    GetDefinitionForSymbol(
                        pattern,
                        symbol
                    );

                if (definition == null)
                {
                    Debug.LogWarning(
                        "BossEncounterController: " +
                        $"'{symbol}'에 사용할 " +
                        "BlockDefinition이 없습니다.",
                        pattern
                    );

                    continue;
                }

                int health =
                    GetHealthForSymbol(
                        pattern,
                        symbol,
                        definition
                    );

                int attackPower =
                    symbol == 'B'
                        ? pattern.BossAttackPower
                        : 0;

                Block spawnedBlock =
                    SpawnPatternBlock(
                        definition,
                        symbol,
                        startColumn,
                        startRow,
                        row,
                        health,
                        attackPower
                    );

                if (spawnedBlock == null)
                {
                    continue;
                }

                encounterBlocks.Add(
                    spawnedBlock
                );

                if (symbol == 'B')
                {
                    currentBossBlock =
                        spawnedBlock;
                }
            }
        }

        bool hasRequiredEnemy = false;

        for (int i = 0; i < encounterBlocks.Count; i++)
        {
            Block block = encounterBlocks[i];
            if (block != null && block.Definition != null &&
                block.Definition.ClearRole == BlockClearRole.RequiredEnemy)
            {
                hasRequiredEnemy = true;
                break;
            }
        }

        if (!hasRequiredEnemy)
        {
            Debug.LogError(
                "BossEncounterController: 패턴에 RequiredEnemy 블록이 없습니다.",
                pattern
            );
            return false;
        }

        blockGridManager
            .RegisterBossEncounterBlocks(
                encounterBlocks
            );

        return true;
    }

    private bool TryFindBossAnchor(
        BossPatternDefinition pattern,
        out Vector2Int bossAnchor)
    {
        bossAnchor =
            new Vector2Int(
                -1,
                -1
            );

        int bossCount = 0;

        for (int row = 0;
             row < pattern.Height;
             row++)
        {
            for (int column = 0;
                 column < pattern.Width;
                 column++)
            {
                if (pattern.GetSymbol(
                        column,
                        row) != 'B')
                {
                    continue;
                }

                bossAnchor =
                    new Vector2Int(
                        column,
                        row
                    );

                bossCount++;
            }
        }

        if (bossCount != 1)
        {
            Debug.LogError(
                "BossEncounterController: " +
                $"패턴에 B가 {bossCount}개 있습니다. " +
                "정확히 1개여야 합니다.",
                pattern
            );

            return false;
        }

        return true;
    }

    private Block SpawnPatternBlock(
        BlockDefinition definition,
        char symbol,
        int startColumn,
        int startRow,
        int patternRow,
        int health,
        int attackPower)
    {
        Vector2Int gridSize =
            new Vector2Int(
                Mathf.Max(
                    definition.GridSize.x,
                    1
                ),
                Mathf.Max(
                    definition.GridSize.y,
                    1
                )
            );

        Vector3 targetPosition =
            GetBlockCenterWorldPosition(
                startColumn,
                startRow,
                gridSize
            );

        Vector3 startPosition =
            entranceAnimator
                .GetStartPosition(
                    boardGrid,
                    targetPosition
                );

        Block selectedPrefab =
            blockPrefabCatalog != null
                ? blockPrefabCatalog.ResolvePrefab(definition)
                : blockPrefab;

        if (selectedPrefab == null)
        {
            selectedPrefab = blockPrefab;
        }

        Block newBlock =
            Instantiate(
                selectedPrefab,
                startPosition,
                boardGrid.transform.rotation,
                bossBlockContainer
            );

        if (newBlock == null)
        {
            return null;
        }

        string symbolName =
            GetSymbolName(
                symbol
            );

        newBlock.name =
            $"BossPattern_{symbolName}" +
            $"_R{startRow}" +
            $"_C{startColumn}";

        newBlock.Initialize(
            definition,
            health,
            attackPower,
            boardGrid.CellSize
        );

        newBlock.SetGridPosition(
            boardGrid,
            startColumn,
            startRow,
            false
        );

        newBlock.transform.position =
            startPosition;

        BossPatternEntranceItem
            entranceItem =
                new BossPatternEntranceItem(
                    newBlock,
                    startPosition,
                    targetPosition,
                    patternRow
                );

        entranceItems.Add(
            entranceItem
        );

        if (symbol == '#' &&
            definition.DestructionRule !=
            BlockDestructionRule.Indestructible)
        {
            Debug.LogWarning(
                "BossEncounterController: " +
                $"{definition.name}의 " +
                "Destruction Rule이 " +
                "Indestructible이 아닙니다.",
                definition
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
            boardGrid.GetCellWorldPosition(
                startColumn,
                startRow
            );

        float horizontalDistance =
            (
                Mathf.Max(
                    gridSize.x,
                    1
                ) -
                1
            ) *
            boardGrid.CellSize *
            0.5f;

        float verticalDistance =
            (
                Mathf.Max(
                    gridSize.y,
                    1
                ) -
                1
            ) *
            boardGrid.CellSize *
            0.5f;

        Vector3 horizontalOffset =
            boardGrid.transform.right *
            horizontalDistance;

        Vector3 verticalOffset =
            -boardGrid.transform.up *
            verticalDistance;

        return startCellPosition +
               horizontalOffset +
               verticalOffset;
    }

    private bool IsFootprintInsideBoard(
        Vector2Int startCell,
        Vector2Int gridSize)
    {
        int endColumn =
            startCell.x +
            gridSize.x -
            1;

        int endRow =
            startCell.y +
            gridSize.y -
            1;

        return startCell.x >= 0 &&
               startCell.y >= 0 &&
               endColumn <
               boardGrid.ColumnCount &&
               endRow <
               boardGrid.RowCount;
    }

    private bool IsInsideFootprint(
        int column,
        int row,
        Vector2Int startCell,
        Vector2Int gridSize)
    {
        int endColumn =
            startCell.x +
            gridSize.x -
            1;

        int endRow =
            startCell.y +
            gridSize.y -
            1;

        return column >=
                   startCell.x &&
               column <=
                   endColumn &&
               row >=
                   startCell.y &&
               row <=
                   endRow;
    }

    private BlockDefinition
        GetDefinitionForSymbol(
        BossPatternDefinition pattern,
        char symbol)
    {
        switch (symbol)
        {
            case 'B':
                return pattern
                    .BossDefinition;

            case 'X':
                return pattern
                    .BreakablePatternDefinition;

            case 'S':
                return activeBossDefinition != null
                    ? activeBossDefinition.GetRandomSpecialBlockDefinition()
                    : null;

            case '#':
                return pattern
                    .IndestructiblePatternDefinition;

            default:
                return null;
        }
    }

    private int GetHealthForSymbol(
        BossPatternDefinition pattern,
        char symbol,
        BlockDefinition definition)
    {
        int baseHealth;

        switch (symbol)
        {
            case 'B':
                baseHealth = pattern
                    .BossHealth;
                break;

            case 'X':
                baseHealth = pattern
                    .PatternBlockHealth;
                break;

            case 'S':
                baseHealth = activeBossDefinition != null
                    ? activeBossDefinition.SpecialBlockHealth
                    : pattern.PatternBlockHealth;
                break;

            case '#':
                baseHealth = 1;
                break;

            default:
                baseHealth = 1;
                break;
        }

        if (activeBossDefinition == null || definition == null ||
            definition.ClearRole != BlockClearRole.RequiredEnemy)
        {
            return baseHealth;
        }

        return Mathf.Max(
            Mathf.CeilToInt(baseHealth *
                activeBossDefinition.RequiredEnemyHealthMultiplier),
            1
        );
    }

    private bool TrySelectBossDefinition()
    {
        activeBossDefinition = null;
        activePattern = null;

        int stageNumber =
            roomNavigator != null && roomNavigator.CurrentMap != null
                ? roomNavigator.CurrentMap.StageNumber
                : 1;

        bool usedFallback = false;

        if (bossRunSequence != null &&
            bossRunSequence.TryResolve(
                stageNumber,
                out activeBossDefinition))
        {
            activePattern =
                activeBossDefinition.PatternDefinition;
        }
        else if (bossCatalog != null && bossCatalog.TryResolve(
                     stageNumber,
                     out activeBossDefinition,
                     out usedFallback))
        {
            activePattern = activeBossDefinition.PatternDefinition;

            Debug.LogWarning(
                "BossEncounterController: " +
                $"런 순서의 스테이지 {stageNumber} 보스가 없어 " +
                $"Catalog {(usedFallback ? "fallback" : "레거시 배정")} " +
                $"'{activeBossDefinition.DisplayName}'을 사용합니다.",
                bossCatalog
            );
        }
        else if (testPattern != null)
        {
            activePattern = testPattern;
            Debug.LogWarning(
                "BossEncounterController: Boss Catalog 선택에 실패해 " +
                "레거시 Test Pattern을 사용합니다.",
                this
            );
        }

        if (activePattern != null)
        {
            return true;
        }

        Debug.LogError(
            "BossEncounterController: 현재 스테이지에 사용할 보스 패턴이 없습니다.",
            this
        );
        return false;
    }

    private string GetActiveBossDisplayName()
    {
        if (activeBossDefinition != null &&
            !string.IsNullOrWhiteSpace(activeBossDefinition.DisplayName))
        {
            return activeBossDefinition.DisplayName;
        }

        return activePattern != null ? activePattern.DisplayName : "Boss";
    }

    private string GetSymbolName(
        char symbol)
    {
        switch (symbol)
        {
            case 'B':
                return "Boss";

            case 'X':
                return "Breakable";

            case 'S':
                return "Special";

            case '#':
                return "Wall";

            default:
                return "Unknown";
        }
    }

    private void TryCompletePendingEncounter()
    {
        if (!isBossDefeatPending ||
            isTransitioning ||
            completeCoroutine != null)
        {
            return;
        }

        if (Ball.ActiveMovingBallCount > 0)
        {
            return;
        }

        if (turnManager != null &&
            turnManager.CurrentState !=
            TurnState.Aiming)
        {
            return;
        }

        completeCoroutine =
            StartCoroutine(
                CompleteBossEncounterRoutine()
            );
    }

    private IEnumerator
        CompleteBossEncounterRoutine()
    {
        isTransitioning = true;
        isEncounterActive = false;

        turnManager?.SetInputLocked(
            true
        );

        Debug.Log(
            "BossEncounterController: " +
            "보스전 종료 처리",
            this
        );

        ClearEncounterObjects();

        yield return null;

        ballCollection?.SetBallsVisible(
            false
        );

        turnManager?.SetInputLocked(
            false
        );

        roomNavigator?.SetNavigationLocked(
            false
        );

        bool roomCompleted =
            blockGridManager
                .CompleteBossRoomEncounterMode();

        if (!roomCompleted)
        {
            Debug.LogError(
                "BossEncounterController: Boss 방 클리어 상태 전환에 실패했습니다. " +
                $"RoomId={activeBossRoomId}",
                this
            );
        }

        isBossDefeatPending = false;
        isTransitioning = false;

        completeCoroutine = null;
        activeBossRoomId = -1;

        BossEncounterCompleted?.Invoke();

        Debug.Log(
            "BossEncounterController: " +
            "보스전 종료 및 Boss 방 클리어 완료",
            this
        );
    }

    public IEnumerator ResolveBossTurnRoutine()
    {
        if (!isEncounterActive || isTransitioning ||
            isBossDefeatPending || activeBossDefinition == null ||
            blockGridManager == null ||
            blockGridManager.RequiredEnemyCount <= 0)
        {
            yield break;
        }

        turnsUntilBossAttack =
            Mathf.Max(turnsUntilBossAttack - 1, 0);

        NotifyBossAttackTurnsChanged();

        if (turnsUntilBossAttack > 0)
        {
            yield break;
        }

        BossAttackDefinition attack =
            activeBossDefinition.GetAttack(nextAttackIndex);

        nextAttackIndex++;

        if (attack == null)
        {
            Debug.LogWarning(
                "BossEncounterController: 실행할 보스 공격 데이터가 없습니다.",
                activeBossDefinition
            );

            ResetBossAttackTurns();
            yield break;
        }

        List<Block> attackers =
            CreateAttackers(attack);

        List<BossAttackTargetOutline> targetOutlines =
            ShowAttackTargetOutlines(attack, attackers);

        if (targetOutlines.Count > 0 && attack.TelegraphDuration > 0f)
        {
            yield return new WaitForSeconds(attack.TelegraphDuration);
        }

        if (attackers.Count > 0 && enemyAttackSequence != null)
        {
            Debug.Log(
                "BossEncounterController: " +
                $"'{attack.DisplayName}' 실행, 공격 블록 {attackers.Count}개",
                this
            );

            yield return enemyAttackSequence.ResolveAttackRoutine(
                attackers,
                attack.Damage
            );
        }

        HideAttackTargetOutlines(targetOutlines);

        if (turnManager != null && turnManager.IsGameOver)
        {
            yield break;
        }

        yield return SpawnPostAttackBlocksRoutine();

        ResetBossAttackTurns();
    }

    private List<Block> CreateAttackers(
        BossAttackDefinition attack)
    {
        List<Block> result = new List<Block>();

        if (attack.AttackType == BossAttackType.DirectBossAttack)
        {
            if (currentBossBlock != null && currentBossBlock.IsAlive)
            {
                result.Add(currentBossBlock);
            }

            return result;
        }

        for (int i = 0; i < encounterBlocks.Count; i++)
        {
            Block block = encounterBlocks[i];

            if (block == null || !block.IsAlive ||
                block == currentBossBlock ||
                block.Definition == null ||
                block.IsIndestructible)
            {
                continue;
            }

            result.Add(block);
        }

        Shuffle(result);

        int selectedCount =
            Mathf.Min(attack.SelectedBlockCount, result.Count);

        if (result.Count > selectedCount)
        {
            result.RemoveRange(
                selectedCount,
                result.Count - selectedCount
            );
        }

        return result;
    }

    private IEnumerator SpawnPostAttackBlocksRoutine()
    {
        int stageNumber =
            roomNavigator != null && roomNavigator.CurrentMap != null
                ? roomNavigator.CurrentMap.StageNumber
                : 1;

        int requestedCount =
            activeBossDefinition.CalculateSpawnedBlockCount(stageNumber);

        BlockDefinition spawnDefinition =
            activePattern != null
                ? activePattern.BreakablePatternDefinition
                : null;

        if (requestedCount <= 0 || spawnDefinition == null)
        {
            yield break;
        }

        List<Vector2Int> emptyCells = FindEmptySpawnCells();
        Shuffle(emptyCells);

        int spawnCount = Mathf.Min(requestedCount, emptyCells.Count);
        List<Block> spawnedBlocks = new List<Block>();
        entranceItems.Clear();

        for (int i = 0; i < spawnCount; i++)
        {
            Vector2Int cell = emptyCells[i];

            bool spawnSpecial =
                UnityEngine.Random.value <
                activeBossDefinition.SpecialBlockSpawnChance;

            BlockDefinition selectedDefinition =
                spawnSpecial
                    ? activeBossDefinition.GetRandomSpecialBlockDefinition()
                    : null;

            char symbol = selectedDefinition != null ? 'S' : 'X';
            if (selectedDefinition == null)
            {
                selectedDefinition = spawnDefinition;
            }

            int health =
                symbol == 'S'
                    ? activeBossDefinition.SpecialBlockHealth
                    : activeBossDefinition.SpawnedBlockHealth;

            Block spawnedBlock = SpawnPatternBlock(
                selectedDefinition,
                symbol,
                cell.x,
                cell.y,
                cell.y,
                health,
                0
            );

            if (spawnedBlock == null)
            {
                continue;
            }

            encounterBlocks.Add(spawnedBlock);
            spawnedBlocks.Add(spawnedBlock);
        }

        blockGridManager.RegisterBossEncounterBlocks(spawnedBlocks);

        if (entranceItems.Count > 0)
        {
            yield return entranceAnimator.PlayRoutine(entranceItems);
            entranceItems.Clear();
        }
    }

    private List<Vector2Int> FindEmptySpawnCells()
    {
        List<Vector2Int> result = new List<Vector2Int>();

        if (boardGrid == null)
        {
            return result;
        }

        int maximumSpawnRow = Mathf.Max(boardGrid.RowCount - 2, -1);

        for (int row = 0; row <= maximumSpawnRow; row++)
        {
            for (int column = 0; column < boardGrid.ColumnCount; column++)
            {
                bool occupied = false;

                for (int i = 0; i < encounterBlocks.Count; i++)
                {
                    Block block = encounterBlocks[i];
                    if (block != null && block.IsAlive &&
                        block.OccupiesCell(column, row))
                    {
                        occupied = true;
                        break;
                    }
                }

                if (!occupied)
                {
                    result.Add(new Vector2Int(column, row));
                }
            }
        }

        return result;
    }

    private static void Shuffle<T>(List<T> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int swapIndex = UnityEngine.Random.Range(0, i + 1);
            (items[i], items[swapIndex]) = (items[swapIndex], items[i]);
        }
    }

    private List<BossAttackTargetOutline> ShowAttackTargetOutlines(
        BossAttackDefinition attack,
        IReadOnlyList<Block> attackers)
    {
        List<BossAttackTargetOutline> result =
            new List<BossAttackTargetOutline>();

        if (attack == null ||
            attack.AttackType != BossAttackType.SelectedBlockAttack ||
            attackers == null)
        {
            return result;
        }

        for (int i = 0; i < attackers.Count; i++)
        {
            Block block = attackers[i];
            if (block == null)
            {
                continue;
            }

            BossAttackTargetOutline outline =
                block.GetComponent<BossAttackTargetOutline>();

            if (outline == null)
            {
                outline = block.gameObject.AddComponent<BossAttackTargetOutline>();
            }

            outline.Show(new Color(1f, 0.82f, 0.08f, 0.95f));
            result.Add(outline);
        }

        return result;
    }

    private static void HideAttackTargetOutlines(
        IReadOnlyList<BossAttackTargetOutline> outlines)
    {
        if (outlines == null)
        {
            return;
        }

        for (int i = 0; i < outlines.Count; i++)
        {
            outlines[i]?.Hide();
        }
    }

    private void NotifyBossAttackTurnsChanged()
    {
        TurnsUntilBossAttackChanged?.Invoke(turnsUntilBossAttack);
    }

    private void ResetBossAttackTurns()
    {
        turnsUntilBossAttack =
            activeBossDefinition != null
                ? activeBossDefinition.AttackIntervalTurns
                : 0;

        NotifyBossAttackTurnsChanged();
    }

    private void RecoverFromFailedStart()
    {
        ClearEncounterObjects();

        isEncounterActive = false;
        isTransitioning = false;
        isBossDefeatPending = false;

        startCoroutine = null;

        blockGridManager
            .CancelBossRoomEncounterMode();

        activeBossRoomId = -1;

        turnManager?.SetInputLocked(
            false
        );

        roomNavigator?.SetNavigationLocked(
            false
        );
    }

    private bool HasRequiredReferences()
    {
        return blockGridManager != null &&
               turnManager != null &&
               ballLauncher != null &&
               boardGrid != null &&
               blockPrefab != null &&
               enemyAttackSequence != null &&
               (bossCatalog != null || testPattern != null);
    }

    private void ClearEncounterObjects()
    {
        for (int i = 0;
             i < encounterBlocks.Count;
             i++)
        {
            Block block =
                encounterBlocks[i];

            if (block == null)
            {
                continue;
            }

            block.gameObject.SetActive(
                false
            );

            Destroy(
                block.gameObject
            );
        }

        encounterBlocks.Clear();
        entranceItems.Clear();

        currentBossBlock =
            null;
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();

        if (startCoroutine != null)
        {
            StopCoroutine(
                startCoroutine
            );
        }

        if (completeCoroutine != null)
        {
            StopCoroutine(
                completeCoroutine
            );
        }

        turnManager?.SetInputLocked(
            false
        );

        roomNavigator?.SetNavigationLocked(
            false
        );
    }
}
