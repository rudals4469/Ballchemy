using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BlockWaveGenerator))]
[RequireComponent(typeof(BlockGridMover))]
[RequireComponent(typeof(EnemyAttackCycle))]
public sealed class BlockGridManager : MonoBehaviour
{
    [Header("Components")]
    [SerializeField]
    private BlockWaveGenerator waveGenerator;

    [SerializeField]
    private BlockGridMover gridMover;

    [SerializeField]
    private EnemyAttackCycle enemyAttackCycle;

    private readonly List<Block> activeBlocks =
        new List<Block>();

    private int currentTurn;
    private int currentWaveIndex;

    public int CurrentTurn =>
        currentTurn;

    public int CurrentWaveNumber =>
        currentWaveIndex + 1;

    public int TurnsUntilAttack =>
        enemyAttackCycle != null
            ? enemyAttackCycle.TurnsUntilAttack
            : 0;

    public int ActiveBlockCount
    {
        get
        {
            RemoveDestroyedBlocks();
            return activeBlocks.Count;
        }
    }

    public int CurrentTotalAttackPower
    {
        get
        {
            if (enemyAttackCycle == null)
            {
                return 0;
            }

            RemoveDestroyedBlocks();

            return enemyAttackCycle
                .CalculateTotalAttackPower(
                    activeBlocks
                );
        }
    }

    public IReadOnlyList<Block> ActiveBlocks =>
        activeBlocks;

    public event Action<int> TurnsUntilAttackChanged;
    public event Action<int> EnemyAttackTriggered;
    public event Action<int> WaveGenerated;

    private void Awake()
    {
        FindComponents();
        ValidateReferences();
        SubscribeEvents();
    }

    private void Start()
    {
        if (!CanInitialize())
        {
            return;
        }

        currentTurn = 0;
        currentWaveIndex = 0;

        enemyAttackCycle.InitializeCycle();

        GenerateInitialWave();
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
    }

    private bool CanInitialize()
    {
        if (waveGenerator == null ||
            gridMover == null ||
            enemyAttackCycle == null)
        {
            return false;
        }

        if (!waveGenerator.IsReady)
        {
            Debug.LogError(
                "BlockGridManager: " +
                "BlockWaveGenerator의 설정이 완료되지 않았습니다.",
                this
            );

            return false;
        }

        return true;
    }

    private void SubscribeEvents()
    {
        if (enemyAttackCycle == null)
        {
            return;
        }

        enemyAttackCycle.TurnsUntilAttackChanged +=
            HandleTurnsUntilAttackChanged;

        enemyAttackCycle.EnemyAttackTriggered +=
            HandleEnemyAttackTriggered;
    }

    private void GenerateInitialWave()
    {
        int rowCount =
            waveGenerator.GetRandomWaveRowCount();

        List<Block> generatedBlocks =
            waveGenerator.GenerateWave(
                rowCount,
                currentWaveIndex
            );

        AddGeneratedBlocks(
            generatedBlocks
        );

        WaveGenerated?.Invoke(
            CurrentWaveNumber
        );

        Debug.Log(
            "BlockGridManager: " +
            $"초기 웨이브 {rowCount}줄 생성, " +
            $"블록 {generatedBlocks.Count}개, " +
            $"적 공격까지 {TurnsUntilAttack}턴",
            this
        );
    }

    public IEnumerator AdvanceTurnRoutine()
    {
        if (!CanInitialize())
        {
            yield break;
        }

        RemoveDestroyedBlocks();

        currentTurn++;

        bool shouldAttack =
            enemyAttackCycle.AdvanceTurn();

        if (!shouldAttack)
        {
            Debug.Log(
                "BlockGridManager: " +
                $"턴 {currentTurn} 종료, " +
                $"적 공격까지 {TurnsUntilAttack}턴, " +
                $"현재 블록 {activeBlocks.Count}개",
                this
            );

            yield break;
        }

        yield return ResolveEnemyPhaseRoutine();
    }

    private IEnumerator ResolveEnemyPhaseRoutine()
    {
        RemoveDestroyedBlocks();

        // 새 웨이브가 생성되기 전에
        // 현재 살아 있는 블록만 공격에 참여한다.
        yield return enemyAttackCycle
            .ResolveAttackRoutine(
                activeBlocks
            );

        int newWaveRowCount =
            waveGenerator.GetRandomWaveRowCount();

        // 새로운 웨이브가 들어올 공간만큼
        // 기존 블록을 아래로 이동시킨다.
        yield return gridMover.MoveDownRoutine(
            activeBlocks,
            newWaveRowCount,
            waveGenerator.CellSize
        );

        currentWaveIndex++;

        List<Block> generatedBlocks =
            waveGenerator.GenerateWave(
                newWaveRowCount,
                currentWaveIndex
            );

        AddGeneratedBlocks(
            generatedBlocks
        );

        enemyAttackCycle.ResetCycle();

        RemoveDestroyedBlocks();

        WaveGenerated?.Invoke(
            CurrentWaveNumber
        );

        Debug.Log(
            "BlockGridManager: " +
            $"웨이브 {CurrentWaveNumber} 생성, " +
            $"{newWaveRowCount}줄, " +
            $"신규 블록 {generatedBlocks.Count}개, " +
            $"전체 블록 {activeBlocks.Count}개, " +
            $"다음 공격까지 {TurnsUntilAttack}턴",
            this
        );
    }

    private void AddGeneratedBlocks(
        List<Block> generatedBlocks)
    {
        if (generatedBlocks == null)
        {
            return;
        }

        foreach (Block block in generatedBlocks)
        {
            if (block == null)
            {
                continue;
            }

            activeBlocks.Add(block);
        }
    }

    private void RemoveDestroyedBlocks()
    {
        activeBlocks.RemoveAll(
            block =>
                block == null ||
                !block.IsAlive
        );
    }

    private void HandleTurnsUntilAttackChanged(
        int remainingTurns)
    {
        TurnsUntilAttackChanged?.Invoke(
            remainingTurns
        );
    }

    private void HandleEnemyAttackTriggered(
        int totalDamage)
    {
        EnemyAttackTriggered?.Invoke(
            totalDamage
        );
    }

    private void OnDestroy()
    {
        if (enemyAttackCycle == null)
        {
            return;
        }

        enemyAttackCycle.TurnsUntilAttackChanged -=
            HandleTurnsUntilAttackChanged;

        enemyAttackCycle.EnemyAttackTriggered -=
            HandleEnemyAttackTriggered;
    }
}