using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RoomTransitionController :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private StageRoomNavigator navigator;

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private RoomFadePresenter fadePresenter;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog;

    private Coroutine transitionCoroutine;

    private bool isTransitioning;

    public bool IsTransitioning =>
        isTransitioning;

    public event Action<bool>
        TransitionStateChanged;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void OnValidate()
    {
        FindReferences();
    }

    private void OnDisable()
    {
        StopTransitionAndRestoreState();
    }

    private void OnDestroy()
    {
        StopTransitionAndRestoreState();
    }

    private void FindReferences()
    {
        if (navigator == null)
        {
            navigator =
                FindFirstObjectByType<
                    StageRoomNavigator
                >();
        }

        if (turnManager == null)
        {
            turnManager =
                FindFirstObjectByType<
                    TurnManager
                >();
        }

        if (fadePresenter == null)
        {
            fadePresenter =
                FindFirstObjectByType<
                    RoomFadePresenter
                >(
                    FindObjectsInactive.Include
                );
        }
    }

    private void ValidateReferences()
    {
        if (navigator == null)
        {
            Debug.LogError(
                "RoomTransitionController: " +
                "StageRoomNavigator가 연결되지 않았습니다.",
                this
            );
        }

        if (turnManager == null)
        {
            Debug.LogError(
                "RoomTransitionController: " +
                "TurnManager가 연결되지 않았습니다.",
                this
            );
        }

        if (fadePresenter == null)
        {
            Debug.LogError(
                "RoomTransitionController: " +
                "RoomFadePresenter가 연결되지 않았습니다.",
                this
            );
        }
    }

    public bool CanRequestDirectionalMove(
        RoomDirection direction)
    {
        if (isTransitioning ||
            navigator == null)
        {
            return false;
        }

        return navigator.CanMove(
            direction
        );
    }

    public bool TryMoveFromDirectionButton(
        RoomDirection direction)
    {
        if (!CanRequestDirectionalMove(
                direction
            ))
        {
            return false;
        }

        StartTransition(
            ExecuteDirectionalMove,
            direction
        );

        return true;
    }

    public bool CanRequestMapMove(
        RoomNode targetRoom)
    {
        if (isTransitioning ||
            navigator == null ||
            targetRoom == null)
        {
            return false;
        }

        return navigator
            .CanFastTravelToRoom(
                targetRoom.RoomId
            );
    }

    public bool TryMoveFromMap(
        RoomNode targetRoom)
    {
        if (!CanRequestMapMove(
                targetRoom
            ))
        {
            return false;
        }

        StopTransitionCoroutine();

        transitionCoroutine =
            StartCoroutine(
                FastTravelRoutine(
                    targetRoom
                )
            );

        return true;
    }

    public bool TryRetreat(
        RoomRetreatController
            retreatController)
    {
        if (isTransitioning ||
            retreatController == null ||
            !retreatController.CanRetreat)
        {
            return false;
        }

        StopTransitionCoroutine();

        transitionCoroutine =
            StartCoroutine(
                RetreatRoutine(
                    retreatController
                )
            );

        return true;
    }

    private void StartTransition(
        Func<RoomDirection, bool>
            movementAction,
        RoomDirection direction)
    {
        StopTransitionCoroutine();

        transitionCoroutine =
            StartCoroutine(
                DirectionalMoveRoutine(
                    movementAction,
                    direction
                )
            );
    }

    private IEnumerator DirectionalMoveRoutine(
        Func<RoomDirection, bool>
            movementAction,
        RoomDirection direction)
    {
        BeginTransition();

        yield return FadeOut();

        bool moved = false;

        UnlockNavigatorForMove();

        if (movementAction != null)
        {
            moved =
                movementAction(
                    direction
                );
        }

        RelockTransitionInput();

        if (!moved)
        {
            Debug.LogWarning(
                "RoomTransitionController: " +
                $"{direction} 방향 이동에 실패했습니다.",
                this
            );
        }
        else if (showDebugLog)
        {
            Debug.Log(
                "RoomTransitionController: " +
                $"방향 이동 완료, " +
                $"현재 방={navigator.CurrentRoomId}",
                this
            );
        }

        yield return FadeIn();

        FinishTransition();
    }

    private IEnumerator FastTravelRoutine(
        RoomNode targetRoom)
    {
        BeginTransition();

        yield return FadeOut();

        bool moved = false;

        UnlockNavigatorForMove();

        if (navigator != null &&
            targetRoom != null)
        {
            moved =
                navigator
                    .TryFastTravelToRoom(
                        targetRoom.RoomId
                    );
        }

        RelockTransitionInput();

        if (!moved)
        {
            Debug.LogWarning(
                "RoomTransitionController: " +
                $"Room {targetRoom?.RoomId}으로 " +
                "빠른 이동하지 못했습니다.",
                this
            );
        }
        else if (showDebugLog)
        {
            Debug.Log(
                "RoomTransitionController: " +
                "맵 빠른 이동 완료, " +
                $"현재 방={navigator.CurrentRoomId}",
                this
            );
        }

        yield return FadeIn();

        FinishTransition();
    }

    private IEnumerator RetreatRoutine(
        RoomRetreatController
            retreatController)
    {
        BeginTransition();

        yield return FadeOut();

        bool retreated = false;

        UnlockNavigatorForMove();

        if (retreatController != null)
        {
            retreated =
                retreatController
                    .ExecuteRetreatDuringFade();
        }

        RelockTransitionInput();

        if (!retreated)
        {
            Debug.LogWarning(
                "RoomTransitionController: " +
                "후퇴 처리에 실패했습니다.",
                this
            );
        }
        else if (showDebugLog)
        {
            Debug.Log(
                "RoomTransitionController: " +
                "암전 후퇴 완료",
                this
            );
        }

        yield return FadeIn();

        FinishTransition();
    }

    private bool ExecuteDirectionalMove(
        RoomDirection direction)
    {
        if (navigator == null)
        {
            return false;
        }

        return navigator.TryMove(
            direction
        );
    }

    private void BeginTransition()
    {
        SetTransitioning(
            true
        );

        navigator?.SetNavigationLocked(
            true
        );

        turnManager?.SetInputLocked(
            true
        );
    }

    private void UnlockNavigatorForMove()
    {
        navigator?.SetNavigationLocked(
            false
        );
    }

    private void RelockTransitionInput()
    {
        navigator?.SetNavigationLocked(
            true
        );

        /*
         * 방 진입 처리에서 TurnManager 입력 잠금이
         * 풀릴 수 있으므로 암전이 끝날 때까지
         * 다시 잠급니다.
         */
        turnManager?.SetInputLocked(
            true
        );
    }

    private IEnumerator FadeOut()
    {
        if (fadePresenter == null)
        {
            yield break;
        }

        yield return
            fadePresenter
                .FadeOutRoutine();
    }

    private IEnumerator FadeIn()
    {
        if (fadePresenter == null)
        {
            yield break;
        }

        yield return
            fadePresenter
                .FadeInRoutine();
    }

    private void FinishTransition()
    {
        turnManager?.SetInputLocked(
            false
        );

        navigator?.SetNavigationLocked(
            false
        );

        transitionCoroutine =
            null;

        SetTransitioning(
            false
        );
    }

    private void SetTransitioning(
        bool shouldTransition)
    {
        if (isTransitioning ==
            shouldTransition)
        {
            return;
        }

        isTransitioning =
            shouldTransition;

        TransitionStateChanged?.Invoke(
            isTransitioning
        );
    }

    private void StopTransitionCoroutine()
    {
        if (transitionCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            transitionCoroutine
        );

        transitionCoroutine =
            null;
    }

    private void StopTransitionAndRestoreState()
    {
        bool wasTransitioning =
            isTransitioning;

        StopTransitionCoroutine();

        fadePresenter?.SetClearImmediate();

        if (!wasTransitioning)
        {
            return;
        }

        turnManager?.SetInputLocked(
            false
        );

        navigator?.SetNavigationLocked(
            false
        );

        SetTransitioning(
            false
        );
    }
}