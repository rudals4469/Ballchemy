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

    [Header("Map Movement Rules")]

    [Tooltip(
        "활성화하면 맵 클릭으로 전투방에 이동할 때 " +
        "클리어한 전투방만 허용합니다."
    )]
    [SerializeField]
    private bool requireClearedCombatRoom = true;

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

        StartFadeTransition(
            direction,
            null,
            false
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

        if (!navigator.CanNavigate)
        {
            return false;
        }

        RoomNode currentRoom =
            navigator.CurrentRoom;

        if (currentRoom == null ||
            currentRoom.RoomId ==
            targetRoom.RoomId)
        {
            return false;
        }

        /*
         * 현재 단계에서는 맵 클릭도
         * 직접 연결된 인접 방만 허용합니다.
         */
        if (!currentRoom.HasConnection(
                targetRoom.RoomId
            ))
        {
            return false;
        }

        /*
         * 아직 방문하지 않은 방은
         * 방향 버튼을 통해 처음 진입합니다.
         */
        if (!navigator.IsRoomVisited(
                targetRoom.RoomId
            ))
        {
            return false;
        }

        if (requireClearedCombatRoom &&
            targetRoom.IsCombatRoom &&
            !navigator.IsRoomCleared(
                targetRoom.RoomId
            ))
        {
            return false;
        }

        return TryResolveDirection(
            currentRoom,
            targetRoom,
            out _
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

        if (!TryResolveDirection(
                navigator.CurrentRoom,
                targetRoom,
                out RoomDirection direction
            ))
        {
            return false;
        }

        StartFadeTransition(
            direction,
            targetRoom,
            true
        );

        return true;
    }

    private void StartFadeTransition(
        RoomDirection direction,
        RoomNode requestedRoom,
        bool requestedFromMap)
    {
        StopTransitionCoroutine();

        transitionCoroutine =
            StartCoroutine(
                FadeTransitionRoutine(
                    direction,
                    requestedRoom,
                    requestedFromMap
                )
            );
    }

    private IEnumerator FadeTransitionRoutine(
        RoomDirection direction,
        RoomNode requestedRoom,
        bool requestedFromMap)
    {
        SetTransitioning(
            true
        );

        LockTransitionInput();

        /*
         * 화면이 완전히 어두워진 뒤에만
         * 실제 방을 변경합니다.
         *
         * 따라서 새 블록과 공이 생성되는 과정은
         * 플레이어에게 보이지 않습니다.
         */
        if (fadePresenter != null)
        {
            yield return
                fadePresenter
                    .FadeOutRoutine();
        }

        bool moved =
            ExecuteNavigatorMove(
                direction
            );

        if (!moved)
        {
            if (requestedFromMap &&
                requestedRoom != null)
            {
                Debug.LogWarning(
                    "RoomTransitionController: " +
                    $"Room {requestedRoom.RoomId}으로 " +
                    "맵 클릭 이동하지 못했습니다.",
                    this
                );
            }
            else
            {
                Debug.LogWarning(
                    "RoomTransitionController: " +
                    $"{direction} 방향으로 " +
                    "이동하지 못했습니다.",
                    this
                );
            }
        }
        else if (showDebugLog)
        {
            string movementSource =
                requestedFromMap
                    ? "맵 클릭"
                    : "방향 버튼";

            Debug.Log(
                "RoomTransitionController: " +
                $"{movementSource} 암전 이동 완료, " +
                $"방향={direction}, " +
                $"현재 방={navigator.CurrentRoomId}",
                this
            );
        }

        /*
         * 방 변경 및 새 방 생성 처리가 끝난 뒤
         * 화면을 다시 밝힙니다.
         */
        if (fadePresenter != null)
        {
            yield return
                fadePresenter
                    .FadeInRoutine();
        }

        FinishTransition();
    }

    private void LockTransitionInput()
    {
        navigator?.SetNavigationLocked(
            true
        );

        turnManager?.SetInputLocked(
            true
        );
    }

    private bool ExecuteNavigatorMove(
        RoomDirection direction)
    {
        if (navigator == null)
        {
            return false;
        }

        /*
         * 실제 이동을 실행하는 순간에만
         * Navigator 잠금을 잠시 해제합니다.
         */
        navigator.SetNavigationLocked(
            false
        );

        bool moved =
            navigator.TryMove(
                direction
            );

        navigator.SetNavigationLocked(
            true
        );

        /*
         * 방 입장 처리에서 입력 잠금이 풀릴 수 있으므로
         * 암전이 끝날 때까지 다시 잠급니다.
         */
        turnManager?.SetInputLocked(
            true
        );

        return moved;
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

    private static bool TryResolveDirection(
        RoomNode currentRoom,
        RoomNode targetRoom,
        out RoomDirection direction)
    {
        direction =
            RoomDirection.Up;

        if (currentRoom == null ||
            targetRoom == null)
        {
            return false;
        }

        Vector2Int difference =
            targetRoom.GridPosition -
            currentRoom.GridPosition;

        if (difference ==
            Vector2Int.up)
        {
            direction =
                RoomDirection.Up;

            return true;
        }

        if (difference ==
            Vector2Int.right)
        {
            direction =
                RoomDirection.Right;

            return true;
        }

        if (difference ==
            Vector2Int.down)
        {
            direction =
                RoomDirection.Down;

            return true;
        }

        if (difference ==
            Vector2Int.left)
        {
            direction =
                RoomDirection.Left;

            return true;
        }

        return false;
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