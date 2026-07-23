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

    [Header("Named Wave")]
    [SerializeField, Min(1)]
    private int namedWaveInterval = 3;

    [SerializeField, Min(1f)]
    private float namedHealthMultiplier = 3f;

    [SerializeField, Min(0f)]
    private float namedAttackMultiplier = 2f;

    private readonly List<Block> activeBlocks =
        new List<Block>();

    private int currentTurn;
    private int currentWaveIndex;

    private bool isBossEncounterActive;

    public int CurrentTurn =>
        currentTurn;

    public int CurrentWaveNumber =>
        currentWaveIndex + 1;

    public int TurnsUntilAttack =>
        enemyAttackCycle != null
            ? enemyAttackCycle.TurnsUntilAttack
            : 0;

    public bool IsBossEncounterActive =>
        isBossEncounterActive;

    public bool IsCurrentNamedWave =>
        IsNamedWave(
            CurrentWaveNumber
        );

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
            if (enemyAttackSequence == null ||
                isBossEncounterActive)
            {
                return 0;
            }

            RemoveDestroyedBlocks();

            return enemyAttackSequence
                .CalculateTotalAttackPower(
                    activeBlocks
                );
        }
    }

    public IReadOnlyList<Block> ActiveBlocks =>
        activeBlocks;

    public event Action<int>
        TurnsUntilAttackChanged;

    public event Action<Block, int>
        EnemyAttackTriggered;

    public event Action<int>
        WaveGenerated;

    private void Awake()
    {
        NormalizeSettings();
        FindComponents();
        ValidateReferences();
        SubscribeEvents();
    }

    private void OnValidate()
    {
        NormalizeSettings();
    }

    private void NormalizeSettings()
    {
        namedWaveInterval =
            Mathf.Max(
                namedWaveInterval,
                1
            );

        namedHealthMultiplier =
            Mathf.Max(
                namedHealthMultiplier,
                1f
            );

        namedAttackMultiplier =
            Mathf.Max(
                namedAttackMultiplier,
                0f
            );
    }

    private void Start()
    {
        if (!CanInitialize())
        {
            return;
        }

        currentTurn = 0;
        currentWaveIndex = 0;
        isBossEncounterActive = false;

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

    private bool CanInitialize()
    {
        if (waveGenerator == null ||
            gridMover == null ||
            enemyAttackCycle == null ||
            enemyAttackSequence == null)
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
    }

    private void GenerateInitialWave()
    {
        int rowCount =
            waveGenerator.GetRandomWaveRowCount();

        List<Block> generatedBlocks =
            waveGenerator.GenerateWave(
                rowCount,
                currentWaveIndex,
                BlockType.Normal
            );

        AddGeneratedBlocks(
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

        RemoveDestroyedBlocks();

        currentTurn++;

        /*
         * 보스전에서는 일반 블록 공격,
         * 하강 및 웨이브 생성을 진행하지 않는다.
         */
        if (isBossEncounterActive)
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

        yield return ResolveEnemyPhaseRoutine();
    }

    private IEnumerator ResolveEnemyPhaseRoutine()
    {
        RemoveDestroyedBlocks();

        yield return enemyAttackSequence
            .ResolveAttackRoutine(
                activeBlocks
            );

        if (enemyAttackSequence.IsTargetDead ||
            isBossEncounterActive)
        {
            yield break;
        }

        int nextWaveIndex =
            currentWaveIndex + 1;

        int nextWaveNumber =
            nextWaveIndex + 1;

        int baseRowCount =
            waveGenerator.GetRandomWaveRowCount();

        bool isNamedWave =
            IsNamedWave(
                nextWaveNumber
            );

        BlockDefinition namedDefinition =
            null;

        if (isNamedWave)
        {
            namedDefinition =
                waveGenerator.GetRandomDefinition(
                    BlockType.Named
                );
        }

        int requiredRowCount =
            waveGenerator.GetRequiredRowCount(
                baseRowCount,
                namedDefinition
            );

        yield return gridMover.MoveDownRoutine(
            activeBlocks,
            requiredRowCount,
            waveGenerator.CellSize
        );

        if (isBossEncounterActive)
        {
            yield break;
        }

        currentWaveIndex =
            nextWaveIndex;

        List<Block> generatedBlocks;

        if (namedDefinition != null)
        {
            generatedBlocks =
                waveGenerator.GenerateFeaturedWave(
                    requiredRowCount,
                    currentWaveIndex,
                    namedDefinition,
                    namedHealthMultiplier,
                    namedAttackMultiplier
                );
        }
        else
        {
            generatedBlocks =
                waveGenerator.GenerateWave(
                    requiredRowCount,
                    currentWaveIndex,
                    BlockType.Normal
                );
        }

        AddGeneratedBlocks(
            generatedBlocks
        );

        enemyAttackCycle.ResetCycle();

        RemoveDestroyedBlocks();

        WaveGenerated?.Invoke(
            CurrentWaveNumber
        );
    }

    private bool IsNamedWave(
        int waveNumber)
    {
        if (waveNumber <= 0)
        {
            return false;
        }

        return waveNumber %
            namedWaveInterval == 0;
    }

    public void BeginBossEncounterMode()
    {
        if (isBossEncounterActive)
        {
            return;
        }

        isBossEncounterActive = true;

        StopAllCoroutines();

        ClearActiveBlocksImmediately();

        Debug.Log(
            "BlockGridManager: " +
            "일반 웨이브를 정지하고 보스전으로 전환합니다.",
            this
        );
    }

    public void CompleteBossEncounterMode()
    {
        if (!isBossEncounterActive)
        {
            return;
        }

        isBossEncounterActive = false;

        currentWaveIndex++;

        int rowCount =
            waveGenerator.GetRandomWaveRowCount();

        List<Block> generatedBlocks =
            waveGenerator.GenerateWave(
                rowCount,
                currentWaveIndex,
                BlockType.Normal
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
            $"보스전 종료, 웨이브 " +
            $"{CurrentWaveNumber}부터 일반 진행 재개",
            this
        );
    }

    private void ClearActiveBlocksImmediately()
    {
        for (int i = 0;
             i < activeBlocks.Count;
             i++)
        {
            Block block =
                activeBlocks[i];

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

        activeBlocks.Clear();
    }

    private void AddGeneratedBlocks(
        List<Block> generatedBlocks)
    {
        if (generatedBlocks == null)
        {
            return;
        }

        foreach (Block block
                 in generatedBlocks)
        {
            if (block == null)
            {
                continue;
            }

            activeBlocks.Add(
                block
            );
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

    private void HandleBlockAttackTriggered(
        Block attackingBlock,
        int damage)
    {
        EnemyAttackTriggered?.Invoke(
            attackingBlock,
            damage
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
    }
}