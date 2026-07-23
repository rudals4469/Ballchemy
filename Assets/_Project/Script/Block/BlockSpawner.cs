using System;
using UnityEngine;

[Serializable]
public sealed class BlockSpawner
{
    [Header("References")]
    [SerializeField]
    private Block blockPrefab;

    [SerializeField]
    private Transform blockContainer;

    [Header("Debug")]
    [SerializeField]
    private bool showSpawnDebugLog;

    public bool IsReady =>
        blockPrefab != null;

    public void Prepare(
        Transform fallbackContainer)
    {
        if (blockContainer == null)
        {
            blockContainer =
                fallbackContainer;
        }
    }

    public void Validate(
        UnityEngine.Object context)
    {
        if (blockPrefab == null)
        {
            Debug.LogError(
                "BlockSpawner: " +
                "Block Prefab이 연결되지 않았습니다.",
                context
            );
        }

        if (blockContainer == null)
        {
            Debug.LogWarning(
                "BlockSpawner: " +
                "Block Container가 연결되지 않았습니다.",
                context
            );
        }
    }

    public Block Spawn(
        BoardGrid boardGrid,
        BlockSpawnRequest request)
    {
        if (boardGrid == null)
        {
            Debug.LogError(
                "BlockSpawner: " +
                "BoardGrid가 없어 블록을 생성할 수 없습니다."
            );

            return null;
        }

        if (blockPrefab == null)
        {
            Debug.LogError(
                "BlockSpawner: " +
                "Block Prefab이 없어 블록을 생성할 수 없습니다."
            );

            return null;
        }

        if (request == null)
        {
            Debug.LogWarning(
                "BlockSpawner: " +
                "BlockSpawnRequest가 비어 있습니다."
            );

            return null;
        }

        Vector3 spawnPosition =
            GetBlockCenterWorldPosition(
                boardGrid,
                request.StartColumn,
                request.StartRow,
                request.GridSize
            );

        Quaternion spawnRotation =
            boardGrid
                .transform
                .rotation;

        Block newBlock =
            UnityEngine.Object.Instantiate(
                blockPrefab,
                spawnPosition,
                spawnRotation,
                blockContainer
            );

        if (newBlock == null)
        {
            return null;
        }

        string definitionId =
            request.Definition != null
                ? request.Definition.BlockId
                : "prefab_default";

        newBlock.name =
            $"Block_{request.RequestedBlockType}" +
            $"_{definitionId}" +
            $"_{request.GridSize.x}x" +
            $"{request.GridSize.y}" +
            $"_W{request.WaveIndex + 1}" +
            $"_R{request.StartRow}" +
            $"_C{request.StartColumn}";

        if (request.Definition != null)
        {
            newBlock.Initialize(
                request.Definition,
                request.Health,
                request.Attack,
                boardGrid.CellSize
            );
        }
        else
        {
            newBlock.Initialize(
                request.Health,
                request.Attack
            );
        }

        newBlock.SetGridPosition(
            boardGrid,
            request.StartColumn,
            request.StartRow,
            true
        );

        if (showSpawnDebugLog)
        {
            Debug.Log(
                "BlockSpawner: 블록 생성, " +
                $"타입={request.RequestedBlockType}, " +
                $"데이터={definitionId}, " +
                $"크기={request.GridSize.x}x" +
                $"{request.GridSize.y}, " +
                $"시작 셀=({request.StartColumn}, " +
                $"{request.StartRow}), " +
                $"끝 셀=({newBlock.EndColumn}, " +
                $"{newBlock.EndRow}), " +
                $"HP={request.Health}, " +
                $"공격력={request.Attack}",
                newBlock
            );
        }

        return newBlock;
    }

    private Vector3 GetBlockCenterWorldPosition(
        BoardGrid boardGrid,
        int startColumn,
        int startRow,
        Vector2Int gridSize)
    {
        Vector3 startCellPosition =
            boardGrid.GetCellWorldPosition(
                startColumn,
                startRow
            );

        float horizontalDistance =
            (
                Mathf.Max(
                    gridSize.x,
                    1
                ) -
                1
            ) *
            boardGrid.CellSize *
            0.5f;

        float verticalDistance =
            (
                Mathf.Max(
                    gridSize.y,
                    1
                ) -
                1
            ) *
            boardGrid.CellSize *
            0.5f;

        Vector3 horizontalOffset =
            boardGrid.transform.right *
            horizontalDistance;

        Vector3 verticalOffset =
            -boardGrid.transform.up *
            verticalDistance;

        return startCellPosition +
               horizontalOffset +
               verticalOffset;
    }
}