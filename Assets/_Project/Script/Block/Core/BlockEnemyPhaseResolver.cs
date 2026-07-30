using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private BallSealController
        ballSealController;

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

        if (waveDirector.IsRunCompleted)
        {
            yield break;
        }

        blockRegistry.RemoveInvalidBlocks();

        /*
         * 먼저 현재 일반 적 블록의 공격을 처리한다.
         */
        yield return enemyAttackSequence
            .ResolveAttackRoutine(
                blockRegistry.ActiveBlocks
            );

        /*
         * 적 공격이 끝나면 남아 있는
         * 모든 Special 블록을 보상 없이 만료시킨다.
         */
        int expiredSpecialBlockCount =
            blockRegistry
                .ExpireSpecialBlocksWithoutReward();

        if (expiredSpecialBlockCount > 0)
        {
            Debug.Log(
                "BlockEnemyPhaseResolver: " +
                $"특수 블록 " +
                $"{expiredSpecialBlockCount}개가 " +
                "적 공격 후 사라졌습니다."
            );
        }

        blockRegistry.RemoveInvalidBlocks();

        if (enemyAttackSequence.IsTargetDead ||
            IsBossEncounterActive() ||
            waveDirector.IsRunCompleted)
        {
            yield break;
        }

        /*
         * 스테이지의 9번째 일반 웨이브가 끝났다면
         * 다음 일반 웨이브를 생성하지 않고
         * 10번째 전투인 보스전을 요청한다.
         */
        if (waveDirector
                .ShouldStartBossAfterCurrentWave())
        {
            bool bossStarted =
                TryStartBossEncounter();

            if (!bossStarted)
            {
                Debug.LogWarning(
                    "BlockEnemyPhaseResolver: " +
                    $"스테이지 " +
                    $"{waveDirector.CurrentStageNumber}의 " +
                    "보스전 시작 요청에 실패했습니다."
                );
            }

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

        if (IsBossEncounterActive() ||
            waveDirector.IsRunCompleted)
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

        NotifyWaveGeneratedToSealController();

        WaveGenerated?.Invoke(
            waveDirector.CurrentWaveNumber
        );

        Debug.Log(
            "BlockEnemyPhaseResolver: " +
            $"스테이지 " +
            $"{waveDirector.CurrentStageNumber}, " +
            $"웨이브 " +
            $"{waveDirector.CurrentWaveNumber} 생성 완료"
        );
    }

    private void
        NotifyWaveGeneratedToSealController()
    {
        if (ballSealController == null)
        {
            ballSealController =
                UnityEngine.Object
                    .FindFirstObjectByType<
                        BallSealController
                    >();
        }

        if (ballSealController == null)
        {
            return;
        }

        ballSealController
            .NotifyWaveGenerated();
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