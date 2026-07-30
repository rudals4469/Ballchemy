using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BallAimController :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private BallLauncher ballLauncher;

    [SerializeField]
    private BallTrajectoryPreview trajectoryPreview;

    [Header("Aim Direction")]
    [SerializeField, Range(0.01f, 1f)]
    private float minimumUpwardDirection = 0.15f;

    [Tooltip("조준 방향이 마우스를 따라가는 속도입니다.")]
    [SerializeField, Min(0f)]
    private float aimSmoothSpeed = 14f;

    [Tooltip(
        "새로운 조준 움직임으로 판단할 " +
        "최소 각도 차이입니다."
    )]
    [SerializeField, Range(0.1f, 10f)]
    private float aimMovementThresholdDegrees = 1.5f;

    [Header("Trajectory Timing")]
    [Tooltip(
        "조준 방향을 유지한 뒤 " +
        "긴 예상 경로를 표시하기까지의 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float stableAimDelay = 1.2f;

    private static readonly List<RaycastResult>
        UiRaycastResults =
            new List<RaycastResult>();

    private Camera mainCamera;

    private Vector2 currentAimDirection =
        Vector2.up;

    private Vector2 targetAimDirection =
        Vector2.up;

    private Vector2 stableReferenceDirection =
        Vector2.up;

    private Vector2 previewDirection =
        Vector2.up;

    private float stableAimTimer;

    private bool hasValidAim;
    private bool hasStableReference;
    private bool isTrajectoryPreviewActive;
    private bool wasAimingLastFrame;

    public Vector2 AimDirection =>
        currentAimDirection;

    private void Awake()
    {
        mainCamera = Camera.main;

        if (turnManager == null)
        {
            turnManager =
                FindFirstObjectByType<
                    TurnManager
                >();
        }

        if (ballLauncher == null)
        {
            ballLauncher =
                GetComponent<
                    BallLauncher
                >();
        }

        if (trajectoryPreview == null)
        {
            trajectoryPreview =
                GetComponent<
                    BallTrajectoryPreview
                >();
        }

        ValidateReferences();
    }

    private void Start()
    {
        currentAimDirection =
            Vector2.up;

        targetAimDirection =
            Vector2.up;

        stableReferenceDirection =
            Vector2.up;

        hasValidAim = true;
        hasStableReference = true;
        wasAimingLastFrame = false;

        ResetTrajectoryPreview();

        if (trajectoryPreview != null)
        {
            trajectoryPreview.Hide();
        }
    }

    private void Update()
    {
        if (turnManager == null ||
            ballLauncher == null)
        {
            return;
        }

        if (!turnManager.CanAim)
        {
            HandleAimingDisabled();

            return;
        }

        HandleAimingEnabled();

        Vector3 mouseScreenPosition =
            Input.mousePosition;

        bool isPointerInsideGameView =
            IsValidPointerPosition(
                mouseScreenPosition
            );

        bool isPointerOverUi =
            IsPointerOverUserInterface(
                mouseScreenPosition
            );

        if (isPointerInsideGameView &&
            !isPointerOverUi)
        {
            UpdateAimInput(
                mouseScreenPosition
            );
        }

        UpdateAimVisual();

        if (isPointerInsideGameView &&
            !isPointerOverUi &&
            Input.GetMouseButtonDown(0) &&
            hasValidAim)
        {
            TryLaunch();
        }
    }

    private void ValidateReferences()
    {
        if (mainCamera == null)
        {
            Debug.LogError(
                "BallAimController: Main Camera를 찾지 못했습니다. " +
                "카메라의 Tag가 MainCamera인지 확인하세요.",
                this
            );
        }

        if (turnManager == null)
        {
            Debug.LogError(
                "BallAimController: TurnManager를 찾지 못했습니다.",
                this
            );
        }

        if (ballLauncher == null)
        {
            Debug.LogError(
                "BallAimController: BallLauncher를 찾지 못했습니다.",
                this
            );
        }

        if (trajectoryPreview == null)
        {
            Debug.LogError(
                "BallAimController: " +
                "BallTrajectoryPreview를 찾지 못했습니다.",
                this
            );
        }
    }

    private void HandleAimingEnabled()
    {
        if (wasAimingLastFrame)
        {
            return;
        }

        wasAimingLastFrame = true;

        if (currentAimDirection.sqrMagnitude <=
            0.001f)
        {
            currentAimDirection =
                Vector2.up;
        }

        targetAimDirection =
            currentAimDirection;

        stableReferenceDirection =
            currentAimDirection;

        hasStableReference = true;
        hasValidAim = true;

        ResetTrajectoryPreview();

        if (trajectoryPreview != null)
        {
            trajectoryPreview.ShowShort(
                currentAimDirection
            );
        }
    }

    private void HandleAimingDisabled()
    {
        if (!wasAimingLastFrame)
        {
            return;
        }

        wasAimingLastFrame = false;

        ResetTrajectoryPreview();

        if (trajectoryPreview != null)
        {
            trajectoryPreview.Hide();
        }
    }

    private void UpdateAimInput(
        Vector3 mouseScreenPosition)
    {
        if (mainCamera == null)
        {
            return;
        }

        float distanceFromCamera =
            transform.position.z -
            mainCamera.transform.position.z;

        Vector3 screenPosition =
            new Vector3(
                mouseScreenPosition.x,
                mouseScreenPosition.y,
                distanceFromCamera
            );

        Vector3 mouseWorldPosition =
            mainCamera.ScreenToWorldPoint(
                screenPosition
            );

        mouseWorldPosition.z =
            transform.position.z;

        Vector2 rawDirection =
            (Vector2)mouseWorldPosition -
            (Vector2)transform.position;

        if (rawDirection.sqrMagnitude <=
            0.001f)
        {
            return;
        }

        rawDirection.y =
            Mathf.Max(
                rawDirection.y,
                minimumUpwardDirection
            );

        Vector2 newTargetDirection =
            rawDirection.normalized;

        bool hasMeaningfulChange =
            HasMeaningfulAimChange(
                newTargetDirection
            );

        targetAimDirection =
            newTargetDirection;

        hasValidAim = true;

        if (!hasMeaningfulChange)
        {
            return;
        }

        stableReferenceDirection =
            newTargetDirection;

        hasStableReference = true;

        ResetTrajectoryPreview();
    }

    private bool HasMeaningfulAimChange(
        Vector2 newDirection)
    {
        if (!hasStableReference)
        {
            stableReferenceDirection =
                newDirection;

            hasStableReference = true;

            return true;
        }

        float angleDifference =
            Vector2.Angle(
                stableReferenceDirection,
                newDirection
            );

        return angleDifference >=
               aimMovementThresholdDegrees;
    }

    private void UpdateAimVisual()
    {
        if (trajectoryPreview == null)
        {
            return;
        }

        if (isTrajectoryPreviewActive)
        {
            currentAimDirection =
                previewDirection;

            return;
        }

        SmoothCurrentAimDirection();

        trajectoryPreview.ShowShort(
            currentAimDirection
        );

        stableAimTimer +=
            Time.deltaTime;

        if (stableAimTimer >=
            stableAimDelay)
        {
            BeginTrajectoryPreview();
        }
    }

    private void SmoothCurrentAimDirection()
    {
        if (targetAimDirection.sqrMagnitude <=
            0.001f)
        {
            return;
        }

        if (aimSmoothSpeed <= 0f)
        {
            currentAimDirection =
                targetAimDirection;

            return;
        }

        float interpolation =
            1f -
            Mathf.Exp(
                -aimSmoothSpeed *
                Time.deltaTime
            );

        Vector2 smoothedDirection =
            Vector2.Lerp(
                currentAimDirection,
                targetAimDirection,
                interpolation
            );

        if (smoothedDirection.sqrMagnitude <=
            0.001f)
        {
            return;
        }

        currentAimDirection =
            smoothedDirection.normalized;
    }

    private void BeginTrajectoryPreview()
    {
        previewDirection =
            currentAimDirection.normalized;

        currentAimDirection =
            previewDirection;

        bool didBegin =
            trajectoryPreview.BeginTrajectory(
                previewDirection
            );

        if (didBegin)
        {
            isTrajectoryPreviewActive = true;
        }
        else
        {
            stableAimTimer = 0f;
        }
    }

    private void ResetTrajectoryPreview()
    {
        stableAimTimer = 0f;
        isTrajectoryPreviewActive = false;

        if (trajectoryPreview != null)
        {
            trajectoryPreview.StopLongPreview();
        }
    }

    private void TryLaunch()
    {
        bool didLaunch =
            ballLauncher.TryLaunch(
                currentAimDirection
            );

        if (!didLaunch)
        {
            return;
        }

        wasAimingLastFrame = false;

        ResetTrajectoryPreview();

        if (trajectoryPreview != null)
        {
            trajectoryPreview.Hide();
        }
    }

    private static bool IsPointerOverUserInterface(
        Vector3 screenPosition)
    {
        EventSystem eventSystem =
            EventSystem.current;

        if (eventSystem == null)
        {
            return false;
        }

        /*
         * 기본 IsPointerOverGameObject 검사도 먼저 사용합니다.
         */
        if (eventSystem.IsPointerOverGameObject())
        {
            return true;
        }

        /*
         * 입력 모듈의 갱신 순서에 따라 위 검사가 false를
         * 반환하는 경우를 대비해 현재 마우스 위치에서
         * UI Graphic Raycast를 직접 실행합니다.
         */
        PointerEventData pointerData =
            new PointerEventData(
                eventSystem
            );

        pointerData.position =
            new Vector2(
                screenPosition.x,
                screenPosition.y
            );

        UiRaycastResults.Clear();

        eventSystem.RaycastAll(
            pointerData,
            UiRaycastResults
        );

        bool hasUiHit =
            UiRaycastResults.Count > 0;

        UiRaycastResults.Clear();

        return hasUiHit;
    }

    private static bool IsValidPointerPosition(
        Vector3 screenPosition)
    {
        if (float.IsNaN(screenPosition.x) ||
            float.IsNaN(screenPosition.y) ||
            float.IsInfinity(screenPosition.x) ||
            float.IsInfinity(screenPosition.y))
        {
            return false;
        }

        if (screenPosition.x < 0f ||
            screenPosition.x > Screen.width ||
            screenPosition.y < 0f ||
            screenPosition.y > Screen.height)
        {
            return false;
        }

        return true;
    }
}