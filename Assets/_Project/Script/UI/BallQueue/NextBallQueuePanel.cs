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

    private void ConfigureProportionalFlaskVisual()
    {
        ConfigureFlaskLayer("FlaskFrame", false);
        ConfigureFlaskLayer("FlaskGlassOverlay", true);
    }

    private void ConfigureFlaskLayer(string childName, bool isGlass)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            return;
        }

        Image stretchedImage = child.GetComponent<Image>();
        if (stretchedImage != null)
        {
            stretchedImage.enabled = false;
        }

        QueueFlaskProportionalGraphic graphic =
            child.GetComponent<QueueFlaskProportionalGraphic>();
        if (graphic == null)
        {
            graphic = child.gameObject.AddComponent<QueueFlaskProportionalGraphic>();
        }

        graphic.Configure(isGlass);
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

        // A prepared queue should already look settled. Only balls revealed
        // later enter from above; rebuilding every item here made the whole
        // column move whenever the queue was refreshed.
        RebuildQueue(false);
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
                ConvertPointToItemRootPosition(
                    spawnPoint
                );

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

/// <summary>
/// 큐 패널의 세로 길이와 무관하게 하단 플라스크 구체를 원형으로 유지한다.
/// 목 부분만 남는 높이를 채우므로 한 장짜리 스프라이트 Stretch 왜곡이 없다.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
internal sealed class QueueFlaskProportionalGraphic : MaskableGraphic
{
    private const int BulbSegments = 24;
    private bool drawGlass;

    public void Configure(bool isGlass)
    {
        drawGlass = isGlass;
        raycastTarget = false;
        color = isGlass
            ? new Color(0.72f, 0.9f, 0.98f, 0.13f)
            : new Color(0.08f, 0.14f, 0.22f, 0.96f);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;
        float outline = Mathf.Clamp(rect.width * 0.055f, 3f, 6f);
        List<Vector2> outer = BuildBottleContour(rect, 0f);

        if (drawGlass)
        {
            AddFilledPolygon(vh, outer, color);
            return;
        }

        List<Vector2> inner = BuildBottleContour(rect, outline);
        AddContourRing(vh, outer, inner, color);
        AddTopRim(vh, rect, outline, color);
        AddTubeTicks(vh, rect, outline, color);
    }

    private static List<Vector2> BuildBottleContour(Rect rect, float inset)
    {
        float halfWidth = Mathf.Max(rect.width * 0.5f - inset, 1f);
        float bulbRadius = Mathf.Max(halfWidth - 2f, 1f);
        float bottom = rect.yMin + inset;
        float centerY = bottom + bulbRadius;
        float neckHalfWidth = Mathf.Max(rect.width * 0.19f - inset * 0.25f, 4f);
        float neckTop = rect.yMax - Mathf.Max(rect.width * 0.12f, 8f) - inset;
        float shoulderAngle = 48f * Mathf.Deg2Rad;

        List<Vector2> points = new List<Vector2>(BulbSegments + 6)
        {
            new Vector2(-neckHalfWidth, neckTop),
            new Vector2(-neckHalfWidth, centerY + bulbRadius * 0.72f)
        };

        for (int i = 0; i <= BulbSegments; i++)
        {
            float t = i / (float)BulbSegments;
            float angle = Mathf.Lerp(
                Mathf.PI - shoulderAngle,
                Mathf.PI * 2f + shoulderAngle,
                t);
            points.Add(new Vector2(
                Mathf.Cos(angle) * bulbRadius,
                centerY + Mathf.Sin(angle) * bulbRadius));
        }

        points.Add(new Vector2(neckHalfWidth, centerY + bulbRadius * 0.72f));
        points.Add(new Vector2(neckHalfWidth, neckTop));
        return points;
    }

    private static void AddFilledPolygon(
        VertexHelper vh,
        List<Vector2> points,
        Color tint)
    {
        Vector2 center = Vector2.zero;
        for (int i = 0; i < points.Count; i++)
        {
            center += points[i];
        }
        center /= points.Count;

        int centerIndex = vh.currentVertCount;
        vh.AddVert(center, tint, Vector2.zero);
        for (int i = 0; i < points.Count; i++)
        {
            vh.AddVert(points[i], tint, Vector2.zero);
        }

        for (int i = 0; i < points.Count; i++)
        {
            vh.AddTriangle(
                centerIndex,
                centerIndex + 1 + i,
                centerIndex + 1 + (i + 1) % points.Count);
        }
    }

    private static void AddContourRing(
        VertexHelper vh,
        List<Vector2> outer,
        List<Vector2> inner,
        Color tint)
    {
        int count = Mathf.Min(outer.Count, inner.Count);
        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;
            int start = vh.currentVertCount;
            vh.AddVert(outer[i], tint, Vector2.zero);
            vh.AddVert(outer[next], tint, Vector2.zero);
            vh.AddVert(inner[next], tint, Vector2.zero);
            vh.AddVert(inner[i], tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }

    private static void AddTopRim(
        VertexHelper vh,
        Rect rect,
        float outline,
        Color tint)
    {
        float rimWidth = rect.width * 0.72f;
        float rimHeight = Mathf.Max(rect.width * 0.1f, 7f);
        float y = rect.yMax - rimHeight * 0.5f;
        AddQuad(vh,
            new Rect(-rimWidth * 0.5f, y - rimHeight * 0.5f, rimWidth, rimHeight),
            tint);

        if (outline > 0f)
        {
            AddQuad(vh,
                new Rect(-rimWidth * 0.5f + outline, y - outline * 0.35f,
                    rimWidth - outline * 2f, outline * 0.7f),
                new Color(0.72f, 0.9f, 0.98f, 0.8f));
        }
    }

    private static void AddTubeTicks(
        VertexHelper vh,
        Rect rect,
        float outline,
        Color tint)
    {
        float bulbRadius = rect.width * 0.5f - 2f;
        float bulbCenterY = rect.yMin + bulbRadius;
        float firstY = bulbCenterY + bulbRadius * 0.95f;
        float lastY = rect.yMax - Mathf.Max(rect.width * 0.18f, 14f);
        float neckEdge = rect.width * 0.19f;
        float spacing = Mathf.Max(rect.width * 0.42f, 28f);
        float tickHeight = Mathf.Max(outline * 0.55f, 2f);
        int index = 0;

        for (float y = firstY; y < lastY; y += spacing)
        {
            bool major = index % 5 == 0;
            float length = major
                ? rect.width * 0.16f
                : rect.width * 0.1f;

            AddQuad(vh,
                new Rect(
                    neckEdge + outline * 0.25f,
                    y - tickHeight * 0.5f,
                    length,
                    tickHeight),
                tint);
            index++;
        }
    }

    private static void AddQuad(VertexHelper vh, Rect rect, Color tint)
    {
        int start = vh.currentVertCount;
        vh.AddVert(new Vector2(rect.xMin, rect.yMin), tint, Vector2.zero);
        vh.AddVert(new Vector2(rect.xMin, rect.yMax), tint, Vector2.zero);
        vh.AddVert(new Vector2(rect.xMax, rect.yMax), tint, Vector2.zero);
        vh.AddVert(new Vector2(rect.xMax, rect.yMin), tint, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }
}
