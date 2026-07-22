using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class BlockGridMover : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip(
        "기존 블록들이 다음 위치까지 이동하는 데 " +
        "걸리는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float moveDuration = 0.35f;

    public IEnumerator MoveDownRoutine(
        IReadOnlyList<Block> blocks,
        int rowCount,
        float cellSize)
    {
        if (blocks == null ||
            blocks.Count == 0)
        {
            yield break;
        }

        rowCount =
            Mathf.Max(
                0,
                rowCount
            );

        cellSize =
            Mathf.Max(
                0f,
                cellSize
            );

        if (rowCount == 0 ||
            cellSize <= 0f)
        {
            yield break;
        }

        float movementDistance =
            rowCount *
            cellSize;

        Vector3 movement =
            Vector3.down *
            movementDistance;

        if (moveDuration <= 0f)
        {
            MoveImmediately(
                blocks,
                movement
            );

            yield break;
        }

        Dictionary<Block, Vector3> startPositions =
            CreateStartPositionMap(
                blocks
            );

        if (startPositions.Count == 0)
        {
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime <
               moveDuration)
        {
            elapsedTime +=
                Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime /
                    moveDuration
                );

            float smoothProgress =
                progress *
                progress *
                (
                    3f -
                    (2f * progress)
                );

            foreach (
                KeyValuePair<Block, Vector3>
                pair in startPositions)
            {
                Block block =
                    pair.Key;

                if (block == null)
                {
                    continue;
                }

                Vector3 targetPosition =
                    pair.Value +
                    movement;

                block.transform.position =
                    Vector3.Lerp(
                        pair.Value,
                        targetPosition,
                        smoothProgress
                    );
            }

            yield return null;
        }

        ApplyFinalPositions(
            startPositions,
            movement
        );
    }

    private void MoveImmediately(
        IReadOnlyList<Block> blocks,
        Vector3 movement)
    {
        foreach (Block block in blocks)
        {
            if (block == null ||
                !block.IsAlive)
            {
                continue;
            }

            block.transform.position +=
                movement;
        }
    }

    private Dictionary<Block, Vector3>
        CreateStartPositionMap(
            IReadOnlyList<Block> blocks)
    {
        Dictionary<Block, Vector3> result =
            new Dictionary<Block, Vector3>();

        foreach (Block block in blocks)
        {
            if (block == null ||
                !block.IsAlive)
            {
                continue;
            }

            result.Add(
                block,
                block.transform.position
            );
        }

        return result;
    }

    private void ApplyFinalPositions(
        Dictionary<Block, Vector3>
            startPositions,
        Vector3 movement)
    {
        foreach (
            KeyValuePair<Block, Vector3>
            pair in startPositions)
        {
            Block block =
                pair.Key;

            if (block == null)
            {
                continue;
            }

            block.transform.position =
                pair.Value +
                movement;
        }
    }
}