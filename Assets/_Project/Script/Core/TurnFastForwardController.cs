using UnityEngine;
using UnityEngine.Serialization;

public sealed class TurnFastForwardController :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BallLauncher ballLauncher;

    [Header("Turn Timeout")]
    [FormerlySerializedAs("noBlockHitDelay")]
    [SerializeField, Min(0.1f)]
    private float fastForwardDelay = 5f;

    [SerializeField, Min(0.1f)]
    private float secondFastForwardDelay = 10f;

    [SerializeField, Min(0.1f)]
    private float forceRecallDelay = 15f;

    [SerializeField, Min(1f)]
    private float fastForwardScale = 1.5f;

    [SerializeField, Min(1f)]
    private float secondFastForwardScale = 2f;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog = true;

    private float normalTimeScale = 1f;
    private float launchCompletedTime;

    private bool isAttackActive;
    private bool hasLaunchCompleted;
    private bool isFastForwarding;
    private bool hasSecondFastForwardStarted;
    private bool hasRecallBeenRequested;

    public bool IsFastForwarding =>
        isFastForwarding;

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

        if (!hasLaunchCompleted)
        {
            if (ballLauncher.IsLaunching)
            {
                return;
            }

            hasLaunchCompleted = true;
            launchCompletedTime = Time.unscaledTime;

            if (showDebugLog)
            {
                Debug.Log(
                    "TurnFastForwardController: 마지막 공 발사 완료, " +
                    "배속/전체 회수 타이머 시작",
                    this
                );
            }
        }

        float elapsedTime =
            Time.unscaledTime -
            launchCompletedTime;

        if (!isFastForwarding &&
            elapsedTime >= fastForwardDelay)
        {
            StartFastForward(
                fastForwardScale,
                fastForwardDelay
            );
        }

        if (!hasSecondFastForwardStarted &&
            elapsedTime >= secondFastForwardDelay)
        {
            hasSecondFastForwardStarted = true;

            StartFastForward(
                secondFastForwardScale,
                secondFastForwardDelay
            );
        }

        if (!hasRecallBeenRequested &&
            elapsedTime >= forceRecallDelay)
        {
            hasRecallBeenRequested = true;
            ballLauncher.ForceRecallRemainingBalls();

            if (showDebugLog)
            {
                Debug.Log(
                    "TurnFastForwardController: 마지막 공 발사 후 " +
                    $"{forceRecallDelay:0.##}초 경과, 전체 공 회수",
                    this
                );
            }
        }
    }

    private void FindReferences()
    {
        if (ballLauncher == null)
        {
            ballLauncher =
                FindFirstObjectByType<BallLauncher>();
        }
    }

    private void NormalizeSettings()
    {
        fastForwardDelay =
            Mathf.Max(fastForwardDelay, 0.1f);

        secondFastForwardDelay =
            Mathf.Max(
                secondFastForwardDelay,
                fastForwardDelay
            );

        forceRecallDelay =
            Mathf.Max(
                forceRecallDelay,
                secondFastForwardDelay
            );

        fastForwardScale =
            Mathf.Max(fastForwardScale, 1f);

        secondFastForwardScale =
            Mathf.Max(
                secondFastForwardScale,
                fastForwardScale
            );
    }

    private void ValidateReferences()
    {
        if (ballLauncher == null)
        {
            Debug.LogError(
                "TurnFastForwardController: BallLauncher를 찾지 못했습니다.",
                this
            );
        }
    }

    private void BeginAttack()
    {
        isAttackActive = true;
        hasLaunchCompleted = false;
        isFastForwarding = false;
        hasSecondFastForwardStarted = false;
        hasRecallBeenRequested = false;

        normalTimeScale = Time.timeScale;

        if (normalTimeScale <= 0f)
        {
            normalTimeScale = 1f;
        }
    }

    private void StartFastForward(
        float scale,
        float delay)
    {
        isFastForwarding = true;

        Time.timeScale =
            normalTimeScale *
            scale;

        if (showDebugLog)
        {
            Debug.Log(
                "TurnFastForwardController: 마지막 공 발사 후 " +
                $"{delay:0.##}초 경과, " +
                $"{scale:0.##}배속 시작",
                this
            );
        }
    }

    private void EndAttack()
    {
        RestoreTimeScale();

        isAttackActive = false;
        hasLaunchCompleted = false;
        isFastForwarding = false;
        hasSecondFastForwardStarted = false;
        hasRecallBeenRequested = false;
    }

    private void RestoreTimeScale()
    {
        if (!isFastForwarding)
        {
            return;
        }

        Time.timeScale = normalTimeScale;
    }

    private void OnDisable()
    {
        RestoreTimeScale();

        isAttackActive = false;
        hasLaunchCompleted = false;
        isFastForwarding = false;
        hasSecondFastForwardStarted = false;
        hasRecallBeenRequested = false;
    }

    private void OnDestroy()
    {
        RestoreTimeScale();
    }
}
