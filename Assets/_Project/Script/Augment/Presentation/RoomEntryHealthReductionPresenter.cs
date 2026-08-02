using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public readonly struct
    RoomEntryHealthReductionResult
{
    public Block Target
    {
        get;
    }

    public int ReducedHealth
    {
        get;
    }

    public RoomEntryHealthReductionResult(
        Block target,
        int reducedHealth)
    {
        Target =
            target;

        ReducedHealth =
            Mathf.Max(
                reducedHealth,
                0
            );
    }
}

[DisallowMultipleComponent]
public sealed class
    RoomEntryHealthReductionPresenter :
        MonoBehaviour
{
    [Header("Popup")]

    [Tooltip(
        "선제 연금 체력 감소 숫자에 사용할 " +
        "데미지 텍스트 스타일입니다."
    )]
    [SerializeField]
    private BallDamageTextStyleDefinition
        popupStyle;

    [Header("Timing")]

    [Tooltip(
        "방 블록 생성 후 연출을 시작하기 전 " +
        "대기 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float initialDelay = 0.1f;

    [Tooltip(
        "각 블록 효과 사이의 시간 간격입니다. " +
        "0이면 모든 블록이 같은 프레임에 반응합니다."
    )]
    [SerializeField, Min(0f)]
    private float blockInterval = 0.025f;

    [Header("Block Feedback")]

    [Tooltip(
        "블록이 밀리는 월드 방향입니다."
    )]
    [SerializeField]
    private Vector2 recoilDirection =
        Vector2.down;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog;

    private readonly List<
        RoomEntryHealthReductionResult
    > pendingResults =
        new List<
            RoomEntryHealthReductionResult
        >();

    private Coroutine presentationCoroutine;

    private void OnValidate()
    {
        initialDelay =
            Mathf.Max(
                initialDelay,
                0f
            );

        blockInterval =
            Mathf.Max(
                blockInterval,
                0f
            );

        if (recoilDirection.sqrMagnitude <=
            0.0001f)
        {
            recoilDirection =
                Vector2.down;
        }
    }

    public void Play(
        IReadOnlyList<
            RoomEntryHealthReductionResult
        > results)
    {
        StopCurrentPresentation();

        pendingResults.Clear();

        if (results == null ||
            results.Count == 0)
        {
            return;
        }

        for (int i = 0;
             i < results.Count;
             i++)
        {
            RoomEntryHealthReductionResult result =
                results[i];

            if (result.Target == null ||
                result.ReducedHealth <= 0)
            {
                continue;
            }

            pendingResults.Add(
                result
            );
        }

        if (pendingResults.Count == 0)
        {
            return;
        }

        presentationCoroutine =
            StartCoroutine(
                PlayRoutine()
            );
    }

    private IEnumerator PlayRoutine()
    {
        if (initialDelay > 0f)
        {
            yield return
                new WaitForSeconds(
                    initialDelay
                );
        }

        for (int i = 0;
             i < pendingResults.Count;
             i++)
        {
            RoomEntryHealthReductionResult result =
                pendingResults[i];

            Block target =
                result.Target;

            if (target == null)
            {
                continue;
            }

            PlayBlockFeedback(
                target
            );

            PublishPopup(
                target,
                result.ReducedHealth
            );

            if (blockInterval > 0f &&
                i <
                pendingResults.Count - 1)
            {
                yield return
                    new WaitForSeconds(
                        blockInterval
                    );
            }
        }

        if (showDebugLog)
        {
            Debug.Log(
                "RoomEntryHealthReductionPresenter: " +
                $"선제 연금 연출 " +
                $"{pendingResults.Count}개 완료",
                this
            );
        }

        pendingResults.Clear();
        presentationCoroutine = null;
    }

    private void PlayBlockFeedback(
        Block target)
    {
        if (target == null)
        {
            return;
        }

        BlockHitFeedback feedback =
            target.GetComponent<
                BlockHitFeedback
            >();

        if (feedback == null)
        {
            feedback =
                target.GetComponentInChildren<
                    BlockHitFeedback
                >(
                    true
                );
        }

        if (feedback == null)
        {
            return;
        }

        feedback.PlayHit(
            recoilDirection.normalized
        );
    }

    private void PublishPopup(
        Block target,
        int reducedHealth)
    {
        if (target == null ||
            reducedHealth <= 0)
        {
            return;
        }

        Vector2 popupPosition =
            target.transform.position;

        BallDamageEvents.Publish(
            new BallDamageEvent(
                null,
                null,
                target,
                reducedHealth,
                reducedHealth,
                popupPosition,
                popupStyle
            )
        );
    }

    private void StopCurrentPresentation()
    {
        if (presentationCoroutine != null)
        {
            StopCoroutine(
                presentationCoroutine
            );

            presentationCoroutine = null;
        }

        pendingResults.Clear();
    }

    private void OnDisable()
    {
        StopCurrentPresentation();
    }

    private void OnDestroy()
    {
        StopCurrentPresentation();
    }
}