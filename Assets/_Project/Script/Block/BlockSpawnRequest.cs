using UnityEngine;

public sealed class BlockSpawnRequest
{
    public int StartColumn { get; }

    public int StartRow { get; }

    public int WaveIndex { get; }

    public BlockDefinition Definition { get; }

    public BlockType RequestedBlockType { get; }

    public Vector2Int GridSize { get; }

    public int Health { get; }

    public int Attack { get; }

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
            requestedBlockType;

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
            Mathf.Max(
                attack,
                0
            );
    }
}