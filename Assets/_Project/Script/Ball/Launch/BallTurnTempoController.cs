using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BallCollection))]
public sealed class BallTurnTempoController :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BallCollection ballCollection;

    [Header("Automatic Speed Up")]
    [SerializeField]
    private bool enableAutomaticSpeedUp = true;

    [Tooltip(
        "남은 공의 비율이 이 값 이하가 되면 " +
        "배속을 시작합니다."
    )]
    [SerializeField, Range(0f, 1f)]
    private float speedUpStartRemainingRatio = 0.5f;

    [SerializeField, Min(1f)]
    private float firstSpeedMultiplier = 1.25f;

    [Tooltip(
        "첫 번째 배속 시작 후 " +
        "두 번째 배속까지 걸리는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float secondSpeedUpDelay = 4f;

    [SerializeField, Min(1f)]
    private float secondSpeedMultiplier = 1.5f;

    [Tooltip(
        "배속이 부드럽게 변경되는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float speedTransitionDuration = 0.3f;

    [Header("Automatic Recall")]
    [SerializeField]
    private bool enableAutomaticRecall = true;

    [Tooltip(
        "남은 공의 비율이 이 값 이하가 되면 " +
        "자동 회수 타이머가 시작됩니다."
    )]
    [SerializeField, Range(0f, 1f)]
    private float recallStartRemainingRatio = 0.2f;

    [Tooltip(
        "회수 조건을 만족한 뒤 " +
        "자동 회수까지 걸리는 시간입니다."
    )]
    [SerializeField, Min(0.1f)]
    private float automaticRecallDelay = 15f;

    private float speedUpElapsedTime;
    private float recallElapsedTime;

    private float currentSpeedMultiplier = 1f;
    private float speedMultiplierVelocity;

    private int totalLaunchedBallCount;
    private int remainingBallCount;

    private bool isAttackActive;
    private bool isLaunchCompleted;

    private bool isSpeedUpPhaseActive;
    private bool isRecallCountdownActive;
    private bool hasRecallRequestBeenSent;

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

    public event Action
        RecallRequested;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
        ValidateReferences();
    }

    private void OnValidate()
    {
        FindReferences();
        NormalizeSettings();
    }

    private void Update()
    {
        if (!isAttackActive ||
            !isLaunchCompleted)
        {
            return;
        }

        UpdateSpeedPhase();
        UpdateRecallCountdown();
    }

    private void FindReferences()
    {
        if (ballCollection == null)
        {
            ballCollection =
                GetComponent<BallCollection>();
        }
    }

    private void NormalizeSettings()
    {
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
        if (ballCollection == null)
        {
            Debug.LogError(
                "BallTurnTempoController: " +
                "BallCollection을 찾지 못했습니다.",
                this
            );
        }
    }

    public void BeginAttack(
        int plannedBallCount)
    {
        totalLaunchedBallCount =
            Mathf.Max(
                plannedBallCount,
                0
            );

        remainingBallCount =
            totalLaunchedBallCount;

        speedUpElapsedTime = 0f;
        recallElapsedTime = 0f;

        currentSpeedMultiplier = 1f;
        speedMultiplierVelocity = 0f;

        isAttackActive =
            totalLaunchedBallCount > 0;

        isLaunchCompleted = false;

        isSpeedUpPhaseActive = false;
        isRecallCountdownActive = false;
        hasRecallRequestBeenSent = false;

        ApplySpeedMultiplierToAllBalls(
            1f
        );
    }

    public void NotifyLaunchCompleted(
        int actualLaunchedBallCount,
        int currentRemainingBallCount)
    {
        if (!isAttackActive)
        {
            return;
        }

        totalLaunchedBallCount =
            Mathf.Max(
                actualLaunchedBallCount,
                0
            );

        remainingBallCount =
            Mathf.Clamp(
                currentRemainingBallCount,
                0,
                totalLaunchedBallCount
            );

        isLaunchCompleted = true;

        EvaluatePhaseConditions();
    }

    public void NotifyBallReturned(
        int currentRemainingBallCount)
    {
        if (!isAttackActive)
        {
            return;
        }

        remainingBallCount =
            Mathf.Clamp(
                currentRemainingBallCount,
                0,
                totalLaunchedBallCount
            );

        if (!isLaunchCompleted)
        {
            return;
        }

        EvaluatePhaseConditions();
    }

    private void EvaluatePhaseConditions()
    {
        if (totalLaunchedBallCount <= 0 ||
            remainingBallCount <= 0)
        {
            return;
        }

        float remainingRatio =
            (float)remainingBallCount /
            totalLaunchedBallCount;

        if (enableAutomaticSpeedUp &&
            !isSpeedUpPhaseActive &&
            remainingRatio <=
            speedUpStartRemainingRatio)
        {
            isSpeedUpPhaseActive = true;
            speedUpElapsedTime = 0f;

            Debug.Log(
                "BallTurnTempoController: " +
                $"남은 공 {remainingBallCount}/" +
                $"{totalLaunchedBallCount}, " +
                "자동 배속 시작",
                this
            );
        }

        if (enableAutomaticRecall &&
            !isRecallCountdownActive &&
            remainingRatio <=
            recallStartRemainingRatio)
        {
            isRecallCountdownActive = true;
            recallElapsedTime = 0f;

            Debug.Log(
                "BallTurnTempoController: " +
                $"남은 공 {remainingBallCount}/" +
                $"{totalLaunchedBallCount}, " +
                $"{automaticRecallDelay:0.##}초 " +
                "자동 회수 타이머 시작",
                this
            );
        }
    }

    private void UpdateSpeedPhase()
    {
        float targetMultiplier = 1f;

        if (enableAutomaticSpeedUp &&
            isSpeedUpPhaseActive)
        {
            speedUpElapsedTime +=
                Time.deltaTime;

            targetMultiplier =
                speedUpElapsedTime >=
                secondSpeedUpDelay
                    ? secondSpeedMultiplier
                    : firstSpeedMultiplier;
        }

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

    private void UpdateRecallCountdown()
    {
        if (!enableAutomaticRecall ||
            !isRecallCountdownActive ||
            hasRecallRequestBeenSent)
        {
            return;
        }

        recallElapsedTime +=
            Time.deltaTime;

        if (recallElapsedTime <
            automaticRecallDelay)
        {
            return;
        }

        hasRecallRequestBeenSent = true;

        RecallRequested?.Invoke();
    }

    private void ApplySpeedMultiplierToMovingBalls(
        float multiplier)
    {
        if (ballCollection == null)
        {
            return;
        }

        IReadOnlyList<Ball> balls =
            ballCollection.Balls;

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

    private void ApplySpeedMultiplierToAllBalls(
        float multiplier)
    {
        if (ballCollection == null)
        {
            return;
        }

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

            ball.SetRuntimeSpeedMultiplier(
                multiplier
            );
        }
    }

    public void EndAttack()
    {
        isAttackActive = false;
        isLaunchCompleted = false;

        totalLaunchedBallCount = 0;
        remainingBallCount = 0;

        speedUpElapsedTime = 0f;
        recallElapsedTime = 0f;

        currentSpeedMultiplier = 1f;
        speedMultiplierVelocity = 0f;

        isSpeedUpPhaseActive = false;
        isRecallCountdownActive = false;
        hasRecallRequestBeenSent = false;

        ApplySpeedMultiplierToAllBalls(
            1f
        );
    }
}