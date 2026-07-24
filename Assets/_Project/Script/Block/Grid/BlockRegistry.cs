using System.Collections.Generic;
using UnityEngine;

public sealed class BlockRegistry
{
    private readonly List<Block> activeBlocks =
        new List<Block>();

    public IReadOnlyList<Block> ActiveBlocks =>
        activeBlocks;

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

    public int ExpireSpecialBlocksWithoutReward()
    {
        int expiredBlockCount = 0;

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

            if (block.BlockType !=
                BlockType.Special)
            {
                continue;
            }

            /*
             * 먼저 Registry에서 제거한다.
             * Destroy는 프레임 종료 시 처리되므로,
             * 참조가 다음 이동 및 웨이브 생성 과정에
             * 남지 않도록 즉시 목록에서 제외한다.
             */
            activeBlocks.RemoveAt(
                i
            );

            bool expired =
                block.ExpireWithoutReward();

            if (expired)
            {
                expiredBlockCount++;
            }
        }

        return expiredBlockCount;
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