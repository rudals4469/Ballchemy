using System.Collections.Generic;

public sealed class BlockGridBossMode
{
    public bool IsActive
    {
        get;
        private set;
    }

    public void Reset()
    {
        IsActive = false;
    }

    public bool Begin(
        BlockRegistry blockRegistry)
    {
        if (IsActive)
        {
            return false;
        }

        IsActive = true;

        /*
         * 기존 일반 웨이브 블록을 모두 제거한 뒤
         * BossEncounterController가 미리 준비된
         * BossPatternDefinition을 생성한다.
         */
        blockRegistry?.ClearAndDestroy();

        return true;
    }

    public List<Block> Complete(
        BlockWaveDirector waveDirector,
        BlockWaveGenerator waveGenerator,
        EnemyAttackCycle enemyAttackCycle,
        out bool runCompleted)
    {
        runCompleted = false;

        if (!IsActive)
        {
            return null;
        }

        IsActive = false;

        if (waveDirector == null ||
            waveGenerator == null)
        {
            return new List<Block>();
        }

        List<Block> generatedBlocks =
            waveDirector
                .CompleteBossAndStartNextStage(
                    waveGenerator
                );

        runCompleted =
            waveDirector.IsRunCompleted;

        /*
         * 다음 스테이지가 시작된 경우에만
         * 일반 적 공격 주기를 초기화한다.
         */
        if (!runCompleted)
        {
            enemyAttackCycle?.ResetCycle();
        }

        return generatedBlocks;
    }
}