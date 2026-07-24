using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class BossPatternEntranceItem
{
    private readonly Collider2D[] colliders;
    private readonly bool[] colliderEnabledStates;

    public Block Block
    {
        get;
    }

    public Vector3 StartPosition
    {
        get;
    }

    public Vector3 TargetPosition
    {
        get;
    }

    public int PatternRow
    {
        get;
    }

    public BossPatternEntranceItem(
        Block block,
        Vector3 startPosition,
        Vector3 targetPosition,
        int patternRow)
    {
        Block =
            block;

        StartPosition =
            startPosition;

        TargetPosition =
            targetPosition;

        PatternRow =
            patternRow;

        if (block == null)
        {
            colliders =
                Array.Empty<Collider2D>();

            colliderEnabledStates =
                Array.Empty<bool>();

            return;
        }

        colliders =
            block.GetComponentsInChildren<Collider2D>(
                true
            );

        colliderEnabledStates =
            new bool[colliders.Length];

        for (int i = 0;
             i < colliders.Length;
             i++)
        {
            Collider2D currentCollider =
                colliders[i];

            if (currentCollider == null)
            {
                continue;
            }

            colliderEnabledStates[i] =
                currentCollider.enabled;

            currentCollider.enabled =
                false;
        }

        SetPosition(
            StartPosition
        );
    }

    public void SetPosition(
        Vector3 position)
    {
        if (Block == null)
        {
            return;
        }

        Block.transform.position =
            position;
    }

    public void Complete()
    {
        SetPosition(
            TargetPosition
        );

        RestoreColliders();
    }

    private void RestoreColliders()
    {
        for (int i = 0;
             i < colliders.Length;
             i++)
        {
            Collider2D currentCollider =
                colliders[i];

            if (currentCollider == null)
            {
                continue;
            }

            currentCollider.enabled =
                colliderEnabledStates[i];
        }
    }
}

[Serializable]
public sealed class BossPatternEntranceAnimator
{
    [Header("Entrance")]
    [Tooltip(
        "블록 하나가 화면 위에서 " +
        "목표 위치까지 내려오는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float entranceDuration = 1.6f;

    [Tooltip(
        "각 행이 내려오기 시작하는 " +
        "시간 차이입니다."
    )]
    [SerializeField, Min(0f)]
    private float rowDelay = 0.025f;

    [Tooltip(
        "패턴의 가장 아래쪽 행이 보드 최상단보다 " +
        "몇 칸 위에서 시작할지 설정합니다."
    )]
    [SerializeField, Min(0f)]
    private float spawnHeightOffsetCells = 8f;

    [Tooltip(
        "체크하면 아래쪽 행부터 먼저 내려와 " +
        "블록이 아래에서부터 쌓이는 느낌을 만듭니다."
    )]
    [SerializeField]
    private bool lowerRowsFirst = true;

    [Header("Landing Impact")]
    [Tooltip(
        "목표 위치를 살짝 지나치는 거리입니다. " +
        "값이 커질수록 충돌 느낌이 강해집니다."
    )]
    [SerializeField, Min(0f)]
    private float impactOvershootCells = 0.1f;

    [Tooltip(
        "목표 위치를 지나친 상태로 " +
        "잠깐 유지하는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float impactPause = 0.025f;

    [Tooltip(
        "지나친 위치에서 정확한 격자 위치로 " +
        "돌아오는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float settleDuration = 0.12f;

    private float runtimeCellSize = 1f;

    public void Normalize()
    {
        entranceDuration =
            Mathf.Max(
                entranceDuration,
                0f
            );

        rowDelay =
            Mathf.Max(
                rowDelay,
                0f
            );

        spawnHeightOffsetCells =
            Mathf.Max(
                spawnHeightOffsetCells,
                0f
            );

        impactOvershootCells =
            Mathf.Max(
                impactOvershootCells,
                0f
            );

        impactPause =
            Mathf.Max(
                impactPause,
                0f
            );

        settleDuration =
            Mathf.Max(
                settleDuration,
                0f
            );
    }

    public Vector3 GetStartPosition(
        BoardGrid boardGrid,
        Vector3 targetPosition)
    {
        if (boardGrid == null)
        {
            return targetPosition;
        }

        Normalize();

        runtimeCellSize =
            Mathf.Max(
                boardGrid.CellSize,
                0.01f
            );

        /*
         * 패턴 전체를 같은 거리만큼 위로 이동한다.
         *
         * 보드가 15행이면 가장 아래쪽 행은
         * 최상단보다 14칸 아래에 있으므로,
         * 14칸 + 추가 오프셋만큼 패턴 전체를 올린다.
         *
         * 이렇게 하면 패턴의 상대적인 행 간격을
         * 그대로 유지하면서 모든 블록이 화면 위에서
         * 내려오기 시작한다.
         */
        float patternLiftCells =
            Mathf.Max(
                boardGrid.RowCount - 1,
                0
            ) +
            spawnHeightOffsetCells;

        Vector3 entranceOffset =
            boardGrid.transform.up.normalized *
            runtimeCellSize *
            patternLiftCells;

        return targetPosition +
               entranceOffset;
    }

    public IEnumerator PlayRoutine(
        IReadOnlyList<BossPatternEntranceItem>
            entranceItems)
    {
        Normalize();

        if (entranceItems == null ||
            entranceItems.Count == 0)
        {
            yield break;
        }

        FindPatternRowRange(
            entranceItems,
            out int minimumPatternRow,
            out int maximumPatternRow
        );

        float elapsedTime = 0f;

        while (true)
        {
            bool allItemsCompleted =
                true;

            for (int i = 0;
                 i < entranceItems.Count;
                 i++)
            {
                BossPatternEntranceItem item =
                    entranceItems[i];

                if (item == null ||
                    item.Block == null)
                {
                    continue;
                }

                float itemDelay =
                    CalculateItemDelay(
                        item,
                        minimumPatternRow,
                        maximumPatternRow
                    );

                float itemElapsedTime =
                    elapsedTime -
                    itemDelay;

                if (itemElapsedTime < 0f)
                {
                    allItemsCompleted =
                        false;

                    continue;
                }

                Vector3 fallDirection =
                    GetFallDirection(
                        item
                    );

                Vector3 impactPosition =
                    item.TargetPosition +
                    fallDirection *
                    runtimeCellSize *
                    impactOvershootCells;

                float fallEndTime =
                    entranceDuration;

                float impactEndTime =
                    fallEndTime +
                    impactPause;

                float settleEndTime =
                    impactEndTime +
                    settleDuration;

                if (itemElapsedTime <
                    fallEndTime)
                {
                    allItemsCompleted =
                        false;

                    float fallProgress =
                        entranceDuration <= 0f
                            ? 1f
                            : Mathf.Clamp01(
                                itemElapsedTime /
                                entranceDuration
                            );

                    /*
                     * 초반에는 조금 천천히 움직이고
                     * 착지 직전에 속도가 붙도록 해서
                     * 블록이 떨어지는 느낌을 강화한다.
                     */
                    float easedFallProgress =
                        EaseInCubic(
                            fallProgress
                        );

                    Vector3 currentPosition =
                        Vector3.Lerp(
                            item.StartPosition,
                            impactPosition,
                            easedFallProgress
                        );

                    item.SetPosition(
                        currentPosition
                    );

                    continue;
                }

                if (itemElapsedTime <
                    impactEndTime)
                {
                    allItemsCompleted =
                        false;

                    item.SetPosition(
                        impactPosition
                    );

                    continue;
                }

                if (itemElapsedTime <
                    settleEndTime)
                {
                    allItemsCompleted =
                        false;

                    float settleProgress =
                        settleDuration <= 0f
                            ? 1f
                            : Mathf.Clamp01(
                                (
                                    itemElapsedTime -
                                    impactEndTime
                                ) /
                                settleDuration
                            );

                    float easedSettleProgress =
                        EaseOutQuad(
                            settleProgress
                        );

                    Vector3 currentPosition =
                        Vector3.Lerp(
                            impactPosition,
                            item.TargetPosition,
                            easedSettleProgress
                        );

                    item.SetPosition(
                        currentPosition
                    );

                    continue;
                }

                item.SetPosition(
                    item.TargetPosition
                );
            }

            if (allItemsCompleted)
            {
                break;
            }

            elapsedTime +=
                Time.deltaTime;

            yield return null;
        }

        /*
         * 모든 블록의 이동이 끝난 뒤에만
         * Collider를 다시 활성화한다.
         *
         * 이동 중 실제 물리 충돌로 인해
         * 격자 위치가 틀어지는 현상을 방지한다.
         */
        for (int i = 0;
             i < entranceItems.Count;
             i++)
        {
            entranceItems[i]?.Complete();
        }
    }

    private float CalculateItemDelay(
        BossPatternEntranceItem item,
        int minimumPatternRow,
        int maximumPatternRow)
    {
        int rowDifference;

        if (lowerRowsFirst)
        {
            rowDifference =
                maximumPatternRow -
                item.PatternRow;
        }
        else
        {
            rowDifference =
                item.PatternRow -
                minimumPatternRow;
        }

        return Mathf.Max(
                   rowDifference,
                   0
               ) *
               rowDelay;
    }

    private Vector3 GetFallDirection(
        BossPatternEntranceItem item)
    {
        Vector3 movementDirection =
            item.TargetPosition -
            item.StartPosition;

        if (movementDirection.sqrMagnitude <=
            0.0001f)
        {
            return Vector3.down;
        }

        return movementDirection.normalized;
    }

    private void FindPatternRowRange(
        IReadOnlyList<BossPatternEntranceItem>
            entranceItems,
        out int minimumPatternRow,
        out int maximumPatternRow)
    {
        minimumPatternRow =
            int.MaxValue;

        maximumPatternRow =
            int.MinValue;

        for (int i = 0;
             i < entranceItems.Count;
             i++)
        {
            BossPatternEntranceItem item =
                entranceItems[i];

            if (item == null ||
                item.Block == null)
            {
                continue;
            }

            minimumPatternRow =
                Mathf.Min(
                    minimumPatternRow,
                    item.PatternRow
                );

            maximumPatternRow =
                Mathf.Max(
                    maximumPatternRow,
                    item.PatternRow
                );
        }

        if (minimumPatternRow ==
            int.MaxValue)
        {
            minimumPatternRow = 0;
        }

        if (maximumPatternRow ==
            int.MinValue)
        {
            maximumPatternRow = 0;
        }
    }

    private static float EaseInCubic(
        float progress)
    {
        progress =
            Mathf.Clamp01(
                progress
            );

        return progress *
               progress *
               progress;
    }

    private static float EaseOutQuad(
        float progress)
    {
        progress =
            Mathf.Clamp01(
                progress
            );

        return 1f -
               (
                   1f -
                   progress
               ) *
               (
                   1f -
                   progress
               );
    }
}