using System;
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

    [Header("Attack Completion")]
    [SerializeField, Min(0f)]
    private float allBallsReturnedDelay = 0.7f;

    [Header("Launch Position Limit")]
    [SerializeField]
    private float minimumLaunchX = -4.3f;

    [SerializeField]
    private float maximumLaunchX = 4.3f;

    private readonly List<Ball>
        currentLaunchSnapshot =
            new List<Ball>();

    private Coroutine launchCoroutine;
    private Coroutine completeAttackCoroutine;

    private Vector2 currentTurnLaunchPosition;
    private Vector2 nextTurnLaunchPosition;

    private float launchBaselineY;

    private int plannedBallCount;
    private int launchedBallCount;
    private int returnedBallCount;

    private bool isInitialized;
    private bool isLaunching;
    private bool hasFirstReturnedBall;
    private bool isAttackCompleted;

    public Ball BallPrefab =>
        ballCollection != null
            ? ballCollection.BallPrefab
            : null;

    public bool IsLaunching =>
        isLaunching;

    public bool IsAttackInProgress =>
        isLaunching ||
        (
            launchedBallCount > 0 &&
            !isAttackCompleted
        );

    public int BallCount =>
        ballCollection != null
            ? ballCollection.Count
            : 0;

    public int RemainingBallsToLaunch =>
        Mathf.Max(
            plannedBallCount -
            launchedBallCount,
            0
        );

    public Ball LastLaunchedBall
    {
        get;
        private set;
    }

    public event Action<int>
        RemainingBallsToLaunchChanged;

    public event Action
        LaunchCycleCompleted;

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

        isInitialized = true;
        isAttackCompleted = true;
    }

    private void FindReferences()
    {
        if (turnManager == null)
        {
            turnManager =
                FindFirstObjectByType<
                    TurnManager
                >();
        }

        if (ballCollection == null)
        {
            ballCollection =
                GetComponent<
                    BallCollection
                >();
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

        allBallsReturnedDelay =
            Mathf.Max(
                allBallsReturnedDelay,
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

        if (IsAttackInProgress ||
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

        NotifyRemainingBallsToLaunchChanged();

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

            ball.ResetTo(
                currentTurnLaunchPosition
            );

            ball.Launch(
                direction
            );

            launchedBallCount++;

            NotifyRemainingBallsToLaunchChanged();

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

    private void NotifyRemainingBallsToLaunchChanged()
    {
        RemainingBallsToLaunchChanged?.Invoke(
            RemainingBallsToLaunch
        );
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

        if (completeAttackCoroutine != null)
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

        /*
         * 모든 공이 하단에 도착한 시점에
         * 공을 정렬하고 배속 처리를 종료한다.
         *
         * 다만 TurnManager에는 아직 공격 종료를
         * 통보하지 않으므로 대기 시간 동안에는
         * 다시 조준하거나 발사할 수 없다.
         */
        tempoController?.EndAttack();

        AlignBallsToNextLaunchPosition();

        if (allBallsReturnedDelay <= 0f)
        {
            CompleteAttack();

            return;
        }

        completeAttackCoroutine =
            StartCoroutine(
                CompleteAttackAfterDelayRoutine()
            );
    }

    private IEnumerator
        CompleteAttackAfterDelayRoutine()
    {
        Debug.Log(
            "BallLauncher: 모든 공 복귀 완료, " +
            $"{allBallsReturnedDelay:0.00}초 대기",
            this
        );

        yield return
            new WaitForSeconds(
                allBallsReturnedDelay
            );

        completeAttackCoroutine = null;

        CompleteAttack();
    }

    private void CompleteAttack()
    {
        if (isAttackCompleted)
        {
            return;
        }

        isAttackCompleted = true;

        currentLaunchSnapshot.Clear();

        LaunchCycleCompleted?.Invoke();

        turnManager?.NotifyAllBallsReturned();

        Debug.Log(
            "BallLauncher: 복귀 대기 종료, " +
            "다음 턴 처리를 시작합니다.",
            this
        );
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

    public bool TryResetLaunchPositionToCenter()
    {
        if (!isInitialized ||
            ballCollection == null)
        {
            Debug.LogWarning(
                "BallLauncher: 초기화 전이라 " +
                "발사 위치를 중앙으로 이동할 수 없습니다.",
                this
            );

            return false;
        }

        if (IsAttackInProgress)
        {
            Debug.LogWarning(
                "BallLauncher: 공격 진행 중에는 " +
                "발사 위치를 중앙으로 이동할 수 없습니다.",
                this
            );

            return false;
        }

        float centerX =
            (
                minimumLaunchX +
                maximumLaunchX
            ) *
            0.5f;

        Vector2 centerPosition =
            new Vector2(
                centerX,
                launchBaselineY
            );

        currentTurnLaunchPosition =
            centerPosition;

        nextTurnLaunchPosition =
            centerPosition;

        ballCollection.AlignAll(
            centerPosition
        );

        transform.position =
            new Vector3(
                centerPosition.x,
                launchBaselineY,
                transform.position.z
            );

        Debug.Log(
            "BallLauncher: 보스전 시작 위치를 " +
            $"중앙 {centerPosition}으로 초기화했습니다.",
            this
        );

        return true;
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

        if (completeAttackCoroutine != null)
        {
            StopCoroutine(
                completeAttackCoroutine
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