using UnityEngine;

[DisallowMultipleComponent]
public sealed class StageKeyRewardController :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private StageRoomNavigator
        roomNavigator;

    [SerializeField]
    private StageKeyState
        stageKeyState;

    [SerializeField]
    private StageKeyPickupPresenter
        pickupPresenter;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog = true;

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
    }

    private void OnValidate()
    {
        FindReferences();
    }

    private void FindReferences()
    {
        if (roomNavigator == null)
        {
            roomNavigator =
                FindFirstObjectByType<
                    StageRoomNavigator
                >();
        }

        if (stageKeyState == null)
        {
            stageKeyState =
                GetComponent<StageKeyState>();
        }

        if (stageKeyState == null &&
            Application.isPlaying)
        {
            stageKeyState =
                FindFirstObjectByType<
                    StageKeyState
                >();
        }

        if (pickupPresenter == null &&
            Application.isPlaying)
        {
            pickupPresenter =
                FindFirstObjectByType<
                    StageKeyPickupPresenter
                >(
                    FindObjectsInactive.Include
                );
        }
    }

    private void ValidateReferences()
    {
        if (roomNavigator == null)
        {
            Debug.LogError(
                "StageKeyRewardController: " +
                "StageRoomNavigator가 연결되지 않았습니다.",
                this
            );
        }

        if (stageKeyState == null)
        {
            Debug.LogError(
                "StageKeyRewardController: " +
                "StageKeyState가 연결되지 않았습니다.",
                this
            );
        }

        if (pickupPresenter == null)
        {
            Debug.LogWarning(
                "StageKeyRewardController: " +
                "StageKeyPickupPresenter가 연결되지 않았습니다. " +
                "열쇠 상태는 정상 적용되지만 " +
                "획득 애니메이션은 표시되지 않습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (roomNavigator == null)
        {
            return;
        }

        roomNavigator.CombatRoomCleared -=
            HandleCombatRoomCleared;

        roomNavigator.CombatRoomCleared +=
            HandleCombatRoomCleared;
    }

    private void UnsubscribeEvents()
    {
        if (roomNavigator == null)
        {
            return;
        }

        roomNavigator.CombatRoomCleared -=
            HandleCombatRoomCleared;
    }

    private void HandleCombatRoomCleared(
        RoomNode clearedRoom)
    {
        if (clearedRoom == null ||
            stageKeyState == null)
        {
            return;
        }

        bool acquired =
            stageKeyState.TryAcquireKey(
                clearedRoom.RoomId
            );

        if (!acquired)
        {
            return;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "StageKeyRewardController: " +
                $"열쇠 방 클리어 감지, " +
                $"RoomId={clearedRoom.RoomId}",
                this
            );
        }

        pickupPresenter?.PlayPickup();
    }
}