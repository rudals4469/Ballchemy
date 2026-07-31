using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BallCountView :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BallCollection ballCollection;

    [SerializeField]
    private BallLauncher ballLauncher;

    [SerializeField]
    private BallSealController
        ballSealController;

    [SerializeField]
    private TMP_Text countLabel;

    [Header("Text")]
    [SerializeField]
    private string normalTextFormat = "x{0}";

    [SerializeField]
    private string sealedTextFormat =
        "x{0}/{1}";

    [Header("Visibility")]
    [SerializeField]
    private bool hideWhenZero = true;

    [Tooltip(
        "BallCollection의 공 표시 상태가 꺼져 있으면 " +
        "공 개수 텍스트도 함께 숨깁니다."
    )]
    [SerializeField]
    private bool followBallVisibility = true;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog;

    private bool isShowingLaunchQueue;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
        ValidateReferences();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();
        RefreshCurrentDisplay();
    }

    private void Start()
    {
        RefreshCurrentDisplay();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        FindReferences();
        NormalizeSettings();

        if (!Application.isPlaying)
        {
            RefreshCurrentDisplay();
        }
    }

    private void FindReferences()
    {
        if (countLabel == null)
        {
            countLabel =
                GetComponent<TMP_Text>();
        }

        if (ballCollection == null)
        {
            ballCollection =
                GetComponentInParent<
                    BallCollection
                >();
        }

        if (ballLauncher == null)
        {
            ballLauncher =
                GetComponentInParent<
                    BallLauncher
                >();
        }

        if (ballSealController == null)
        {
            ballSealController =
                GetComponentInParent<
                    BallSealController
                >();
        }

        if (Application.isPlaying)
        {
            if (ballCollection == null)
            {
                ballCollection =
                    FindFirstObjectByType<
                        BallCollection
                    >();
            }

            if (ballLauncher == null)
            {
                ballLauncher =
                    FindFirstObjectByType<
                        BallLauncher
                    >();
            }

            if (ballSealController == null)
            {
                ballSealController =
                    FindFirstObjectByType<
                        BallSealController
                    >();
            }
        }
    }

    private void NormalizeSettings()
    {
        if (string.IsNullOrWhiteSpace(
                normalTextFormat))
        {
            normalTextFormat =
                "x{0}";
        }

        if (string.IsNullOrWhiteSpace(
                sealedTextFormat))
        {
            sealedTextFormat =
                "x{0}/{1}";
        }
    }

    private void ValidateReferences()
    {
        if (countLabel == null)
        {
            Debug.LogError(
                "BallCountView: " +
                "TMP_Text를 찾지 못했습니다.",
                this
            );
        }

        if (ballCollection == null)
        {
            Debug.LogError(
                "BallCountView: " +
                "BallCollection을 찾지 못했습니다.",
                this
            );
        }

        if (ballLauncher == null)
        {
            Debug.LogError(
                "BallCountView: " +
                "BallLauncher를 찾지 못했습니다.",
                this
            );
        }

        if (ballSealController == null)
        {
            Debug.LogWarning(
                "BallCountView: " +
                "BallSealController를 찾지 못했습니다. " +
                "봉인 UI는 표시되지 않습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (ballCollection != null)
        {
            ballCollection.BallCountChanged -=
                HandleBallCountChanged;

            ballCollection.BallCountChanged +=
                HandleBallCountChanged;

            ballCollection.BallsVisibilityChanged -=
                HandleBallsVisibilityChanged;

            ballCollection.BallsVisibilityChanged +=
                HandleBallsVisibilityChanged;
        }

        if (ballLauncher != null)
        {
            ballLauncher
                .RemainingBallsToLaunchChanged -=
                HandleRemainingBallsChanged;

            ballLauncher
                .RemainingBallsToLaunchChanged +=
                HandleRemainingBallsChanged;

            ballLauncher.LaunchCycleCompleted -=
                HandleLaunchCycleCompleted;

            ballLauncher.LaunchCycleCompleted +=
                HandleLaunchCycleCompleted;
        }

        if (ballSealController != null)
        {
            ballSealController.SealApplied -=
                HandleSealApplied;

            ballSealController.SealApplied +=
                HandleSealApplied;

            ballSealController.SealConsumed -=
                HandleSealConsumed;

            ballSealController.SealConsumed +=
                HandleSealConsumed;
        }
    }

    private void UnsubscribeEvents()
    {
        if (ballCollection != null)
        {
            ballCollection.BallCountChanged -=
                HandleBallCountChanged;

            ballCollection.BallsVisibilityChanged -=
                HandleBallsVisibilityChanged;
        }

        if (ballLauncher != null)
        {
            ballLauncher
                .RemainingBallsToLaunchChanged -=
                HandleRemainingBallsChanged;

            ballLauncher.LaunchCycleCompleted -=
                HandleLaunchCycleCompleted;
        }

        if (ballSealController != null)
        {
            ballSealController.SealApplied -=
                HandleSealApplied;

            ballSealController.SealConsumed -=
                HandleSealConsumed;
        }
    }

    private void HandleBallCountChanged(
        int totalBallCount)
    {
        if (isShowingLaunchQueue)
        {
            return;
        }

        RefreshIdleDisplay();
    }

    private void HandleBallsVisibilityChanged(
        bool areBallsVisible)
    {
        RefreshCurrentDisplay();

        if (showDebugLog)
        {
            Debug.Log(
                "BallCountView: 공 표시 상태 변경, " +
                (
                    areBallsVisible
                        ? "개수 텍스트 표시 가능"
                        : "개수 텍스트 숨김"
                ),
                this
            );
        }
    }

    private void HandleRemainingBallsChanged(
        int remainingBallCount)
    {
        isShowingLaunchQueue = true;

        SetNormalCount(
            remainingBallCount
        );
    }

    private void HandleLaunchCycleCompleted()
    {
        isShowingLaunchQueue = false;

        RefreshIdleDisplay();
    }

    private void HandleSealApplied(
        int sealedBallCount,
        int launchableBallCount)
    {
        if (isShowingLaunchQueue)
        {
            return;
        }

        int totalBallCount =
            ballCollection != null
                ? ballCollection.Count
                : launchableBallCount +
                  sealedBallCount;

        SetSealedCount(
            launchableBallCount,
            totalBallCount
        );

        if (showDebugLog)
        {
            Debug.Log(
                "BallCountView: " +
                $"봉인 UI 표시 " +
                $"{launchableBallCount}/" +
                $"{totalBallCount}",
                this
            );
        }
    }

    private void HandleSealConsumed(
        int consumedSealedBallCount)
    {
        /*
         * TryLaunch 내부에서 이 이벤트 직후
         * RemainingBallsToLaunchChanged가 호출됩니다.
         *
         * 우선 현재 상태를 갱신하고,
         * 같은 프레임에 발사 대기 숫자로 덮어씁니다.
         */
        if (!isShowingLaunchQueue)
        {
            RefreshIdleDisplay();
        }
    }

    private void RefreshCurrentDisplay()
    {
        if (isShowingLaunchQueue)
        {
            int remainingBallCount =
                ballLauncher != null
                    ? ballLauncher
                        .RemainingBallsToLaunch
                    : 0;

            SetNormalCount(
                remainingBallCount
            );

            return;
        }

        RefreshIdleDisplay();
    }

    private void RefreshIdleDisplay()
    {
        if (countLabel == null ||
            ballCollection == null)
        {
            return;
        }

        if (!ShouldShowCount())
        {
            countLabel.enabled =
                false;

            return;
        }

        int totalBallCount =
            ballCollection.Count;

        if (ballSealController != null &&
            ballSealController.HasPendingSeal)
        {
            int launchableBallCount =
                ballSealController
                    .GetLaunchableBallCount(
                        totalBallCount
                    );

            SetSealedCount(
                launchableBallCount,
                totalBallCount
            );

            return;
        }

        SetNormalCount(
            totalBallCount
        );
    }

    private void SetNormalCount(
        int count)
    {
        count =
            Mathf.Max(
                count,
                0
            );

        if (countLabel == null)
        {
            return;
        }

        if (!ShouldShowCount())
        {
            countLabel.enabled =
                false;

            return;
        }

        if (hideWhenZero &&
            count <= 0)
        {
            countLabel.enabled =
                false;

            return;
        }

        countLabel.enabled =
            true;

        countLabel.text =
            FormatNormalCount(
                count
            );
    }

    private void SetSealedCount(
        int launchableBallCount,
        int totalBallCount)
    {
        launchableBallCount =
            Mathf.Max(
                launchableBallCount,
                0
            );

        totalBallCount =
            Mathf.Max(
                totalBallCount,
                launchableBallCount
            );

        if (countLabel == null)
        {
            return;
        }

        if (!ShouldShowCount())
        {
            countLabel.enabled =
                false;

            return;
        }

        if (hideWhenZero &&
            launchableBallCount <= 0)
        {
            countLabel.enabled =
                false;

            return;
        }

        countLabel.enabled =
            true;

        countLabel.text =
            FormatSealedCount(
                launchableBallCount,
                totalBallCount
            );
    }

    private bool ShouldShowCount()
    {
        if (!followBallVisibility)
        {
            return true;
        }

        if (ballCollection == null)
        {
            return false;
        }

        return ballCollection.AreBallsVisible;
    }

    private string FormatNormalCount(
        int count)
    {
        try
        {
            return string.Format(
                normalTextFormat,
                count
            );
        }
        catch
        {
            return $"x{count}";
        }
    }

    private string FormatSealedCount(
        int launchableBallCount,
        int totalBallCount)
    {
        try
        {
            return string.Format(
                sealedTextFormat,
                launchableBallCount,
                totalBallCount
            );
        }
        catch
        {
            return
                $"x{launchableBallCount}/" +
                $"{totalBallCount}";
        }
    }
}