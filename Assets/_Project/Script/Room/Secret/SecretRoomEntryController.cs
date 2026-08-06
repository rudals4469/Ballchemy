using UnityEngine;

[DisallowMultipleComponent]
public sealed class SecretRoomEntryController :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private StageRoomNavigator navigator;

    [SerializeField]
    private RoomTransitionController
        transitionController;

    [SerializeField]
    private SecretRoomState secretRoomState;

    [SerializeField]
    private SecretRoomKeyState
        secretRoomKeyState;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog = true;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
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

        if (transitionController == null)
        {
            transitionController =
                FindFirstObjectByType<
                    RoomTransitionController
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
    }

    private void ValidateReferences()
    {
        if (navigator == null)
        {
            Debug.LogError(
                "SecretRoomEntryController: " +
                "StageRoomNavigator가 연결되지 않았습니다.",
                this
            );
        }

        if (transitionController == null)
        {
            Debug.LogError(
                "SecretRoomEntryController: " +
                "RoomTransitionController가 연결되지 않았습니다.",
                this
            );
        }

        if (secretRoomState == null)
        {
            Debug.LogError(
                "SecretRoomEntryController: " +
                "SecretRoomState가 연결되지 않았습니다.",
                this
            );
        }

        if (secretRoomKeyState == null)
        {
            Debug.LogError(
                "SecretRoomEntryController: " +
                "SecretRoomKeyState가 연결되지 않았습니다.",
                this
            );
        }
    }

    public bool HasLockedSecretRoomEntrance(
        RoomDirection direction)
    {
        return TryGetLockedSecretRoom(
            direction,
            out _
        );
    }

    public bool CanUnlockAndMove(
        RoomDirection direction)
    {
        if (navigator == null ||
            transitionController == null ||
            secretRoomState == null ||
            secretRoomKeyState == null)
        {
            return false;
        }

        if (transitionController.IsTransitioning)
        {
            return false;
        }

        if (!navigator.CanNavigate)
        {
            return false;
        }

        if (!secretRoomKeyState.HasKey)
        {
            return false;
        }

        return TryGetLockedSecretRoom(
            direction,
            out _
        );
    }

    public bool TryUnlockAndMove(
        RoomDirection direction)
    {
        if (!CanUnlockAndMove(
                direction
            ))
        {
            return false;
        }

        if (!TryGetLockedSecretRoom(
                direction,
                out RoomNode secretRoom
            ))
        {
            return false;
        }

        RoomNode currentRoom =
            navigator.CurrentRoom;

        if (currentRoom == null)
        {
            return false;
        }

        bool keyConsumed =
            secretRoomKeyState.TryConsumeKey();

        if (!keyConsumed)
        {
            Debug.LogWarning(
                "SecretRoomEntryController: " +
                "비밀문 공명석 소비에 실패했습니다.",
                this
            );

            return false;
        }

        bool unlocked =
            secretRoomState.UnlockSecretRoom();

        if (!unlocked)
        {
            bool keyRestored =
                secretRoomKeyState.AcquireKey();

            if (!keyRestored)
            {
                Debug.LogError(
                    "SecretRoomEntryController: " +
                    "비밀방 개방 실패 후 공명석 복구에도 " +
                    "실패했습니다.",
                    this
                );
            }

            Debug.LogWarning(
                "SecretRoomEntryController: " +
                "비밀방 개방에 실패했습니다.",
                this
            );

            return false;
        }

        bool transitionRequested =
            transitionController
                .TryMoveFromDirectionButton(
                    direction
                );

        if (!transitionRequested)
        {
            /*
             * 이 시점에는 비밀방이 이미 개방됐기 때문에
             * 다시 잠그거나 공명석을 복구하지 않습니다.
             *
             * 사전 조건을 모두 통과했으므로 정상 상황에서는
             * 여기까지 실패하지 않아야 합니다.
             */
            Debug.LogError(
                "SecretRoomEntryController: " +
                "비밀방은 개방됐지만 방 이동 연출 요청에 " +
                "실패했습니다. " +
                $"CurrentRoomId={currentRoom.RoomId}, " +
                $"SecretRoomId={secretRoom.RoomId}, " +
                $"Direction={direction}",
                this
            );

            return false;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "SecretRoomEntryController: " +
                "비밀문 공명석 사용 및 비밀방 개방 완료. " +
                $"EntranceRoomId={currentRoom.RoomId}, " +
                $"SecretRoomId={secretRoom.RoomId}, " +
                $"Direction={direction}",
                this
            );
        }

        return true;
    }

    private bool TryGetLockedSecretRoom(
        RoomDirection direction,
        out RoomNode secretRoom)
    {
        secretRoom =
            null;

        if (navigator == null ||
            secretRoomState == null)
        {
            return false;
        }

        if (secretRoomState.IsSecretRoomUnlocked)
        {
            return false;
        }

        StageMap map =
            navigator.CurrentMap;

        RoomNode currentRoom =
            navigator.CurrentRoom;

        if (map == null ||
            currentRoom == null)
        {
            return false;
        }

        Vector2Int targetPosition =
            currentRoom.GridPosition +
            RoomDirectionUtility.ToOffset(
                direction
            );

        RoomNode targetRoom =
            map.GetRoomAt(
                targetPosition
            );

        if (targetRoom == null ||
            targetRoom.RoomType !=
                RoomType.Secret)
        {
            return false;
        }

        if (!currentRoom.HasConnection(
                targetRoom.RoomId
            ))
        {
            return false;
        }

        if (!secretRoomState.HasEntrance(
                currentRoom.RoomId,
                targetRoom.RoomId
            ))
        {
            return false;
        }

        secretRoom =
            targetRoom;

        return true;
    }
}