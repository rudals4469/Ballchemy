using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class BallLauncher : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private Ball ballPrefab;

    [SerializeField]
    private LineRenderer aimLine;

    [Header("Ball Settings")]
    [SerializeField, Min(1)]
    private int ballCount = 5;

    [SerializeField, Min(0f)]
    private float launchInterval = 0.08f;

    [Header("Aim Settings")]
    [SerializeField, Range(0.01f, 1f)]
    private float minimumUpwardDirection = 0.15f;

    [Tooltip("조준선이 마우스를 따라가는 부드러움입니다.")]
    [SerializeField, Min(0f)]
    private float aimSmoothSpeed = 14f;

    [Tooltip(
        "이 각도 이상 움직이면 " +
        "조준 방향이 변경된 것으로 판단합니다."
    )]
    [SerializeField, Range(0.1f, 10f)]
    private float aimMovementThresholdDegrees = 1.5f;

    [Header("Short Aim Line")]
    [Tooltip("마우스를 움직이는 동안 표시되는 짧은 조준선 길이입니다.")]
    [SerializeField, Min(0.5f)]
    private float shortAimLineLength = 3.5f;

    [Tooltip("짧은 조준선 색상입니다.")]
    [SerializeField]
    private Color shortAimLineColor =
        new Color(1f, 1f, 1f, 0.9f);

    [Header("Trajectory Preview Timing")]
    [Tooltip(
        "조준 방향을 유지한 뒤 " +
        "긴 예상 경로가 나타나기까지의 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float stableAimDelay = 1.2f;

    [Tooltip("예상 경로가 끝까지 늘어나는 데 걸리는 시간입니다.")]
    [SerializeField, Min(0f)]
    private float trajectoryRevealDuration = 0.45f;

    [Tooltip("긴 예상 경로의 반투명 색상입니다.")]
    [SerializeField]
    private Color trajectoryLineColor =
        new Color(1f, 1f, 1f, 0.28f);

    [Header("Trajectory Preview")]
    [Tooltip("예상 경로 전체 길이입니다.")]
    [SerializeField, Min(1f)]
    private float trajectoryDistance = 35f;

    [Tooltip("예상 경로의 최대 반사 횟수입니다.")]
    [SerializeField, Range(0, 20)]
    private int maximumTrajectoryBounces = 8;

    [Tooltip(
        "Ball Prefab의 CircleCollider2D 크기를 " +
        "예상 경로 계산에 사용합니다."
    )]
    [SerializeField]
    private bool useBallPrefabRadius = true;

    [Tooltip(
        "Ball Prefab 반지름을 사용하지 않을 때 적용되는 " +
        "예상 공 반지름입니다."
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

    [Header("Launch Position Limit")]
    [SerializeField]
    private float minimumLaunchX = -4.3f;

    [SerializeField]
    private float maximumLaunchX = 4.3f;

    private readonly List<Ball> balls =
        new List<Ball>();

    private readonly List<Vector3> trajectoryPoints =
        new List<Vector3>();

    private readonly List<Vector3> visibleTrajectoryPoints =
        new List<Vector3>();

    private Camera mainCamera;
    private Coroutine launchCoroutine;

    private Vector2 currentAimDirection =
        Vector2.up;

    private Vector2 targetAimDirection =
        Vector2.up;

    private Vector2 stableReferenceDirection =
        Vector2.up;

    private Vector2 previewDirection =
        Vector2.up;

    private Vector2 currentTurnLaunchPosition;
    private Vector2 nextTurnLaunchPosition;

    private int launchedBallCount;
    private int returnedBallCount;

    private float cachedTrajectoryRadius;
    private float stableAimTimer;
    private float trajectoryRevealTimer;
    private float trajectoryTotalLength;

    private bool hasValidAim;
    private bool isLaunching;
    private bool hasFirstReturnedBall;
    private bool isAttackCompleted;
    private bool isTrajectoryPreviewActive;
    private bool hasStableReference;
    private bool wasAimingLastFrame;

    private void Awake()
    {
        mainCamera = Camera.main;

        if (turnManager == null)
        {
            turnManager =
                FindFirstObjectByType<TurnManager>();
        }

        if (aimLine == null)
        {
            aimLine =
                GetComponentInChildren<LineRenderer>(
                    true
                );
        }

        ValidateReferences();
        InitializeAimLine();
        InitializeTrajectoryRadius();
    }

    private void Start()
    {
        if (ballPrefab == null)
        {
            return;
        }

        currentTurnLaunchPosition =
            transform.position;

        nextTurnLaunchPosition =
            transform.position;

        CreateBalls();

        currentAimDirection = Vector2.up;
        targetAimDirection = Vector2.up;
        stableReferenceDirection = Vector2.up;

        hasStableReference = true;
        hasValidAim = true;
        wasAimingLastFrame = false;

        ResetAimPreview();
        ShowShortAimLine(currentAimDirection);
    }

    private void Update()
    {
        if (turnManager == null ||
            balls.Count == 0)
        {
            return;
        }

        if (!turnManager.CanAim)
        {
            if (wasAimingLastFrame)
            {
                wasAimingLastFrame = false;
                ResetAimPreview();
            }

            HideAimLine();
            return;
        }

        if (!wasAimingLastFrame)
        {
            wasAimingLastFrame = true;

            stableReferenceDirection =
                currentAimDirection;

            hasStableReference = true;

            ResetAimPreview();
            ShowShortAimLine(currentAimDirection);
        }

        Vector3 mouseScreenPosition =
            Input.mousePosition;

        bool isPointerInsideGameView =
            IsValidPointerPosition(
                mouseScreenPosition
            );

        if (isPointerInsideGameView)
        {
            UpdateAimInput(
                mouseScreenPosition
            );
        }

        UpdateAimVisual();

        if (isPointerInsideGameView &&
            Input.GetMouseButtonDown(0) &&
            hasValidAim)
        {
            TryLaunchBalls();
        }
    }

    private void ValidateReferences()
    {
        if (mainCamera == null)
        {
            Debug.LogError(
                "BallLauncher: Main Camera를 찾지 못했습니다. " +
                "Main Camera의 Tag가 MainCamera인지 확인하세요.",
                this
            );
        }

        if (turnManager == null)
        {
            Debug.LogError(
                "BallLauncher: TurnManager를 찾지 못했습니다.",
                this
            );
        }

        if (ballPrefab == null)
        {
            Debug.LogError(
                "BallLauncher: Ball Prefab이 연결되지 않았습니다.",
                this
            );
        }

        if (aimLine == null)
        {
            Debug.LogError(
                "BallLauncher: Aim Line이 연결되지 않았습니다.",
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
                "BallLauncher: Ball Prefab에 " +
                "CircleCollider2D가 없어 수동 반지름을 사용합니다.",
                this
            );

            return;
        }

        Vector3 prefabScale =
            ballPrefab.transform.localScale;

        float largestScale =
            Mathf.Max(
                Mathf.Abs(prefabScale.x),
                Mathf.Abs(prefabScale.y)
            );

        cachedTrajectoryRadius =
            Mathf.Max(
                0.01f,
                circleCollider.radius *
                largestScale
            );
    }

    private void CreateBalls()
    {
        balls.Clear();

        for (int i = 0; i < ballCount; i++)
        {
            Ball newBall = Instantiate(
                ballPrefab,
                transform.position,
                Quaternion.identity
            );

            newBall.name =
                $"Ball_{i + 1}";

            newBall.Returned +=
                HandleBallReturned;

            newBall.ResetTo(
                transform.position
            );

            balls.Add(newBall);
        }

        IgnoreBallCollisions();

        Debug.Log(
            $"BallLauncher: 공 {balls.Count}개 생성 완료",
            this
        );
    }

    private void IgnoreBallCollisions()
    {
        for (int i = 0;
             i < balls.Count;
             i++)
        {
            Collider2D firstCollider =
                balls[i].GetComponent<Collider2D>();

            if (firstCollider == null)
            {
                continue;
            }

            for (int j = i + 1;
                 j < balls.Count;
                 j++)
            {
                Collider2D secondCollider =
                    balls[j].GetComponent<Collider2D>();

                if (secondCollider == null)
                {
                    continue;
                }

                Physics2D.IgnoreCollision(
                    firstCollider,
                    secondCollider,
                    true
                );
            }
        }
    }

    private void UpdateAimInput(
        Vector3 mouseScreenPosition)
    {
        if (mainCamera == null)
        {
            return;
        }

        float distanceFromCamera =
            transform.position.z -
            mainCamera.transform.position.z;

        Vector3 screenPosition =
            new Vector3(
                mouseScreenPosition.x,
                mouseScreenPosition.y,
                distanceFromCamera
            );

        Vector3 mouseWorldPosition =
            mainCamera.ScreenToWorldPoint(
                screenPosition
            );

        mouseWorldPosition.z =
            transform.position.z;

        Vector2 rawDirection =
            (Vector2)mouseWorldPosition -
            (Vector2)transform.position;

        if (rawDirection.sqrMagnitude <=
            0.001f)
        {
            return;
        }

        rawDirection.y =
            Mathf.Max(
                rawDirection.y,
                minimumUpwardDirection
            );

        Vector2 newTargetDirection =
            rawDirection.normalized;

        bool hasMeaningfulChange =
            HasMeaningfulAimChange(
                newTargetDirection
            );

        targetAimDirection =
            newTargetDirection;

        hasValidAim = true;

        if (!hasMeaningfulChange)
        {
            return;
        }

        stableReferenceDirection =
            newTargetDirection;

        hasStableReference = true;

        ResetAimPreview();
    }

    private bool HasMeaningfulAimChange(
        Vector2 newDirection)
    {
        if (!hasStableReference)
        {
            stableReferenceDirection =
                newDirection;

            hasStableReference = true;

            return true;
        }

        float angleDifference =
            Vector2.Angle(
                stableReferenceDirection,
                newDirection
            );

        return angleDifference >=
               aimMovementThresholdDegrees;
    }

    private void UpdateAimVisual()
    {
        if (!isTrajectoryPreviewActive)
        {
            SmoothCurrentAimDirection();

            ShowShortAimLine(
                currentAimDirection
            );

            stableAimTimer +=
                Time.deltaTime;

            if (stableAimTimer >=
                stableAimDelay)
            {
                BeginTrajectoryPreview();
            }

            return;
        }

        currentAimDirection =
            previewDirection;

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
            (3f - (2f * revealProgress));

        RenderTrajectory(
            smoothProgress
        );
    }

    private void SmoothCurrentAimDirection()
    {
        if (targetAimDirection.sqrMagnitude <=
            0.001f)
        {
            return;
        }

        if (aimSmoothSpeed <= 0f)
        {
            currentAimDirection =
                targetAimDirection;

            return;
        }

        float interpolation =
            1f -
            Mathf.Exp(
                -aimSmoothSpeed *
                Time.deltaTime
            );

        Vector2 smoothedDirection =
            Vector2.Lerp(
                currentAimDirection,
                targetAimDirection,
                interpolation
            );

        if (smoothedDirection.sqrMagnitude >
            0.001f)
        {
            currentAimDirection =
                smoothedDirection.normalized;
        }
    }

    private void ResetAimPreview()
    {
        stableAimTimer = 0f;
        trajectoryRevealTimer = 0f;
        trajectoryTotalLength = 0f;

        isTrajectoryPreviewActive = false;

        trajectoryPoints.Clear();
        visibleTrajectoryPoints.Clear();
    }

    private void BeginTrajectoryPreview()
    {
        previewDirection =
            currentAimDirection.normalized;

        currentAimDirection =
            previewDirection;

        CalculateTrajectoryPoints(
            previewDirection
        );

        if (trajectoryPoints.Count < 2)
        {
            ResetAimPreview();
            return;
        }

        trajectoryTotalLength =
            CalculateTrajectoryTotalLength();

        trajectoryRevealTimer = 0f;
        isTrajectoryPreviewActive = true;

        RenderTrajectory(0f);
    }

    private void ShowShortAimLine(
        Vector2 direction)
    {
        if (aimLine == null ||
            direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

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
            Mathf.Clamp01(progress);

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

        if (visibleTrajectoryPoints.Count == 1)
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

    private void ApplyLineColor(
        Color color)
    {
        if (aimLine == null)
        {
            return;
        }

        aimLine.startColor = color;
        aimLine.endColor = color;
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

    private void TryLaunchBalls()
    {
        if (isLaunching ||
            balls.Count == 0 ||
            turnManager == null)
        {
            return;
        }

        if (!turnManager.TryStartAttack())
        {
            Debug.LogWarning(
                "BallLauncher: 현재 턴 상태에서는 " +
                "발사할 수 없습니다. " +
                $"현재 상태: {turnManager.CurrentState}",
                this
            );

            return;
        }

        currentTurnLaunchPosition =
            transform.position;

        nextTurnLaunchPosition =
            currentTurnLaunchPosition;

        launchedBallCount = 0;
        returnedBallCount = 0;

        hasFirstReturnedBall = false;
        isAttackCompleted = false;
        isLaunching = true;

        ResetAimPreview();
        HideAimLine();

        launchCoroutine = StartCoroutine(
            LaunchBallsRoutine(
                currentAimDirection
            )
        );
    }

    private IEnumerator LaunchBallsRoutine(
        Vector2 direction)
    {
        Debug.Log(
            $"BallLauncher: 공 {balls.Count}개 순차 발사 시작",
            this
        );

        foreach (Ball ball in balls)
        {
            if (ball == null)
            {
                continue;
            }

            launchedBallCount++;

            ball.ResetTo(
                currentTurnLaunchPosition
            );

            ball.Launch(direction);

            if (launchInterval > 0f)
            {
                yield return
                    new WaitForSeconds(
                        launchInterval
                    );
            }
            else
            {
                yield return null;
            }
        }

        isLaunching = false;
        launchCoroutine = null;

        TryCompleteAttack();
    }

    private void HandleBallReturned(
        Ball returnedBall)
    {
        if (returnedBall == null ||
            isAttackCompleted)
        {
            return;
        }

        if (!hasFirstReturnedBall)
        {
            float nextLaunchX =
                Mathf.Clamp(
                    returnedBall
                        .transform
                        .position
                        .x,
                    minimumLaunchX,
                    maximumLaunchX
                );

            nextTurnLaunchPosition =
                new Vector2(
                    nextLaunchX,
                    currentTurnLaunchPosition.y
                );

            hasFirstReturnedBall = true;

            Debug.Log(
                "BallLauncher: 첫 번째 공 복귀 위치 저장, " +
                $"다음 시작 X = {nextLaunchX:F2}",
                this
            );
        }

        returnedBallCount++;

        Debug.Log(
            "BallLauncher: 공 복귀 " +
            $"{returnedBallCount}/" +
            $"{launchedBallCount}",
            this
        );

        TryCompleteAttack();
    }

    private void TryCompleteAttack()
    {
        if (isAttackCompleted)
        {
            return;
        }

        if (isLaunching)
        {
            return;
        }

        if (launchedBallCount == 0)
        {
            return;
        }

        if (launchedBallCount <
            balls.Count)
        {
            return;
        }

        if (returnedBallCount <
            launchedBallCount)
        {
            return;
        }

        isAttackCompleted = true;

        Debug.Log(
            "BallLauncher: 마지막 공 복귀 완료, " +
            "모든 공을 첫 번째 공 위치로 정렬합니다.",
            this
        );

        AlignBallsToNextLaunchPosition();

        turnManager.NotifyAllBallsReturned();

        hasValidAim = true;
    }

    private void AlignBallsToNextLaunchPosition()
    {
        foreach (Ball ball in balls)
        {
            if (ball == null)
            {
                continue;
            }

            ball.ResetTo(
                nextTurnLaunchPosition
            );
        }

        transform.position =
            new Vector3(
                nextTurnLaunchPosition.x,
                nextTurnLaunchPosition.y,
                transform.position.z
            );
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
                    (
                        castDirection *
                        remainingDistance
                    );

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

            Vector2 reflectedDirection =
                Vector2.Reflect(
                    castDirection,
                    closestHit.normal
                ).normalized;

            if (reflectedDirection
                    .sqrMagnitude <= 0.001f)
            {
                break;
            }

            bounceCount++;

            castDirection =
                reflectedDirection;

            castOrigin =
                collisionCenter +
                (
                    reflectedDirection *
                    trajectorySkinWidth
                );

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

        closestHit = default;

        float closestDistance =
            float.MaxValue;

        bool foundHit = false;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
            {
                continue;
            }

            if (hit.distance <=
                trajectorySkinWidth * 0.5f)
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

            closestHit = hit;
            foundHit = true;
        }

        return foundHit;
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

    private void HideAimLine()
    {
        if (aimLine == null)
        {
            return;
        }

        aimLine.enabled = false;
        aimLine.positionCount = 0;
    }

    private static bool IsValidPointerPosition(
        Vector3 screenPosition)
    {
        if (float.IsNaN(
                screenPosition.x) ||
            float.IsNaN(
                screenPosition.y) ||
            float.IsInfinity(
                screenPosition.x) ||
            float.IsInfinity(
                screenPosition.y))
        {
            return false;
        }

        if (screenPosition.x < 0f ||
            screenPosition.x > Screen.width ||
            screenPosition.y < 0f ||
            screenPosition.y > Screen.height)
        {
            return false;
        }

        return true;
    }

    private void OnDestroy()
    {
        if (launchCoroutine != null)
        {
            StopCoroutine(
                launchCoroutine
            );
        }

        foreach (Ball ball in balls)
        {
            if (ball != null)
            {
                ball.Returned -=
                    HandleBallReturned;
            }
        }
    }
}