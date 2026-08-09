using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BlockWaveGenerator))]
[RequireComponent(typeof(BlockGridMover))]
[RequireComponent(typeof(EnemyAttackCycle))]
[RequireComponent(typeof(EnemyAttackSequence))]
[RequireComponent(typeof(BlockElementSystem))]
public sealed class BlockGridManager :
    MonoBehaviour
{
    [Header("Components")]
    [SerializeField]
    private BlockWaveGenerator waveGenerator;

    [SerializeField]
    private BlockGridMover gridMover;

    [SerializeField]
    private EnemyAttackCycle enemyAttackCycle;

    [SerializeField]
    private EnemyAttackSequence enemyAttackSequence;

    [SerializeField]
    private BlockElementSystem blockElementSystem;

    [Header("Optional References")]
    [SerializeField]
    private BallSealController ballSealController;

    [Header("Wave Progression")]
    [SerializeField]
    private BlockWaveDirector waveDirector =
        new BlockWaveDirector();

    private readonly BlockRegistry blockRegistry =
        new BlockRegistry();

    private readonly BlockGridBossMode bossMode =
        new BlockGridBossMode();

    private BlockEnemyPhaseResolver
        enemyPhaseResolver;

    private int currentTurn;
    private int currentRoomId = -1;

    private bool isCurrentRoomCleared;
    private bool isRuntimeInitialized;

    private RoomCombatState currentRoomState =
        RoomCombatState.Unvisited;

    public int CurrentTurn =>
        currentTurn;

    public int CurrentStageNumber =>
        waveDirector != null
            ? waveDirector.CurrentStageNumber
            : 1;

    public int TotalStageCount =>
        waveDirector != null
            ? waveDirector.TotalStageCount
            : 1;

    public int CurrentWaveNumber =>
        waveDirector != null
            ? waveDirector.CurrentWaveNumber
            : 1;

    public int CurrentBossWaveNumber =>
        waveDirector != null
            ? waveDirector.BossWaveNumber
            : 10;

    public int TurnsUntilAttack =>
        enemyAttackCycle != null
            ? enemyAttackCycle.TurnsUntilAttack
            : 0;

    public bool IsBossEncounterActive =>
        bossMode.IsActive;

    public bool IsCurrentNamedWave =>
        waveDirector != null &&
        waveDirector.IsCurrentNamedWave;

    public bool IsFinalStage =>
        waveDirector != null &&
        waveDirector.IsFinalStage;

    public bool IsRunCompleted =>
        waveDirector != null &&
        waveDirector.IsRunCompleted;

    public int ActiveBlockCount =>
        blockRegistry.Count;

    public int RequiredEnemyCount =>
        blockRegistry.RequiredEnemyCount;

    public bool IsCurrentRoomCleared =>
        isCurrentRoomCleared;

    public RoomCombatState CurrentRoomState =>
        currentRoomState;

    public bool CanResetCurrentRoomCombat =>
        !bossMode.IsActive &&
        !IsRunCompleted &&
        !isCurrentRoomCleared &&
        currentRoomState ==
        RoomCombatState.InCombat &&
        waveGenerator != null &&
        waveGenerator.HasLastGeneratedWave;

    public IReadOnlyList<Block> ActiveBlocks =>
        blockRegistry.ActiveBlocks;

    public int CurrentTotalAttackPower
    {
        get
        {
            if (enemyAttackSequence == null ||
                bossMode.IsActive ||
                IsRunCompleted)
            {
                return 0;
            }

            blockRegistry.RemoveInvalidBlocks();

            return enemyAttackSequence
                .CalculateTotalAttackPower(
                    blockRegistry.ActiveBlocks
                );
        }
    }

    public event Action<int>
        TurnsUntilAttackChanged;

    public event Action<Block, int>
        EnemyAttackTriggered;

    public event Action<int>
        WaveGenerated;

    public event Action<int>
        StageStarted;

    public event Action
        RoomCleared;

    public event Action<RoomCombatState>
        RoomStateChanged;

    public event Action
        RoomCombatReset;

    public event Action
        BossEncounterRequested;

    public event Action
        RunCompleted;

    public event Action<IReadOnlyList<Block>>
        BlocksReachedBottom;

    public event Action<IReadOnlyList<Block>>
        BlocksExceededBottom;

    private void Awake()
    {
        EnsureServices();
        FindComponents();
        NormalizeSettings();
        ValidateReferences();
        CreateEnemyPhaseResolver();
        SubscribeEvents();
    }

    private void OnValidate()
    {
        EnsureServices();
        FindComponents();
        NormalizeSettings();
    }

    private void Start()
    {
        InitializeRuntimeIfNeeded();

        /*
         * 고정형 방 전투 전환:
         *
         * 게임 시작과 동시에 블록을 생성하지 않는다.
         * 실제 블록 생성은 StageRoomNavigator가
         * 전투방 입장을 알렸을 때 실행한다.
         */
        PrepareEmptyRoom();
    }

    private void EnsureServices()
    {
        if (waveDirector == null)
        {
            waveDirector =
                new BlockWaveDirector();
        }
    }

    private void NormalizeSettings()
    {
        waveDirector?.Normalize();
    }

    private void FindComponents()
    {
        if (waveGenerator == null)
        {
            waveGenerator =
                GetComponent<
                    BlockWaveGenerator
                >();
        }

        if (gridMover == null)
        {
            gridMover =
                GetComponent<
                    BlockGridMover
                >();
        }

        if (enemyAttackCycle == null)
        {
            enemyAttackCycle =
                GetComponent<
                    EnemyAttackCycle
                >();
        }

        if (enemyAttackSequence == null)
        {
            enemyAttackSequence =
                GetComponent<
                    EnemyAttackSequence
                >();
        }

        if (blockElementSystem == null)
        {
            blockElementSystem =
                GetComponent<
                    BlockElementSystem
                >();
        }

        if (ballSealController == null)
        {
            ballSealController =
                FindFirstObjectByType<
                    BallSealController
                >();
        }
    }

    private void ValidateReferences()
    {
        if (waveGenerator == null)
        {
            Debug.LogError(
                "BlockGridManager: " +
                "BlockWaveGenerator를 찾지 못했습니다.",
                this
            );
        }

        if (gridMover == null)
        {
            Debug.LogError(
                "BlockGridManager: " +
                "BlockGridMover를 찾지 못했습니다.",
                this
            );
        }

        if (enemyAttackCycle == null)
        {
            Debug.LogError(
                "BlockGridManager: " +
                "EnemyAttackCycle을 찾지 못했습니다.",
                this
            );
        }

        if (enemyAttackSequence == null)
        {
            Debug.LogError(
                "BlockGridManager: " +
                "EnemyAttackSequence를 찾지 못했습니다.",
                this
            );
        }

        if (blockElementSystem == null)
        {
            Debug.LogError(
                "BlockGridManager: " +
                "BlockElementSystem을 찾지 못했습니다.",
                this
            );
        }

        if (ballSealController == null)
        {
            Debug.LogWarning(
                "BlockGridManager: " +
                "BallSealController를 찾지 못했습니다. " +
                "보스전 및 스테이지 전환 시 " +
                "봉인을 자동 해제할 수 없습니다.",
                this
            );
        }
    }

    private void CreateEnemyPhaseResolver()
    {
        if (waveGenerator == null ||
            gridMover == null ||
            enemyAttackCycle == null ||
            enemyAttackSequence == null)
        {
            return;
        }

        enemyPhaseResolver =
            new BlockEnemyPhaseResolver(
                waveGenerator,
                gridMover,
                enemyAttackCycle,
                enemyAttackSequence,
                waveDirector,
                blockRegistry,
                () => bossMode.IsActive,
                TryRequestBossEncounter
            );
    }

    private bool CanInitialize()
    {
        if (waveGenerator == null ||
            gridMover == null ||
            enemyAttackCycle == null ||
            enemyAttackSequence == null ||
            blockElementSystem == null ||
            enemyPhaseResolver == null)
        {
            return false;
        }

        if (!waveGenerator.IsReady)
        {
            Debug.LogError(
                "BlockGridManager: " +
                "BlockWaveGenerator의 설정이 " +
                "완료되지 않았습니다.",
                this
            );

            return false;
        }

        return true;
    }

    private bool InitializeRuntimeIfNeeded()
    {
        if (isRuntimeInitialized)
        {
            return true;
        }

        if (!CanInitialize())
        {
            return false;
        }

        currentTurn = 0;
        isCurrentRoomCleared = false;

        bossMode.Reset();
        waveDirector.Initialize();
        enemyAttackCycle.InitializeCycle();

        ballSealController?.ClearPendingSeal();

        isRuntimeInitialized = true;

        StageStarted?.Invoke(
            CurrentStageNumber
        );

        return true;
    }

    private void SubscribeEvents()
    {
        if (enemyAttackCycle != null)
        {
            enemyAttackCycle
                .TurnsUntilAttackChanged +=
                HandleTurnsUntilAttackChanged;
        }

        if (enemyAttackSequence != null)
        {
            enemyAttackSequence
                .BlockAttackTriggered +=
                HandleBlockAttackTriggered;
        }

        if (gridMover != null)
        {
            gridMover.BottomRowReached +=
                HandleBlocksReachedBottom;

            gridMover.BottomBoundaryExceeded +=
                HandleBlocksExceededBottom;
        }

    }

    /*
     * StageRoomNavigator가 일반 또는 네임드
     * 전투방에 입장했을 때 호출한다.
     */
    public bool StartRoomCombat(
        RoomType roomType,
        int roomId)
    {
        if (roomType !=
                RoomType.NormalCombat &&
            roomType !=
                RoomType.NamedCombat)
        {
            Debug.LogWarning(
                "BlockGridManager: " +
                $"{roomType}은 현재 단계의 " +
                "전투 생성 대상이 아닙니다.",
                this
            );

            return false;
        }

        if (!InitializeRuntimeIfNeeded() ||
            IsRunCompleted ||
            bossMode.IsActive)
        {
            return false;
        }

        blockRegistry.ClearAndDestroy();

        currentTurn = 0;
        isCurrentRoomCleared = false;

        enemyAttackCycle.InitializeCycle();

        ballSealController?.ClearPendingSeal();

        currentRoomId = roomId;

        bool hasSavedRoomWave =
            waveGenerator.HasGeneratedRoomWave(
                roomId
            );

        List<Block> generatedBlocks =
            hasSavedRoomWave
                ? waveGenerator.RegenerateRoomWave(
                    roomId
                )
                : waveDirector.GenerateRoomWave(
                    waveGenerator,
                    roomType
                );

        if (!hasSavedRoomWave)
        {
            waveGenerator.SaveLastWaveForRoom(
                roomId
            );
        }

        blockRegistry.AddRange(
            generatedBlocks
        );

        blockRegistry.RemoveInvalidBlocks();

        if (blockRegistry.Count <= 0)
        {
            Debug.LogError(
                "BlockGridManager: " +
                $"{roomType} 방의 블록을 " +
                "생성하지 못했습니다.",
                this
            );

            ChangeRoomState(
                RoomCombatState.Unvisited
            );

            return false;
        }

        ChangeRoomState(
            RoomCombatState.InCombat
        );

        WaveGenerated?.Invoke(
            CurrentWaveNumber
        );

        Debug.Log(
            "BlockGridManager: " +
            $"{roomType} 방 전투 시작, " +
            $"블록 {blockRegistry.Count}개, " +
            $"필수 적 {blockRegistry.RequiredEnemyCount}개",
            this
        );

        /*
         * RequiredEnemy가 없는 잘못된 배치가 생성됐다면
         * 이동 불가 상태로 남기지 않고 즉시 클리어한다.
         */
        if (!blockRegistry.HasAliveRequiredEnemies)
        {
            TryCompleteCurrentRoom();
        }

        return true;
    }

    public void ClearRoomWaveSnapshots()
    {
        currentRoomId = -1;
        waveGenerator?.ClearRoomWaveSnapshots();
    }

    /*
     * 시작방 또는 이미 클리어한 방에 들어갈 때 호출한다.
     *
     * 기존 블록을 제거하고 이동 가능한 빈 방 상태로 만든다.
     */
    public void PrepareEmptyRoom()
    {
        InitializeRuntimeIfNeeded();

        if (bossMode.IsActive)
        {
            Debug.LogWarning(
                "BlockGridManager: " +
                "보스전 진행 중에는 일반 빈 방으로 " +
                "전환할 수 없습니다.",
                this
            );

            return;
        }

        blockRegistry.ClearAndDestroy();

        currentTurn = 0;
        isCurrentRoomCleared = true;

        enemyAttackCycle?.InitializeCycle();

        ballSealController?.ClearPendingSeal();

        ChangeRoomState(
            RoomCombatState.Cleared
        );

        Debug.Log(
            "BlockGridManager: " +
            "현재 보드를 빈 방 상태로 전환했습니다.",
            this
        );
    }

    public IEnumerator AdvanceTurnRoutine()
    {
        if (!InitializeRuntimeIfNeeded() ||
            IsRunCompleted ||
            isCurrentRoomCleared ||
            currentRoomState !=
            RoomCombatState.InCombat)
        {
            yield break;
        }

        blockRegistry.RemoveInvalidBlocks();

        currentTurn++;

        blockElementSystem.ResolveTurnEffects(
            blockRegistry.ActiveBlocks
        );

        blockRegistry.RemoveInvalidBlocks();

        if (!bossMode.IsActive &&
            TryCompleteCurrentRoom())
        {
            yield break;
        }

        if (bossMode.IsActive)
        {
            Debug.Log(
                "BlockGridManager: " +
                $"스테이지 {CurrentStageNumber}, " +
                $"보스전 턴 {currentTurn} 종료",
                this
            );

            yield break;
        }

        bool shouldAttack =
            enemyAttackCycle.AdvanceTurn();

        if (!shouldAttack)
        {
            yield break;
        }

        yield return enemyPhaseResolver
            .ResolveRoutine();

        blockRegistry.RemoveInvalidBlocks();

        if (!bossMode.IsActive)
        {
            TryCompleteCurrentRoom();
        }
    }

    private bool TryCompleteCurrentRoom()
    {
        if (isCurrentRoomCleared)
        {
            return true;
        }

        if (currentRoomState !=
            RoomCombatState.InCombat)
        {
            return false;
        }

        blockRegistry.RemoveInvalidBlocks();

        if (blockRegistry.HasAliveRequiredEnemies)
        {
            return false;
        }

        isCurrentRoomCleared = true;

        int removedOptionalBlockCount =
            blockRegistry
                .RemoveOptionalAndSpecialBlocksWithoutEffects();

        ChangeRoomState(
            RoomCombatState.Cleared
        );

        ballSealController?.ClearPendingSeal();

        Debug.Log(
            "BlockGridManager: " +
            "필수 적 블록이 모두 파괴되어 " +
            "현재 방을 클리어했습니다.",
            this
        );

        if (removedOptionalBlockCount > 0)
        {
            Debug.Log(
                "BlockGridManager: " +
                $"남은 Optional/Special 블록 " +
                $"{removedOptionalBlockCount}개를 " +
                "보상과 페널티 없이 정리했습니다.",
                this
            );
        }

        RoomCleared?.Invoke();

        return true;
    }

    public bool TryResetCurrentRoomCombat()
    {
        if (!CanResetCurrentRoomCombat)
        {
            Debug.LogWarning(
                "BlockGridManager: 현재 상태에서는 " +
                "방 전투를 초기화할 수 없습니다.",
                this
            );

            return false;
        }

        List<Block> restoredBlocks =
            waveGenerator.RegenerateRoomWave(
                currentRoomId
            );

        if (restoredBlocks == null ||
            restoredBlocks.Count == 0)
        {
            Debug.LogError(
                "BlockGridManager: 저장된 최초 블록 배치를 " +
                "복원하지 못했습니다.",
                this
            );

            return false;
        }

        blockRegistry.ClearAndDestroy();

        blockRegistry.AddRange(
            restoredBlocks
        );

        currentTurn = 0;
        isCurrentRoomCleared = false;

        enemyAttackCycle.InitializeCycle();

        ballSealController?.ClearPendingSeal();

        ChangeRoomState(
            RoomCombatState.InCombat
        );

        WaveGenerated?.Invoke(
            CurrentWaveNumber
        );

        RoomCombatReset?.Invoke();

        Debug.Log(
            "BlockGridManager: 미클리어 방을 " +
            "최초 입장 상태로 초기화했습니다.",
            this
        );

        return true;
    }

    public bool BeginBossEncounterMode()
    {
        return BeginBossRoomEncounterMode(-1);
    }

    public bool BeginBossRoomEncounterMode(
        int roomId)
    {
        if (!InitializeRuntimeIfNeeded() ||
            bossMode.IsActive ||
            IsRunCompleted)
        {
            return false;
        }

        ballSealController?.ClearPendingSeal();

        currentRoomId = roomId;
        currentTurn = 0;

        isCurrentRoomCleared = false;

        ChangeRoomState(
            RoomCombatState.InCombat
        );

        bool started =
            bossMode.Begin(
                blockRegistry
            );

        if (!started)
        {
            return false;
        }

        Debug.Log(
            "BlockGridManager: " +
            $"스테이지 {CurrentStageNumber}, " +
            $"웨이브 {CurrentBossWaveNumber} 보스전으로 전환",
            this
        );

        return true;
    }

    public bool CompleteBossRoomEncounterMode()
    {
        if (!bossMode.CompleteRoomEncounter())
        {
            return false;
        }

        blockRegistry.ClearAndDestroy();
        isCurrentRoomCleared = true;
        currentTurn = 0;
        ballSealController?.ClearPendingSeal();

        ChangeRoomState(
            RoomCombatState.Cleared
        );

        RoomCleared?.Invoke();

        Debug.Log(
            "BlockGridManager: Boss 방 전투를 완료했습니다. " +
            $"RoomId={currentRoomId}",
            this
        );

        return true;
    }

    public void RegisterBossEncounterBlocks(
        IEnumerable<Block> blocks)
    {
        if (!bossMode.IsActive ||
            blocks == null)
        {
            return;
        }

        blockRegistry.AddRange(blocks);
        blockRegistry.RemoveInvalidBlocks();
    }

    public bool CancelBossRoomEncounterMode()
    {
        if (!bossMode.CompleteRoomEncounter())
        {
            return false;
        }

        blockRegistry.ClearAndDestroy();
        isCurrentRoomCleared = false;
        currentTurn = 0;

        ChangeRoomState(
            RoomCombatState.Unvisited
        );

        return true;
    }

    public bool CompleteBossEncounterMode()
    {
        if (!bossMode.IsActive)
        {
            return false;
        }

        int completedStageNumber =
            CurrentStageNumber;

        List<Block> generatedBlocks =
            bossMode.Complete(
                waveDirector,
                waveGenerator,
                enemyAttackCycle,
                out bool runCompleted
            );

        blockRegistry.RemoveInvalidBlocks();

        if (runCompleted)
        {
            ballSealController?.ClearPendingSeal();

            Debug.Log(
                "BlockGridManager: " +
                $"최종 스테이지 " +
                $"{completedStageNumber} 완료, " +
                "런 종료",
                this
            );

            RunCompleted?.Invoke();

            return false;
        }

        isCurrentRoomCleared = false;
        currentTurn = 0;

        ChangeRoomState(
            RoomCombatState.InCombat
        );

        blockRegistry.AddRange(
            generatedBlocks
        );

        blockRegistry.RemoveInvalidBlocks();

        ballSealController?.ClearPendingSeal();

        StageStarted?.Invoke(
            CurrentStageNumber
        );

        WaveGenerated?.Invoke(
            CurrentWaveNumber
        );

        Debug.Log(
            "BlockGridManager: " +
            $"스테이지 {completedStageNumber} 종료, " +
            $"스테이지 {CurrentStageNumber}의 " +
            $"웨이브 {CurrentWaveNumber} 시작",
            this
        );

        return true;
    }

    private bool TryRequestBossEncounter()
    {
        if (bossMode.IsActive ||
            IsRunCompleted ||
            BossEncounterRequested == null)
        {
            return false;
        }

        BossEncounterRequested.Invoke();

        return bossMode.IsActive;
    }

    private void HandleTurnsUntilAttackChanged(
        int remainingTurns)
    {
        TurnsUntilAttackChanged?.Invoke(
            remainingTurns
        );
    }

    private void HandleBlockAttackTriggered(
        Block attackingBlock,
        int damage)
    {
        EnemyAttackTriggered?.Invoke(
            attackingBlock,
            damage
        );
    }

    private void HandleBlocksReachedBottom(
        IReadOnlyList<Block> blocks)
    {
        if (blocks == null ||
            blocks.Count == 0)
        {
            return;
        }

        Debug.LogWarning(
            "BlockGridManager: " +
            $"{blocks.Count}개의 블록이 " +
            "최하단 Row에 도달했습니다.",
            this
        );

        BlocksReachedBottom?.Invoke(
            blocks
        );
    }

    private void HandleBlocksExceededBottom(
        IReadOnlyList<Block> blocks)
    {
        if (blocks == null ||
            blocks.Count == 0)
        {
            return;
        }

        Debug.LogError(
            "BlockGridManager: " +
            $"{blocks.Count}개의 블록이 " +
            "보드 최하단을 넘어갔습니다.",
            this
        );

        BlocksExceededBottom?.Invoke(
            blocks
        );
    }

    private void ChangeRoomState(
        RoomCombatState nextState)
    {
        if (currentRoomState ==
            nextState)
        {
            return;
        }

        currentRoomState =
            nextState;

        Debug.Log(
            "BlockGridManager: 방 상태 변경, " +
            $"{currentRoomState}",
            this
        );

        RoomStateChanged?.Invoke(
            currentRoomState
        );
    }

    private void OnDestroy()
    {
        if (enemyAttackCycle != null)
        {
            enemyAttackCycle
                .TurnsUntilAttackChanged -=
                HandleTurnsUntilAttackChanged;
        }

        if (enemyAttackSequence != null)
        {
            enemyAttackSequence
                .BlockAttackTriggered -=
                HandleBlockAttackTriggered;
        }

        if (gridMover != null)
        {
            gridMover.BottomRowReached -=
                HandleBlocksReachedBottom;

            gridMover.BottomBoundaryExceeded -=
                HandleBlocksExceededBottom;
        }

    }
}
