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
    private BallSealController ballSealController;

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
    private PlayerHealth playerHealth;

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
    private bool enableBossTestKey = true;

    private readonly List<Block>
        encounterBlocks =
            new List<Block>();

    private readonly List<
        BossPatternEntranceItem
    > entranceItems =
        new List<
            BossPatternEntranceItem
        >();

    private readonly List<BossGrowthCellOutline>
        growthOutlines =
            new List<BossGrowthCellOutline>();

    private static readonly HashSet<string>
        DescendingWaveExcludedSpecialBlockIds =
            new HashSet<string>
            {
                "special_teleport",
                "special_heal",
                "special_curse",
                "special_gold"
            };

    private readonly BossColonyGrowthState
        colonyGrowthState =
            new BossColonyGrowthState();

    private readonly List<Block> reactorBombs =
        new List<Block>();

    private readonly List<Block> reactorBlockers =
        new List<Block>();

    private readonly List<Block> trapBlocks = new List<Block>();
    private readonly List<Block> trapTerrainBlocks = new List<Block>();
    private readonly List<Block> temporaryTrapWalls = new List<Block>();
    private readonly List<Block> bossArenaNormalBlocks = new List<Block>();
    private Block trapAmplifierBlock;

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
    private int descendingWavesSpawned;
    private int descendingTurnsResolved;
    private int destroyedReactorBombCount;
    private int reactorExplosionDepth;
    private int reactorBossHitAxisMask;
    private bool reactorCrossLockTriggered;
    private int bossArenaNormalRegenerationTurns;

    public bool IsEncounterActive =>
        isEncounterActive;

    public bool IsTransitioning =>
        isTransitioning;

    public Block CurrentBossBlock =>
        currentBossBlock;

    public int TurnsUntilBossAttack =>
        turnsUntilBossAttack;

    public bool IsDescendingWaveEncounter =>
        isEncounterActive &&
        activeBossDefinition != null &&
        activeBossDefinition.IsDescendingWave;

    public int RemainingDescendingWaves =>
        activeBossDefinition != null &&
        activeBossDefinition.IsDescendingWave
            ? Mathf.Max(
                activeBossDefinition.DescendingWaveCount -
                descendingTurnsResolved,
                0
            )
            : 0;

    public event Action
        BossEncounterStarted;

    public event Action
        BossEncounterCompleted;

    public event Action<int>
        TurnsUntilBossAttackChanged;

    public event Action<int>
        DescendingWavesRemainingChanged;

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
            if (roomNavigator != null)
            {
                roomNavigator.TryDebugEnterBossRoom();
            }
        }

        if (!isEncounterActive ||
            isTransitioning ||
            isBossDefeatPending)
        {
            return;
        }

        if (activeBossDefinition != null &&
            (activeBossDefinition.IsDescendingWave ||
             activeBossDefinition.IsBombReactor))
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

        if (ballSealController == null)
        {
            ballSealController =
                FindFirstObjectByType<
                    BallSealController
                >();
        }

        if (playerHealth == null)
        {
            playerHealth =
                FindFirstObjectByType<
                    PlayerHealth
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

        if (blockGridManager != null)
        {
            blockGridManager.BlocksReachedBottom -=
                HandleBlocksReachedBottom;

            blockGridManager.BlocksReachedBottom +=
                HandleBlocksReachedBottom;

            blockGridManager.BlocksExceededBottom -=
                HandleBlocksReachedBottom;

            blockGridManager.BlocksExceededBottom +=
                HandleBlocksReachedBottom;
        }
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

        if (blockGridManager != null)
        {
            blockGridManager.BlocksReachedBottom -=
                HandleBlocksReachedBottom;

            blockGridManager.BlocksExceededBottom -=
                HandleBlocksReachedBottom;
        }
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

    private void HandleBlocksReachedBottom(
        IReadOnlyList<Block> blocks)
    {
        if (!isEncounterActive ||
            activeBossDefinition == null ||
            blocks == null)
        {
            return;
        }

        int escapedBlockCount = 0;

        for (int i = 0; i < blocks.Count; i++)
        {
            Block block = blocks[i];

            if (block == null || !block.IsAlive)
            {
                continue;
            }

            bool isDescendingBlock =
                activeBossDefinition.IsDescendingWave;

            if (!isDescendingBlock)
            {
                continue;
            }

            escapedBlockCount++;
            encounterBlocks.Remove(block);
            reactorBombs.Remove(block);
            block.ExpireWithoutReward();
        }

        if (escapedBlockCount <= 0)
        {
            return;
        }

        int damage =
            escapedBlockCount *
            activeBossDefinition.DescendingDamagePerEscapedBlock;

        playerHealth?.TakeDamage(damage);

        Debug.LogWarning(
            "BossEncounterController: " +
            $"하강 웨이브 블록 {escapedBlockCount}개 이탈, " +
            $"플레이어 피해 {damage}",
            this
        );
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

        if (activeBossDefinition != null &&
            activeBossDefinition.IsDescendingWave &&
            playerHealth == null)
        {
            Debug.LogError(
                "BossEncounterController: 하강 웨이브 피해를 적용할 PlayerHealth가 없습니다.",
                this
            );

            return false;
        }

        if ((activeBossDefinition == null ||
             !activeBossDefinition.IsDescendingWave) &&
            !activePattern.TryValidatePattern(
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
        descendingWavesSpawned = 0;
        descendingTurnsResolved = 0;
        destroyedReactorBombCount = 0;
        reactorExplosionDepth = 0;
        reactorBossHitAxisMask = 0;
        reactorCrossLockTriggered = false;
        bossArenaNormalRegenerationTurns = 0;
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
            activeBossDefinition != null &&
            activeBossDefinition.IsDescendingWave
                ? TrySpawnDescendingWave()
                : TrySpawnPattern(
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

        if (activeBossDefinition != null &&
            activeBossDefinition.IsBombReactor)
        {
            currentBossBlock.HitReceived -=
                HandleReactorBossHitReceived;

            currentBossBlock.HitReceived +=
                HandleReactorBossHitReceived;

            SpawnReactorBlockers();
        }
        else if (activeBossDefinition != null &&
                 activeBossDefinition.IsTrapMaster)
        {
            SpawnTrapMasterSetup();
        }

        if (activeBossDefinition != null &&
            (activeBossDefinition.IsBombReactor ||
             activeBossDefinition.IsTrapMaster))
        {
            SpawnBossArenaNormalBlocks(
                activeBossDefinition.ArenaNormalBlockCount
            );
        }

        yield return entranceAnimator
            .PlayRoutine(
                entranceItems
            );

        entranceItems.Clear();

        if (activeBossDefinition != null &&
            activeBossDefinition.IsProliferatingColony)
        {
            yield return
                RunInitialColonyGrowthRoutine();
        }

        else if (activeBossDefinition != null &&
                 activeBossDefinition.IsBombReactor)
        {
            SpawnReactorBombs(
                activeBossDefinition.ReactorInitialBombCount
            );

            if (entranceItems.Count > 0)
            {
                yield return entranceAnimator.PlayRoutine(
                    entranceItems
                );

                entranceItems.Clear();
            }
        }

        nextAttackIndex = 0;
        turnsUntilBossAttack =
            activeBossDefinition != null &&
            activeBossDefinition.IsProliferatingColony
                ? 1
                : activeBossDefinition != null
                ? activeBossDefinition.AttackIntervalTurns
                : 1;

        NotifyBossAttackTurnsChanged();

        isEncounterActive =
            activeBossDefinition != null &&
            activeBossDefinition.IsDescendingWave
                ? descendingWavesSpawned > 0
                : blockGridManager.RequiredEnemyCount > 0;

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

        if (IsDescendingWaveEncounter)
        {
            DescendingWavesRemainingChanged?.Invoke(
                RemainingDescendingWaves
            );
        }

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

        if (activeBossDefinition != null &&
            activeBossDefinition.IsProliferatingColony)
        {
            return TrySpawnColonyPattern(
                pattern
            );
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

        if ((bossGridSize.x % 2 == 0 ||
             bossGridSize.y % 2 == 0) &&
            (activeBossDefinition == null ||
             !activeBossDefinition.IsBombReactor))
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

    private bool TrySpawnDescendingWave(
        int rowCount = 0)
    {
        if (activeBossDefinition == null ||
            !activeBossDefinition.IsDescendingWave ||
            blockGridManager == null)
        {
            return false;
        }

        if (rowCount <= 0)
        {
            rowCount = UnityEngine.Random.Range(
                activeBossDefinition.DescendingMinimumRowCount,
                activeBossDefinition.DescendingMaximumRowCount + 1
            );
        }

        List<Block> generatedBlocks =
            blockGridManager.GenerateBossEncounterWave(
                rowCount,
                descendingWavesSpawned,
                DescendingWaveExcludedSpecialBlockIds
            );

        if (generatedBlocks == null ||
            generatedBlocks.Count == 0)
        {
            return false;
        }

        encounterBlocks.AddRange(generatedBlocks);
        descendingWavesSpawned++;

        Debug.Log(
            "BossEncounterController: " +
            $"하강 웨이브 {descendingWavesSpawned}/" +
            $"{activeBossDefinition.DescendingWaveCount} 생성, " +
            $"높이 {rowCount}줄",
            this
        );

        return true;
    }

    private void SpawnReactorBombs(
        int requestedCount)
    {
        if (activeBossDefinition == null ||
            !activeBossDefinition.IsBombReactor ||
            activeBossDefinition.ReactorBombDefinition == null ||
            boardGrid == null)
        {
            return;
        }

        List<Vector2Int> emptyCells = FindEmptySpawnCells();
        Shuffle(emptyCells);

        int spawnCount = Mathf.Min(
            Mathf.Max(requestedCount, 0),
            emptyCells.Count
        );

        List<Block> spawnedBlocks = new List<Block>();

        for (int i = 0; i < spawnCount; i++)
        {
            Vector2Int cell = emptyCells[i];
            Block bomb = SpawnPatternBlock(
                activeBossDefinition.ReactorBombDefinition,
                'X',
                cell.x,
                cell.y,
                cell.y,
                activeBossDefinition.ReactorBombHealth,
                0
            );

            if (bomb == null)
            {
                continue;
            }

            bomb.Destroyed += HandleReactorBombDestroyed;
            reactorBombs.Add(bomb);
            encounterBlocks.Add(bomb);
            spawnedBlocks.Add(bomb);
        }

        blockGridManager.RegisterBossEncounterBlocks(
            spawnedBlocks
        );
    }

    private void HandleReactorBossHitReceived(
        Block boss,
        int unusedDamage)
    {
        if (boss == null ||
            boss != currentBossBlock ||
            activeBossDefinition == null ||
            !activeBossDefinition.IsBombReactor)
        {
            return;
        }

        boss.TakeScriptedDamage(1);

        if (!boss.IsAlive)
        {
            isBossDefeatPending = true;
            TryCompletePendingEncounter();
        }
    }

    private void SpawnReactorBlockers()
    {
        if (currentBossBlock == null ||
            activeBossDefinition == null ||
            activeBossDefinition.ReactorBlockerDefinition == null)
        {
            return;
        }

        List<Block> spawnedBlocks = new List<Block>();
        List<Vector2Int> emptyCells = FindEmptySpawnCells();
        Shuffle(emptyCells);

        List<Vector2Int> blockerCells =
            SelectSeparatedReactorBlockerCells(
                emptyCells,
                activeBossDefinition.ReactorBlockerCount
            );

        for (int i = 0; i < blockerCells.Count; i++)
        {
            Vector2Int cell = blockerCells[i];

            Block blocker = SpawnPatternBlock(
                activeBossDefinition.ReactorBlockerDefinition,
                '#',
                cell.x,
                cell.y,
                cell.y,
                1,
                0
            );

            if (blocker == null)
            {
                continue;
            }

            reactorBlockers.Add(blocker);
            encounterBlocks.Add(blocker);
            spawnedBlocks.Add(blocker);
        }

        blockGridManager.RegisterBossEncounterBlocks(spawnedBlocks);
    }

    private static List<Vector2Int>
        SelectSeparatedReactorBlockerCells(
            IReadOnlyList<Vector2Int> candidates,
            int requestedCount)
    {
        List<Vector2Int> bestSelection = new List<Vector2Int>();

        if (candidates == null || requestedCount <= 0)
        {
            return bestSelection;
        }

        const int maximumAttempts = 24;

        for (int attempt = 0;
             attempt < maximumAttempts;
             attempt++)
        {
            List<Vector2Int> shuffledCandidates =
                new List<Vector2Int>();

            for (int i = 0; i < candidates.Count; i++)
            {
                shuffledCandidates.Add(candidates[i]);
            }

            Shuffle(shuffledCandidates);
            List<Vector2Int> selected = new List<Vector2Int>();

            for (int i = 0;
                 i < shuffledCandidates.Count &&
                 selected.Count < requestedCount;
                 i++)
            {
                Vector2Int candidate = shuffledCandidates[i];
                bool touchesSelectedCell = false;

                for (int selectedIndex = 0;
                     selectedIndex < selected.Count;
                     selectedIndex++)
                {
                    Vector2Int existing = selected[selectedIndex];

                    if (Mathf.Abs(candidate.x - existing.x) <= 1 &&
                        Mathf.Abs(candidate.y - existing.y) <= 1)
                    {
                        touchesSelectedCell = true;
                        break;
                    }
                }

                if (!touchesSelectedCell)
                {
                    selected.Add(candidate);
                }
            }

            if (selected.Count > bestSelection.Count)
            {
                bestSelection = selected;
            }

            if (bestSelection.Count >= requestedCount)
            {
                break;
            }
        }

        return bestSelection;
    }

    private void HandleReactorBombDestroyed(
        Block bomb)
    {
        if (bomb == null ||
            activeBossDefinition == null ||
            !activeBossDefinition.IsBombReactor ||
            !reactorBombs.Remove(bomb))
        {
            return;
        }

        bomb.Destroyed -= HandleReactorBombDestroyed;
        destroyedReactorBombCount++;

        bool isRootExplosion = reactorExplosionDepth == 0;
        if (isRootExplosion)
        {
            reactorBossHitAxisMask = 0;
            reactorCrossLockTriggered = false;
        }

        reactorExplosionDepth++;
        ResolveReactorBombExplosion(bomb);
        reactorExplosionDepth--;

        if (currentBossBlock != null &&
            currentBossBlock.IsAlive)
        {
            return;
        }

        isBossDefeatPending = true;
        TryCompletePendingEncounter();
    }

    private void ResolveReactorBombExplosion(
        Block bomb)
    {
        if (bomb == null || !bomb.HasGridPosition)
        {
            return;
        }

        Vector2Int center = bomb.GridPosition;

        GameObject effectObject = new GameObject(
            "ReactorCrossBlastEffect"
        );

        BossReactorBlastLineEffect lineEffect =
            effectObject.AddComponent<
                BossReactorBlastLineEffect
            >();

        lineEffect.Play(
            boardGrid,
            GetReactorBlastEndpoint(center, Vector2Int.left),
            GetReactorBlastEndpoint(center, Vector2Int.right),
            GetReactorBlastEndpoint(center, Vector2Int.down),
            GetReactorBlastEndpoint(center, Vector2Int.up)
        );

        List<Block> targets = new List<Block>();

        for (int i = 0; i < encounterBlocks.Count; i++)
        {
            Block target = encounterBlocks[i];

            if (target == null ||
                target == bomb ||
                !target.IsAlive ||
                !target.HasGridPosition)
            {
                continue;
            }

            if (TryGetReactorBlastAxis(
                    center,
                    target,
                    out _))
            {
                targets.Add(target);
            }
        }

        for (int i = 0; i < targets.Count; i++)
        {
            Block target = targets[i];

            if (target == null || !target.IsAlive)
            {
                continue;
            }

            if (target == currentBossBlock)
            {
                TryGetReactorBlastAxis(
                    center,
                    target,
                    out int hitAxis
                );

                reactorBossHitAxisMask |= hitAxis;

                int bossDamage =
                    activeBossDefinition.ReactorBombDamage;

                if (!reactorCrossLockTriggered &&
                    reactorBossHitAxisMask == 3)
                {
                    reactorCrossLockTriggered = true;
                    bossDamage +=
                        activeBossDefinition.ReactorCrossLockBonusDamage;

                    turnsUntilBossAttack +=
                        activeBossDefinition.ReactorCrossLockAttackDelay;

                    NotifyBossAttackTurnsChanged();
                }

                target.TakeScriptedDamage(bossDamage);
                continue;
            }

            target.TakeDamage(
                activeBossDefinition.ReactorExplosionBlockDamage
            );
        }
    }

    private bool TryGetReactorBlastAxis(
        Vector2Int center,
        Block target,
        out int hitAxis)
    {
        hitAxis = 0;

        if (target == null || !target.HasGridPosition)
        {
            return false;
        }

        Vector2Int origin = target.GridPosition;
        Vector2Int size = target.GridSize;

        for (int rowOffset = 0; rowOffset < size.y; rowOffset++)
        {
            for (int columnOffset = 0;
                 columnOffset < size.x;
                 columnOffset++)
            {
                Vector2Int cell = origin + new Vector2Int(
                    columnOffset,
                    rowOffset
                );

                if (cell.y == center.y &&
                    !IsReactorBlastBlocked(center, cell))
                {
                    hitAxis |= 1;
                }

                if (cell.x == center.x &&
                    !IsReactorBlastBlocked(center, cell))
                {
                    hitAxis |= 2;
                }
            }
        }

        return hitAxis != 0;
    }

    private bool IsReactorBlastBlocked(
        Vector2Int center,
        Vector2Int target)
    {
        for (int i = 0; i < reactorBlockers.Count; i++)
        {
            Block blocker = reactorBlockers[i];

            if (blocker == null ||
                !blocker.IsAlive ||
                !blocker.HasGridPosition)
            {
                continue;
            }

            Vector2Int blockerCell = blocker.GridPosition;

            if (target.y == center.y &&
                blockerCell.y == center.y &&
                IsStrictlyBetween(
                    blockerCell.x,
                    center.x,
                    target.x))
            {
                return true;
            }

            if (target.x == center.x &&
                blockerCell.x == center.x &&
                IsStrictlyBetween(
                    blockerCell.y,
                    center.y,
                    target.y))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsStrictlyBetween(
        int value,
        int first,
        int second)
    {
        return value > Mathf.Min(first, second) &&
               value < Mathf.Max(first, second);
    }

    private Vector2Int GetReactorBlastEndpoint(
        Vector2Int center,
        Vector2Int direction)
    {
        Vector2Int endpoint = center;

        while (true)
        {
            Vector2Int next = endpoint + direction;
            if (next.x < 0 ||
                next.x >= boardGrid.ColumnCount ||
                next.y < 0 ||
                next.y >= boardGrid.RowCount)
            {
                return endpoint;
            }

            endpoint = next;

            for (int i = 0; i < reactorBlockers.Count; i++)
            {
                Block blocker = reactorBlockers[i];
                if (blocker != null &&
                    blocker.IsAlive &&
                    blocker.OccupiesCell(endpoint))
                {
                    return endpoint;
                }
            }
        }
    }

    private void SpawnTrapMasterSetup()
    {
        SpawnTrapTerrain(
            activeBossDefinition.TrapTerrainCount
        );

        SpawnTrapBlocks(
            activeBossDefinition.TrapInitialCount,
            true
        );

        SpawnTrapAmplifier();
    }

    private void SpawnBossArenaNormalBlocks(int requestedCount)
    {
        BlockDefinition definition =
            activeBossDefinition.ArenaNormalBlockDefinition;

        if (definition == null ||
            requestedCount <= 0)
        {
            return;
        }

        bossArenaNormalBlocks.RemoveAll(
            block => block == null || !block.IsAlive
        );

        int availableCount = Mathf.Max(
            activeBossDefinition.ArenaNormalBlockCount -
            bossArenaNormalBlocks.Count,
            0
        );

        List<Vector2Int> cells = FindEmptySpawnCells();
        Shuffle(cells);

        int count = Mathf.Min(
            Mathf.Min(requestedCount, availableCount),
            cells.Count
        );

        List<Block> spawned = new List<Block>();

        for (int i = 0; i < count; i++)
        {
            Vector2Int cell = cells[i];
            Block block = SpawnPatternBlock(
                definition,
                'X',
                cell.x,
                cell.y,
                cell.y,
                activeBossDefinition.ArenaNormalBlockHealth,
                0
            );

            if (block == null)
            {
                continue;
            }

            encounterBlocks.Add(block);
            bossArenaNormalBlocks.Add(block);
            spawned.Add(block);
        }

        blockGridManager.RegisterBossEncounterBlocks(spawned);
    }

    private void AdvanceBossArenaNormalRegeneration()
    {
        bossArenaNormalRegenerationTurns++;

        if (bossArenaNormalRegenerationTurns <
            activeBossDefinition.ArenaNormalRegenerationIntervalTurns)
        {
            return;
        }

        bossArenaNormalRegenerationTurns = 0;

        SpawnBossArenaNormalBlocks(
            activeBossDefinition.ArenaNormalRegenerationCount
        );
    }

    private void SpawnTrapTerrain(int requestedCount)
    {
        BlockDefinition definition =
            activeBossDefinition.TrapTerrainDefinition;

        if (definition == null || requestedCount <= 0)
        {
            return;
        }

        List<Vector2Int> cells = FindEmptySpawnCells();
        Shuffle(cells);
        int count = Mathf.Min(requestedCount, cells.Count);
        List<Block> spawned = new List<Block>();

        for (int i = 0; i < count; i++)
        {
            Vector2Int cell = cells[i];
            Block terrain = SpawnPatternBlock(
                definition,
                '#',
                cell.x,
                cell.y,
                cell.y,
                1,
                0
            );

            if (terrain == null)
            {
                continue;
            }

            trapTerrainBlocks.Add(terrain);
            encounterBlocks.Add(terrain);
            spawned.Add(terrain);
        }

        blockGridManager.RegisterBossEncounterBlocks(spawned);
    }

    private void SpawnTrapBlocks(
        int requestedCount,
        bool allowLifeTrap)
    {
        if (requestedCount <= 0 ||
            activeBossDefinition.TrapDefinitionCount <= 0)
        {
            return;
        }

        trapBlocks.RemoveAll(
            trap => trap == null || !trap.IsAlive
        );

        int availableCount = Mathf.Max(
            activeBossDefinition.TrapMaximumCount -
            trapBlocks.Count,
            0
        );

        int spawnCount = Mathf.Min(requestedCount, availableCount);
        List<Vector2Int> cells = FindEmptySpawnCells();
        Shuffle(cells);
        spawnCount = Mathf.Min(spawnCount, cells.Count);

        int trapHealth = GetTrapHealth(
            activeBossDefinition.TrapHealthBallRatio
        );

        List<Block> spawned = new List<Block>();

        for (int i = 0; i < spawnCount; i++)
        {
            BlockDefinition definition =
                SelectRandomTrapDefinition(allowLifeTrap);

            if (definition == null)
            {
                continue;
            }

            Vector2Int cell = cells[i];
            Block trap = SpawnPatternBlock(
                definition,
                'X',
                cell.x,
                cell.y,
                cell.y,
                trapHealth,
                0
            );

            if (trap == null)
            {
                continue;
            }

            trap.Destroyed += HandleTrapDestroyed;
            trapBlocks.Add(trap);
            encounterBlocks.Add(trap);
            spawned.Add(trap);
        }

        blockGridManager.RegisterBossEncounterBlocks(spawned);
    }

    private BlockDefinition SelectRandomTrapDefinition(
        bool allowLifeTrap)
    {
        List<BlockDefinition> candidates =
            new List<BlockDefinition>();

        int livingLifeTrapCount = 0;
        BlockDefinition lifeDefinition =
            activeBossDefinition.GetTrapDefinition(0);

        for (int i = 0; i < trapBlocks.Count; i++)
        {
            Block trap = trapBlocks[i];
            if (trap != null &&
                trap.IsAlive &&
                trap.Definition == lifeDefinition)
            {
                livingLifeTrapCount++;
            }
        }

        for (int i = 0;
             i < activeBossDefinition.TrapDefinitionCount;
             i++)
        {
            BlockDefinition definition =
                activeBossDefinition.GetTrapDefinition(i);

            if (definition == null ||
                (i == 0 &&
                 (!allowLifeTrap || livingLifeTrapCount >= 2)))
            {
                continue;
            }

            candidates.Add(definition);
        }

        return candidates.Count > 0
            ? candidates[UnityEngine.Random.Range(0, candidates.Count)]
            : null;
    }

    private void SpawnTrapAmplifier()
    {
        BlockDefinition definition =
            activeBossDefinition.TrapAmplifierDefinition;

        if (definition == null)
        {
            return;
        }

        List<Vector2Int> cells = FindEmptySpawnCells();
        Shuffle(cells);

        if (cells.Count <= 0)
        {
            return;
        }

        Vector2Int cell = cells[0];
        int trapHealth = GetTrapHealth(
            activeBossDefinition.TrapAmplifierHealthBallRatio
        );

        Block amplifier = SpawnPatternBlock(
            definition,
            'X',
            cell.x,
            cell.y,
            cell.y,
            trapHealth,
            0
        );

        if (amplifier == null)
        {
            return;
        }

        amplifier.Destroyed += HandleTrapAmplifierDestroyed;
        trapAmplifierBlock = amplifier;
        encounterBlocks.Add(amplifier);
        blockGridManager.RegisterBossEncounterBlocks(
            new List<Block> { amplifier }
        );
    }

    private int GetTrapHealth(float ballRatio)
    {
        int minimumBallCount = ballCollection != null
            ? ballCollection.StartingBallCount
            : 15;

        return Mathf.Max(
            Mathf.CeilToInt(minimumBallCount * ballRatio),
            1
        );
    }

    private void HandleTrapDestroyed(Block trap)
    {
        if (trap == null ||
            activeBossDefinition == null ||
            !activeBossDefinition.IsTrapMaster ||
            !trapBlocks.Remove(trap))
        {
            return;
        }

        trap.Destroyed -= HandleTrapDestroyed;
        BlockDefinition definition = trap.Definition;

        if (definition == activeBossDefinition.GetTrapDefinition(0))
        {
            currentBossBlock?.IncreaseMaxHealthAndHeal(
                activeBossDefinition.TrapBossHealthIncrease
            );
        }
        else if (definition == activeBossDefinition.GetTrapDefinition(1))
        {
            if (ballSealController != null && ballCollection != null)
            {
                float ratio = ballSealController.HasPendingSeal
                    ? Mathf.Min(
                        activeBossDefinition.TrapBallSealRatio * 2f,
                        0.4f)
                    : activeBossDefinition.TrapBallSealRatio;

                ballSealController.ApplySealRatio(
                    ratio,
                    ballCollection.Count
                );
            }
        }
        else if (definition == activeBossDefinition.GetTrapDefinition(2))
        {
            MoveTrapMasterBossToRandomCell();
        }
        else if (definition == activeBossDefinition.GetTrapDefinition(3))
        {
            SpawnTrapBlocks(2, false);
        }
        else if (definition == activeBossDefinition.GetTrapDefinition(4))
        {
            SpawnTemporaryTrapWalls(2);
        }
    }

    private void HandleTrapAmplifierDestroyed(Block amplifier)
    {
        if (amplifier == null ||
            activeBossDefinition == null ||
            !activeBossDefinition.IsTrapMaster ||
            amplifier != trapAmplifierBlock)
        {
            return;
        }

        amplifier.Destroyed -= HandleTrapAmplifierDestroyed;
        trapAmplifierBlock = null;

        if (currentBossBlock != null && currentBossBlock.IsAlive)
        {
            int shieldCount =
                activeBossDefinition.TrapAmplifierBaseShield +
                GetCurrentStageNumber();

            currentBossBlock.AddShield(
                shieldCount,
                shieldCount
            );
        }
    }

    private void SpawnTemporaryTrapWalls(int requestedCount)
    {
        BlockDefinition definition =
            activeBossDefinition.TrapTerrainDefinition;

        if (definition == null || currentBossBlock == null)
        {
            return;
        }

        Vector2Int bossCell = currentBossBlock.GridPosition;
        List<Vector2Int> candidates = new List<Vector2Int>
        {
            bossCell + Vector2Int.left,
            bossCell + Vector2Int.right,
            bossCell + Vector2Int.up,
            bossCell + Vector2Int.down
        };

        Shuffle(candidates);
        List<Block> spawned = new List<Block>();

        for (int i = 0;
             i < candidates.Count && spawned.Count < requestedCount;
             i++)
        {
            Vector2Int cell = candidates[i];

            if (cell.x < 0 ||
                cell.x >= boardGrid.ColumnCount ||
                cell.y < 0 ||
                cell.y >= boardGrid.RowCount ||
                IsEncounterCellOccupied(cell))
            {
                continue;
            }

            Block wall = SpawnPatternBlock(
                definition,
                '#',
                cell.x,
                cell.y,
                cell.y,
                1,
                0
            );

            if (wall == null)
            {
                continue;
            }

            temporaryTrapWalls.Add(wall);
            encounterBlocks.Add(wall);
            spawned.Add(wall);
        }

        blockGridManager.RegisterBossEncounterBlocks(spawned);
    }

    private bool IsEncounterCellOccupied(Vector2Int cell)
    {
        for (int i = 0; i < encounterBlocks.Count; i++)
        {
            Block block = encounterBlocks[i];
            if (block != null &&
                block.IsAlive &&
                block.OccupiesCell(cell))
            {
                return true;
            }
        }

        return false;
    }

    private bool TrySpawnColonyPattern(
        BossPatternDefinition pattern)
    {
        BlockDefinition coreDefinition =
            pattern.BossDefinition;

        BlockDefinition growthDefinition =
            pattern.BreakablePatternDefinition;

        if (coreDefinition == null ||
            growthDefinition == null)
        {
            Debug.LogError(
                "BossEncounterController: 증식형 보스의 " +
                "핵 또는 증식 블록 Definition이 비어 있습니다.",
                pattern
            );

            return false;
        }

        if (coreDefinition.GridSize != Vector2Int.one ||
            growthDefinition.GridSize != Vector2Int.one)
        {
            Debug.LogError(
                "BossEncounterController: 현재 증식형 보스는 " +
                "1×1 핵과 증식 블록만 지원합니다.",
                pattern
            );

            return false;
        }

        int stageNumber =
            GetCurrentStageNumber();

        int requestedCoreCount =
            activeBossDefinition
                .CalculateColonyCoreCount(
                    stageNumber
                );

        List<Vector2Int> coreCells =
            SelectColonyCoreCells(
                requestedCoreCount,
                activeBossDefinition
                    .ColonyMinimumCoreDistance
            );

        if (coreCells.Count < requestedCoreCount)
        {
            Debug.LogError(
                "BossEncounterController: 증식형 보스 핵을 " +
                $"{requestedCoreCount}개 배치하지 못했습니다. " +
                $"배치 성공={coreCells.Count}",
                this
            );

            return false;
        }

        colonyGrowthState.Clear();

        for (int i = 0;
             i < coreCells.Count;
             i++)
        {
            Vector2Int cell = coreCells[i];

            int health =
                GetHealthForSymbol(
                    pattern,
                    'B',
                    coreDefinition
                );

            Block core =
                SpawnPatternBlock(
                    coreDefinition,
                    'B',
                    cell.x,
                    cell.y,
                    cell.y,
                    health,
                    pattern.BossAttackPower
                );

            if (core == null)
            {
                continue;
            }

            core.name =
                $"BossColony_Core_{i + 1}";

            encounterBlocks.Add(core);
            colonyGrowthState.RegisterMember(core);

            if (currentBossBlock == null)
            {
                currentBossBlock = core;
            }
        }

        blockGridManager.RegisterBossEncounterBlocks(
            encounterBlocks
        );

        return blockGridManager.RequiredEnemyCount > 0;
    }

    private List<Vector2Int> SelectColonyCoreCells(
        int requestedCount,
        int preferredMinimumDistance)
    {
        List<Vector2Int> candidates =
            new List<Vector2Int>();

        List<Vector2Int> selected =
            new List<Vector2Int>();

        if (boardGrid == null ||
            requestedCount <= 0)
        {
            return selected;
        }

        int maximumSpawnRow =
            GetMaximumBossSpawnRow();

        for (int row = 1;
             row <= maximumSpawnRow;
             row++)
        {
            for (int column = 1;
                 column < boardGrid.ColumnCount - 1;
                 column++)
            {
                candidates.Add(
                    new Vector2Int(
                        column,
                        row
                    )
                );
            }
        }

        Shuffle(candidates);

        for (int minimumDistance =
                 Mathf.Max(preferredMinimumDistance, 0);
             minimumDistance >= 0;
             minimumDistance--)
        {
            selected.Clear();

            for (int i = 0;
                 i < candidates.Count &&
                 selected.Count < requestedCount;
                 i++)
            {
                Vector2Int candidate =
                    candidates[i];

                bool isFarEnough = true;

                for (int selectedIndex = 0;
                     selectedIndex < selected.Count;
                     selectedIndex++)
                {
                    Vector2Int difference =
                        candidate -
                        selected[selectedIndex];

                    int distance =
                        Mathf.Abs(difference.x) +
                        Mathf.Abs(difference.y);

                    if (distance < minimumDistance)
                    {
                        isFarEnough = false;
                        break;
                    }
                }

                if (isFarEnough)
                {
                    selected.Add(candidate);
                }
            }

            if (selected.Count >= requestedCount)
            {
                break;
            }
        }

        return selected;
    }

    private IEnumerator RunInitialColonyGrowthRoutine()
    {
        int initialGrowthCount =
            activeBossDefinition
                .ColonyInitialGrowthCount;

        for (int growthIndex = 0;
             growthIndex < initialGrowthCount;
             growthIndex++)
        {
            PrepareColonyGrowthPreview();

            if (colonyGrowthState.Reservations.Count <= 0)
            {
                break;
            }

            float delay =
                activeBossDefinition
                    .ColonyInitialGrowthStepDelay;

            if (delay > 0f)
            {
                yield return
                    new WaitForSeconds(delay);
            }
            else
            {
                yield return null;
            }

            SpawnReservedColonyGrowthBlocks();
            yield return null;
        }

        PrepareColonyGrowthPreview();
    }

    private void PrepareColonyGrowthPreview()
    {
        HideGrowthOutlines();

        colonyGrowthState.PrepareReservations(
            boardGrid,
            encounterBlocks,
            GetMaximumBossSpawnRow()
        );

        IReadOnlyList<BossColonyGrowthReservation>
            reservations =
                colonyGrowthState.Reservations;

        for (int i = 0;
             i < reservations.Count;
             i++)
        {
            BossColonyGrowthReservation reservation =
                reservations[i];

            if (reservation == null)
            {
                continue;
            }

            GameObject outlineObject =
                new GameObject(
                    $"BossGrowthPreview_" +
                    $"R{reservation.Cell.y}_" +
                    $"C{reservation.Cell.x}"
                );

            outlineObject.transform.SetParent(
                bossBlockContainer,
                false
            );

            BossGrowthCellOutline outline =
                outlineObject.AddComponent<
                    BossGrowthCellOutline>();

            outline.Configure(
                boardGrid,
                reservation.Cell,
                activeBossDefinition
                    .ColonyGrowthOutlineColor,
                reservation.Parent
            );

            growthOutlines.Add(outline);
        }
    }

    private void SpawnReservedColonyGrowthBlocks()
    {
        HideGrowthOutlines();

        IReadOnlyList<BossColonyGrowthReservation>
            reservations =
                colonyGrowthState.Reservations;

        List<Block> spawnedBlocks =
            new List<Block>();

        BlockDefinition growthDefinition =
            activePattern != null
                ? activePattern
                    .BreakablePatternDefinition
                : null;

        if (growthDefinition == null)
        {
            colonyGrowthState.ClearReservations();
            return;
        }

        for (int i = 0;
             i < reservations.Count;
             i++)
        {
            BossColonyGrowthReservation reservation =
                reservations[i];

            if (!colonyGrowthState.CanResolve(
                    reservation) ||
                IsCellOccupied(
                    reservation.Cell))
            {
                continue;
            }

            bool spawnSpecial =
                UnityEngine.Random.value <
                activeBossDefinition.SpecialBlockSpawnChance;

            BlockDefinition selectedDefinition =
                spawnSpecial
                    ? activeBossDefinition
                        .GetRandomSpecialBlockDefinition()
                    : null;

            char symbol = selectedDefinition != null
                ? 'S'
                : 'X';

            if (selectedDefinition == null)
            {
                selectedDefinition = growthDefinition;
            }

            int health =
                activeBossDefinition
                    .CalculateColonyGrowthBlockHealth(
                        GetColonyBossHealth()
                    );

            Block child =
                SpawnPatternBlock(
                    selectedDefinition,
                    symbol,
                    reservation.Cell.x,
                    reservation.Cell.y,
                    reservation.Cell.y,
                    health,
                    0
                );

            if (child == null)
            {
                continue;
            }

            child.name =
                $"BossColony_Growth_" +
                $"R{reservation.Cell.y}_" +
                $"C{reservation.Cell.x}";

            encounterBlocks.Add(child);
            spawnedBlocks.Add(child);
            colonyGrowthState.RegisterMember(child);
        }

        for (int i = 0;
             i < entranceItems.Count;
             i++)
        {
            entranceItems[i]?.Complete();
        }

        entranceItems.Clear();
        colonyGrowthState.ClearReservations();

        blockGridManager.RegisterBossEncounterBlocks(
            spawnedBlocks
        );
    }

    private bool IsCellOccupied(
        Vector2Int cell)
    {
        for (int i = 0;
             i < encounterBlocks.Count;
             i++)
        {
            Block block = encounterBlocks[i];

            if (block != null &&
                block.IsAlive &&
                block.OccupiesCell(
                    cell.x,
                    cell.y
                ))
            {
                return true;
            }
        }

        return false;
    }

    private int GetMaximumBossSpawnRow()
    {
        if (boardGrid == null)
        {
            return 0;
        }

        float maximumRowRatio =
            activeBossDefinition != null
                ? activeBossDefinition
                    .ColonyMaximumSpawnRowRatio
                : 0.5f;

        return Mathf.Clamp(
            Mathf.FloorToInt(
                (boardGrid.RowCount - 1) *
                maximumRowRatio
            ),
            0,
            Mathf.Max(boardGrid.RowCount - 1, 0)
        );
    }

    private int GetColonyBossHealth()
    {
        if (activePattern == null)
        {
            return 1;
        }

        return GetHealthForSymbol(
            activePattern,
            'B',
            activePattern.BossDefinition
        );
    }

    private int GetCurrentStageNumber()
    {
        return roomNavigator != null &&
               roomNavigator.CurrentMap != null
            ? roomNavigator.CurrentMap.StageNumber
            : 1;
    }

    private void HideGrowthOutlines()
    {
        for (int i = 0;
             i < growthOutlines.Count;
             i++)
        {
            BossGrowthCellOutline outline =
                growthOutlines[i];

            if (outline != null)
            {
                Destroy(outline.gameObject);
            }
        }

        growthOutlines.Clear();
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

        if (bossCatalog != null &&
            bossCatalog.TryGetTestBoss(
                out activeBossDefinition))
        {
            activePattern =
                activeBossDefinition.PatternDefinition;

            Debug.LogWarning(
                "BossEncounterController: Boss Catalog 테스트 오버라이드로 " +
                $"'{activeBossDefinition.DisplayName}' 보스를 사용합니다.",
                bossCatalog
            );
        }
        else if (bossRunSequence != null &&
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

        if (activeBossDefinition != null &&
            activeBossDefinition.IsDescendingWave)
        {
            return true;
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
            blockGridManager == null)
        {
            yield break;
        }

        if (activeBossDefinition.IsDescendingWave)
        {
            yield return ResolveDescendingWaveTurnRoutine();
            yield break;
        }

        if (activeBossDefinition.IsBombReactor)
        {
            yield return ResolveBombReactorTurnRoutine();
            yield break;
        }

        if (activeBossDefinition.IsTrapMaster)
        {
            yield return ResolveTrapMasterTurnRoutine();
            yield break;
        }

        if (blockGridManager.RequiredEnemyCount <= 0)
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

        if (activeBossDefinition.AttackCount <= 0)
        {
            turnsUntilBossAttack = 0;
            NotifyBossAttackTurnsChanged();
            yield break;
        }

        if (activeBossDefinition.IsProliferatingColony)
        {
            SpawnReservedColonyGrowthBlocks();

            if (blockGridManager.RequiredEnemyCount <= 0)
            {
                yield break;
            }

            PrepareColonyGrowthPreview();
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

        if (attack.AttackType ==
            BossAttackType.ColonyDoubleGrowth)
        {
            yield return ResolveColonyDoubleGrowthRoutine(
                attack.TelegraphDuration
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

    private IEnumerator ResolveDescendingWaveTurnRoutine()
    {
        int rowCount = UnityEngine.Random.Range(
            activeBossDefinition.DescendingMinimumRowCount,
            activeBossDefinition.DescendingMaximumRowCount + 1
        );

        int movementRowCount =
            blockGridManager.GetBossEncounterWaveLayoutRowCount(
                rowCount
            );

        yield return blockGridManager
            .MoveBossEncounterBlocksDownRoutine(
                movementRowCount
            );

        descendingTurnsResolved++;

        DescendingWavesRemainingChanged?.Invoke(
            RemainingDescendingWaves
        );

        if (turnManager != null && turnManager.IsGameOver)
        {
            yield break;
        }

        if (descendingTurnsResolved >=
            activeBossDefinition.DescendingWaveCount)
        {
            isBossDefeatPending = true;

            Debug.Log(
                "BossEncounterController: " +
                $"하강 웨이브 {activeBossDefinition.DescendingWaveCount}회 생존 완료",
                this
            );

            TryCompletePendingEncounter();
            yield break;
        }

        if (!TrySpawnDescendingWave(rowCount))
        {
            Debug.LogError(
                "BossEncounterController: 다음 하강 웨이브 생성에 실패했습니다.",
                this
            );
        }
    }

    private IEnumerator ResolveBombReactorTurnRoutine()
    {
        reactorBombs.RemoveAll(
            bomb => bomb == null || !bomb.IsAlive
        );

        turnsUntilBossAttack = Mathf.Max(
            turnsUntilBossAttack - 1,
            0
        );

        NotifyBossAttackTurnsChanged();

        if (turnsUntilBossAttack <= 0 &&
            currentBossBlock != null &&
            currentBossBlock.IsAlive)
        {
            int attackDamage =
                activeBossDefinition.ReactorBaseAttackDamage +
                reactorBombs.Count;

            yield return enemyAttackSequence.ResolveAttackRoutine(
                new List<Block> { currentBossBlock },
                attackDamage
            );

            turnsUntilBossAttack =
                activeBossDefinition.AttackIntervalTurns;

            NotifyBossAttackTurnsChanged();

            if (turnManager != null && turnManager.IsGameOver)
            {
                yield break;
            }
        }

        MoveReactorBossToRandomEmptyCell();

        AdvanceBossArenaNormalRegeneration();

        SpawnReactorBombs(
            activeBossDefinition.ReactorBombsPerTurn
        );

        if (entranceItems.Count > 0)
        {
            yield return entranceAnimator.PlayRoutine(
                entranceItems
            );

            entranceItems.Clear();
        }
    }

    private IEnumerator ResolveTrapMasterTurnRoutine()
    {
        ClearTemporaryTrapWalls();
        MoveTrapMasterBossToRandomCell();
        AdvanceBossArenaNormalRegeneration();

        SpawnTrapBlocks(
            activeBossDefinition.TrapSpawnCountPerTurn,
            true
        );

        if (entranceItems.Count > 0)
        {
            yield return entranceAnimator.PlayRoutine(
                entranceItems
            );

            entranceItems.Clear();
        }
    }

    private void MoveTrapMasterBossToRandomCell()
    {
        if (currentBossBlock == null ||
            !currentBossBlock.IsAlive ||
            boardGrid == null)
        {
            return;
        }

        Vector2Int size = currentBossBlock.GridSize;
        int maximumRow = Mathf.Min(
            boardGrid.RowCount - size.y - 1,
            Mathf.FloorToInt(boardGrid.RowCount * 0.7f)
        );

        int maximumColumn = boardGrid.ColumnCount - size.x;
        List<Vector2Int> candidates = new List<Vector2Int>();

        for (int row = 2; row <= maximumRow; row++)
        {
            for (int column = 0; column <= maximumColumn; column++)
            {
                Vector2Int candidate = new Vector2Int(column, row);

                if (candidate != currentBossBlock.GridPosition &&
                    CanPlaceReactorBossAt(candidate, size))
                {
                    candidates.Add(candidate);
                }
            }
        }

        if (candidates.Count <= 0)
        {
            return;
        }

        Vector2Int target = candidates[
            UnityEngine.Random.Range(0, candidates.Count)
        ];

        currentBossBlock.SetGridPosition(
            boardGrid,
            target.x,
            target.y,
            true
        );
    }

    private void ClearTemporaryTrapWalls()
    {
        for (int i = temporaryTrapWalls.Count - 1; i >= 0; i--)
        {
            Block wall = temporaryTrapWalls[i];
            temporaryTrapWalls.RemoveAt(i);

            if (wall == null)
            {
                continue;
            }

            encounterBlocks.Remove(wall);
            wall.ExpireWithoutReward();
        }
    }

    private void MoveReactorBossToRandomEmptyCell()
    {
        if (currentBossBlock == null ||
            !currentBossBlock.IsAlive ||
            boardGrid == null)
        {
            return;
        }

        List<Vector2Int> candidates = new List<Vector2Int>();

        Vector2Int bossSize = currentBossBlock.GridSize;
        int maximumRow = boardGrid.RowCount - bossSize.y - 1;
        int maximumColumn = boardGrid.ColumnCount - bossSize.x;

        for (int row = 1; row <= maximumRow; row++)
        {
            for (int column = 0; column <= maximumColumn; column++)
            {
                Vector2Int candidate = new Vector2Int(column, row);

                if (candidate == currentBossBlock.GridPosition ||
                    !CanPlaceReactorBossAt(candidate, bossSize))
                {
                    continue;
                }

                candidates.Add(candidate);
            }
        }

        if (candidates.Count <= 0)
        {
            return;
        }

        Vector2Int target = candidates[
            UnityEngine.Random.Range(0, candidates.Count)
        ];

        currentBossBlock.SetGridPosition(
            boardGrid,
            target.x,
            target.y,
            true
        );

        MoveReactorBlockersToRandomEmptyCells();
    }

    private bool CanPlaceReactorBossAt(
        Vector2Int origin,
        Vector2Int size)
    {
        for (int rowOffset = 0; rowOffset < size.y; rowOffset++)
        {
            for (int columnOffset = 0;
                 columnOffset < size.x;
                 columnOffset++)
            {
                Vector2Int cell = origin + new Vector2Int(
                    columnOffset,
                    rowOffset
                );

                for (int i = 0; i < encounterBlocks.Count; i++)
                {
                    Block block = encounterBlocks[i];

                    if (block == null ||
                        block == currentBossBlock ||
                        reactorBlockers.Contains(block) ||
                        !block.IsAlive)
                    {
                        continue;
                    }

                    if (block.OccupiesCell(cell))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private void MoveReactorBlockersToRandomEmptyCells()
    {
        List<Vector2Int> emptyCells = new List<Vector2Int>();

        for (int row = 0; row < boardGrid.RowCount - 1; row++)
        {
            for (int column = 0;
                 column < boardGrid.ColumnCount;
                 column++)
            {
                Vector2Int cell = new Vector2Int(column, row);
                bool occupied = false;

                for (int i = 0; i < encounterBlocks.Count; i++)
                {
                    Block block = encounterBlocks[i];

                    if (block == null ||
                        reactorBlockers.Contains(block) ||
                        !block.IsAlive)
                    {
                        continue;
                    }

                    if (block.OccupiesCell(cell))
                    {
                        occupied = true;
                        break;
                    }
                }

                if (!occupied)
                {
                    emptyCells.Add(cell);
                }
            }
        }

        Shuffle(emptyCells);
        List<Vector2Int> blockerCells =
            SelectSeparatedReactorBlockerCells(
                emptyCells,
                reactorBlockers.Count
            );

        int nextCellIndex = 0;

        for (int i = 0; i < reactorBlockers.Count; i++)
        {
            Block blocker = reactorBlockers[i];

            if (blocker == null ||
                !blocker.IsAlive ||
                nextCellIndex >= blockerCells.Count)
            {
                continue;
            }

            Vector2Int cell = blockerCells[nextCellIndex++];
            blocker.SetGridPosition(
                boardGrid,
                cell.x,
                cell.y,
                true
            );
        }
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

            if (activeBossDefinition != null &&
                activeBossDefinition.IsProliferatingColony &&
                activePattern != null &&
                block.Definition == activePattern.BossDefinition)
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

    private IEnumerator ResolveColonyDoubleGrowthRoutine(
        float stepDelay)
    {
        if (stepDelay > 0f)
        {
            yield return new WaitForSeconds(stepDelay);
        }
        else
        {
            yield return null;
        }

        SpawnReservedColonyGrowthBlocks();

        if (blockGridManager.RequiredEnemyCount > 0)
        {
            PrepareColonyGrowthPreview();
        }
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
        HideGrowthOutlines();
        colonyGrowthState.Clear();

        if (activeBossDefinition != null &&
            activeBossDefinition.IsTrapMaster)
        {
            ballSealController?.ClearEncounterSeal();
        }

        if (currentBossBlock != null)
        {
            currentBossBlock.HitReceived -=
                HandleReactorBossHitReceived;
        }

        for (int i = 0; i < reactorBombs.Count; i++)
        {
            Block bomb = reactorBombs[i];
            if (bomb != null)
            {
                bomb.Destroyed -= HandleReactorBombDestroyed;
            }
        }

        reactorBombs.Clear();
        reactorBlockers.Clear();

        for (int i = 0; i < trapBlocks.Count; i++)
        {
            Block trap = trapBlocks[i];
            if (trap == null)
            {
                continue;
            }

            trap.Destroyed -= HandleTrapDestroyed;
            trap.Destroyed -= HandleTrapAmplifierDestroyed;
        }

        trapBlocks.Clear();

        if (trapAmplifierBlock != null)
        {
            trapAmplifierBlock.Destroyed -=
                HandleTrapAmplifierDestroyed;

            trapAmplifierBlock = null;
        }

        trapTerrainBlocks.Clear();
        temporaryTrapWalls.Clear();
        bossArenaNormalBlocks.Clear();

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

[DisallowMultipleComponent]
public sealed class BossReactorBlastLineEffect : MonoBehaviour
{
    private const float Duration = 0.28f;
    private LineRenderer horizontalLine;
    private LineRenderer verticalLine;
    private Material runtimeMaterial;
    private Color baseColor;
    private float elapsedTime;

    public void Play(
        BoardGrid grid,
        Vector2Int leftCell,
        Vector2Int rightCell,
        Vector2Int bottomCell,
        Vector2Int topCell)
    {
        if (grid == null)
        {
            Destroy(gameObject);
            return;
        }

        baseColor = new Color(1f, 0.04f, 0.02f, 0.95f);
        Shader shader = Shader.Find("Sprites/Default");

        if (shader != null)
        {
            runtimeMaterial = new Material(shader);
        }

        horizontalLine = CreateLine("HorizontalBlastLine", grid);
        verticalLine = CreateLine("VerticalBlastLine", grid);

        horizontalLine.SetPosition(0, grid.GetCellWorldPosition(leftCell.x, leftCell.y));
        horizontalLine.SetPosition(1, grid.GetCellWorldPosition(rightCell.x, rightCell.y));
        verticalLine.SetPosition(0, grid.GetCellWorldPosition(bottomCell.x, bottomCell.y));
        verticalLine.SetPosition(1, grid.GetCellWorldPosition(topCell.x, topCell.y));

        ApplyColor(baseColor);
    }

    private LineRenderer CreateLine(string lineName, BoardGrid grid)
    {
        GameObject lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.numCapVertices = 4;
        line.startWidth = grid.CellSize * 0.14f;
        line.endWidth = grid.CellSize * 0.14f;
        line.sortingOrder = 120;

        if (runtimeMaterial != null)
        {
            line.sharedMaterial = runtimeMaterial;
        }

        return line;
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsedTime / Duration);
        Color color = baseColor;
        color.a *= 1f - progress;
        ApplyColor(color);

        float width = Mathf.Lerp(1f, 0.25f, progress);
        ApplyWidth(horizontalLine, width);
        ApplyWidth(verticalLine, width);

        if (elapsedTime >= Duration)
        {
            Destroy(gameObject);
        }
    }

    private void ApplyColor(Color color)
    {
        if (horizontalLine != null)
        {
            horizontalLine.startColor = color;
            horizontalLine.endColor = color;
        }

        if (verticalLine != null)
        {
            verticalLine.startColor = color;
            verticalLine.endColor = color;
        }
    }

    private static void ApplyWidth(LineRenderer line, float width)
    {
        if (line != null)
        {
            line.widthMultiplier = width;
        }
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}
