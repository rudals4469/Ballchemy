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
        if (!CanInitialize())
        {
            return;
        }

        currentTurn = 0;

        bossMode.Reset();
        waveDirector.Initialize();
        enemyAttackCycle.InitializeCycle();

        ballSealController?.ClearPendingSeal();

        GenerateInitialWave();

        StageStarted?.Invoke(
            CurrentStageNumber
        );
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

        if (enemyPhaseResolver != null)
        {
            enemyPhaseResolver.WaveGenerated +=
                HandleWaveGenerated;
        }
    }

    private void GenerateInitialWave()
    {
        List<Block> generatedBlocks =
            waveDirector.GenerateInitialWave(
                waveGenerator
            );

        blockRegistry.AddRange(
            generatedBlocks
        );

        WaveGenerated?.Invoke(
            CurrentWaveNumber
        );

        Debug.Log(
            "BlockGridManager: " +
            $"스테이지 {CurrentStageNumber}/" +
            $"{TotalStageCount}, " +
            $"웨이브 {CurrentWaveNumber} 시작",
            this
        );
    }

    public IEnumerator AdvanceTurnRoutine()
    {
        if (!CanInitialize() ||
            IsRunCompleted)
        {
            yield break;
        }

        blockRegistry.RemoveInvalidBlocks();

        currentTurn++;

        blockElementSystem.ResolveTurnEffects(
            blockRegistry.ActiveBlocks
        );

        blockRegistry.RemoveInvalidBlocks();

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
    }

    public bool BeginBossEncounterMode()
    {
        if (bossMode.IsActive ||
            IsRunCompleted)
        {
            return false;
        }

        /*
         * 이전 일반 웨이브에서 남은 공 봉인은
         * 보스전으로 가져가지 않는다.
         */
        ballSealController?.ClearPendingSeal();

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

    private void HandleWaveGenerated(
        int waveNumber)
    {
        WaveGenerated?.Invoke(
            waveNumber
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

        if (enemyPhaseResolver != null)
        {
            enemyPhaseResolver.WaveGenerated -=
                HandleWaveGenerated;
        }
    }
}