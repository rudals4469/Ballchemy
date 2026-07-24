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

        /*
         * Definition이 있다면 Definition의 타입을
         * 최종 타입으로 사용한다.
         */
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

        /*
         * 일반 스테이지의 모든 Special 블록은
         * 플레이어를 공격하지 않는다.
         *
         * 생성 측에서 공격력을 잘못 전달하더라도
         * SpawnRequest 단계에서 0으로 고정한다.
         */
        Attack =
            RequestedBlockType ==
            BlockType.Special
                ? 0
                : Mathf.Max(
                    attack,
                    0
                );
    }
}