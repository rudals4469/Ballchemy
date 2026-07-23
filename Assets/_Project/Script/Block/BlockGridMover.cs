using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class BlockGridMover : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField]
    private BlockGridMoveAnimator moveAnimator =
        new BlockGridMoveAnimator();

    private readonly BlockGridBoundaryChecker
        boundaryChecker =
            new BlockGridBoundaryChecker();

    public BlockGridBoundaryReport LastBoundaryReport
    {
        get;
        private set;
    } = BlockGridBoundaryReport.Empty;

    public bool IsMoving
    {
        get;
        private set;
    }

    public event Action<IReadOnlyList<Block>>
        BottomRowReached;

    public event Action<IReadOnlyList<Block>>
        BottomBoundaryExceeded;

    private void Awake()
    {
        EnsureHelpers();
    }

    private void OnValidate()
    {
        EnsureHelpers();

        moveAnimator.Normalize();
    }

    private void EnsureHelpers()
    {
        if (moveAnimator == null)
        {
            moveAnimator =
                new BlockGridMoveAnimator();
        }
    }

    public IEnumerator MoveDownRoutine(
        IReadOnlyList<Block> blocks,
        int rowCount)
    {
        if (IsMoving)
        {
            Debug.LogWarning(
                "BlockGridMover: " +
                "이미 블록 이동이 진행 중입니다.",
                this
            );

            yield break;
        }

        if (blocks == null ||
            blocks.Count == 0)
        {
            yield break;
        }

        rowCount =
            Mathf.Max(
                rowCount,
                0
            );

        if (rowCount == 0)
        {
            yield break;
        }

        EnsureHelpers();

        List<Block> movedBlocks =
            new List<Block>();

        List<BlockGridMoveTarget> moveTargets =
            CreateMoveTargets(
                blocks,
                rowCount,
                movedBlocks
            );

        if (moveTargets.Count == 0)
        {
            yield break;
        }

        IsMoving =
            true;

        yield return moveAnimator
            .AnimateRoutine(
                moveTargets
            );

        IsMoving =
            false;

        EvaluateBoundaries(
            movedBlocks
        );
    }

    /*
     * 기존 코드를 바로 전부 수정하지 않아도 되도록
     * 이전 호출 형식을 유지하는 호환용 메서드다.
     *
     * Cell Size는 이제 Block과 BoardGrid가 관리하므로
     * 여기서는 사용하지 않는다.
     */
    public IEnumerator MoveDownRoutine(
        IReadOnlyList<Block> blocks,
        int rowCount,
        float unusedCellSize)
    {
        return MoveDownRoutine(
            blocks,
            rowCount
        );
    }

    private List<BlockGridMoveTarget>
        CreateMoveTargets(
            IReadOnlyList<Block> blocks,
            int rowCount,
            List<Block> movedBlocks)
    {
        List<BlockGridMoveTarget> result =
            new List<BlockGridMoveTarget>();

        for (int i = 0;
             i < blocks.Count;
             i++)
        {
            Block block =
                blocks[i];

            if (block == null ||
                !block.IsAlive)
            {
                continue;
            }

            if (!block.HasGridPosition)
            {
                Debug.LogWarning(
                    "BlockGridMover: " +
                    $"{block.name}에 Grid Position이 없어 " +
                    "이동 대상에서 제외합니다.",
                    block
                );

                continue;
            }

            Vector3 startPosition =
                block.transform.position;

            bool moved =
                block.MoveGridRows(
                    rowCount,
                    false
                );

            if (!moved)
            {
                continue;
            }

            Vector3 targetPosition =
                block.GetGridWorldPosition();

            result.Add(
                new BlockGridMoveTarget(
                    block,
                    startPosition,
                    targetPosition
                )
            );

            movedBlocks.Add(
                block
            );
        }

        return result;
    }

    private void EvaluateBoundaries(
        IReadOnlyList<Block> movedBlocks)
    {
        LastBoundaryReport =
            boundaryChecker.Evaluate(
                movedBlocks
            );

        if (LastBoundaryReport
            .HasTouchingBottomBlocks)
        {
            BottomRowReached?.Invoke(
                LastBoundaryReport
                    .TouchingBottomBlocks
            );
        }

        if (LastBoundaryReport
            .HasOutsideBottomBlocks)
        {
            BottomBoundaryExceeded?.Invoke(
                LastBoundaryReport
                    .OutsideBottomBlocks
            );
        }
    }
}