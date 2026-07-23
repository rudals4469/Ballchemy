using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BlockWaveGenerator))]
[RequireComponent(typeof(BlockGridMover))]
[RequireComponent(typeof(EnemyAttackCycle))]
[RequireComponent(typeof(EnemyAttackSequence))]
public sealed class BlockGridManager : MonoBehaviour
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

    public int CurrentWaveNumber =>
        waveDirector != null
            ? waveDirector.CurrentWaveNumber
            : 1;

    public int TurnsUntilAttack =>
        enemyAttackCycle != null
            ? enemyAttackCycle.TurnsUntilAttack
            : 0;

    public bool IsBossEncounterActive =>
        bossMode.IsActive;

    public bool IsCurrentNamedWave =>
        waveDirector != null &&
        waveDirector.IsCurrentNamedWave;

    public int ActiveBlockCount =>
        blockRegistry.Count;

    public IReadOnlyList<Block> ActiveBlocks =>
        blockRegistry.ActiveBlocks;

    public int CurrentTotalAttackPower
    {
        get
        {
            if (enemyAttackSequence == null ||
                bossMode.IsActive)
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

        GenerateInitialWave();
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
                GetComponent<BlockWaveGenerator>();
        }

        if (gridMover == null)
        {
            gridMover =
                GetComponent<BlockGridMover>();
        }

        if (enemyAttackCycle == null)
        {
            enemyAttackCycle =
                GetComponent<EnemyAttackCycle>();
        }

        if (enemyAttackSequence == null)
        {
            enemyAttackSequence =
                GetComponent<EnemyAttackSequence>();
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
                () => bossMode.IsActive
            );
    }

    private bool CanInitialize()
    {
        if (waveGenerator == null ||
            gridMover == null ||
            enemyAttackCycle == null ||
            enemyAttackSequence == null ||
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
    }

    public IEnumerator AdvanceTurnRoutine()
    {
        if (!CanInitialize())
        {
            yield break;
        }

        blockRegistry.RemoveInvalidBlocks();

        currentTurn++;

        if (bossMode.IsActive)
        {
            Debug.Log(
                "BlockGridManager: " +
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

    public void BeginBossEncounterMode()
    {
        if (bossMode.IsActive)
        {
            return;
        }

        StopAllCoroutines();

        bool started =
            bossMode.Begin(
                blockRegistry
            );

        if (!started)
        {
            return;
        }

        Debug.Log(
            "BlockGridManager: " +
            "일반 웨이브를 정지하고 " +
            "보스전으로 전환합니다.",
            this
        );
    }

    public void CompleteBossEncounterMode()
    {
        if (!bossMode.IsActive)
        {
            return;
        }

        List<Block> generatedBlocks =
            bossMode.Complete(
                waveDirector,
                waveGenerator,
                enemyAttackCycle
            );

        blockRegistry.AddRange(
            generatedBlocks
        );

        blockRegistry.RemoveInvalidBlocks();

        WaveGenerated?.Invoke(
            CurrentWaveNumber
        );

        Debug.Log(
            "BlockGridManager: " +
            $"보스전 종료, 웨이브 " +
            $"{CurrentWaveNumber}부터 " +
            "일반 진행 재개",
            this
        );
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