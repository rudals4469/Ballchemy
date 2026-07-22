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

    [Header("Ball Settings")]
    [SerializeField, Min(1)]
    private int ballCount = 5;

    [SerializeField, Min(0f)]
    private float launchInterval = 0.08f;

    [Header("Launch Position Limit")]
    [SerializeField]
    private float minimumLaunchX = -4.3f;

    [SerializeField]
    private float maximumLaunchX = 4.3f;

    private readonly List<Ball> balls =
        new List<Ball>();

    private Coroutine launchCoroutine;

    private Vector2 currentTurnLaunchPosition;
    private Vector2 nextTurnLaunchPosition;

    private int launchedBallCount;
    private int returnedBallCount;

    private bool isLaunching;
    private bool hasFirstReturnedBall;
    private bool isAttackCompleted;

    public Ball BallPrefab => ballPrefab;
    public bool IsLaunching => isLaunching;
    public int BallCount => balls.Count;

    private void Awake()
    {
        if (turnManager == null)
        {
            turnManager =
                FindFirstObjectByType<TurnManager>();
        }

        ValidateReferences();
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
    }

    private void ValidateReferences()
    {
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
            if (balls[i] == null)
            {
                continue;
            }

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
                if (balls[j] == null)
                {
                    continue;
                }

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

    public bool TryLaunch(
        Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        if (isLaunching ||
            balls.Count == 0 ||
            turnManager == null)
        {
            return false;
        }

        if (!turnManager.TryStartAttack())
        {
            Debug.LogWarning(
                "BallLauncher: 현재 턴 상태에서는 " +
                "발사할 수 없습니다. " +
                $"현재 상태: {turnManager.CurrentState}",
                this
            );

            return false;
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

        launchCoroutine = StartCoroutine(
            LaunchBallsRoutine(
                direction.normalized
            )
        );

        return true;
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
                yield return new WaitForSeconds(
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
                    returnedBall.transform.position.x,
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

        // 모든 공의 순차 발사가 끝날 때까지 기다린다.
        if (isLaunching)
        {
            return;
        }

        if (launchedBallCount == 0)
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