using System;
using System.Collections;
using UnityEngine;

public sealed class BlockEnemyPhaseResolver
{
    /*
     * 기존 BlockGridManager 생성자 연결을 유지하기 위한 참조다.
     *
     * 고정형 방 전투 1-1단계에서는
     * 웨이브 생성과 블록 하강에 사용하지 않는다.
     *
     * 이후 기존 웨이브 시스템 참조가 완전히 제거되는 단계에서
     * 생성자와 함께 정리한다.
     */
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

    /*
     * 기존 생성자와 참조 관계를 유지하기 위한 콜백이다.
     *
     * 고정형 방 전투에서는 적 공격 단계가
     * 보스 조우를 자동으로 요청하지 않는다.
     */
    private readonly Func<bool>
        tryStartBossEncounter;

    /*
     * 기존 BlockGridManager의 이벤트 구독을
     * 깨뜨리지 않기 위해 현재 단계에서는 유지한다.
     *
     * 고정형 방 전투 중에는 새 웨이브를 생성하지 않으므로
     * 이 이벤트는 호출되지 않는다.
     */
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
         * 보스전은 BossEncounterController가 별도로 진행한다.
         * 일반 방 적 공격 처리와 섞지 않는다.
         */
        if (IsBossEncounterActive())
        {
            yield break;
        }

        /*
         * 현재 방에서 살아 있는 공격 가능 적들의
         * 공격을 순차적으로 처리한다.
         */
        yield return enemyAttackSequence
            .ResolveAttackRoutine(
                blockRegistry.ActiveBlocks
            );

        /*
         * 공격이 발동한 뒤 다음 공격까지의 카운트를
         * 다시 기본 간격으로 초기화한다.
         *
         * 이 처리가 없으면 turnsUntilAttack이 0에 머물러
         * 이후 모든 턴마다 적 공격이 실행된다.
         */
        enemyAttackCycle.ResetCycle();

        blockRegistry.RemoveInvalidBlocks();

        if (enemyAttackSequence.IsTargetDead)
        {
            yield break;
        }

        /*
         * 고정형 방 전투 전환 1-1단계
         *
         * 기존 처리:
         * - 다음 웨이브 계획 생성
         * - 블록 한 행 하강
         * - 다음 웨이브 생성
         * - 보스 웨이브 자동 요청
         * - BallSealController 웨이브 갱신
         *
         * 현재 처리:
         * - 현재 방의 배치를 그대로 유지
         * - 다음 플레이어 턴으로 복귀
         *
         * 방 클리어 판정은 다음 구현 단계에서
         * 별도 책임으로 추가한다.
         */
        Debug.Log(
            "BlockEnemyPhaseResolver: " +
            "적 공격 단계 완료. " +
            "고정형 방 전투이므로 블록 하강과 " +
            "다음 웨이브 생성을 실행하지 않습니다."
        );
    }

    private bool HasRequiredReferences()
    {
        /*
         * waveGenerator와 gridMover는 기존 생성자 연결을
         * 유지하기 위해 보관하지만 현재 단계 실행에는
         * 필요하지 않다.
         */
        return enemyAttackCycle != null &&
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
