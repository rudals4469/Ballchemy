using UnityEngine;

[DisallowMultipleComponent]
public sealed class MultiTrajectoryPreview :
    MonoBehaviour
{
    [Header("Trajectory Previews")]

    [Tooltip(
        "증강이 없을 때 사용하는 중앙 조준선입니다.\n" +
        "3갈래 발사에서도 중앙 방향을 담당합니다."
    )]
    [SerializeField]
    private BallTrajectoryPreview centerPreview;

    [Tooltip(
        "다각도 발사의 왼쪽 방향을 담당하는 조준선입니다."
    )]
    [SerializeField]
    private BallTrajectoryPreview leftPreview;

    [Tooltip(
        "다각도 발사의 오른쪽 방향을 담당하는 조준선입니다."
    )]
    [SerializeField]
    private BallTrajectoryPreview rightPreview;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog;

    private Vector2 currentBaseDirection =
        Vector2.up;

    private MultiDirectionLaunchSettings
        currentSettings =
            MultiDirectionLaunchSettings.Default;

    private bool isLongPreviewActive;

    public bool IsLongPreviewActive =>
        isLongPreviewActive;

    public int ActivePreviewCount =>
        ResolveActiveBranchCount(
            currentSettings
        );

    private void Awake()
    {
        ValidateReferences();
        Hide();
    }

    private void OnValidate()
    {
        ValidateDuplicateReferences();
    }

    public void ShowShort(
        Vector2 baseDirection)
    {
        if (!TryNormalizeDirection(
                baseDirection,
                out Vector2 normalizedDirection
            ))
        {
            Hide();

            return;
        }

        currentBaseDirection =
            normalizedDirection;

        currentSettings =
            MultiDirectionLaunchAugmentSystem
                .GetCurrentSettings();

        isLongPreviewActive =
            false;

        StopAllLongPreviews();

        int branchCount =
            ResolveActiveBranchCount(
                currentSettings
            );

        float spreadAngle =
            ResolveSpreadAngle(
                currentSettings
            );

        switch (branchCount)
        {
            case 2:
                ShowTwoBranchShortPreview(
                    normalizedDirection,
                    spreadAngle
                );
                break;

            case 3:
                ShowThreeBranchShortPreview(
                    normalizedDirection,
                    spreadAngle
                );
                break;

            case 1:
            default:
                ShowSingleShortPreview(
                    normalizedDirection
                );
                break;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "MultiTrajectoryPreview: " +
                $"짧은 조준선 표시, " +
                $"갈래={branchCount}, " +
                $"각도=±{spreadAngle:0.#}°",
                this
            );
        }
    }

    public bool BeginTrajectory(
        Vector2 baseDirection)
    {
        if (!TryNormalizeDirection(
                baseDirection,
                out Vector2 normalizedDirection
            ))
        {
            Hide();

            return false;
        }

        currentBaseDirection =
            normalizedDirection;

        currentSettings =
            MultiDirectionLaunchAugmentSystem
                .GetCurrentSettings();

        StopAllLongPreviews();
        HideAllPreviews();

        int branchCount =
            ResolveActiveBranchCount(
                currentSettings
            );

        float spreadAngle =
            ResolveSpreadAngle(
                currentSettings
            );

        bool didBegin;

        switch (branchCount)
        {
            case 2:
                didBegin =
                    BeginTwoBranchTrajectory(
                        normalizedDirection,
                        spreadAngle
                    );
                break;

            case 3:
                didBegin =
                    BeginThreeBranchTrajectory(
                        normalizedDirection,
                        spreadAngle
                    );
                break;

            case 1:
            default:
                didBegin =
                    BeginSingleTrajectory(
                        normalizedDirection
                    );
                break;
        }

        isLongPreviewActive =
            didBegin;

        if (showDebugLog)
        {
            Debug.Log(
                "MultiTrajectoryPreview: " +
                $"긴 예상 경로 시작 결과={didBegin}, " +
                $"갈래={branchCount}, " +
                $"각도=±{spreadAngle:0.#}°",
                this
            );
        }

        return didBegin;
    }

    public void StopLongPreview()
    {
        isLongPreviewActive =
            false;

        StopAllLongPreviews();
    }

    public void Hide()
    {
        isLongPreviewActive =
            false;

        currentBaseDirection =
            Vector2.up;

        currentSettings =
            MultiDirectionLaunchSettings.Default;

        HideAllPreviews();
    }

    private void ShowSingleShortPreview(
        Vector2 baseDirection)
    {
        HidePreview(
            leftPreview
        );

        HidePreview(
            rightPreview
        );

        centerPreview?.ShowShort(
            baseDirection
        );
    }

    private void ShowTwoBranchShortPreview(
        Vector2 baseDirection,
        float spreadAngle)
    {
        HidePreview(
            centerPreview
        );

        Vector2 leftDirection =
            MultiDirectionLaunchAugmentSystem
                .GetBranchDirection(
                    baseDirection,
                    0,
                    2,
                    spreadAngle
                );

        Vector2 rightDirection =
            MultiDirectionLaunchAugmentSystem
                .GetBranchDirection(
                    baseDirection,
                    1,
                    2,
                    spreadAngle
                );

        leftPreview?.ShowShort(
            leftDirection
        );

        rightPreview?.ShowShort(
            rightDirection
        );
    }

    private void ShowThreeBranchShortPreview(
        Vector2 baseDirection,
        float spreadAngle)
    {
        Vector2 leftDirection =
            MultiDirectionLaunchAugmentSystem
                .GetBranchDirection(
                    baseDirection,
                    0,
                    3,
                    spreadAngle
                );

        Vector2 centerDirection =
            MultiDirectionLaunchAugmentSystem
                .GetBranchDirection(
                    baseDirection,
                    1,
                    3,
                    spreadAngle
                );

        Vector2 rightDirection =
            MultiDirectionLaunchAugmentSystem
                .GetBranchDirection(
                    baseDirection,
                    2,
                    3,
                    spreadAngle
                );

        leftPreview?.ShowShort(
            leftDirection
        );

        centerPreview?.ShowShort(
            centerDirection
        );

        rightPreview?.ShowShort(
            rightDirection
        );
    }

    private bool BeginSingleTrajectory(
        Vector2 baseDirection)
    {
        HidePreview(
            leftPreview
        );

        HidePreview(
            rightPreview
        );

        return
            centerPreview != null &&
            centerPreview.BeginTrajectory(
                baseDirection
            );
    }

    private bool BeginTwoBranchTrajectory(
        Vector2 baseDirection,
        float spreadAngle)
    {
        HidePreview(
            centerPreview
        );

        Vector2 leftDirection =
            MultiDirectionLaunchAugmentSystem
                .GetBranchDirection(
                    baseDirection,
                    0,
                    2,
                    spreadAngle
                );

        Vector2 rightDirection =
            MultiDirectionLaunchAugmentSystem
                .GetBranchDirection(
                    baseDirection,
                    1,
                    2,
                    spreadAngle
                );

        bool leftStarted =
            leftPreview != null &&
            leftPreview.BeginTrajectory(
                leftDirection
            );

        bool rightStarted =
            rightPreview != null &&
            rightPreview.BeginTrajectory(
                rightDirection
            );

        return
            leftStarted ||
            rightStarted;
    }

    private bool BeginThreeBranchTrajectory(
        Vector2 baseDirection,
        float spreadAngle)
    {
        Vector2 leftDirection =
            MultiDirectionLaunchAugmentSystem
                .GetBranchDirection(
                    baseDirection,
                    0,
                    3,
                    spreadAngle
                );

        Vector2 centerDirection =
            MultiDirectionLaunchAugmentSystem
                .GetBranchDirection(
                    baseDirection,
                    1,
                    3,
                    spreadAngle
                );

        Vector2 rightDirection =
            MultiDirectionLaunchAugmentSystem
                .GetBranchDirection(
                    baseDirection,
                    2,
                    3,
                    spreadAngle
                );

        bool leftStarted =
            leftPreview != null &&
            leftPreview.BeginTrajectory(
                leftDirection
            );

        bool centerStarted =
            centerPreview != null &&
            centerPreview.BeginTrajectory(
                centerDirection
            );

        bool rightStarted =
            rightPreview != null &&
            rightPreview.BeginTrajectory(
                rightDirection
            );

        return
            leftStarted ||
            centerStarted ||
            rightStarted;
    }

    private void StopAllLongPreviews()
    {
        centerPreview?.StopLongPreview();
        leftPreview?.StopLongPreview();
        rightPreview?.StopLongPreview();
    }

    private void HideAllPreviews()
    {
        HidePreview(
            centerPreview
        );

        HidePreview(
            leftPreview
        );

        HidePreview(
            rightPreview
        );
    }

    private static void HidePreview(
        BallTrajectoryPreview preview)
    {
        preview?.Hide();
    }

    private static int ResolveActiveBranchCount(
        MultiDirectionLaunchSettings settings)
    {
        if (!settings.IsActive)
        {
            return 1;
        }

        return Mathf.Clamp(
            settings.BranchCount,
            1,
            3
        );
    }

    private static float ResolveSpreadAngle(
        MultiDirectionLaunchSettings settings)
    {
        if (!settings.IsActive)
        {
            return 0f;
        }

        return Mathf.Clamp(
            settings.SpreadAngle,
            0f,
            45f
        );
    }

    private static bool TryNormalizeDirection(
        Vector2 direction,
        out Vector2 normalizedDirection)
    {
        if (direction.sqrMagnitude <=
            0.001f)
        {
            normalizedDirection =
                Vector2.up;

            return false;
        }

        normalizedDirection =
            direction.normalized;

        return true;
    }

    private void ValidateReferences()
    {
        ValidateDuplicateReferences();

        if (centerPreview == null)
        {
            Debug.LogError(
                "MultiTrajectoryPreview: " +
                "Center Preview가 연결되지 않았습니다.",
                this
            );
        }

        if (leftPreview == null)
        {
            Debug.LogWarning(
                "MultiTrajectoryPreview: " +
                "Left Preview가 연결되지 않았습니다. " +
                "다각도 발사의 왼쪽 조준선이 표시되지 않습니다.",
                this
            );
        }

        if (rightPreview == null)
        {
            Debug.LogWarning(
                "MultiTrajectoryPreview: " +
                "Right Preview가 연결되지 않았습니다. " +
                "다각도 발사의 오른쪽 조준선이 표시되지 않습니다.",
                this
            );
        }
    }

    private void ValidateDuplicateReferences()
    {
        if (centerPreview != null &&
            centerPreview == leftPreview)
        {
            Debug.LogError(
                "MultiTrajectoryPreview: " +
                "Center Preview와 Left Preview에 " +
                "같은 컴포넌트가 연결되어 있습니다.",
                this
            );
        }

        if (centerPreview != null &&
            centerPreview == rightPreview)
        {
            Debug.LogError(
                "MultiTrajectoryPreview: " +
                "Center Preview와 Right Preview에 " +
                "같은 컴포넌트가 연결되어 있습니다.",
                this
            );
        }

        if (leftPreview != null &&
            leftPreview == rightPreview)
        {
            Debug.LogError(
                "MultiTrajectoryPreview: " +
                "Left Preview와 Right Preview에 " +
                "같은 컴포넌트가 연결되어 있습니다.",
                this
            );
        }
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        Hide();
    }
}