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
    [SerializeField, Min(0.5f)]
    private float aimLineLength = 4f;

    [SerializeField, Range(0.01f, 1f)]
    private float minimumUpwardDirection = 0.15f;

    [Header("Launch Position Limit")]
    [Tooltip("다음 턴 시작 위치가 왼쪽 벽에 너무 붙지 않도록 제한합니다.")]
    [SerializeField]
    private float minimumLaunchX = -4.3f;

    [Tooltip("다음 턴 시작 위치가 오른쪽 벽에 너무 붙지 않도록 제한합니다.")]
    [SerializeField]
    private float maximumLaunchX = 4.3f;

    private readonly List<Ball> balls = new List<Ball>();

    private Camera mainCamera;
    private Coroutine launchCoroutine;

    private Vector2 currentAimDirection = Vector2.up;

    // 현재 턴에서 모든 공이 출발하는 위치
    private Vector2 currentTurnLaunchPosition;

    // 첫 번째로 복귀한 공을 기준으로 정해지는 다음 턴 시작 위치
    private Vector2 nextTurnLaunchPosition;

    private int launchedBallCount;
    private int returnedBallCount;

    private bool hasValidAim;
    private bool isLaunching;
    private bool hasFirstReturnedBall;
    private bool isAttackCompleted;

    private void Awake()
    {
        mainCamera = Camera.main;

        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }

        if (aimLine == null)
        {
            aimLine = GetComponentInChildren<LineRenderer>(true);
        }

        ValidateReferences();
        InitializeAimLine();
    }

    private void Start()
    {
        if (ballPrefab == null)
        {
            return;
        }

        currentTurnLaunchPosition = transform.position;
        nextTurnLaunchPosition = transform.position;

        CreateBalls();

        currentAimDirection = Vector2.up;
        hasValidAim = true;

        ShowAimLine(currentAimDirection);
    }

    private void Update()
    {
        if (turnManager == null || balls.Count == 0)
        {
            return;
        }

        if (!turnManager.CanAim)
        {
            HideAimLine();
            return;
        }

        Vector3 mouseScreenPosition = Input.mousePosition;

        bool isPointerInsideGameView =
            IsValidPointerPosition(mouseScreenPosition);

        if (isPointerInsideGameView)
        {
            UpdateAim(mouseScreenPosition);
        }

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
                "카메라의 Tag가 MainCamera인지 확인하세요.",
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
        aimLine.positionCount = 2;
        aimLine.enabled = false;
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

            newBall.name = $"Ball_{i + 1}";
            newBall.Returned += HandleBallReturned;
            newBall.ResetTo(transform.position);

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
        for (int i = 0; i < balls.Count; i++)
        {
            Collider2D firstCollider =
                balls[i].GetComponent<Collider2D>();

            if (firstCollider == null)
            {
                continue;
            }

            for (int j = i + 1; j < balls.Count; j++)
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

    private void UpdateAim(Vector3 mouseScreenPosition)
    {
        if (mainCamera == null || aimLine == null)
        {
            return;
        }

        float distanceFromCamera =
            transform.position.z -
            mainCamera.transform.position.z;

        Vector3 screenPosition = new Vector3(
            mouseScreenPosition.x,
            mouseScreenPosition.y,
            distanceFromCamera
        );

        Vector3 mouseWorldPosition =
            mainCamera.ScreenToWorldPoint(screenPosition);

        mouseWorldPosition.z = transform.position.z;

        Vector2 rawDirection =
            (Vector2)mouseWorldPosition -
            (Vector2)transform.position;

        if (rawDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        // 공이 아래쪽으로 발사되지 않도록
        // 최소한 위쪽 방향을 가지도록 보정한다.
        rawDirection.y = Mathf.Max(
            rawDirection.y,
            minimumUpwardDirection
        );

        currentAimDirection = rawDirection.normalized;
        hasValidAim = true;

        ShowAimLine(currentAimDirection);
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
                $"BallLauncher: 현재 턴 상태에서는 발사할 수 없습니다. " +
                $"현재 상태: {turnManager.CurrentState}",
                this
            );

            return;
        }

        // 이번 턴의 출발 위치를 고정한다.
        // 공이 도중에 복귀해도 남은 공은 기존 위치에서 발사된다.
        currentTurnLaunchPosition = transform.position;
        nextTurnLaunchPosition = currentTurnLaunchPosition;

        launchedBallCount = 0;
        returnedBallCount = 0;

        hasFirstReturnedBall = false;
        isAttackCompleted = false;
        isLaunching = true;

        HideAimLine();

        launchCoroutine = StartCoroutine(
            LaunchBallsRoutine(currentAimDirection)
        );
    }

    private IEnumerator LaunchBallsRoutine(Vector2 direction)
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

            ball.ResetTo(currentTurnLaunchPosition);
            ball.Launch(direction);

            if (launchInterval > 0f)
            {
                yield return new WaitForSeconds(launchInterval);
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

    private void HandleBallReturned(Ball returnedBall)
    {
        if (returnedBall == null || isAttackCompleted)
        {
            return;
        }

        // 첫 번째로 바닥에 닿은 공의 X 좌표만 저장한다.
        // 이 시점에서는 Launcher나 다른 공을 이동시키지 않는다.
        if (!hasFirstReturnedBall)
        {
            float nextLaunchX = Mathf.Clamp(
                returnedBall.transform.position.x,
                minimumLaunchX,
                maximumLaunchX
            );

            nextTurnLaunchPosition = new Vector2(
                nextLaunchX,
                currentTurnLaunchPosition.y
            );

            hasFirstReturnedBall = true;

            Debug.Log(
                $"BallLauncher: 첫 번째 공 복귀 위치 저장, " +
                $"다음 시작 X = {nextLaunchX:F2}",
                this
            );
        }

        // Ball.cs에서 공의 이동은 이미 멈춘 상태다.
        // 여기서는 ResetTo를 호출하지 않고,
        // 공이 실제로 떨어진 위치에 그대로 둔다.

        returnedBallCount++;

        Debug.Log(
            $"BallLauncher: 공 복귀 " +
            $"{returnedBallCount}/{launchedBallCount}",
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

        // 아직 공을 순차 발사하는 중이면 기다린다.
        if (isLaunching)
        {
            return;
        }

        if (launchedBallCount == 0)
        {
            return;
        }

        // 모든 공이 아직 발사되지 않았다면 기다린다.
        if (launchedBallCount < balls.Count)
        {
            return;
        }

        // 아직 돌아오지 않은 공이 있다면 기다린다.
        if (returnedBallCount < launchedBallCount)
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
        ShowAimLine(currentAimDirection);
    }

    private void AlignBallsToNextLaunchPosition()
    {
        // 마지막 공까지 돌아온 순간에만
        // 모든 공을 첫 번째 공 기준 위치로 정렬한다.
        foreach (Ball ball in balls)
        {
            if (ball == null)
            {
                continue;
            }

            ball.ResetTo(nextTurnLaunchPosition);
        }

        transform.position = nextTurnLaunchPosition;
    }

    private void ShowAimLine(Vector2 direction)
    {
        if (aimLine == null ||
            direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Vector3 startPosition = transform.position;

        Vector3 endPosition =
            startPosition +
            (Vector3)(
                direction.normalized *
                aimLineLength
            );

        aimLine.enabled = true;
        aimLine.positionCount = 2;

        aimLine.SetPosition(0, startPosition);
        aimLine.SetPosition(1, endPosition);
    }

    private void HideAimLine()
    {
        if (aimLine != null)
        {
            aimLine.enabled = false;
        }
    }

    private static bool IsValidPointerPosition(
        Vector3 screenPosition)
    {
        if (float.IsNaN(screenPosition.x) ||
            float.IsNaN(screenPosition.y) ||
            float.IsInfinity(screenPosition.x) ||
            float.IsInfinity(screenPosition.y))
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
            StopCoroutine(launchCoroutine);
        }

        foreach (Ball ball in balls)
        {
            if (ball != null)
            {
                ball.Returned -= HandleBallReturned;
            }
        }
    }
}