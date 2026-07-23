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

        blockRegistry?.ClearAndDestroy();

        return true;
    }

    public List<Block> Complete(
        BlockWaveDirector waveDirector,
        BlockWaveGenerator waveGenerator,
        EnemyAttackCycle enemyAttackCycle)
    {
        if (!IsActive)
        {
            return null;
        }

        IsActive = false;

        List<Block> generatedBlocks =
            waveDirector
                .GenerateNormalWaveAfterBoss(
                    waveGenerator
                );

        enemyAttackCycle?.ResetCycle();

        return generatedBlocks;
    }
}