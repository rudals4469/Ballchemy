using UnityEngine;

public sealed class TurnFastForwardController :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BallLauncher ballLauncher;

    [Header("Fast Forward")]
    [Tooltip(
        "마지막 공이 블록을 맞힌 뒤 " +
        "추가 타격이 없을 때 기다리는 시간입니다."
    )]
    [SerializeField, Min(0.1f)]
    private float noBlockHitDelay = 2f;

    [SerializeField, Min(1f)]
    private float fastForwardScale = 3f;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog = true;

    private float normalTimeScale = 1f;
    private float lastBlockHitTime;

    private bool isAttackActive;
    private bool lastBallHasHitBlock;
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

        if (isFastForwarding)
        {
            return;
        }

        if (ballLauncher.IsLaunching)
        {
            return;
        }

        if (!lastBallHasHitBlock)
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
                FindFirstObjectByType<BallLauncher>();
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
        lastBallHasHitBlock = false;
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

    private void HandleBlockHit(
        Ball hitBall)
    {
        if (hitBall == null ||
            ballLauncher == null)
        {
            return;
        }

        if (!isAttackActive)
        {
            BeginAttack();
        }

        if (isFastForwarding)
        {
            return;
        }

        if (!lastBallHasHitBlock)
        {
            if (hitBall !=
                ballLauncher.LastLaunchedBall)
            {
                return;
            }

            lastBallHasHitBlock = true;

            if (showDebugLog)
            {
                Debug.Log(
                    "TurnFastForwardController: " +
                    "마지막 공이 블록을 처음 타격함",
                    this
                );
            }
        }

        // 마지막 공이 한 번 타격한 이후부터는
        // 어떤 공이든 블록을 때릴 때마다 타이머를 초기화한다.
        lastBlockHitTime =
            Time.unscaledTime;
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
        lastBallHasHitBlock = false;
        isFastForwarding = false;

        if (showDebugLog)
        {
            Debug.Log(
                "TurnFastForwardController: " +
                "턴 종료, 정상 속도 복구",
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
        lastBallHasHitBlock = false;
        isFastForwarding = false;
    }

    private void OnDestroy()
    {
        Unsubscribe();
        RestoreTimeScale();
    }
}