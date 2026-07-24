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

        blockRegistry.RemoveInvalidBlocks();

        /*
         * 먼저 일반 적 블록의 공격을 모두 처리한다.
         */
        yield return enemyAttackSequence
            .ResolveAttackRoutine(
                blockRegistry.ActiveBlocks
            );

        /*
         * 공격 주기가 실행된 직후
         * 기존의 모든 Special 블록을 제거한다.
         *
         * Block.Destroyed 이벤트를 발생시키지 않으므로
         * 공 추가, 회복 등의 보상은 지급되지 않는다.
         *
         * 봉인 블록은 이 과정에서
         * BallSealController에 봉인을 적용한다.
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
            IsBossEncounterActive())
        {
            yield break;
        }

        /*
         * 보스전 진입 시에는 일반 웨이브를 생성하지 않는다.
         *
         * 보스전 시작 과정에서 BallLauncher가
         * 남아 있는 봉인을 즉시 해제한다.
         */
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

        /*
         * 특수 블록 제거 이후 새 웨이브를 생성하므로,
         * 여기서 새로 등장한 특수 블록은
         * 이번 공격 주기에 제거되지 않는다.
         */
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

        /*
         * 실제 블록 생성이 끝난 시점을
         * 봉인 시스템에 전달한다.
         *
         * 봉인 적용 직후 발생한 첫 알림에서는 유지되고,
         * 다음 공격 주기의 웨이브 생성 알림에서 해제된다.
         */
        NotifyWaveGeneratedToSealController();

        WaveGenerated?.Invoke(
            waveDirector.CurrentWaveNumber
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