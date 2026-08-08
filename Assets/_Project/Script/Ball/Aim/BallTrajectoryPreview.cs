using System.Collections.Generic;
using UnityEngine;

public sealed class BallTrajectoryPreview :
    MonoBehaviour
{
    private const int MaximumPreviewTeleports = 4;

    [Header("References")]
    [Tooltip(
        "Unity 기본 Circle Sprite로 만든 " +
        "AimDot 프리팹입니다."
    )]
    [SerializeField]
    private SpriteRenderer aimDotPrefab;

    [Tooltip(
        "생성된 점들을 담아둘 AimDots 오브젝트입니다."
    )]
    [SerializeField]
    private Transform dotsRoot;

    [SerializeField]
    private Ball ballPrefab;

    [Tooltip(
        "기존 LineRenderer입니다. " +
        "연결하지 않아도 자동으로 찾아 비활성화합니다."
    )]
    [SerializeField]
    private LineRenderer legacyAimLine;

    [Header("Short Aim Dots")]
    [Tooltip(
        "마우스를 움직일 때 표시되는 " +
        "짧은 조준 경로의 전체 길이입니다."
    )]
    [SerializeField, Min(0.1f)]
    private float shortAimDistance = 1.75f;

    [Tooltip(
        "짧은 조준 경로의 최대 반사 횟수입니다."
    )]
    [SerializeField, Range(0, 3)]
    private int shortAimMaximumBounces = 1;

    [Tooltip(
        "짧은 조준 경로 점 사이의 간격입니다."
    )]
    [SerializeField, Min(0.05f)]
    private float shortDotSpacing = 0.25f;

    [Tooltip(
        "AimDot 프리팹 크기에 곱해지는 값입니다."
    )]
    [SerializeField, Min(0.01f)]
    private float shortDotScaleMultiplier = 1f;

    [SerializeField]
    private Color shortDotColor =
        new Color(
            1f,
            1f,
            1f,
            0.95f
        );

    [Header("Long Trajectory Dots")]
    [Tooltip(
        "긴 예상 경로가 끝까지 나타나는 데 " +
        "걸리는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float trajectoryRevealDuration = 0.45f;

    [Tooltip(
        "긴 예상 경로 점 사이의 간격입니다."
    )]
    [SerializeField, Min(0.05f)]
    private float longDotSpacing = 0.32f;

    [Tooltip(
        "AimDot 프리팹 크기에 곱해지는 값입니다."
    )]
    [SerializeField, Min(0.01f)]
    private float longDotScaleMultiplier = 0.9f;

    [SerializeField]
    private Color longDotColor =
        new Color(
            1f,
            1f,
            1f,
            0.6f
        );

    [Header("Dot Placement")]
    [Tooltip(
        "발사 위치와 첫 번째 점 사이의 거리입니다. " +
        "발사체와 점이 겹치는 것을 방지합니다."
    )]
    [SerializeField, Min(0f)]
    private float firstDotOffset = 0.15f;

    [Tooltip(
        "게임 시작 시 미리 생성해둘 점 개수입니다. " +
        "부족하면 자동으로 추가 생성됩니다."
    )]
    [SerializeField, Min(0)]
    private int prewarmDotCount = 120;

    [Header("Trajectory Calculation")]
    [Tooltip(
        "긴 예상 경로의 최대 전체 길이입니다."
    )]
    [SerializeField, Min(1f)]
    private float trajectoryDistance = 35f;

    [Tooltip(
        "긴 예상 경로의 최대 반사 횟수입니다."
    )]
    [SerializeField, Range(0, 20)]
    private int maximumTrajectoryBounces = 8;

    [SerializeField]
    private BallBounceResolver bounceResolver =
        new BallBounceResolver();

    [Tooltip(
        "Ball Prefab의 CircleCollider2D 크기를 " +
        "예상 경로 계산에 사용합니다."
    )]
    [SerializeField]
    private bool useBallPrefabRadius = true;

    [Tooltip(
        "Ball Prefab 반지름을 사용하지 않을 때 " +
        "적용되는 공 반지름입니다."
    )]
    [SerializeField, Min(0.01f)]
    private float manualTrajectoryRadius = 0.2f;

    [Tooltip(
        "반사 직후 같은 충돌면에 다시 걸리지 않도록 " +
        "충돌면에서 떨어뜨리는 거리입니다."
    )]
    [SerializeField, Min(0.001f)]
    private float trajectorySkinWidth = 0.02f;

    [Tooltip(
        "벽, 블록, ReturnZone 레이어가 " +
        "포함되어야 합니다."
    )]
    [SerializeField]
    private LayerMask trajectoryCollisionMask = ~0;

    private readonly List<Vector3>
        shortTrajectoryPoints =
            new List<Vector3>();

    private readonly List<Vector3>
        trajectoryPoints =
            new List<Vector3>();

    private readonly List<SpriteRenderer>
        dotPool =
            new List<SpriteRenderer>();

    private Vector3 prefabDotScale =
        Vector3.one;

    private float cachedTrajectoryRadius;
    private float trajectoryRevealTimer;
    private float trajectoryTotalLength;

    private bool isLongPreviewActive;

    private void Awake()
    {
        EnsureHelpers();
        FindReferences();
        ValidateReferences();

        DisableLegacyLine();
        InitializeTrajectoryRadius();
        InitializeDotPool();
    }

    private void OnValidate()
    {
        shortAimDistance =
            Mathf.Max(
                shortAimDistance,
                0.1f
            );

        shortDotSpacing =
            Mathf.Max(
                shortDotSpacing,
                0.05f
            );

        longDotSpacing =
            Mathf.Max(
                longDotSpacing,
                0.05f
            );

        shortDotScaleMultiplier =
            Mathf.Max(
                shortDotScaleMultiplier,
                0.01f
            );

        longDotScaleMultiplier =
            Mathf.Max(
                longDotScaleMultiplier,
                0.01f
            );

        firstDotOffset =
            Mathf.Max(
                firstDotOffset,
                0f
            );

        prewarmDotCount =
            Mathf.Max(
                prewarmDotCount,
                0
            );

        trajectoryRevealDuration =
            Mathf.Max(
                trajectoryRevealDuration,
                0f
            );

        trajectoryDistance =
            Mathf.Max(
                trajectoryDistance,
                1f
            );

        manualTrajectoryRadius =
            Mathf.Max(
                manualTrajectoryRadius,
                0.01f
            );

        trajectorySkinWidth =
            Mathf.Max(
                trajectorySkinWidth,
                0.001f
            );

        EnsureHelpers();

        bounceResolver.Normalize();
    }

    private void Update()
    {
        if (!isLongPreviewActive)
        {
            return;
        }

        trajectoryRevealTimer +=
            Time.deltaTime;

        float revealProgress;

        if (trajectoryRevealDuration <= 0f)
        {
            revealProgress = 1f;
        }
        else
        {
            revealProgress =
                Mathf.Clamp01(
                    trajectoryRevealTimer /
                    trajectoryRevealDuration
                );
        }

        float smoothProgress =
            revealProgress *
            revealProgress *
            (
                3f -
                2f *
                revealProgress
            );

        RenderDots(
            trajectoryPoints,
            trajectoryTotalLength,
            longDotSpacing,
            longDotColor,
            longDotScaleMultiplier,
            smoothProgress
        );
    }

    private void EnsureHelpers()
    {
        if (bounceResolver == null)
        {
            bounceResolver =
                new BallBounceResolver();
        }
    }

    private void FindReferences()
    {
        if (dotsRoot == null)
        {
            Transform existingDotsRoot =
                transform.Find(
                    "AimDots"
                );

            if (existingDotsRoot != null)
            {
                dotsRoot =
                    existingDotsRoot;
            }
        }

        if (dotsRoot == null)
        {
            GameObject dotsRootObject =
                new GameObject(
                    "AimDots"
                );

            dotsRoot =
                dotsRootObject.transform;

            dotsRoot.SetParent(
                transform,
                false
            );
        }

        if (ballPrefab == null)
        {
            BallLauncher ballLauncher =
                GetComponent<BallLauncher>();

            if (ballLauncher != null)
            {
                ballPrefab =
                    ballLauncher.BallPrefab;
            }
        }

        if (legacyAimLine == null)
        {
            legacyAimLine =
                GetComponentInChildren<
                    LineRenderer
                >(true);
        }
    }

    private void ValidateReferences()
    {
        if (aimDotPrefab == null)
        {
            Debug.LogError(
                "BallTrajectoryPreview: " +
                "Aim Dot Prefab이 연결되지 않았습니다.",
                this
            );
        }

        if (dotsRoot == null)
        {
            Debug.LogError(
                "BallTrajectoryPreview: " +
                "AimDots Root를 찾지 못했습니다.",
                this
            );
        }

        if (ballPrefab == null)
        {
            Debug.LogWarning(
                "BallTrajectoryPreview: " +
                "Ball Prefab이 연결되지 않았습니다. " +
                "수동 반지름을 사용합니다.",
                this
            );
        }
    }

    private void DisableLegacyLine()
    {
        if (legacyAimLine == null)
        {
            return;
        }

        legacyAimLine.enabled =
            false;

        legacyAimLine.positionCount =
            0;
    }

    private void InitializeDotPool()
    {
        if (aimDotPrefab == null ||
            dotsRoot == null)
        {
            return;
        }

        prefabDotScale =
            aimDotPrefab
                .transform
                .localScale;

        EnsureDotPoolSize(
            prewarmDotCount
        );

        HideAllDots();
    }

    private void InitializeTrajectoryRadius()
    {
        cachedTrajectoryRadius =
            manualTrajectoryRadius;

        if (!useBallPrefabRadius ||
            ballPrefab == null)
        {
            return;
        }

        CircleCollider2D circleCollider =
            ballPrefab.GetComponent<
                CircleCollider2D
            >();

        if (circleCollider == null)
        {
            Debug.LogWarning(
                "BallTrajectoryPreview: " +
                "Ball Prefab에 CircleCollider2D가 없어 " +
                "수동 반지름을 사용합니다.",
                this
            );

            return;
        }

        Vector3 prefabScale =
            ballPrefab.transform.localScale;

        float largestScale =
            Mathf.Max(
                Mathf.Abs(
                    prefabScale.x
                ),
                Mathf.Abs(
                    prefabScale.y
                )
            );

        cachedTrajectoryRadius =
            Mathf.Max(
                0.01f,
                circleCollider.radius *
                largestScale
            );
    }

    public void ShowShort(
        Vector2 direction)
    {
        if (aimDotPrefab == null ||
            direction.sqrMagnitude <=
            0.001f)
        {
            HideAllDots();

            return;
        }

        StopLongPreview();

        CalculatePathPoints(
            direction.normalized,
            shortAimDistance,
            shortAimMaximumBounces,
            shortTrajectoryPoints
        );

        float shortPathLength =
            CalculatePathTotalLength(
                shortTrajectoryPoints
            );

        RenderDots(
            shortTrajectoryPoints,
            shortPathLength,
            shortDotSpacing,
            shortDotColor,
            shortDotScaleMultiplier,
            1f
        );
    }

    public bool BeginTrajectory(
        Vector2 direction)
    {
        if (aimDotPrefab == null ||
            direction.sqrMagnitude <=
            0.001f)
        {
            return false;
        }

        CalculatePathPoints(
            direction.normalized,
            trajectoryDistance,
            maximumTrajectoryBounces,
            trajectoryPoints
        );

        if (trajectoryPoints.Count < 2)
        {
            return false;
        }

        trajectoryTotalLength =
            CalculatePathTotalLength(
                trajectoryPoints
            );

        if (trajectoryTotalLength <=
            0.001f)
        {
            return false;
        }

        trajectoryRevealTimer =
            0f;

        isLongPreviewActive =
            true;

        HideAllDots();

        return true;
    }

    public void StopLongPreview()
    {
        isLongPreviewActive =
            false;

        trajectoryRevealTimer =
            0f;

        trajectoryTotalLength =
            0f;

        trajectoryPoints.Clear();
    }

    public void Hide()
    {
        StopLongPreview();

        shortTrajectoryPoints.Clear();

        HideAllDots();
    }

    private void CalculatePathPoints(
        Vector2 initialDirection,
        float maximumDistance,
        int maximumBounces,
        List<Vector3> outputPoints)
    {
        outputPoints.Clear();

        if (initialDirection.sqrMagnitude <=
            0.001f)
        {
            return;
        }

        Vector2 castOrigin =
            transform.position;

        Vector2 castDirection =
            initialDirection.normalized;

        float remainingDistance =
            Mathf.Max(
                maximumDistance,
                0f
            );

        int bounceCount = 0;
        int teleportCount = 0;
        int safetyIteration = 0;

        Collider2D previousCollider =
            null;

        AddPathPoint(
            outputPoints,
            castOrigin
        );

        while (remainingDistance >
                   trajectorySkinWidth &&
               safetyIteration < 64)
        {
            safetyIteration++;

            bool hasHit =
                TryGetClosestTrajectoryHit(
                    castOrigin,
                    castDirection,
                    remainingDistance,
                    previousCollider,
                    out RaycastHit2D closestHit
                );

            if (!hasHit)
            {
                Vector2 finalPoint =
                    castOrigin +
                    castDirection *
                    remainingDistance;

                AddPathPoint(
                    outputPoints,
                    finalPoint
                );

                break;
            }

            Vector2 collisionCenter =
                closestHit.centroid;

            AddPathPoint(
                outputPoints,
                collisionCenter
            );

            remainingDistance -=
                Mathf.Max(
                    closestHit.distance,
                    0f
                );

            bool reachedReturnZone =
                closestHit.collider != null &&
                closestHit.collider.CompareTag(
                    "ReturnZone"
                );

            if (reachedReturnZone)
            {
                break;
            }

            TeleportPortalController portal =
                closestHit.collider != null
                    ? closestHit.collider.GetComponent<TeleportPortalController>()
                    : null;

            if (portal != null &&
                portal.IsLinked &&
                teleportCount < MaximumPreviewTeleports &&
                portal.Partner.TryResolvePredictedExit(
                    castDirection,
                    cachedTrajectoryRadius,
                    out Vector2 portalExit,
                    out Vector2 portalExitDirection))
            {
                AddPathBreak(outputPoints);
                AddPathPoint(outputPoints, portalExit);

                teleportCount++;
                castDirection = portalExitDirection;
                previousCollider = portal.Partner.GetComponent<Collider2D>();
                castOrigin = portalExit + castDirection * trajectorySkinWidth;
                remainingDistance -= trajectorySkinWidth;
                continue;
            }

            if (bounceCount >=
                maximumBounces)
            {
                break;
            }

            bool resolvedBounce =
                bounceResolver.TryResolve(
                    closestHit.collider,
                    closestHit.point,
                    closestHit.normal,
                    castDirection,
                    1f,
                    out Vector2 reflectedDirection,
                    out Vector2 resolvedNormal
                );

            if (!resolvedBounce ||
                reflectedDirection.sqrMagnitude <=
                0.001f)
            {
                break;
            }

            reflectedDirection.Normalize();

            if (resolvedNormal.sqrMagnitude <=
                0.001f)
            {
                resolvedNormal =
                    closestHit.normal;
            }

            if (resolvedNormal.sqrMagnitude >
                0.001f)
            {
                resolvedNormal.Normalize();
            }

            bounceCount++;

            castDirection =
                reflectedDirection;

            previousCollider =
                closestHit.collider;

            castOrigin =
                collisionCenter +
                resolvedNormal *
                trajectorySkinWidth +
                reflectedDirection *
                trajectorySkinWidth;

            remainingDistance -=
                trajectorySkinWidth *
                2f;
        }
    }

    private bool TryGetClosestTrajectoryHit(
        Vector2 origin,
        Vector2 direction,
        float distance,
        Collider2D previousCollider,
        out RaycastHit2D closestHit)
    {
        RaycastHit2D[] hits =
            Physics2D.CircleCastAll(
                origin,
                cachedTrajectoryRadius,
                direction,
                distance,
                trajectoryCollisionMask
            );

        closestHit =
            default;

        float closestDistance =
            float.MaxValue;

        bool foundHit =
            false;

        for (int i = 0;
             i < hits.Length;
             i++)
        {
            RaycastHit2D hit =
                hits[i];

            if (hit.collider == null)
            {
                continue;
            }

            Ball hitBall =
                hit.collider
                    .GetComponentInParent<
                        Ball
                    >();

            if (hitBall != null)
            {
                continue;
            }

            bool isTeleportPortal =
                hit.collider.GetComponent<TeleportPortalController>() != null;

            if (hit.collider.isTrigger &&
                !hit.collider.CompareTag("ReturnZone") &&
                !isTeleportPortal)
            {
                continue;
            }

            if (hit.collider ==
                    previousCollider &&
                hit.distance <=
                    trajectorySkinWidth *
                    1.5f)
            {
                continue;
            }

            if (hit.distance <=
                0.0001f)
            {
                continue;
            }

            if (hit.distance >=
                closestDistance)
            {
                continue;
            }

            closestDistance =
                hit.distance;

            closestHit =
                hit;

            foundHit =
                true;
        }

        return foundHit;
    }

    private void RenderDots(
        IReadOnlyList<Vector3> pathPoints,
        float totalPathLength,
        float spacing,
        Color dotColor,
        float scaleMultiplier,
        float progress)
    {
        if (pathPoints == null ||
            pathPoints.Count < 2 ||
            totalPathLength <= 0.001f)
        {
            HideAllDots();

            return;
        }

        float visibleDistance =
            totalPathLength *
            Mathf.Clamp01(
                progress
            );

        if (visibleDistance <=
            0.001f)
        {
            HideAllDots();

            return;
        }

        spacing =
            Mathf.Max(
                spacing,
                0.05f
            );

        float firstDistance =
            Mathf.Min(
                firstDotOffset,
                visibleDistance
            );

        int requiredDotCount =
            Mathf.FloorToInt(
                (
                    visibleDistance -
                    firstDistance
                ) /
                spacing
            ) +
            1;

        requiredDotCount =
            Mathf.Max(
                requiredDotCount,
                1
            );

        EnsureDotPoolSize(
            requiredDotCount
        );

        int renderedDotCount = 0;

        for (int i = 0;
             i < requiredDotCount;
             i++)
        {
            float distanceAlongPath =
                firstDistance +
                spacing *
                i;

            if (distanceAlongPath >
                visibleDistance +
                0.0001f)
            {
                break;
            }

            bool foundPosition =
                TryGetPositionAlongPath(
                    pathPoints,
                    distanceAlongPath,
                    out Vector3 dotPosition
                );

            if (!foundPosition)
            {
                break;
            }

            SpriteRenderer dot =
                dotPool[
                    renderedDotCount
                ];

            ApplyDotAppearance(
                dot,
                dotPosition,
                dotColor,
                scaleMultiplier
            );

            renderedDotCount++;
        }

        SetVisibleDotCount(
            renderedDotCount
        );
    }

    private bool TryGetPositionAlongPath(
        IReadOnlyList<Vector3> pathPoints,
        float targetDistance,
        out Vector3 position)
    {
        position =
            pathPoints[0];

        float accumulatedDistance =
            0f;

        for (int i = 1;
             i < pathPoints.Count;
             i++)
        {
            Vector3 segmentStart =
                pathPoints[i - 1];

            Vector3 segmentEnd =
                pathPoints[i];

            if (!IsFinite(segmentStart) ||
                !IsFinite(segmentEnd))
            {
                continue;
            }

            float segmentLength =
                Vector3.Distance(
                    segmentStart,
                    segmentEnd
                );

            if (segmentLength <=
                0.0001f)
            {
                continue;
            }

            float segmentEndDistance =
                accumulatedDistance +
                segmentLength;

            if (targetDistance <=
                segmentEndDistance)
            {
                float distanceInsideSegment =
                    targetDistance -
                    accumulatedDistance;

                float segmentProgress =
                    Mathf.Clamp01(
                        distanceInsideSegment /
                        segmentLength
                    );

                position =
                    Vector3.Lerp(
                        segmentStart,
                        segmentEnd,
                        segmentProgress
                    );

                return true;
            }

            accumulatedDistance =
                segmentEndDistance;
        }

        position =
            pathPoints[
                pathPoints.Count - 1
            ];

        return true;
    }

    private void ApplyDotAppearance(
        SpriteRenderer dot,
        Vector3 worldPosition,
        Color dotColor,
        float scaleMultiplier)
    {
        if (dot == null)
        {
            return;
        }

        if (!dot.gameObject.activeSelf)
        {
            dot.gameObject.SetActive(
                true
            );
        }

        Transform dotTransform =
            dot.transform;

        dotTransform.position =
            worldPosition;

        dotTransform.rotation =
            Quaternion.identity;

        dotTransform.localScale =
            prefabDotScale *
            scaleMultiplier;

        dot.color =
            dotColor;
    }

    private void EnsureDotPoolSize(
        int requiredCount)
    {
        if (aimDotPrefab == null ||
            dotsRoot == null)
        {
            return;
        }

        while (dotPool.Count <
               requiredCount)
        {
            SpriteRenderer newDot =
                Instantiate(
                    aimDotPrefab,
                    dotsRoot
                );

            newDot.name =
                "AimDot_" +
                dotPool.Count;

            newDot.gameObject.SetActive(
                false
            );

            dotPool.Add(
                newDot
            );
        }
    }

    private void SetVisibleDotCount(
        int visibleCount)
    {
        for (int i = 0;
             i < dotPool.Count;
             i++)
        {
            bool shouldBeVisible =
                i < visibleCount;

            GameObject dotObject =
                dotPool[i].gameObject;

            if (dotObject.activeSelf !=
                shouldBeVisible)
            {
                dotObject.SetActive(
                    shouldBeVisible
                );
            }
        }
    }

    private void HideAllDots()
    {
        SetVisibleDotCount(
            0
        );
    }

    private float CalculatePathTotalLength(
        IReadOnlyList<Vector3> points)
    {
        if (points == null ||
            points.Count < 2)
        {
            return 0f;
        }

        float totalLength = 0f;

        for (int i = 1;
             i < points.Count;
             i++)
        {
            if (!IsFinite(points[i - 1]) || !IsFinite(points[i]))
            {
                continue;
            }

            totalLength +=
                Vector3.Distance(
                    points[i - 1],
                    points[i]
                );
        }

        return totalLength;
    }

    private void AddPathPoint(
        List<Vector3> points,
        Vector2 point)
    {
        Vector3 newPoint =
            new Vector3(
                point.x,
                point.y,
                transform.position.z
            );

        if (points.Count > 0)
        {
            Vector3 previousPoint =
                points[
                    points.Count - 1
                ];

            if (Vector3.Distance(
                    previousPoint,
                    newPoint
                ) <= 0.0001f)
            {
                return;
            }
        }

        points.Add(
            newPoint
        );
    }

    private void AddPathBreak(List<Vector3> points)
    {
        if (points.Count > 0 && IsFinite(points[points.Count - 1]))
        {
            points.Add(new Vector3(float.NaN, float.NaN, float.NaN));
        }
    }

    private static bool IsFinite(Vector3 point)
    {
        return !float.IsNaN(point.x) &&
               !float.IsNaN(point.y) &&
               !float.IsNaN(point.z) &&
               !float.IsInfinity(point.x) &&
               !float.IsInfinity(point.y) &&
               !float.IsInfinity(point.z);
    }
}
