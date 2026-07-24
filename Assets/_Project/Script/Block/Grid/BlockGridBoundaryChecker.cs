using System.Collections.Generic;

public sealed class BlockGridBoundaryChecker
{
    public BlockGridBoundaryReport Evaluate(
        IReadOnlyList<Block> blocks)
    {
        List<Block> touchingBottomBlocks =
            new List<Block>();

        List<Block> outsideBottomBlocks =
            new List<Block>();

        if (blocks == null)
        {
            return new BlockGridBoundaryReport(
                touchingBottomBlocks,
                outsideBottomBlocks
            );
        }

        for (int i = 0;
             i < blocks.Count;
             i++)
        {
            Block block =
                blocks[i];

            if (block == null ||
                !block.IsAlive ||
                !block.HasGridPosition)
            {
                continue;
            }

            if (block.IsOutsideBottom)
            {
                outsideBottomBlocks.Add(
                    block
                );

                continue;
            }

            if (block.IsTouchingBottomRow)
            {
                touchingBottomBlocks.Add(
                    block
                );
            }
        }

        return new BlockGridBoundaryReport(
            touchingBottomBlocks,
            outsideBottomBlocks
        );
    }
}