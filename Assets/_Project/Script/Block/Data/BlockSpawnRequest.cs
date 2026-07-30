using UnityEngine;

public sealed class BlockSpawnRequest
{
    public int StartColumn
    {
        get;
    }

    public int StartRow
    {
        get;
    }

    public int WaveIndex
    {
        get;
    }

    public BlockDefinition Definition
    {
        get;
    }

    public BlockType RequestedBlockType
    {
        get;
    }

    public Vector2Int GridSize
    {
        get;
    }

    public int Health
    {
        get;
    }

    public int Attack
    {
        get;
    }

    public BlockSpawnRequest(
        int startColumn,
        int startRow,
        int waveIndex,
        BlockDefinition definition,
        BlockType requestedBlockType,
        Vector2Int gridSize,
        int health,
        int attack)
    {
        StartColumn =
            startColumn;

        StartRow =
            startRow;

        WaveIndex =
            Mathf.Max(
                waveIndex,
                0
            );

        Definition =
            definition;

        RequestedBlockType =
            definition != null
                ? definition.BlockType
                : requestedBlockType;

        GridSize =
            new Vector2Int(
                Mathf.Max(
                    gridSize.x,
                    1
                ),
                Mathf.Max(
                    gridSize.y,
                    1
                )
            );

        Health =
            Mathf.Max(
                health,
                1
            );

        Attack =
            RequestedBlockType ==
            BlockType.Special
                ? 0
                : Mathf.Max(
                    attack,
                    0
                );
    }

    public BlockSpawnRequest(
        BlockSpawnRequest source)
        : this(
            source != null
                ? source.StartColumn
                : 0,
            source != null
                ? source.StartRow
                : 0,
            source != null
                ? source.WaveIndex
                : 0,
            source != null
                ? source.Definition
                : null,
            source != null
                ? source.RequestedBlockType
                : BlockType.Normal,
            source != null
                ? source.GridSize
                : Vector2Int.one,
            source != null
                ? source.Health
                : 1,
            source != null
                ? source.Attack
                : 0
        )
    {
    }

    public BlockSpawnRequest CreateCopy()
    {
        return new BlockSpawnRequest(
            this
        );
    }
}