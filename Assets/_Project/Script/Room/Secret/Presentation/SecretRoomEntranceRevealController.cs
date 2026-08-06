using UnityEngine;

[DisallowMultipleComponent]
public sealed class SecretRoomEntranceRevealController :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private StageRoomNavigator navigator;

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private RoomTransitionController
        transitionController;

    [SerializeField]
    private RoomRewardController
        roomRewardController;

    [SerializeField]
    private SecretRoomState secretRoomState;

    [SerializeField]
    private SecretRoomKeyState secretRoomKeyState;

    [SerializeField]
    private SecretRoomEntranceRevealPresenter
        revealPresenter;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog = true;

    private bool isRevealProcessing;

    private RoomNode pendingClearedRoom;

    private SecretRoomEntrance pendingEntrance;

    private RoomNode pendingRevisitRoom;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        CancelProcessing();
        ClearPendingStates();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        FindReferences();
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

        if (transitionController == null)
        {
            transitionController =
                FindFirstObjectByType<
                    RoomTransitionController
                >();
        }

        if (roomRewardController == null)
        {
            roomRewardController =
                FindFirstObjectByType<
                    RoomRewardController
                >();
        }

        if (secretRoomState == null)
        {
            secretRoomState =
                FindFirstObjectByType<
                    SecretRoomState
                >();
        }

        if (secretRoomKeyState == null)
        {
            secretRoomKeyState =
                FindFirstObjectByType<
                    SecretRoomKeyState
                >();
        }

        if (revealPresenter == null)
        {
            revealPresenter =
                FindFirstObjectByType<
                    SecretRoomEntranceRevealPresenter
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
                "SecretRoomEntranceRevealController: " +
                "StageRoomNavigator가 연결되지 않았습니다.",
                this
            );
        }

        if (turnManager == null)
        {
            Debug.LogError(
                "SecretRoomEntranceRevealController: " +
                "TurnManager가 연결되지 않았습니다.",
                this
            );
        }

        if (transitionController == null)
        {
            Debug.LogError(
                "SecretRoomEntranceRevealController: " +
                "RoomTransitionController가 연결되지 않았습니다.",
                this
            );
        }

        if (roomRewardController == null)
        {
            Debug.LogError(
                "SecretRoomEntranceRevealController: " +
                "RoomRewardController가 연결되지 않았습니다.",
                this
            );
        }

        if (secretRoomState == null)
        {
            Debug.LogError(
                "SecretRoomEntranceRevealController: " +
                "SecretRoomState가 연결되지 않았습니다.",
                this
            );
        }

        if (secretRoomKeyState == null)
        {
            Debug.LogError(
                "SecretRoomEntranceRevealController: " +
                "SecretRoomKeyState가 연결되지 않았습니다.",
                this
            );
        }

        if (revealPresenter == null)
        {
            Debug.LogError(
                "SecretRoomEntranceRevealController: " +
                "SecretRoomEntranceRevealPresenter가 " +
                "연결되지 않았습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (navigator != null)
        {
            navigator.CombatRoomCleared -=
                HandleCombatRoomCleared;

            navigator.CombatRoomCleared +=
                HandleCombatRoomCleared;

            navigator.MapInitialized -=
                HandleMapInitialized;

            navigator.MapInitialized +=
                HandleMapInitialized;

            navigator.RoomChanged -=
                HandleRoomChanged;

            navigator.RoomChanged +=
                HandleRoomChanged;
        }

        if (roomRewardController != null)
        {
            roomRewardController
                .RewardCompletionHandoffRequested -=
                HandleRewardCompletionHandoff;

            roomRewardController
                .RewardCompletionHandoffRequested +=
                HandleRewardCompletionHandoff;
        }

        if (transitionController != null)
        {
            transitionController
                .TransitionStateChanged -=
                HandleTransitionStateChanged;

            transitionController
                .TransitionStateChanged +=
                HandleTransitionStateChanged;
        }

        if (secretRoomKeyState != null)
        {
            secretRoomKeyState.KeyStateChanged -=
                HandleSecretRoomKeyStateChanged;

            secretRoomKeyState.KeyStateChanged +=
                HandleSecretRoomKeyStateChanged;
        }
    }

    private void UnsubscribeEvents()
    {
        if (navigator != null)
        {
            navigator.CombatRoomCleared -=
                HandleCombatRoomCleared;

            navigator.MapInitialized -=
                HandleMapInitialized;

            navigator.RoomChanged -=
                HandleRoomChanged;
        }

        if (roomRewardController != null)
        {
            roomRewardController
                .RewardCompletionHandoffRequested -=
                HandleRewardCompletionHandoff;
        }

        if (transitionController != null)
        {
            transitionController
                .TransitionStateChanged -=
                HandleTransitionStateChanged;
        }

        if (secretRoomKeyState != null)
        {
            secretRoomKeyState.KeyStateChanged -=
                HandleSecretRoomKeyStateChanged;
        }
    }

    private void HandleCombatRoomCleared(
        RoomNode clearedRoom)
    {
        ClearPendingRewardReveal();

        if (clearedRoom == null ||
            isRevealProcessing ||
            !CanUseSecretRoomKey())
        {
            return;
        }

        if (!TryFindEntrance(
                clearedRoom.RoomId,
                out SecretRoomEntrance entrance
            ))
        {
            return;
        }

        pendingClearedRoom =
            clearedRoom;

        pendingEntrance =
            entrance;

        if (showDebugLog)
        {
            Debug.Log(
                "SecretRoomEntranceRevealController: " +
                "보상 선택 후 실행할 비밀방 개방을 예약했습니다. " +
                $"RoomId={clearedRoom.RoomId}, " +
                $"Direction=" +
                $"{entrance.DirectionFromConnectedRoom}",
                this
            );
        }
    }

    /*
     * RoomRewardController가 입력 잠금을 풀기 전에 호출합니다.
     *
     * true를 반환하면 현재 컨트롤러가 잠금을 인계받으며,
     * RoomRewardController는 잠금을 해제하지 않습니다.
     */
    private bool HandleRewardCompletionHandoff(
        RoomNode completedRoom)
    {
        if (completedRoom == null ||
            pendingClearedRoom == null ||
            pendingEntrance == null ||
            completedRoom.RoomId !=
                pendingClearedRoom.RoomId ||
            !CanUseSecretRoomKey())
        {
            ClearPendingRewardReveal();

            return false;
        }

        RoomNode revealRoom =
            pendingClearedRoom;

        SecretRoomEntrance entrance =
            pendingEntrance;

        ClearPendingRewardReveal();

        return BeginReveal(
            revealRoom,
            entrance
        );
    }

    private void HandleRoomChanged(
        RoomNode previousRoom,
        RoomNode currentRoom)
    {
        ClearPendingRewardReveal();

        pendingRevisitRoom =
            null;

        if (currentRoom == null ||
            isRevealProcessing)
        {
            return;
        }

        /*
         * 최초 클리어 직후에는 보상 시스템이 처리하므로
         * 여기서는 이미 클리어된 방 재방문만 대상으로 합니다.
         */
        if (!navigator.IsRoomCleared(
                currentRoom.RoomId
            ))
        {
            return;
        }

        if (!CanUseSecretRoomKey())
        {
            return;
        }

        if (!TryFindEntrance(
                currentRoom.RoomId,
                out _
            ))
        {
            return;
        }

        pendingRevisitRoom =
            currentRoom;

        /*
         * RoomChanged는 암전 도중 발생하므로,
         * 실제 연출은 방 전환이 완전히 끝난 뒤 시작합니다.
         */
        if (transitionController == null ||
            !transitionController.IsTransitioning)
        {
            TryBeginPendingRevisitReveal();
        }
    }

    private void HandleTransitionStateChanged(
        bool isTransitioning)
    {
        if (isTransitioning)
        {
            return;
        }

        TryBeginPendingRevisitReveal();
    }

    private void HandleSecretRoomKeyStateChanged(
        bool hasKey)
    {
        if (!hasKey ||
            isRevealProcessing ||
            navigator == null)
        {
            return;
        }

        RoomNode currentRoom =
            navigator.CurrentRoom;

        if (currentRoom == null ||
            !navigator.IsRoomCleared(
                currentRoom.RoomId
            ))
        {
            return;
        }

        if (!TryFindEntrance(
                currentRoom.RoomId,
                out _
            ))
        {
            return;
        }

        pendingRevisitRoom =
            currentRoom;

        if (transitionController == null ||
            !transitionController.IsTransitioning)
        {
            TryBeginPendingRevisitReveal();
        }
    }

    private void TryBeginPendingRevisitReveal()
    {
        if (pendingRevisitRoom == null ||
            isRevealProcessing ||
            !CanUseSecretRoomKey())
        {
            return;
        }

        if (transitionController != null &&
            transitionController.IsTransitioning)
        {
            return;
        }

        if (roomRewardController != null &&
            roomRewardController.IsRewardPending)
        {
            return;
        }

        RoomNode currentRoom =
            navigator != null
                ? navigator.CurrentRoom
                : null;

        if (currentRoom == null ||
            currentRoom.RoomId !=
                pendingRevisitRoom.RoomId ||
            !navigator.IsRoomCleared(
                currentRoom.RoomId
            ))
        {
            pendingRevisitRoom =
                null;

            return;
        }

        if (!TryFindEntrance(
                currentRoom.RoomId,
                out SecretRoomEntrance entrance
            ))
        {
            pendingRevisitRoom =
                null;

            return;
        }

        RoomNode revealRoom =
            pendingRevisitRoom;

        pendingRevisitRoom =
            null;

        BeginReveal(
            revealRoom,
            entrance
        );
    }

    private bool BeginReveal(
        RoomNode revealRoom,
        SecretRoomEntrance entrance)
    {
        if (revealRoom == null ||
            entrance == null ||
            isRevealProcessing ||
            !CanUseSecretRoomKey())
        {
            return false;
        }

        isRevealProcessing =
            true;

        /*
         * 보상 시스템 또는 방 전환 시스템이 유지하던 잠금을
         * 그대로 이어받습니다.
         */
        navigator?.SetNavigationLocked(
            true
        );

        turnManager?.SetInputLocked(
            true
        );

        secretRoomState.DiscoverEntrance(
            entrance.ConnectedRoomId,
            entrance.SecretRoomId
        );

        bool started =
            revealPresenter.PlayReveal(
                entrance.DirectionFromConnectedRoom,
                () =>
                    CompleteReveal(
                        revealRoom,
                        entrance
                    )
            );

        if (!started)
        {
            Debug.LogError(
                "SecretRoomEntranceRevealController: " +
                "공명석 이동 연출을 시작하지 못했습니다.",
                this
            );

            isRevealProcessing =
                false;

            ReleaseRevealLocks();

            return false;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "SecretRoomEntranceRevealController: " +
                "비밀방 입구 개방 연출 시작. " +
                $"RoomId={revealRoom.RoomId}, " +
                $"Direction=" +
                $"{entrance.DirectionFromConnectedRoom}",
                this
            );
        }

        return true;
    }

    private void CompleteReveal(
        RoomNode revealRoom,
        SecretRoomEntrance entrance)
    {
        if (!isRevealProcessing)
        {
            return;
        }

        bool consumed =
            secretRoomKeyState.TryConsumeKey();

        if (!consumed)
        {
            Debug.LogError(
                "SecretRoomEntranceRevealController: " +
                "비밀문 공명석 소비에 실패했습니다.",
                this
            );

            isRevealProcessing =
                false;

            ReleaseRevealLocks();

            return;
        }

        bool unlocked =
            secretRoomState.UnlockSecretRoom();

        if (!unlocked)
        {
            bool restored =
                secretRoomKeyState.AcquireKey();

            if (!restored)
            {
                Debug.LogError(
                    "SecretRoomEntranceRevealController: " +
                    "비밀방 개방 실패 후 공명석 복구에도 " +
                    "실패했습니다.",
                    this
                );
            }

            isRevealProcessing =
                false;

            ReleaseRevealLocks();

            return;
        }

        isRevealProcessing =
            false;

        ReleaseRevealLocks();

        if (showDebugLog)
        {
            Debug.Log(
                "SecretRoomEntranceRevealController: " +
                "비밀방 개방 완료. " +
                $"EntranceRoomId={revealRoom.RoomId}, " +
                $"SecretRoomId={entrance.SecretRoomId}, " +
                $"Direction=" +
                $"{entrance.DirectionFromConnectedRoom}",
                this
            );
        }
    }

    private bool CanUseSecretRoomKey()
    {
        return
            navigator != null &&
            turnManager != null &&
            secretRoomState != null &&
            secretRoomKeyState != null &&
            revealPresenter != null &&
            secretRoomState.HasSecretRoom &&
            !secretRoomState.IsSecretRoomUnlocked &&
            secretRoomKeyState.HasKey;
    }

    private bool TryFindEntrance(
        int connectedRoomId,
        out SecretRoomEntrance entrance)
    {
        entrance =
            null;

        if (secretRoomState == null ||
            secretRoomState.Entrances == null)
        {
            return false;
        }

        for (int i = 0;
             i < secretRoomState.Entrances.Count;
             i++)
        {
            SecretRoomEntrance candidate =
                secretRoomState.Entrances[i];

            if (candidate == null ||
                candidate.ConnectedRoomId !=
                    connectedRoomId ||
                candidate.SecretRoomId !=
                    secretRoomState.SecretRoomId)
            {
                continue;
            }

            entrance =
                candidate;

            return true;
        }

        return false;
    }

    private void HandleMapInitialized(
        StageMap map)
    {
        CancelProcessing();
        ClearPendingStates();
    }

    private void ClearPendingRewardReveal()
    {
        pendingClearedRoom =
            null;

        pendingEntrance =
            null;
    }

    private void ClearPendingStates()
    {
        ClearPendingRewardReveal();

        pendingRevisitRoom =
            null;
    }

    private void CancelProcessing()
    {
        bool wasProcessing =
            isRevealProcessing;

        isRevealProcessing =
            false;

        revealPresenter?.CancelReveal();

        if (wasProcessing)
        {
            ReleaseRevealLocks();
        }
    }

    private void ReleaseRevealLocks()
    {
        turnManager?.SetInputLocked(
            false
        );

        navigator?.SetNavigationLocked(
            false
        );
    }
}