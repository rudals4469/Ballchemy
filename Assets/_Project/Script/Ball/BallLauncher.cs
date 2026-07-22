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

    private float launchBaselineY;

    private int launchedBallCount;
    private int returnedBallCount;

    private bool isLaunching;
    private bool hasFirstReturnedBall;
    private bool isAttackCompleted;

    public Ball BallPrefab =>
        ballPrefab;

    public bool IsLaunching =>
        isLaunching;

    public int BallCount =>
        balls.Count;

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

        // 게임 시작 시 Launcher의 Y 위치를
        // 모든 턴의 고정 발사 기준선으로 사용한다.
        launchBaselineY =
            transform.position.y;

        currentTurnLaunchPosition =
            new Vector2(
                transform.position.x,
                launchBaselineY
            );

        nextTurnLaunchPosition =
            currentTurnLaunchPosition;

        NormalizeLauncherPosition();

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

        for (int i = 0;
             i < ballCount;
             i++)
        {
            Ball newBall = Instantiate(
                ballPrefab,
                currentTurnLaunchPosition,
                Quaternion.identity
            );

            newBall.name =
                $"Ball_{i + 1}";

            newBall.Returned +=
                HandleBallReturned;

            newBall.ResetTo(
                currentTurnLaunchPosition
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
        if (direction.sqrMagnitude <=
            0.001f)
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
            new Vector2(
                transform.position.x,
                launchBaselineY
            );

        nextTurnLaunchPosition =
            currentTurnLaunchPosition;

        NormalizeLauncherPosition();

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

            ball.Launch(
                direction
            );

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

        float normalizedReturnX =
            Mathf.Clamp(
                returnedBall
                    .transform
                    .position
                    .x,
                minimumLaunchX,
                maximumLaunchX
            );

        Vector2 normalizedReturnPosition =
            new Vector2(
                normalizedReturnX,
                launchBaselineY
            );

        // ReturnZone에 닿은 순간 바로 발사 기준선 Y로 이동시킨다.
        // 따라서 복귀 위치와 다음 발사 위치의 높이가 동일하다.
        returnedBall.ResetTo(
            normalizedReturnPosition
        );

        if (!hasFirstReturnedBall)
        {
            nextTurnLaunchPosition =
                normalizedReturnPosition;

            hasFirstReturnedBall = true;

            Debug.Log(
                "BallLauncher: 첫 번째 공 복귀 위치 저장, " +
                $"다음 시작 위치 = " +
                $"{nextTurnLaunchPosition}",
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
        nextTurnLaunchPosition =
            new Vector2(
                nextTurnLaunchPosition.x,
                launchBaselineY
            );

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
                launchBaselineY,
                transform.position.z
            );
    }

    private void NormalizeLauncherPosition()
    {
        transform.position =
            new Vector3(
                transform.position.x,
                launchBaselineY,
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