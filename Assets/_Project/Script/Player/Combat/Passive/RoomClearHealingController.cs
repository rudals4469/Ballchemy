using UnityEngine;

[DisallowMultipleComponent]
public sealed class RoomClearHealingController :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private StageRoomNavigator
        roomNavigator;

    [SerializeField]
    private PlayerHealth
        playerHealth;

    [SerializeField]
    private RoomClearHealingState
        healingState;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog =
        true;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void OnEnable()
    {
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
            healingState == null ||
            playerHealth == null)
        {
            return;
        }

        if (!healingState.IsActive)
        {
            return;
        }

        /*
         * 생존자의 문장은 일반 전투방과
         * 네임드 전투방만 적용합니다.
         *
         * Boss는 CombatRoomCleared 이벤트에
         * 포함될 수 있지만 여기서 명시적으로 제외합니다.
         */
        bool isEligibleRoom =
            clearedRoom.RoomType ==
                RoomType.NormalCombat ||
            clearedRoom.RoomType ==
                RoomType.NamedCombat;

        if (!isEligibleRoom)
        {
            return;
        }

        if (playerHealth.IsDead)
        {
            return;
        }

        if (playerHealth.CurrentHealth >=
            playerHealth.MaxHealth)
        {
            if (showDebugLog)
            {
                Debug.Log(
                    "RoomClearHealingController: " +
                    "체력이 최대라 생존자의 문장 회복을 " +
                    "적용하지 않습니다. " +
                    $"RoomId={clearedRoom.RoomId}",
                    this
                );
            }

            return;
        }

        int healingAmount =
            healingState
                .HealingAmountPerCombatRoomClear;

        if (healingAmount <= 0)
        {
            return;
        }

        int healthBefore =
            playerHealth.CurrentHealth;

        playerHealth.Heal(
            healingAmount
        );

        int appliedHealing =
            playerHealth.CurrentHealth -
            healthBefore;

        if (showDebugLog &&
            appliedHealing > 0)
        {
            Debug.Log(
                "RoomClearHealingController: " +
                "생존자의 문장 회복 적용. " +
                $"RoomId={clearedRoom.RoomId}, " +
                $"RoomType={clearedRoom.RoomType}, " +
                $"Healing={appliedHealing}, " +
                $"Health={playerHealth.CurrentHealth}/" +
                $"{playerHealth.MaxHealth}",
                this
            );
        }
    }

    private void FindReferences()
    {
        if (roomNavigator == null)
        {
            roomNavigator =
                FindFirstObjectByType<
                    StageRoomNavigator
                >(
                    FindObjectsInactive.Include
                );
        }

        if (playerHealth == null)
        {
            playerHealth =
                FindFirstObjectByType<
                    PlayerHealth
                >(
                    FindObjectsInactive.Include
                );
        }

        if (healingState == null)
        {
            healingState =
                GetComponent<
                    RoomClearHealingState
                >();
        }

        if (healingState == null)
        {
            healingState =
                FindFirstObjectByType<
                    RoomClearHealingState
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
                "RoomClearHealingController: " +
                "StageRoomNavigator가 연결되지 않았습니다.",
                this
            );
        }

        if (playerHealth == null)
        {
            Debug.LogError(
                "RoomClearHealingController: " +
                "PlayerHealth가 연결되지 않았습니다.",
                this
            );
        }

        if (healingState == null)
        {
            Debug.LogError(
                "RoomClearHealingController: " +
                "RoomClearHealingState가 연결되지 않았습니다.",
                this
            );
        }
    }
}