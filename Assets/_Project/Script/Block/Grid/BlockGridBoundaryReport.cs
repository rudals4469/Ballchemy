using System.Collections.Generic;

public sealed class BlockGridBoundaryReport
{
    private readonly List<Block>
        touchingBottomBlocks;

    private readonly List<Block>
        outsideBottomBlocks;

    public static BlockGridBoundaryReport Empty { get; } =
        new BlockGridBoundaryReport(
            null,
            null
        );

    public IReadOnlyList<Block> TouchingBottomBlocks =>
        touchingBottomBlocks;

    public IReadOnlyList<Block> OutsideBottomBlocks =>
        outsideBottomBlocks;

    public int TouchingBottomCount =>
        touchingBottomBlocks.Count;

    public int OutsideBottomCount =>
        outsideBottomBlocks.Count;

    public bool HasTouchingBottomBlocks =>
        touchingBottomBlocks.Count > 0;

    public bool HasOutsideBottomBlocks =>
        outsideBottomBlocks.Count > 0;

    public BlockGridBoundaryReport(
        IEnumerable<Block> touchingBottom,
        IEnumerable<Block> outsideBottom)
    {
        touchingBottomBlocks =
            touchingBottom != null
                ? new List<Block>(
                    touchingBottom
                )
                : new List<Block>();

        outsideBottomBlocks =
            outsideBottom != null
                ? new List<Block>(
                    outsideBottom
                )
                : new List<Block>();
    }
}