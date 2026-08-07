using System.Collections.Generic;
using UnityEngine;

public sealed class BlockRegistry
{
    private readonly List<Block> activeBlocks =
        new List<Block>();

    public IReadOnlyList<Block> ActiveBlocks =>
        activeBlocks;

    public int RequiredEnemyCount
    {
        get
        {
            RemoveInvalidBlocks();

            int count = 0;

            for (int i = 0;
                 i < activeBlocks.Count;
                 i++)
            {
                Block block =
                    activeBlocks[i];

                if (block == null ||
                    !block.IsAlive ||
                    block.Definition == null ||
                    block.Definition.ClearRole !=
                    BlockClearRole.RequiredEnemy)
                {
                    continue;
                }

                count++;
            }

            return count;
        }
    }

    public bool HasAliveRequiredEnemies =>
        RequiredEnemyCount > 0;

    public int Count
    {
        get
        {
            RemoveInvalidBlocks();

            return activeBlocks.Count;
        }
    }

    public void Add(
        Block block)
    {
        if (block == null ||
            activeBlocks.Contains(block))
        {
            return;
        }

        activeBlocks.Add(
            block
        );
    }

    public void AddRange(
        IEnumerable<Block> blocks)
    {
        if (blocks == null)
        {
            return;
        }

        foreach (Block block in blocks)
        {
            Add(
                block
            );
        }
    }

    public void RemoveInvalidBlocks()
    {
        activeBlocks.RemoveAll(
            block =>
                block == null ||
                !block.IsAlive
        );
    }

    public int RemoveOptionalAndSpecialBlocksWithoutEffects()
    {
        int removedBlockCount = 0;

        for (int i =
                 activeBlocks.Count - 1;
             i >= 0;
             i--)
        {
            Block block =
                activeBlocks[i];

            if (block == null ||
                !block.IsAlive)
            {
                activeBlocks.RemoveAt(
                    i
                );

                continue;
            }

            bool isOptional =
                block.Definition != null &&
                block.Definition.ClearRole ==
                BlockClearRole.Optional;

            if (!isOptional &&
                block.BlockType !=
                BlockType.Special)
            {
                continue;
            }

            activeBlocks.RemoveAt(
                i
            );

            block.gameObject.SetActive(
                false
            );

            Object.Destroy(
                block.gameObject
            );

            removedBlockCount++;
        }

        return removedBlockCount;
    }

    public void ClearAndDestroy()
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

            Object.Destroy(
                block.gameObject
            );
        }

        activeBlocks.Clear();
    }

    public void ClearReferences()
    {
        activeBlocks.Clear();
    }
}
