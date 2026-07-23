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

    [Header("Automatic Speed Up")]
    [Tooltip(
        "턴 후반에 남아 있는 공들의 속도를 " +
        "자동으로 증가시킵니다."
    )]
    [SerializeField]
    private bool enableAutomaticSpeedUp = true;

    [Tooltip(
        "남은 공의 비율이 이 값 이하가 되면 " +
        "배속을 시작합니다. " +
        "0.5는 전체 공의 50%를 의미합니다."
    )]
    [SerializeField, Range(0f, 1f)]
    private float speedUpStartRemainingRatio = 0.5f;

    [Tooltip(
        "배속 시작 직후 적용되는 첫 번째 배속입니다."
    )]
    [SerializeField, Min(1f)]
    private float firstSpeedMultiplier = 1.25f;

    [Tooltip(
        "배속이 시작된 뒤 두 번째 배속까지의 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float secondSpeedUpDelay = 4f;

    [Tooltip(
        "두 번째 단계에서 적용되는 배속입니다."
    )]
    [SerializeField, Min(1f)]
    private float secondSpeedMultiplier = 1.5f;

    [Tooltip(
        "현재 배속에서 목표 배속까지 " +
        "부드럽게 전환되는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float speedTransitionDuration = 0.3f;

    [Header("Automatic Recall")]
    [Tooltip(
        "남은 공이 적어지면 자동 회수 타이머를 시작합니다."
    )]
    [SerializeField]
    private bool enableAutomaticRecall = true;

    [Tooltip(
        "남은 공의 비율이 이 값 이하가 되면 " +
        "자동 회수 타이머를 시작합니다. " +
        "0.2는 전체 공의 20%를 의미합니다."
    )]
    [SerializeField, Range(0f, 1f)]
    private float recallStartRemainingRatio = 0.2f;

    [Tooltip(
        "회수 조건을 만족한 순간부터 " +
        "남은 공이 자동 회수되기까지의 시간입니다."
    )]
    [SerializeField, Min(0.1f)]
    private float automaticRecallDelay = 15f;

    private readonly List<Ball> balls =
        new List<Ball>();

    private Coroutine launchCoroutine;

    private Vector2 currentTurnLaunchPosition;
    private Vector2 nextTurnLaunchPosition;

    private float launchBaselineY;

    private float attackElapsedTime;
    private float speedUpElapsedTime;
    private float recallElapsedTime;

    private float currentSpeedMultiplier = 1f;
    private float speedMultiplierVelocity;

    private int launchedBallCount;
    private int returnedBallCount;

    private bool isLaunching;
    private bool hasFirstReturnedBall;
    private bool isAttackCompleted;

    private bool isAttackTempoActive;
    private bool isSpeedUpPhaseActive;
    private bool isRecallCountdownActive;
    private bool hasAutomaticRecallTriggered;

    public Ball BallPrefab =>
        ballPrefab;

    public bool IsLaunching =>
        isLaunching;

    public int BallCount =>
        balls.Count;

    public float AttackElapsedTime =>
        attackElapsedTime;

    public float CurrentSpeedMultiplier =>
        currentSpeedMultiplier;

    public bool IsSpeedUpPhaseActive =>
        isSpeedUpPhaseActive;

    public bool IsRecallCountdownActive =>
        isRecallCountdownActive;

    public float RecallElapsedTime =>
        recallElapsedTime;

    public float RecallRemainingTime =>
        isRecallCountdownActive
            ? Mathf.Max(
                automaticRecallDelay -
                recallElapsedTime,
                0f
            )
            : automaticRecallDelay;

    public Ball LastLaunchedBall
    {
        get;
        private set;
    }

    private void Awake()
    {
        if (turnManager == null)
        {
            turnManager =
                FindFirstObjectByType<TurnManager>();
        }

        NormalizeSettings();
        ValidateReferences();
    }

    private void OnValidate()
    {
        NormalizeSettings();
    }

    private void Start()
    {
        if (ballPrefab == null)
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
        CreateBalls();
    }

    private void Update()
    {
        UpdateAttackTempo();
    }

    private void NormalizeSettings()
    {
        ballCount =
            Mathf.Max(
                ballCount,
                1
            );

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

        speedUpStartRemainingRatio =
            Mathf.Clamp01(
                speedUpStartRemainingRatio
            );

        firstSpeedMultiplier =
            Mathf.Max(
                firstSpeedMultiplier,
                1f
            );

        secondSpeedUpDelay =
            Mathf.Max(
                secondSpeedUpDelay,
                0f
            );

        secondSpeedMultiplier =
            Mathf.Max(
                secondSpeedMultiplier,
                firstSpeedMultiplier
            );

        speedTransitionDuration =
            Mathf.Max(
                speedTransitionDuration,
                0f
            );

        recallStartRemainingRatio =
            Mathf.Clamp01(
                recallStartRemainingRatio
            );

        automaticRecallDelay =
            Mathf.Max(
                automaticRecallDelay,
                0.1f
            );
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
            Ball newBall =
                Instantiate(
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

            balls.Add(
                newBall
            );
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
                balls[i]
                    .GetComponent<Collider2D>();

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
                    balls[j]
                        .GetComponent<Collider2D>();

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

        LastLaunchedBall =
            FindLastAvailableBall();

        BeginAttackTempo();

        launchCoroutine =
            StartCoroutine(
                LaunchBallsRoutine(
                    direction.normalized
                )
            );

        return true;
    }

    private void BeginAttackTempo()
    {
        attackElapsedTime = 0f;
        speedUpElapsedTime = 0f;
        recallElapsedTime = 0f;

        currentSpeedMultiplier = 1f;
        speedMultiplierVelocity = 0f;

        isSpeedUpPhaseActive = false;
        isRecallCountdownActive = false;
        hasAutomaticRecallTriggered = false;

        isAttackTempoActive =
            enableAutomaticSpeedUp ||
            enableAutomaticRecall;

        ApplySpeedMultiplierToBalls(
            1f
        );
    }

    private void UpdateAttackTempo()
    {
        if (!isAttackTempoActive ||
            isAttackCompleted)
        {
            return;
        }

        attackElapsedTime +=
            Time.deltaTime;

        /*
         * 모든 공이 발사된 이후부터
         * 남은 공 비율을 기준으로 템포를 조절한다.
         */
        if (isLaunching)
        {
            return;
        }

        UpdateSpeedUpPhase();
        UpdateAutomaticSpeed();
        UpdateRecallCountdown();
    }

    private void UpdateSpeedUpPhase()
    {
        if (!enableAutomaticSpeedUp)
        {
            isSpeedUpPhaseActive = false;

            return;
        }

        if (!isSpeedUpPhaseActive)
        {
            bool reachedSpeedUpCondition =
                IsRemainingRatioAtOrBelow(
                    speedUpStartRemainingRatio
                );

            if (!reachedSpeedUpCondition)
            {
                return;
            }

            isSpeedUpPhaseActive = true;
            speedUpElapsedTime = 0f;

            Debug.Log(
                "BallLauncher: 남은 공이 " +
                $"{speedUpStartRemainingRatio * 100f:0}% 이하가 되어 " +
                "자동 배속을 시작합니다.",
                this
            );
        }

        speedUpElapsedTime +=
            Time.deltaTime;
    }

    private void UpdateAutomaticSpeed()
    {
        float targetMultiplier =
            GetTargetSpeedMultiplier();

        float nextMultiplier;

        if (speedTransitionDuration <= 0f)
        {
            nextMultiplier =
                targetMultiplier;
        }
        else
        {
            nextMultiplier =
                Mathf.SmoothDamp(
                    currentSpeedMultiplier,
                    targetMultiplier,
                    ref speedMultiplierVelocity,
                    speedTransitionDuration
                );
        }

        if (Mathf.Abs(
                currentSpeedMultiplier -
                nextMultiplier
            ) <= 0.0001f)
        {
            return;
        }

        currentSpeedMultiplier =
            nextMultiplier;

        ApplySpeedMultiplierToMovingBalls(
            currentSpeedMultiplier
        );
    }

    private float GetTargetSpeedMultiplier()
    {
        if (!enableAutomaticSpeedUp ||
            !isSpeedUpPhaseActive)
        {
            return 1f;
        }

        if (speedUpElapsedTime >=
            secondSpeedUpDelay)
        {
            return secondSpeedMultiplier;
        }

        return firstSpeedMultiplier;
    }

    private void UpdateRecallCountdown()
    {
        if (!enableAutomaticRecall ||
            hasAutomaticRecallTriggered)
        {
            return;
        }

        if (!isRecallCountdownActive)
        {
            bool reachedRecallCondition =
                IsRemainingRatioAtOrBelow(
                    recallStartRemainingRatio
                );

            if (!reachedRecallCondition)
            {
                return;
            }

            isRecallCountdownActive = true;
            recallElapsedTime = 0f;

            Debug.Log(
                "BallLauncher: 남은 공이 " +
                $"{recallStartRemainingRatio * 100f:0}% 이하가 되어 " +
                $"{automaticRecallDelay:0.##}초 자동 회수 타이머를 시작합니다.",
                this
            );

            return;
        }

        recallElapsedTime +=
            Time.deltaTime;

        if (recallElapsedTime <
            automaticRecallDelay)
        {
            return;
        }

        ForceRecallRemainingBalls();
    }

    private bool IsRemainingRatioAtOrBelow(
        float targetRatio)
    {
        if (launchedBallCount <= 0)
        {
            return false;
        }

        int remainingBallCount =
            GetRemainingBallCount();

        if (remainingBallCount <= 0)
        {
            return false;
        }

        float remainingRatio =
            (float)remainingBallCount /
            launchedBallCount;

        return remainingRatio <=
               targetRatio;
    }

    private int GetRemainingBallCount()
    {
        return Mathf.Max(
            launchedBallCount -
            returnedBallCount,
            0
        );
    }

    private void ApplySpeedMultiplierToMovingBalls(
        float multiplier)
    {
        for (int i = 0;
             i < balls.Count;
             i++)
        {
            Ball ball =
                balls[i];

            if (ball == null ||
                !ball.IsMoving)
            {
                continue;
            }

            ball.SetRuntimeSpeedMultiplier(
                multiplier
            );
        }
    }

    private void ApplySpeedMultiplierToBalls(
        float multiplier)
    {
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

            ball.SetRuntimeSpeedMultiplier(
                multiplier
            );
        }
    }

    private void ForceRecallRemainingBalls()
    {
        if (hasAutomaticRecallTriggered)
        {
            return;
        }

        hasAutomaticRecallTriggered = true;
        isAttackTempoActive = false;

        currentSpeedMultiplier = 1f;
        speedMultiplierVelocity = 0f;

        Debug.Log(
            "BallLauncher: 자동 회수 타이머 종료, " +
            "남은 공을 회수합니다.",
            this
        );

        for (int i = 0;
             i < balls.Count;
             i++)
        {
            Ball ball =
                balls[i];

            if (ball == null ||
                !ball.IsMoving)
            {
                continue;
            }

            ball.SetRuntimeSpeedMultiplier(
                1f
            );

            ball.ForceReturn();
        }

        TryCompleteAttack();
    }

    private Ball FindLastAvailableBall()
    {
        for (int i = balls.Count - 1;
             i >= 0;
             i--)
        {
            if (balls[i] != null)
            {
                return balls[i];
            }
        }

        return null;
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

            ball.SetRuntimeSpeedMultiplier(
                currentSpeedMultiplier
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
                $"다음 시작 위치 = {nextTurnLaunchPosition}",
                this
            );
        }

        returnedBallCount++;

        Debug.Log(
            "BallLauncher: 공 복귀 " +
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

        StopAttackTempo();
        AlignBallsToNextLaunchPosition();

        turnManager.NotifyAllBallsReturned();
    }

    private void StopAttackTempo()
    {
        isAttackTempoActive = false;

        attackElapsedTime = 0f;
        speedUpElapsedTime = 0f;
        recallElapsedTime = 0f;

        currentSpeedMultiplier = 1f;
        speedMultiplierVelocity = 0f;

        isSpeedUpPhaseActive = false;
        isRecallCountdownActive = false;
        hasAutomaticRecallTriggered = false;

        ApplySpeedMultiplierToBalls(
            1f
        );
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