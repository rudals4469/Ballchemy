using System;
using System.Collections;
using System.Collections.Generic;

public sealed class BlockEnemyPhaseResolver
{
    private readonly BlockWaveGenerator
        waveGenerator;

    private readonly BlockGridMover
        gridMover;

    private readonly EnemyAttackCycle
        enemyAttackCycle;

    private readonly EnemyAttackSequence
        enemyAttackSequence;

    private readonly BlockWaveDirector
        waveDirector;

    private readonly BlockRegistry
        blockRegistry;

    private readonly Func<bool>
        isBossEncounterActive;

    private readonly Func<bool>
        tryStartBossEncounter;

    public event Action<int>
        WaveGenerated;

    public BlockEnemyPhaseResolver(
        BlockWaveGenerator waveGenerator,
        BlockGridMover gridMover,
        EnemyAttackCycle enemyAttackCycle,
        EnemyAttackSequence enemyAttackSequence,
        BlockWaveDirector waveDirector,
        BlockRegistry blockRegistry,
        Func<bool> isBossEncounterActive,
        Func<bool> tryStartBossEncounter)
    {
        this.waveGenerator =
            waveGenerator;

        this.gridMover =
            gridMover;

        this.enemyAttackCycle =
            enemyAttackCycle;

        this.enemyAttackSequence =
            enemyAttackSequence;

        this.waveDirector =
            waveDirector;

        this.blockRegistry =
            blockRegistry;

        this.isBossEncounterActive =
            isBossEncounterActive;

        this.tryStartBossEncounter =
            tryStartBossEncounter;
    }

    public IEnumerator ResolveRoutine()
    {
        if (!HasRequiredReferences())
        {
            yield break;
        }

        blockRegistry.RemoveInvalidBlocks();

        yield return enemyAttackSequence
            .ResolveAttackRoutine(
                blockRegistry.ActiveBlocks
            );

        blockRegistry.RemoveInvalidBlocks();

        if (enemyAttackSequence.IsTargetDead ||
            IsBossEncounterActive())
        {
            yield break;
        }

        if (waveDirector
                .ShouldStartBossAfterCurrentWave() &&
            TryStartBossEncounter())
        {
            yield break;
        }

        BlockWavePlan nextWavePlan =
            waveDirector.CreateNextWavePlan(
                waveGenerator
            );

        if (nextWavePlan == null)
        {
            yield break;
        }

        yield return gridMover.MoveDownRoutine(
            blockRegistry.ActiveBlocks,
            nextWavePlan.RequiredRowCount
        );

        if (IsBossEncounterActive())
        {
            yield break;
        }

        List<Block> generatedBlocks =
            waveDirector.GeneratePlannedWave(
                waveGenerator,
                nextWavePlan
            );

        blockRegistry.AddRange(
            generatedBlocks
        );

        enemyAttackCycle.ResetCycle();

        blockRegistry.RemoveInvalidBlocks();

        WaveGenerated?.Invoke(
            waveDirector.CurrentWaveNumber
        );
    }

    private bool TryStartBossEncounter()
    {
        return tryStartBossEncounter != null &&
               tryStartBossEncounter.Invoke();
    }

    private bool HasRequiredReferences()
    {
        return waveGenerator != null &&
               gridMover != null &&
               enemyAttackCycle != null &&
               enemyAttackSequence != null &&
               waveDirector != null &&
               blockRegistry != null;
    }

    private bool IsBossEncounterActive()
    {
        return isBossEncounterActive != null &&
               isBossEncounterActive.Invoke();
    }
}