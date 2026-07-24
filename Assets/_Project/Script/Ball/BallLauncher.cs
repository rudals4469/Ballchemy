using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BallCollection))]
[RequireComponent(typeof(BallTurnTempoController))]
public sealed class BallLauncher :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private BallCollection ballCollection;

    [SerializeField]
    private BallTurnTempoController
        tempoController;

    [Header("Launch Settings")]
    [SerializeField, Min(0f)]
    private float launchInterval = 0.08f;

    [Header("Launch Position Limit")]
    [SerializeField]
    private float minimumLaunchX = -4.3f;

    [SerializeField]
    private float maximumLaunchX = 4.3f;

    private readonly List<Ball>
        currentLaunchSnapshot =
            new List<Ball>();

    private Coroutine launchCoroutine;

    private Vector2 currentTurnLaunchPosition;
    private Vector2 nextTurnLaunchPosition;

    private float launchBaselineY;

    private int plannedBallCount;
    private int launchedBallCount;
    private int returnedBallCount;

    private bool isLaunching;
    private bool hasFirstReturnedBall;
    private bool isAttackCompleted;

    public Ball BallPrefab =>
        ballCollection != null
            ? ballCollection.BallPrefab
            : null;

    public bool IsLaunching =>
        isLaunching;

    public int BallCount =>
        ballCollection != null
            ? ballCollection.Count
            : 0;

    public Ball LastLaunchedBall
    {
        get;
        private set;
    }

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
        ValidateReferences();
        SubscribeEvents();
    }

    private void OnValidate()
    {
        FindReferences();
        NormalizeSettings();
    }

    private void Start()
    {
        if (ballCollection == null ||
            ballCollection.BallPrefab == null)
        {
            return;
        }

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

        ballCollection.Initialize(
            currentTurnLaunchPosition
        );
    }

    private void FindReferences()
    {
        if (turnManager == null)
        {
            turnManager =
                FindFirstObjectByType<TurnManager>();
        }

        if (ballCollection == null)
        {
            ballCollection =
                GetComponent<BallCollection>();
        }

        if (tempoController == null)
        {
            tempoController =
                GetComponent<
                    BallTurnTempoController
                >();
        }
    }

    private void NormalizeSettings()
    {
        launchInterval =
            Mathf.Max(
                launchInterval,
                0f
            );

        if (minimumLaunchX >
            maximumLaunchX)
        {
            float previousMinimum =
                minimumLaunchX;

            minimumLaunchX =
                maximumLaunchX;

            maximumLaunchX =
                previousMinimum;
        }
    }

    private void ValidateReferences()
    {
        if (turnManager == null)
        {
            Debug.LogError(
                "BallLauncher: " +
                "TurnManager를 찾지 못했습니다.",
                this
            );
        }

        if (ballCollection == null)
        {
            Debug.LogError(
                "BallLauncher: " +
                "BallCollection을 찾지 못했습니다.",
                this
            );
        }

        if (tempoController == null)
        {
            Debug.LogError(
                "BallLauncher: " +
                "BallTurnTempoController를 찾지 못했습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (ballCollection != null)
        {
            ballCollection.BallCreated -=
                HandleBallCreated;

            ballCollection.BallCreated +=
                HandleBallCreated;

            IReadOnlyList<Ball> existingBalls =
                ballCollection.Balls;

            for (int i = 0;
                 i < existingBalls.Count;
                 i++)
            {
                HandleBallCreated(
                    existingBalls[i]
                );
            }
        }

        if (tempoController != null)
        {
            tempoController.RecallRequested -=
                ForceRecallRemainingBalls;

            tempoController.RecallRequested +=
                ForceRecallRemainingBalls;
        }
    }

    private void HandleBallCreated(
        Ball createdBall)
    {
        if (createdBall == null)
        {
            return;
        }

        createdBall.Returned -=
            HandleBallReturned;

        createdBall.Returned +=
            HandleBallReturned;
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
            ballCollection == null ||
            turnManager == null)
        {
            return false;
        }

        List<Ball> launchSnapshot =
            ballCollection.CreateSnapshot();

        if (launchSnapshot.Count == 0)
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

        currentLaunchSnapshot.Clear();
        currentLaunchSnapshot.AddRange(
            launchSnapshot
        );

        plannedBallCount =
            currentLaunchSnapshot.Count;

        launchedBallCount = 0;
        returnedBallCount = 0;

        hasFirstReturnedBall = false;
        isAttackCompleted = false;
        isLaunching = true;

        LastLaunchedBall =
            currentLaunchSnapshot[
                currentLaunchSnapshot.Count - 1
            ];

        tempoController?.BeginAttack(
            plannedBallCount
        );

        launchCoroutine =
            StartCoroutine(
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
            "BallLauncher: " +
            $"공 {plannedBallCount}개 순차 발사 시작",
            this
        );

        for (int i = 0;
             i < currentLaunchSnapshot.Count;
             i++)
        {
            Ball ball =
                currentLaunchSnapshot[i];

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

        tempoController
            ?.NotifyLaunchCompleted(
                launchedBallCount,
                GetRemainingLaunchedBallCount()
            );

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
                returnedBall.transform.position.x,
                minimumLaunchX,
                maximumLaunchX
            );

        Vector2 normalizedReturnPosition =
            new Vector2(
                normalizedReturnX,
                launchBaselineY
            );

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

        tempoController
            ?.NotifyBallReturned(
                GetRemainingLaunchedBallCount()
            );

        Debug.Log(
            "BallLauncher: 공 복귀 " +
            $"{returnedBallCount}/" +
            $"{launchedBallCount}",
            this
        );

        TryCompleteAttack();
    }

    private int GetRemainingLaunchedBallCount()
    {
        return Mathf.Max(
            launchedBallCount -
            returnedBallCount,
            0
        );
    }

    private void ForceRecallRemainingBalls()
    {
        if (isAttackCompleted ||
            isLaunching ||
            ballCollection == null)
        {
            return;
        }

        Debug.Log(
            "BallLauncher: 자동 회수 시간 종료, " +
            "남은 공을 회수합니다.",
            this
        );

        List<Ball> recallSnapshot =
            ballCollection.CreateSnapshot();

        for (int i = 0;
             i < recallSnapshot.Count;
             i++)
        {
            Ball ball =
                recallSnapshot[i];

            if (ball == null ||
                !ball.IsMoving)
            {
                continue;
            }

            ball.ForceReturn();
        }

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

        if (launchedBallCount <= 0)
        {
            return;
        }

        if (returnedBallCount <
            launchedBallCount)
        {
            return;
        }

        isAttackCompleted = true;

        tempoController?.EndAttack();

        AlignBallsToNextLaunchPosition();

        currentLaunchSnapshot.Clear();

        turnManager.NotifyAllBallsReturned();
    }

    private void AlignBallsToNextLaunchPosition()
    {
        nextTurnLaunchPosition =
            new Vector2(
                nextTurnLaunchPosition.x,
                launchBaselineY
            );

        ballCollection?.AlignAll(
            nextTurnLaunchPosition
        );

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

        if (ballCollection != null)
        {
            ballCollection.BallCreated -=
                HandleBallCreated;

            IReadOnlyList<Ball> balls =
                ballCollection.Balls;

            for (int i = 0;
                 i < balls.Count;
                 i++)
            {
                Ball ball =
                    balls[i];

                if (ball == null)
                {
                    continue;
                }

                ball.Returned -=
                    HandleBallReturned;
            }
        }

        if (tempoController != null)
        {
            tempoController.RecallRequested -=
                ForceRecallRemainingBalls;
        }
    }
}