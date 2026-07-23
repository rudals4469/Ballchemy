using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class BlockGridMoveAnimator
{
    [Header("Movement")]
    [Tooltip(
        "기존 블록들이 목표 격자 위치까지 이동하는 데 " +
        "걸리는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float moveDuration = 0.35f;

    public float MoveDuration =>
        moveDuration;

    public void Normalize()
    {
        moveDuration =
            Mathf.Max(
                moveDuration,
                0f
            );
    }

    public IEnumerator AnimateRoutine(
        IReadOnlyList<BlockGridMoveTarget> targets)
    {
        if (targets == null ||
            targets.Count == 0)
        {
            yield break;
        }

        Normalize();

        if (moveDuration <= 0f)
        {
            ApplyFinalPositions(
                targets
            );

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
                    2f * progress
                );

            for (int i = 0;
                 i < targets.Count;
                 i++)
            {
                BlockGridMoveTarget target =
                    targets[i];

                if (target == null ||
                    target.Block == null)
                {
                    continue;
                }

                target.Block.transform.position =
                    Vector3.Lerp(
                        target.StartPosition,
                        target.TargetPosition,
                        smoothProgress
                    );
            }

            yield return null;
        }

        ApplyFinalPositions(
            targets
        );
    }

    private void ApplyFinalPositions(
        IReadOnlyList<BlockGridMoveTarget> targets)
    {
        if (targets == null)
        {
            return;
        }

        for (int i = 0;
             i < targets.Count;
             i++)
        {
            BlockGridMoveTarget target =
                targets[i];

            if (target == null ||
                target.Block == null)
            {
                continue;
            }

            target.Block.transform.position =
                target.TargetPosition;

            target.Block.SnapToGridPosition();
        }
    }
}