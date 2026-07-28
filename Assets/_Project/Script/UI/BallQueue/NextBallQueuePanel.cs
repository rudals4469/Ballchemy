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

    [Tooltip(
        "남은 공이 표시 제한 개수보다 많을 때 " +
        "큐 상단에 표시할 세로 점 이미지입니다."
    )]
    [SerializeField]
    private Image overflowMarkerImage;

    [Header("Queue Position Points")]
    [Tooltip(
        "새로운 공이 큐 위쪽에서 등장하는 시작 위치입니다."
    )]
    [SerializeField]
    private RectTransform spawnPoint;

    [Tooltip(
        "큐에 표시되는 공들의 가장 위쪽 위치입니다."
    )]
    [SerializeField]
    private RectTransform queueTopPoint;

    [Tooltip(
        "바로 다음에 발사될 공이 위치하는 가장 아래쪽 위치입니다."
    )]
    [SerializeField]
    private RectTransform queueBottomPoint;

    [Tooltip(
        "발사된 공 UI가 아래쪽으로 빠져나가는 위치입니다."
    )]
    [SerializeField]
    private RectTransform launchExitPoint;

    [Header("Display")]
    [Tooltip(
        "화면에 직접 표시할 최대 공 개수입니다. " +
        "이 개수보다 남은 공이 많으면 오버플로 이미지를 표시합니다."
    )]
    [SerializeField, Min(1)]
    private int maximumVisibleCount = 10;

    [Header("Star Display")]
    [Tooltip(
        "첫 번째 공이 발사되어 큐가 움직이기 시작하면 " +
        "별 표시를 숨깁니다."
    )]
    [SerializeField]
    private bool hideStarsAfterQueueStartsMoving = true;

    [Header("Move Animation")]
    [SerializeField, Min(0f)]
    private float moveDuration = 0.18f;

    [SerializeField]
    private Ease moveEase = Ease.OutCubic;

    [Header("Exit Animation")]
    [SerializeField, Min(0f)]
    private float exitDuration = 0.12f;

    [SerializeField, Range(0f, 1f)]
    private float exitScale = 0.7f;

    [SerializeField]
    private Ease exitEase = Ease.InQuad;

    [Header("Turn Start Animation")]
    [SerializeField, Min(0f)]
    private float turnStartOffsetY = 40f;

    [SerializeField, Min(0f)]
    private float turnStartDelayPerItem = 0.015f;

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

        moveDuration =
            Mathf.Max(
                moveDuration,
                0f
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

        turnStartDelayPerItem =
            Mathf.Max(
                turnStartDelayPerItem,
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

        if (overflowMarkerImage == null)
        {
            Debug.LogError(
                "NextBallQueuePanel: " +
                "Overflow Marker Image가 연결되지 않았습니다.",
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
            exitScale,
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

            item.PlayMove(
                ResolveQueuePosition(i),
                moveDuration,
                moveEase,
                i == 0
            );
        }

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
                moveDuration,
                0f,
                moveEase,
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
                moveDuration,
                i * turnStartDelayPerItem,
                moveEase,
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