using System.Collections.Generic;
using UnityEngine;

public sealed class BallTrajectoryPreview : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private LineRenderer aimLine;

    [SerializeField]
    private Ball ballPrefab;

    [Header("Short Aim Line")]
    [Tooltip(
        "마우스를 움직이는 동안 표시되는 " +
        "짧은 조준선 길이입니다."
    )]
    [SerializeField, Min(0.5f)]
    private float shortAimLineLength = 3.5f;

    [SerializeField]
    private Color shortAimLineColor =
        new Color(
            1f,
            1f,
            1f,
            0.9f
        );

    [Header("Long Trajectory Appearance")]
    [Tooltip(
        "긴 예상 경로가 끝까지 늘어나는 데 " +
        "걸리는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float trajectoryRevealDuration = 0.45f;

    [SerializeField]
    private Color trajectoryLineColor =
        new Color(
            1f,
            1f,
            1f,
            0.28f
        );

    [Header("Trajectory Calculation")]
    [Tooltip(
        "예상 경로의 최대 전체 길이입니다."
    )]
    [SerializeField, Min(1f)]
    private float trajectoryDistance = 35f;

    [Tooltip(
        "예상 경로의 최대 반사 횟수입니다."
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
        "약간 떨어뜨리는 거리입니다."
    )]
    [SerializeField, Min(0.001f)]
    private float trajectorySkinWidth = 0.02f;

    [Tooltip(
        "예상 경로가 충돌을 검사할 레이어입니다. " +
        "벽, 블록, ReturnZone 레이어가 포함돼야 합니다."
    )]
    [SerializeField]
    private LayerMask trajectoryCollisionMask = ~0;

    private readonly List<Vector3>
        trajectoryPoints =
            new List<Vector3>();

    private readonly List<Vector3>
        visibleTrajectoryPoints =
            new List<Vector3>();

    private float cachedTrajectoryRadius;
    private float trajectoryRevealTimer;
    private float trajectoryTotalLength;

    private bool isLongPreviewActive;

    private void Awake()
    {
        EnsureHelpers();
        FindReferences();
        ValidateReferences();
        InitializeAimLine();
        InitializeTrajectoryRadius();
    }

    private void OnValidate()
    {
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

        trajectoryRevealDuration =
            Mathf.Max(
                trajectoryRevealDuration,
                0f
            );

        EnsureHelpers();

        bounceResolver.Normalize();
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
        if (aimLine == null)
        {
            aimLine =
                GetComponentInChildren<LineRenderer>(
                    true
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

        RenderTrajectory(
            smoothProgress
        );
    }

    private void ValidateReferences()
    {
        if (aimLine == null)
        {
            Debug.LogError(
                "BallTrajectoryPreview: " +
                "LineRenderer를 찾지 못했습니다.",
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

    private void InitializeAimLine()
    {
        if (aimLine == null)
        {
            return;
        }

        aimLine.useWorldSpace = true;
        aimLine.positionCount = 0;
        aimLine.enabled = false;
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
            ballPrefab.GetComponent<CircleCollider2D>();

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
        if (aimLine == null ||
            direction.sqrMagnitude <=
            0.001f)
        {
            return;
        }

        StopLongPreview();

        ApplyLineColor(
            shortAimLineColor
        );

        Vector3 startPosition =
            transform.position;

        Vector3 endPosition =
            startPosition +
            (Vector3)(
                direction.normalized *
                shortAimLineLength
            );

        aimLine.enabled = true;
        aimLine.positionCount = 2;

        aimLine.SetPosition(
            0,
            startPosition
        );

        aimLine.SetPosition(
            1,
            endPosition
        );
    }

    public bool BeginTrajectory(
        Vector2 direction)
    {
        if (aimLine == null ||
            direction.sqrMagnitude <=
            0.001f)
        {
            return false;
        }

        CalculateTrajectoryPoints(
            direction.normalized
        );

        if (trajectoryPoints.Count < 2)
        {
            return false;
        }

        trajectoryTotalLength =
            CalculateTrajectoryTotalLength();

        if (trajectoryTotalLength <=
            0.001f)
        {
            return false;
        }

        trajectoryRevealTimer = 0f;
        isLongPreviewActive = true;

        ApplyLineColor(
            trajectoryLineColor
        );

        RenderTrajectory(
            0f
        );

        return true;
    }

    public void StopLongPreview()
    {
        isLongPreviewActive = false;

        trajectoryRevealTimer = 0f;
        trajectoryTotalLength = 0f;

        trajectoryPoints.Clear();
        visibleTrajectoryPoints.Clear();
    }

    public void Hide()
    {
        StopLongPreview();

        if (aimLine == null)
        {
            return;
        }

        aimLine.enabled = false;
        aimLine.positionCount = 0;
    }

    private void CalculateTrajectoryPoints(
        Vector2 initialDirection)
    {
        trajectoryPoints.Clear();

        Vector2 castOrigin =
            transform.position;

        Vector2 castDirection =
            initialDirection.normalized;

        float remainingDistance =
            trajectoryDistance;

        int bounceCount = 0;

        AddTrajectoryPoint(
            castOrigin
        );

        while (remainingDistance >
               trajectorySkinWidth)
        {
            bool hasHit =
                TryGetClosestTrajectoryHit(
                    castOrigin,
                    castDirection,
                    remainingDistance,
                    out RaycastHit2D closestHit
                );

            if (!hasHit)
            {
                Vector2 finalPoint =
                    castOrigin +
                    castDirection *
                    remainingDistance;

                AddTrajectoryPoint(
                    finalPoint
                );

                break;
            }

            Vector2 collisionCenter =
                closestHit.centroid;

            AddTrajectoryPoint(
                collisionCenter
            );

            remainingDistance -=
                closestHit.distance;

            bool reachedReturnZone =
                closestHit.collider != null &&
                closestHit.collider.CompareTag(
                    "ReturnZone"
                );

            if (reachedReturnZone)
            {
                break;
            }

            if (bounceCount >=
                maximumTrajectoryBounces)
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

            bounceCount++;

            castDirection =
                reflectedDirection;

            castOrigin =
                collisionCenter +
                reflectedDirection *
                trajectorySkinWidth;

            remainingDistance -=
                trajectorySkinWidth;
        }
    }

    private bool TryGetClosestTrajectoryHit(
        Vector2 origin,
        Vector2 direction,
        float distance,
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

            if (hit.distance <=
                trajectorySkinWidth *
                0.5f)
            {
                continue;
            }

            Ball hitBall =
                hit.collider
                    .GetComponentInParent<Ball>();

            if (hitBall != null)
            {
                continue;
            }

            if (hit.collider.isTrigger &&
                !hit.collider.CompareTag(
                    "ReturnZone"
                ))
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

    private void RenderTrajectory(
        float progress)
    {
        if (aimLine == null ||
            trajectoryPoints.Count < 2)
        {
            return;
        }

        ApplyLineColor(
            trajectoryLineColor
        );

        visibleTrajectoryPoints.Clear();

        Vector3 firstPoint =
            trajectoryPoints[0];

        visibleTrajectoryPoints.Add(
            firstPoint
        );

        float targetDistance =
            trajectoryTotalLength *
            Mathf.Clamp01(
                progress
            );

        if (targetDistance <= 0f)
        {
            visibleTrajectoryPoints.Add(
                firstPoint
            );

            ApplyVisibleTrajectoryPoints();

            return;
        }

        float remainingDistance =
            targetDistance;

        for (int i = 1;
             i < trajectoryPoints.Count;
             i++)
        {
            Vector3 segmentStart =
                trajectoryPoints[i - 1];

            Vector3 segmentEnd =
                trajectoryPoints[i];

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

            if (remainingDistance >=
                segmentLength)
            {
                visibleTrajectoryPoints.Add(
                    segmentEnd
                );

                remainingDistance -=
                    segmentLength;

                continue;
            }

            float segmentProgress =
                remainingDistance /
                segmentLength;

            Vector3 partialPoint =
                Vector3.Lerp(
                    segmentStart,
                    segmentEnd,
                    segmentProgress
                );

            visibleTrajectoryPoints.Add(
                partialPoint
            );

            break;
        }

        if (visibleTrajectoryPoints.Count ==
            1)
        {
            visibleTrajectoryPoints.Add(
                firstPoint
            );
        }

        ApplyVisibleTrajectoryPoints();
    }

    private void ApplyVisibleTrajectoryPoints()
    {
        aimLine.enabled = true;

        aimLine.positionCount =
            visibleTrajectoryPoints.Count;

        for (int i = 0;
             i < visibleTrajectoryPoints.Count;
             i++)
        {
            aimLine.SetPosition(
                i,
                visibleTrajectoryPoints[i]
            );
        }
    }

    private float CalculateTrajectoryTotalLength()
    {
        float totalLength = 0f;

        for (int i = 1;
             i < trajectoryPoints.Count;
             i++)
        {
            totalLength +=
                Vector3.Distance(
                    trajectoryPoints[i - 1],
                    trajectoryPoints[i]
                );
        }

        return totalLength;
    }

    private void ApplyLineColor(
        Color color)
    {
        if (aimLine == null)
        {
            return;
        }

        aimLine.startColor =
            color;

        aimLine.endColor =
            color;
    }

    private void AddTrajectoryPoint(
        Vector2 point)
    {
        trajectoryPoints.Add(
            new Vector3(
                point.x,
                point.y,
                transform.position.z
            )
        );
    }
}