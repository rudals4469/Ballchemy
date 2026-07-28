using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class NextBallQueuePanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BallTurnQueueController queueController;

    [SerializeField]
    private NextBallQueueItemView itemPrefab;

    [SerializeField]
    private RectTransform itemRoot;

    [SerializeField]
    private Image overflowMarkerImage;

    [Header("Queue Position Points")]
    [SerializeField]
    private RectTransform spawnPoint;

    [SerializeField]
    private RectTransform queueTopPoint;

    [SerializeField]
    private RectTransform queueBottomPoint;

    [SerializeField]
    private RectTransform launchExitPoint;

    [Header("Display")]
    [SerializeField, Min(1)]
    private int maximumVisibleCount = 10;

    [Header("Star Display")]
    [SerializeField]
    private bool hideStarsAfterQueueStartsMoving = true;

    [Header("Continuous Flow")]
    [Tooltip(
        "값이 커질수록 공이 목표 위치를 천천히 따라가며 " +
        "아래로 흐르는 느낌이 강해집니다."
    )]
    [SerializeField, Min(0.01f)]
    private float flowSmoothTime = 0.1f;

    [Tooltip(
        "공이 목표 위치를 따라갈 수 있는 최대 이동 속도입니다."
    )]
    [SerializeField, Min(1f)]
    private float flowMaxSpeed = 2500f;

    [Header("Exit Animation")]
    [Tooltip(
        "맨 아래 공이 화면 아래로 빠지는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float exitDuration = 0.14f;

    [Tooltip(
        "밑으로 내려갈수록 속도가 붙는 느낌을 위해 " +
        "In Quad 또는 In Cubic을 권장합니다."
    )]
    [SerializeField]
    private Ease exitEase = Ease.InQuad;

    [Header("Turn Start")]
    [Tooltip(
        "새 턴을 준비할 때 공이 시작 위치보다 " +
        "조금 위에서 내려오는 거리입니다."
    )]
    [SerializeField, Min(0f)]
    private float turnStartOffsetY = 10f;

    private readonly List<NextBallQueueItemView>
        activeItems =
            new List<NextBallQueueItemView>();

    private readonly List<Ball>
        upcomingBalls =
            new List<Ball>();

    private bool hasQueueStartedMoving;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
        ValidateReferences();
        RefreshOverflowMarker();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();

        if (queueController != null)
        {
            hasQueueStartedMoving =
                queueController.IsTurnActive &&
                queueController.NextLaunchIndex > 0;
        }

        if (Application.isPlaying &&
            queueController != null &&
            queueController.IsQueuePrepared)
        {
            RebuildQueue(false);
        }
        else
        {
            RefreshOverflowMarker();
        }
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        FindReferences();
        NormalizeSettings();
    }

    private void FindReferences()
    {
        if (queueController == null &&
            Application.isPlaying)
        {
            queueController =
                FindFirstObjectByType<
                    BallTurnQueueController
                >();
        }
    }

    private void NormalizeSettings()
    {
        maximumVisibleCount =
            Mathf.Clamp(
                maximumVisibleCount,
                1,
                100
            );

        flowSmoothTime =
            Mathf.Max(
                flowSmoothTime,
                0.01f
            );

        flowMaxSpeed =
            Mathf.Max(
                flowMaxSpeed,
                1f
            );

        exitDuration =
            Mathf.Max(
                exitDuration,
                0f
            );

        turnStartOffsetY =
            Mathf.Max(
                turnStartOffsetY,
                0f
            );
    }

    private void ValidateReferences()
    {
        if (queueController == null)
        {
            Debug.LogWarning(
                "NextBallQueuePanel: " +
                "Queue Controller가 연결되지 않았습니다.",
                this
            );
        }

        if (itemPrefab == null)
        {
            Debug.LogError(
                "NextBallQueuePanel: " +
                "Item Prefab이 연결되지 않았습니다.",
                this
            );
        }

        if (itemRoot == null)
        {
            Debug.LogError(
                "NextBallQueuePanel: " +
                "Item Root가 연결되지 않았습니다.",
                this
            );
        }

        if (spawnPoint == null)
        {
            Debug.LogError(
                "NextBallQueuePanel: " +
                "Spawn Point가 연결되지 않았습니다.",
                this
            );
        }

        if (queueTopPoint == null)
        {
            Debug.LogError(
                "NextBallQueuePanel: " +
                "Queue Top Point가 연결되지 않았습니다.",
                this
            );
        }

        if (queueBottomPoint == null)
        {
            Debug.LogError(
                "NextBallQueuePanel: " +
                "Queue Bottom Point가 연결되지 않았습니다.",
                this
            );
        }

        if (launchExitPoint == null)
        {
            Debug.LogError(
                "NextBallQueuePanel: " +
                "Launch Exit Point가 연결되지 않았습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (queueController == null)
        {
            return;
        }

        queueController.TurnQueuePrepared -=
            HandleTurnQueuePrepared;

        queueController.TurnQueuePrepared +=
            HandleTurnQueuePrepared;

        queueController.BallLaunchedFromQueue -=
            HandleBallLaunchedFromQueue;

        queueController.BallLaunchedFromQueue +=
            HandleBallLaunchedFromQueue;
    }

    private void UnsubscribeEvents()
    {
        if (queueController == null)
        {
            return;
        }

        queueController.TurnQueuePrepared -=
            HandleTurnQueuePrepared;

        queueController.BallLaunchedFromQueue -=
            HandleBallLaunchedFromQueue;
    }

    private void HandleTurnQueuePrepared()
    {
        hasQueueStartedMoving = false;

        RebuildQueue(true);
    }

    private void HandleBallLaunchedFromQueue(
        Ball launchedBall,
        int launchedQueueIndex)
    {
        hasQueueStartedMoving = true;

        if (hideStarsAfterQueueStartsMoving)
        {
            SetStarsVisibleForAllItems(false);
        }

        if (activeItems.Count <= 0)
        {
            RebuildQueue(false);
            return;
        }

        NextBallQueueItemView launchedItem =
            activeItems[0];

        if (launchedItem == null ||
            launchedItem.BoundBall != launchedBall)
        {
            RebuildQueue(false);
            return;
        }

        activeItems.RemoveAt(0);

        launchedItem.PlayExit(
            ConvertPointToItemRootPosition(
                launchExitPoint
            ),
            exitDuration,
            exitEase
        );

        RefreshAfterLaunch();
    }

    private void RefreshAfterLaunch()
    {
        CopyUpcomingBalls();
        RefreshOverflowMarker();

        if (!DoExistingItemsMatchQueue())
        {
            RebuildQueue(false);
            return;
        }

        while (activeItems.Count >
               upcomingBalls.Count)
        {
            int lastIndex =
                activeItems.Count - 1;

            NextBallQueueItemView unusedItem =
                activeItems[lastIndex];

            activeItems.RemoveAt(lastIndex);

            if (unusedItem != null)
            {
                unusedItem.DisposeImmediate();
            }
        }

        /*
         * 기존 공에는 새 Tween을 걸지 않습니다.
         * 목표 위치만 한 칸 아래로 변경합니다.
         *
         * 공은 현재 속도를 유지한 채 새로운 목표 위치를
         * 계속 따라가기 때문에 움직임이 끊기지 않습니다.
         */
        for (int i = 0;
             i < activeItems.Count;
             i++)
        {
            NextBallQueueItemView item =
                activeItems[i];

            if (item == null)
            {
                continue;
            }

            item.SetStarVisible(
                ShouldShowStars()
            );

            item.SetTargetState(
                ResolveQueuePosition(i),
                i == 0
            );
        }

        /*
         * 상단에 새로 들어오는 공은 SpawnPoint에서 시작해서
         * 자신의 큐 위치를 자연스럽게 따라갑니다.
         */
        for (int i = activeItems.Count;
             i < upcomingBalls.Count;
             i++)
        {
            Ball newBall =
                upcomingBalls[i];

            NextBallQueueItemView newItem =
                CreateItem(newBall);

            if (newItem == null)
            {
                continue;
            }

            activeItems.Add(newItem);

            newItem.PlayEnter(
                ConvertPointToItemRootPosition(
                    spawnPoint
                ),
                ResolveQueuePosition(i),
                i == 0
            );
        }
    }

    private void RebuildQueue(
        bool playEntranceAnimation)
    {
        ClearActiveItems();
        CopyUpcomingBalls();
        RefreshOverflowMarker();

        for (int i = 0;
             i < upcomingBalls.Count;
             i++)
        {
            Ball ball =
                upcomingBalls[i];

            NextBallQueueItemView item =
                CreateItem(ball);

            if (item == null)
            {
                continue;
            }

            activeItems.Add(item);

            Vector3 targetPosition =
                ResolveQueuePosition(i);

            bool isNextBall =
                i == 0;

            if (!playEntranceAnimation)
            {
                item.SetImmediateState(
                    targetPosition,
                    isNextBall
                );

                continue;
            }

            Vector3 startPosition =
                targetPosition +
                Vector3.up *
                turnStartOffsetY;

            item.PlayEnter(
                startPosition,
                targetPosition,
                isNextBall
            );
        }
    }

    private NextBallQueueItemView CreateItem(
        Ball ball)
    {
        if (itemPrefab == null ||
            itemRoot == null)
        {
            return null;
        }

        NextBallQueueItemView item =
            Instantiate(
                itemPrefab,
                itemRoot
            );

        item.name =
            ball != null
                ? $"QueueItem_{ball.name}"
                : "QueueItem_None";

        item.ConfigureFlow(
            flowSmoothTime,
            flowMaxSpeed
        );

        item.Bind(ball);

        item.SetStarVisible(
            ShouldShowStars()
        );

        return item;
    }

    private void CopyUpcomingBalls()
    {
        upcomingBalls.Clear();

        if (queueController == null)
        {
            return;
        }

        queueController.CopyUpcomingBalls(
            upcomingBalls,
            maximumVisibleCount
        );
    }

    private void RefreshOverflowMarker()
    {
        if (overflowMarkerImage == null)
        {
            return;
        }

        bool shouldShowOverflow =
            queueController != null &&
            queueController.IsQueuePrepared &&
            queueController.RemainingBallCount >
            maximumVisibleCount;

        overflowMarkerImage.gameObject.SetActive(
            shouldShowOverflow
        );
    }

    private Vector3 ResolveQueuePosition(
        int upcomingIndex)
    {
        if (queueTopPoint == null ||
            queueBottomPoint == null ||
            itemRoot == null)
        {
            return Vector3.zero;
        }

        float normalizedPosition;

        if (maximumVisibleCount <= 1)
        {
            normalizedPosition = 0f;
        }
        else
        {
            normalizedPosition =
                Mathf.Clamp01(
                    (float)upcomingIndex /
                    (maximumVisibleCount - 1)
                );
        }

        Vector3 worldPosition =
            Vector3.Lerp(
                queueBottomPoint.position,
                queueTopPoint.position,
                normalizedPosition
            );

        return itemRoot.InverseTransformPoint(
            worldPosition
        );
    }

    private Vector3 ConvertPointToItemRootPosition(
        RectTransform targetPoint)
    {
        if (itemRoot == null ||
            targetPoint == null)
        {
            return Vector3.zero;
        }

        return itemRoot.InverseTransformPoint(
            targetPoint.position
        );
    }

    private bool ShouldShowStars()
    {
        if (!hideStarsAfterQueueStartsMoving)
        {
            return true;
        }

        return !hasQueueStartedMoving;
    }

    private void SetStarsVisibleForAllItems(
        bool visible)
    {
        for (int i = 0;
             i < activeItems.Count;
             i++)
        {
            NextBallQueueItemView item =
                activeItems[i];

            if (item == null)
            {
                continue;
            }

            item.SetStarVisible(visible);
        }
    }

    private bool DoExistingItemsMatchQueue()
    {
        int comparableCount =
            Mathf.Min(
                activeItems.Count,
                upcomingBalls.Count
            );

        for (int i = 0;
             i < comparableCount;
             i++)
        {
            NextBallQueueItemView item =
                activeItems[i];

            if (item == null ||
                item.BoundBall !=
                upcomingBalls[i])
            {
                return false;
            }
        }

        return true;
    }

    private void ClearActiveItems()
    {
        for (int i = 0;
             i < activeItems.Count;
             i++)
        {
            NextBallQueueItemView item =
                activeItems[i];

            if (item == null)
            {
                continue;
            }

            item.DisposeImmediate();
        }

        activeItems.Clear();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        ClearActiveItems();
    }
}