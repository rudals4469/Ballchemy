using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BallCollection))]
[RequireComponent(typeof(BallTurnQueueController))]
[RequireComponent(typeof(BallTurnTempoController))]
[RequireComponent(typeof(BallSealController))]
[RequireComponent(typeof(BallGatherAnimator))]
public sealed class BallLauncher :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private BallCollection ballCollection;

    [SerializeField]
    private BallTurnQueueController
        turnQueueController;

    [SerializeField]
    private BallTurnTempoController
        tempoController;

    [SerializeField]
    private BallSealController
        ballSealController;

    [SerializeField]
    private BallGatherAnimator
        gatherAnimator;

    [SerializeField]
    private BallTemporaryUpgradePresenter
        temporaryUpgradePresenter;

    [SerializeField]
    private BoardGrid boardGrid;

    [Header("Launch Settings")]

    [Tooltip(
        "공이 적을 때 적용되는 발사 묶음 사이의 시간 간격입니다."
    )]
    [SerializeField, Min(0f)]
    private float sparseBallLaunchInterval = 0.12f;

    [Tooltip(
        "공이 많을 때 적용되는 발사 묶음 사이의 최소 시간 간격입니다."
    )]
    [SerializeField, Min(0f)]
    private float crowdedBallLaunchInterval = 0.03f;

    [Tooltip(
        "이 발사 묶음 수부터 가장 빠른 발사 간격을 적용합니다.\n" +
        "다각도 발사는 같은 프레임에 나가는 공들을 한 묶음으로 계산합니다."
    )]
    [SerializeField, Min(2)]
    private int fullSpeedBundleCount = 60;

    [Tooltip(
        "발사 시작 방향을 중심으로 아직 발사되지 않은 공을 조향할 수 있는 최대 각도입니다.")]
    [SerializeField, Range(0f, 80f)]
    private float steeringAngle = 10f;

    [Header("Attack Completion")]

    [Tooltip(
        "마지막 공이 복귀한 뒤 " +
        "공 모으기 애니메이션을 시작하기 전까지의 대기 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float allBallsReturnedDelay = 0.7f;

    [Header("Launch Position Limit")]

    [SerializeField]
    private float minimumLaunchX = -4.3f;

    [SerializeField]
    private float maximumLaunchX = 4.3f;

    [Header("Debug")]

    [SerializeField]
    private bool showTemporaryUpgradeDebugLog;

    [SerializeField]
    private bool showMultiDirectionDebugLog;

    private readonly List<Ball>
        currentLaunchSnapshot =
            new List<Ball>();

    private Coroutine launchCoroutine;
    private Coroutine completeAttackCoroutine;

    private Vector2 currentTurnLaunchPosition;
    private Vector2 nextTurnLaunchPosition;
    private Vector2 initialLaunchDirection = Vector2.up;
    private Vector2 currentQueuedLaunchDirection = Vector2.up;
    private MultiDirectionLaunchSettings currentLaunchDirectionSettings =
        MultiDirectionLaunchSettings.Default;

    private float launchBaselineY;

    private int plannedBallCount;
    private int launchedBallCount;
    private int returnedBallCount;
    private int originalLaunchedBallCount;

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

    public Vector2 CurrentLaunchPosition =>
        new Vector2(
            transform.position.x,
            launchBaselineY
        );

    public Vector2 CurrentQueuedLaunchDirection =>
        currentQueuedLaunchDirection;

    public Vector2 InitialLaunchDirection =>
        initialLaunchDirection;

    public float SteeringAngle =>
        steeringAngle;

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

        turnQueueController
            ?.EnsureQueuePrepared();

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

        if (turnQueueController == null)
        {
            turnQueueController =
                GetComponent<
                    BallTurnQueueController
                >();
        }

        if (tempoController == null)
        {
            tempoController =
                GetComponent<
                    BallTurnTempoController
                >();
        }

        if (ballSealController == null)
        {
            ballSealController =
                GetComponent<
                    BallSealController
                >();
        }

        if (gatherAnimator == null)
        {
            gatherAnimator =
                GetComponent<
                    BallGatherAnimator
                >();
        }

        if (temporaryUpgradePresenter == null)
        {
            temporaryUpgradePresenter =
                GetComponent<
                    BallTemporaryUpgradePresenter
                >();
        }

        if (boardGrid == null)
        {
            boardGrid =
                FindFirstObjectByType<BoardGrid>();
        }
    }

    private void NormalizeSettings()
    {
        sparseBallLaunchInterval =
            Mathf.Max(
                sparseBallLaunchInterval,
                0f
            );

        crowdedBallLaunchInterval =
            Mathf.Clamp(
                crowdedBallLaunchInterval,
                0f,
                sparseBallLaunchInterval
            );

        fullSpeedBundleCount =
            Mathf.Max(
                fullSpeedBundleCount,
                2
            );

        steeringAngle =
            Mathf.Clamp(
                steeringAngle,
                0f,
                80f
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

        if (turnQueueController == null)
        {
            Debug.LogError(
                "BallLauncher: " +
                "BallTurnQueueController를 찾지 못했습니다.",
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

        if (ballSealController == null)
        {
            Debug.LogError(
                "BallLauncher: " +
                "BallSealController를 찾지 못했습니다.",
                this
            );
        }

        if (gatherAnimator == null)
        {
            Debug.LogError(
                "BallLauncher: " +
                "BallGatherAnimator를 찾지 못했습니다.",
                this
            );
        }

        if (temporaryUpgradePresenter == null)
        {
            Debug.LogWarning(
                "BallLauncher: " +
                "BallTemporaryUpgradePresenter가 없습니다. " +
                "임시 승급 기능은 작동하지만 " +
                "링과 UP 연출은 표시되지 않습니다.",
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
            turnQueueController == null ||
            turnManager == null)
        {
            return false;
        }

        turnQueueController
            .EnsureQueuePrepared();

        int availableBallCount =
            turnQueueController
                .PreparedBallCount;

        if (availableBallCount <= 0)
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

        int launchableBallCount =
            availableBallCount;

        if (ballSealController != null)
        {
            launchableBallCount =
                ballSealController
                    .GetLaunchableBallCount(
                        availableBallCount
                    );
        }

        launchableBallCount =
            Mathf.Clamp(
                launchableBallCount,
                1,
                availableBallCount
            );

        currentTurnLaunchPosition =
            new Vector2(
                transform.position.x,
                launchBaselineY
            );

        nextTurnLaunchPosition =
            currentTurnLaunchPosition;

        NormalizeLauncherPosition();

        currentLaunchSnapshot.Clear();

        List<Ball> queueSnapshot =
            turnQueueController
                .CreateLaunchSnapshot(
                    launchableBallCount
                );

        for (int i = 0;
             i < queueSnapshot.Count;
             i++)
        {
            Ball ball =
                queueSnapshot[i];

            if (ball == null)
            {
                continue;
            }

            currentLaunchSnapshot.Add(
                ball
            );
        }

        if (currentLaunchSnapshot.Count == 0)
        {
            Debug.LogError(
                "BallLauncher: " +
                "발사 가능한 공이 없습니다.",
                this
            );

            turnManager.NotifyAllBallsReturned();

            return false;
        }

        plannedBallCount =
            currentLaunchSnapshot.Count;

        launchedBallCount = 0;
        returnedBallCount = 0;
        originalLaunchedBallCount = 0;

        hasFirstReturnedBall = false;
        isAttackCompleted = false;
        isLaunching = true;

        LastLaunchedBall =
            currentLaunchSnapshot[
                currentLaunchSnapshot.Count - 1
            ];

        bool queueStarted =
            turnQueueController.BeginLaunch(
                plannedBallCount
            );

        if (!queueStarted)
        {
            Debug.LogError(
                "BallLauncher: " +
                "턴 발사 큐를 시작하지 못했습니다.",
                this
            );

            isLaunching = false;
            isAttackCompleted = true;

            turnManager.NotifyAllBallsReturned();

            return false;
        }

        tempoController?.BeginAttack(
            plannedBallCount
        );

        NotifyRemainingBallsToLaunchChanged();

        /*
         * 공격 도중 증강 레벨이 바뀌더라도
         * 이미 시작한 턴의 갈래 수와 각도가 변하지 않도록
         * 발사 시작 시 설정을 한 번만 저장합니다.
         */
        MultiDirectionLaunchSettings
            multiDirectionSettings =
                MultiDirectionLaunchAugmentSystem
                    .GetCurrentSettings();

        Vector2 steeredBaseDirection =
            MultiDirectionLaunchAugmentSystem
                .GetSteeredBaseDirection(
                    direction,
                    multiDirectionSettings,
                    0.15f
                );

        initialLaunchDirection = steeredBaseDirection;
        currentQueuedLaunchDirection = steeredBaseDirection;
        currentLaunchDirectionSettings = multiDirectionSettings;

        launchCoroutine =
            StartCoroutine(
                LaunchBallsRoutine(
                    steeredBaseDirection,
                    multiDirectionSettings
                )
            );

        return true;
    }

    public bool TrySetLaunchPositionX(
        float requestedX)
    {
        if (!isInitialized ||
            IsAttackInProgress ||
            ballCollection == null)
        {
            return false;
        }

        ResolveLaunchPositionLimits();

        Vector2 selectedPosition =
            new Vector2(
                Mathf.Clamp(
                    requestedX,
                    minimumLaunchX,
                    maximumLaunchX
                ),
                launchBaselineY
            );

        currentTurnLaunchPosition =
            selectedPosition;
        nextTurnLaunchPosition =
            selectedPosition;

        ballCollection.AlignAll(
            selectedPosition
        );
        ballCollection.SetStandbyPosition(
            selectedPosition
        );

        transform.position =
            new Vector3(
                selectedPosition.x,
                launchBaselineY,
                transform.position.z
            );

        return true;
    }

    public bool TryUpdateQueuedLaunchDirection(
        Vector2 requestedDirection)
    {
        if (!isLaunching ||
            requestedDirection.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        Vector2 fieldClampedDirection =
            MultiDirectionLaunchAugmentSystem
                .GetSteeredBaseDirection(
                    requestedDirection,
                    currentLaunchDirectionSettings,
                    0.15f
                );

        float signedOffset = Vector2.SignedAngle(
            initialLaunchDirection,
            fieldClampedDirection);

        float clampedOffset = Mathf.Clamp(
            signedOffset,
            -steeringAngle,
            steeringAngle);

        Vector2 steeredDirection = Quaternion.Euler(
            0f,
            0f,
            clampedOffset) * initialLaunchDirection;

        currentQueuedLaunchDirection =
            MultiDirectionLaunchAugmentSystem
                .GetSteeredBaseDirection(
                    steeredDirection,
                    currentLaunchDirectionSettings,
                    0.15f
                );

        return true;
    }

    private IEnumerator LaunchBallsRoutine(
        Vector2 baseDirection,
        MultiDirectionLaunchSettings
            multiDirectionSettings)
    {
        int branchCount =
            multiDirectionSettings.IsActive
                ? multiDirectionSettings.BranchCount
                : 1;

        branchCount =
            Mathf.Max(
                branchCount,
                1
            );

        float spreadAngle =
            multiDirectionSettings.IsActive
                ? multiDirectionSettings.SpreadAngle
                : 0f;

        if (showMultiDirectionDebugLog)
        {
            Debug.Log(
                "BallLauncher: " +
                $"공 {plannedBallCount}개 발사 시작, " +
                $"갈래={branchCount}, " +
                $"확산 각도=±{spreadAngle:0.#}°",
                this
            );
        }

        int nextBallIndex = 0;
        int bundleCount = Mathf.CeilToInt(
            currentLaunchSnapshot.Count /
            (float)branchCount);
        float currentLaunchInterval =
            GetLaunchInterval(bundleCount);

        while (nextBallIndex <
               currentLaunchSnapshot.Count)
        {
            Vector2 currentBundleDirection =
                currentQueuedLaunchDirection;

            /*
             * 같은 묶음에서는 최대 branchCount개의 공을
             * 같은 프레임에 서로 다른 방향으로 발사합니다.
             */
            for (int branchIndex = 0;
                 branchIndex < branchCount;
                 branchIndex++)
            {
                if (nextBallIndex >=
                    currentLaunchSnapshot.Count)
                {
                    break;
                }

                Ball ball =
                    currentLaunchSnapshot[
                        nextBallIndex
                    ];

                nextBallIndex++;

                if (ball == null)
                {
                    continue;
                }

                Vector2 branchDirection =
                    MultiDirectionLaunchAugmentSystem
                        .GetBranchDirection(
                            currentBundleDirection,
                            branchIndex,
                            branchCount,
                            spreadAngle
                        );

                LaunchSingleBall(
                    ball,
                    branchDirection,
                    branchIndex,
                    branchCount
                );
            }

            /*
             * 마지막 묶음 이후에는 불필요한 대기를 하지 않습니다.
             */
            if (nextBallIndex >=
                currentLaunchSnapshot.Count)
            {
                break;
            }

            if (currentLaunchInterval > 0f)
            {
                yield return
                    new WaitForSeconds(
                        currentLaunchInterval
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

    private float GetLaunchInterval(
        int bundleCount)
    {
        float progress = Mathf.InverseLerp(
            1f,
            fullSpeedBundleCount,
            Mathf.Max(bundleCount, 1));

        progress = Mathf.SmoothStep(
            0f,
            1f,
            progress);

        return Mathf.Lerp(
            sparseBallLaunchInterval,
            crowdedBallLaunchInterval,
            progress);
    }

    private void LaunchSingleBall(
        Ball ball,
        Vector2 direction,
        int branchIndex,
        int branchCount)
    {
        if (ball == null)
        {
            return;
        }

        /*
         * 이전 비행에서 남은 임시 상태를 정리하고
         * 현재 턴 발사 위치로 옮깁니다.
         */
        ball.ResetTo(
            currentTurnLaunchPosition
        );

        TemporaryBallUpgradeResult
            temporaryUpgradeResult =
                TemporaryBallUpgradeAugmentSystem
                    .RollForBall(
                        ball
                    );

        bool temporaryUpgradeApplied =
            ball.ApplyTemporaryUpgrade(
                temporaryUpgradeResult
            );

        if (temporaryUpgradeApplied &&
            temporaryUpgradeResult.WasActivated)
        {
            temporaryUpgradePresenter?.Play(
                ball,
                temporaryUpgradeResult
                    .UsedMaximumGradeBonus
            );

            if (showTemporaryUpgradeDebugLog)
            {
                Debug.Log(
                    "BallLauncher: " +
                    $"{ball.name} 불안정한 진화 발동, " +
                    $"승급 단계=" +
                    $"{temporaryUpgradeResult.AppliedUpgradeStepCount}, " +
                    $"최종 등급 보너스=" +
                    $"{temporaryUpgradeResult.MaximumGradeDamageBonus}",
                    ball
                );
            }
        }

        ball.Launch(
            direction
        );

        launchedBallCount++;
        originalLaunchedBallCount++;

        turnQueueController
            ?.NotifyBallLaunched(
                ball
            );

        if (originalLaunchedBallCount % 5 == 0 &&
            AugmentCombatModifiers.GetRuleInteger(
                RuleAugmentEffectKind.ChainClone) > 0)
        {
            SpawnAugmentClone(ball, ball.transform.position,
                Quaternion.Euler(0f, 0f, 8f) * direction,
                AugmentCombatModifiers.GetRuleInteger(
                    RuleAugmentEffectKind.ChainClone));
        }

        NotifyRemainingBallsToLaunchChanged();

        if (showMultiDirectionDebugLog &&
            branchCount > 1)
        {
            Debug.Log(
                "BallLauncher: " +
                $"{ball.name} " +
                $"갈래 {branchIndex + 1}/" +
                $"{branchCount} 발사, " +
                $"방향={direction}",
                ball
            );
        }
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

        ResolveLaunchPositionLimits();

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

        bool isTemporaryAugmentBall =
            returnedBall.GetComponent<TemporaryAugmentBall>() != null;
        if (!hasFirstReturnedBall && !isTemporaryAugmentBall)
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

    public void ForceRecallRemainingBalls()
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

        tempoController?.EndAttack();

        completeAttackCoroutine =
            StartCoroutine(
                CompleteAttackAfterDelayRoutine()
            );
    }

    private IEnumerator
        CompleteAttackAfterDelayRoutine()
    {
        if (allBallsReturnedDelay > 0f)
        {
            Debug.Log(
                "BallLauncher: 모든 공 복귀 완료, " +
                $"{allBallsReturnedDelay:0.00}초 후 " +
                "공 모으기 시작",
                this
            );

            yield return
                new WaitForSeconds(
                    allBallsReturnedDelay
                );
        }

        nextTurnLaunchPosition =
            new Vector2(
                nextTurnLaunchPosition.x,
                launchBaselineY
            );

        if (gatherAnimator != null)
        {
            yield return gatherAnimator
                .PlayGatherRoutine(
                    nextTurnLaunchPosition
                );
        }
        else
        {
            ballCollection?.AlignAll(
                nextTurnLaunchPosition
            );
        }

        ApplyNextLaunchPosition();

        completeAttackCoroutine = null;

        CompleteAttack();
    }

    private void ApplyNextLaunchPosition()
    {
        ballCollection?.SetStandbyPosition(
            nextTurnLaunchPosition
        );

        transform.position =
            new Vector3(
                nextTurnLaunchPosition.x,
                launchBaselineY,
                transform.position.z
            );
    }

    private void CompleteAttack()
    {
        if (isAttackCompleted)
        {
            return;
        }

        isAttackCompleted = true;

        currentLaunchSnapshot.Clear();

        turnQueueController
            ?.CompleteTurnAndPrepareNextQueue();

        LaunchCycleCompleted?.Invoke();

        turnManager?.NotifyAllBallsReturned();

        Debug.Log(
            "BallLauncher: 공 모으기 완료, " +
            "다음 턴 공 순서 셔플 완료",
            this
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

        ResolveLaunchPositionLimits();

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

        ballSealController
            ?.ClearPendingSeal();

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

        turnQueueController
            ?.EnsureQueuePrepared();

        Debug.Log(
            "BallLauncher: 보스전 시작 상태 초기화, " +
            $"중앙 위치={centerPosition}, " +
            "공 봉인 해제",
            this
        );

        return true;
    }

    public Ball SpawnAugmentClone(
        Ball source, Vector2 position, Vector2 direction, int damagePercent)
    {
        if (source == null || source.Definition == null || ballCollection == null ||
            source.GetComponent<TemporaryAugmentBall>() != null) return null;

        Ball clone = ballCollection.CreateTemporaryAugmentBall(source.Definition);
        if (clone == null) return null;
        clone.gameObject.AddComponent<TemporaryAugmentBall>();
        clone.ResetTo(position + direction.normalized * 0.15f);
        int targetDamage = Mathf.Max(1,
            Mathf.RoundToInt(source.CurrentDamage * damagePercent / 100f));
        clone.CombatController.SetIndividualDirectDamageBonus(
            targetDamage - clone.CombatController.BaseDamage);
        clone.Returned += HandleTemporaryAugmentBallReturned;
        clone.Launch(direction);
        launchedBallCount++;
        return clone;
    }

    private void HandleTemporaryAugmentBallReturned(Ball ball)
    {
        if (ball == null) return;
        ball.Returned -= HandleTemporaryAugmentBallReturned;
        ballCollection?.DestroyTemporaryAugmentBall(ball);
    }

    private void ResolveLaunchPositionLimits()
    {
        if (boardGrid == null ||
            boardGrid.Settings == null ||
            BallPrefab == null)
        {
            return;
        }

        CircleCollider2D ballCollider =
            BallPrefab.GetComponent<CircleCollider2D>();

        if (ballCollider == null)
        {
            return;
        }

        Vector3 colliderScale =
            ballCollider.transform.lossyScale;

        float ballRadius =
            ballCollider.radius *
            Mathf.Max(
                Mathf.Abs(colliderScale.x),
                Mathf.Abs(colliderScale.y)
            );

        float halfBoardWidth =
            boardGrid.Settings.HalfBoardWidth;

        minimumLaunchX =
            boardGrid.transform.position.x -
            halfBoardWidth +
            ballRadius;

        maximumLaunchX =
            boardGrid.transform.position.x +
            halfBoardWidth -
            ballRadius;
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

                ball.ClearTemporaryUpgradeRuntime();
            }
        }

        if (tempoController != null)
        {
            tempoController.RecallRequested -=
                ForceRecallRemainingBalls;
        }
    }
}
