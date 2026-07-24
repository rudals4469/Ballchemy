using UnityEngine;

public sealed class TurnFastForwardController :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BallLauncher ballLauncher;

    [Header("Fast Forward")]
    [Tooltip(
        "마지막 공이 하강을 시작한 뒤, " +
        "블록 타격이 없을 때 기다리는 시간입니다."
    )]
    [SerializeField, Min(0.1f)]
    private float noBlockHitDelay = 2f;

    [Tooltip(
        "복귀 구간에 적용할 배속입니다."
    )]
    [SerializeField, Min(1f)]
    private float fastForwardScale = 3f;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog = true;

    private float normalTimeScale = 1f;
    private float lastBlockHitTime;

    private bool isAttackActive;
    private bool hasEnteredReturnPhase;
    private bool isFastForwarding;
    private bool isSubscribed;

    public bool IsFastForwarding =>
        isFastForwarding;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void OnValidate()
    {
        noBlockHitDelay =
            Mathf.Max(
                0.1f,
                noBlockHitDelay
            );

        fastForwardScale =
            Mathf.Max(
                1f,
                fastForwardScale
            );
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Update()
    {
        if (ballLauncher == null)
        {
            return;
        }

        bool attackIsRunning =
            ballLauncher.IsLaunching ||
            Ball.ActiveMovingBallCount > 0;

        if (!attackIsRunning)
        {
            if (isAttackActive)
            {
                EndAttack();
            }

            return;
        }

        if (!isAttackActive)
        {
            BeginAttack();
        }

        if (!hasEnteredReturnPhase)
        {
            TryEnterReturnPhase();
            return;
        }

        if (isFastForwarding)
        {
            return;
        }

        float elapsedNoHitTime =
            Time.unscaledTime -
            lastBlockHitTime;

        if (elapsedNoHitTime <
            noBlockHitDelay)
        {
            return;
        }

        StartFastForward();
    }

    private void FindReferences()
    {
        if (ballLauncher == null)
        {
            ballLauncher =
                FindFirstObjectByType<
                    BallLauncher
                >();
        }
    }

    private void ValidateReferences()
    {
        if (ballLauncher == null)
        {
            Debug.LogError(
                "TurnFastForwardController: " +
                "BallLauncher를 찾지 못했습니다.",
                this
            );
        }
    }

    private void Subscribe()
    {
        if (isSubscribed)
        {
            return;
        }

        Ball.BlockHitOccurred +=
            HandleBlockHit;

        Ball.MovingBallCountChanged +=
            HandleMovingBallCountChanged;

        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
        {
            return;
        }

        Ball.BlockHitOccurred -=
            HandleBlockHit;

        Ball.MovingBallCountChanged -=
            HandleMovingBallCountChanged;

        isSubscribed = false;
    }

    private void BeginAttack()
    {
        isAttackActive = true;
        hasEnteredReturnPhase = false;
        isFastForwarding = false;

        normalTimeScale =
            Time.timeScale;

        if (normalTimeScale <= 0f)
        {
            normalTimeScale = 1f;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "TurnFastForwardController: " +
                "새 공격 시작",
                this
            );
        }
    }

    private void TryEnterReturnPhase()
    {
        // 모든 공이 발사되기 전에는
        // 복귀 구간으로 판단하지 않는다.
        if (ballLauncher.IsLaunching)
        {
            return;
        }

        Ball lastBall =
            ballLauncher.LastLaunchedBall;

        if (lastBall == null)
        {
            return;
        }

        if (!lastBall.HasStartedDescending)
        {
            return;
        }

        hasEnteredReturnPhase = true;

        // 마지막 공이 하강을 시작한 순간부터
        // 무타격 시간을 새로 측정한다.
        lastBlockHitTime =
            Time.unscaledTime;

        if (showDebugLog)
        {
            Debug.Log(
                "TurnFastForwardController: " +
                "마지막 공 하강 시작, " +
                "무타격 시간 측정 시작",
                this
            );
        }
    }

    private void HandleBlockHit(
        Ball hitBall)
    {
        if (hitBall == null)
        {
            return;
        }

        if (!isAttackActive)
        {
            BeginAttack();
        }

        if (!hasEnteredReturnPhase ||
            isFastForwarding)
        {
            return;
        }

        // 하강 구간에 들어온 이후에는
        // 어떤 공이 블록을 때려도 타이머를 초기화한다.
        lastBlockHitTime =
            Time.unscaledTime;

        if (showDebugLog)
        {
            Debug.Log(
                "TurnFastForwardController: " +
                "하강 중 블록 타격, " +
                "배속 타이머 초기화",
                this
            );
        }
    }

    private void HandleMovingBallCountChanged(
        int movingBallCount)
    {
        if (movingBallCount > 0)
        {
            if (!isAttackActive)
            {
                BeginAttack();
            }

            return;
        }

        if (ballLauncher != null &&
            ballLauncher.IsLaunching)
        {
            return;
        }

        if (isAttackActive)
        {
            EndAttack();
        }
    }

    private void StartFastForward()
    {
        if (isFastForwarding)
        {
            return;
        }

        isFastForwarding = true;

        Time.timeScale =
            normalTimeScale *
            fastForwardScale;

        if (showDebugLog)
        {
            Debug.Log(
                "TurnFastForwardController: " +
                $"{fastForwardScale}배속 시작",
                this
            );
        }
    }

    private void EndAttack()
    {
        RestoreTimeScale();

        isAttackActive = false;
        hasEnteredReturnPhase = false;
        isFastForwarding = false;

        if (showDebugLog)
        {
            Debug.Log(
                "TurnFastForwardController: " +
                "공 복귀 완료, 정상 속도 복구",
                this
            );
        }
    }

    private void RestoreTimeScale()
    {
        if (!isFastForwarding)
        {
            return;
        }

        Time.timeScale =
            normalTimeScale;
    }

    private void OnDisable()
    {
        Unsubscribe();
        RestoreTimeScale();

        isAttackActive = false;
        hasEnteredReturnPhase = false;
        isFastForwarding = false;
    }

    private void OnDestroy()
    {
        Unsubscribe();
        RestoreTimeScale();
    }
}