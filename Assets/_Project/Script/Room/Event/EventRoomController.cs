using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EventRoomController :
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
    private BallCollection
        ballCollection;

    [SerializeField]
    private EventRoomState
        eventRoomState;

    [SerializeField]
    private EventSelectionUI
        selectionUI;

    [Header("Unknown Event")]

    [SerializeField]
    private UnknownEventPool
        unknownEventPool;

    [Header("Choice Values")]

    [Tooltip(
        "현재 체력 회복 선택 시 최대 체력에 곱할 비율입니다."
    )]
    [SerializeField, Range(0.01f, 1f)]
    private float healMaxHealthRatio =
        0.4f;

    [Tooltip(
        "최대 체력 증가 선택 시 증가하는 고정 수치입니다."
    )]
    [SerializeField, Min(1)]
    private int maxHealthIncrease =
        5;

    [Header("Icons")]

    [SerializeField]
    private Sprite healIcon;

    [SerializeField]
    private Sprite maxHealthIcon;

    [SerializeField]
    private Sprite unknownIcon;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog = true;

    private readonly List<EventChoiceData>
        currentChoices =
            new List<EventChoiceData>();

    private UnknownEventApplyContext
        unknownEventContext;

    private int activeEventRoomId =
        -1;

    private bool isChoiceOpen;

    private void Awake()
    {
        FindReferences();
        CreateUnknownEventContext();
        ValidateReferences();
    }

    private void OnEnable()
    {
        FindReferences();
        CreateUnknownEventContext();
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();

        isChoiceOpen =
            false;

        activeEventRoomId =
            -1;

        currentChoices.Clear();
    }

    private void OnValidate()
    {
        healMaxHealthRatio =
            Mathf.Clamp01(
                healMaxHealthRatio
            );

        maxHealthIncrease =
            Mathf.Max(
                maxHealthIncrease,
                1
            );
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

        if (playerHealth == null)
        {
            playerHealth =
                FindFirstObjectByType<
                    PlayerHealth
                >();
        }

        if (ballCollection == null)
        {
            ballCollection =
                FindFirstObjectByType<
                    BallCollection
                >();
        }

        if (eventRoomState == null)
        {
            eventRoomState =
                GetComponent<
                    EventRoomState
                >();
        }

        if (eventRoomState == null)
        {
            eventRoomState =
                FindFirstObjectByType<
                    EventRoomState
                >();
        }

        if (selectionUI == null)
        {
            selectionUI =
                FindFirstObjectByType<
                    EventSelectionUI
                >(
                    FindObjectsInactive.Include
                );
        }
    }

    private void CreateUnknownEventContext()
    {
        unknownEventContext =
            new UnknownEventApplyContext(
                ballCollection,
                playerHealth
            );
    }

    private void ValidateReferences()
    {
        if (roomNavigator == null)
        {
            Debug.LogError(
                "EventRoomController: " +
                "StageRoomNavigator가 연결되지 않았습니다.",
                this
            );
        }

        if (playerHealth == null)
        {
            Debug.LogError(
                "EventRoomController: " +
                "PlayerHealth가 연결되지 않았습니다.",
                this
            );
        }

        if (ballCollection == null)
        {
            Debug.LogError(
                "EventRoomController: " +
                "BallCollection이 연결되지 않았습니다.",
                this
            );
        }

        if (eventRoomState == null)
        {
            Debug.LogError(
                "EventRoomController: " +
                "EventRoomState가 연결되지 않았습니다.",
                this
            );
        }

        if (selectionUI == null)
        {
            Debug.LogError(
                "EventRoomController: " +
                "EventSelectionUI가 연결되지 않았습니다.",
                this
            );
        }

        if (unknownEventPool == null)
        {
            Debug.LogError(
                "EventRoomController: " +
                "UnknownEventPool이 연결되지 않았습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (roomNavigator != null)
        {
            roomNavigator.RoomChanged -=
                HandleRoomChanged;

            roomNavigator.RoomChanged +=
                HandleRoomChanged;

            roomNavigator.MapInitialized -=
                HandleMapInitialized;

            roomNavigator.MapInitialized +=
                HandleMapInitialized;
        }

        if (selectionUI != null)
        {
            selectionUI.ChoiceSelected -=
                HandleChoiceSelected;

            selectionUI.ChoiceSelected +=
                HandleChoiceSelected;
        }
    }

    private void UnsubscribeEvents()
    {
        if (roomNavigator != null)
        {
            roomNavigator.RoomChanged -=
                HandleRoomChanged;

            roomNavigator.MapInitialized -=
                HandleMapInitialized;
        }

        if (selectionUI != null)
        {
            selectionUI.ChoiceSelected -=
                HandleChoiceSelected;
        }
    }

    private void HandleMapInitialized(
        StageMap stageMap)
    {
        if (eventRoomState != null)
        {
            eventRoomState.ResetForNewStage();
        }

        CloseSelection(
            false
        );
    }

    private void HandleRoomChanged(
        RoomNode previousRoom,
        RoomNode currentRoom)
    {
        if (currentRoom == null ||
            currentRoom.RoomType !=
                RoomType.Event)
        {
            return;
        }

        if (eventRoomState != null &&
            eventRoomState.IsEventRoomUsed(
                currentRoom.RoomId
            ))
        {
            return;
        }

        OpenSelection(
            currentRoom.RoomId
        );
    }

    private void OpenSelection(
        int roomId)
    {
        if (selectionUI == null ||
            playerHealth == null ||
            roomNavigator == null)
        {
            return;
        }

        activeEventRoomId =
            roomId;

        isChoiceOpen =
            true;

        roomNavigator.SetNavigationLocked(
            true
        );

        BuildChoices();

        selectionUI.ShowChoices(
            currentChoices
        );

        if (showDebugLog)
        {
            Debug.Log(
                "EventRoomController: " +
                $"이벤트 선택 UI 표시, RoomId={roomId}",
                this
            );
        }
    }

    private void BuildChoices()
    {
        currentChoices.Clear();

        int healingAmount =
            Mathf.Max(
                1,
                Mathf.CeilToInt(
                    playerHealth.MaxHealth *
                    healMaxHealthRatio
                )
            );

        bool canHeal =
            !playerHealth.IsDead &&
            playerHealth.CurrentHealth <
            playerHealth.MaxHealth;

        bool canUseUnknownEvent =
            !playerHealth.IsDead &&
            unknownEventPool != null &&
            unknownEventPool.HasApplicableEvent(
                unknownEventContext
            );

        currentChoices.Add(
            new EventChoiceData(
                EventChoiceType.HealCurrentHealth,
                "현재 체력 회복",
                $"체력 +{healingAmount}",
                "최대 체력은 변하지 않습니다.",
                healIcon,
                canHeal
            )
        );

        currentChoices.Add(
            new EventChoiceData(
                EventChoiceType.IncreaseMaxHealth,
                "최대 체력 증가",
                $"최대 체력 +{maxHealthIncrease}",
                "현재 체력은 그대로 유지됩니다.",
                maxHealthIcon,
                !playerHealth.IsDead
            )
        );

        currentChoices.Add(
            new EventChoiceData(
                EventChoiceType.Unknown,
                "???",
                "알 수 없는 효과",
                "무슨 일이 일어날지 알 수 없습니다.",
                unknownIcon,
                canUseUnknownEvent
            )
        );
    }

    private void HandleChoiceSelected(
        EventChoiceData selectedChoice)
    {
        if (!isChoiceOpen ||
            selectedChoice == null ||
            playerHealth == null)
        {
            return;
        }

        bool applied =
            ApplyChoice(
                selectedChoice
            );

        if (!applied)
        {
            Debug.LogWarning(
                "EventRoomController: " +
                $"이벤트 선택 적용 실패, " +
                $"Choice={selectedChoice.ChoiceType}",
                this
            );

            BuildChoices();

            if (selectionUI != null)
            {
                selectionUI.ShowChoices(
                    currentChoices
                );
            }

            return;
        }

        if (eventRoomState != null)
        {
            eventRoomState.TryMarkEventRoomUsed(
                activeEventRoomId
            );
        }

        CloseSelection(
            true
        );
    }

    private bool ApplyChoice(
        EventChoiceData selectedChoice)
    {
        switch (selectedChoice.ChoiceType)
        {
            case EventChoiceType
                .HealCurrentHealth:
            {
                if (playerHealth.CurrentHealth >=
                    playerHealth.MaxHealth)
                {
                    return false;
                }

                int amount =
                    Mathf.Max(
                        1,
                        Mathf.CeilToInt(
                            playerHealth.MaxHealth *
                            healMaxHealthRatio
                        )
                    );

                int previousHealth =
                    playerHealth.CurrentHealth;

                playerHealth.Heal(
                    amount
                );

                return
                    playerHealth.CurrentHealth >
                    previousHealth;
            }

            case EventChoiceType
                .IncreaseMaxHealth:
            {
                return playerHealth
                    .TryIncreaseMaxHealth(
                        maxHealthIncrease
                    );
            }

            case EventChoiceType.Unknown:
            {
                return TryApplyUnknownEvent();
            }

            default:
                return false;
        }
    }

    private bool TryApplyUnknownEvent()
    {
        if (unknownEventPool == null ||
            unknownEventContext == null)
        {
            return false;
        }

        bool drewEvent =
            unknownEventPool.TryDraw(
                unknownEventContext,
                out UnknownEventDefinition
                    selectedEvent
            );

        if (!drewEvent ||
            selectedEvent == null)
        {
            Debug.LogWarning(
                "EventRoomController: " +
                "적용 가능한 비밀 이벤트가 없습니다.",
                this
            );

            return false;
        }

        UnknownEventResult result =
            selectedEvent.Apply(
                unknownEventContext
            );

        if (result == null ||
            !result.WasApplied)
        {
            Debug.LogWarning(
                "EventRoomController: " +
                $"비밀 이벤트 적용 실패, " +
                $"Event={selectedEvent.DisplayName}",
                this
            );

            return false;
        }

        Debug.Log(
            "EventRoomController: ??? 결과 — " +
            $"{selectedEvent.DisplayName}\n" +
            $"{result.ResultText}",
            this
        );

        return true;
    }

    private void CloseSelection(
        bool wasCompleted)
    {
        if (selectionUI != null)
        {
            selectionUI.Hide();
        }

        if (roomNavigator != null)
        {
            roomNavigator.SetNavigationLocked(
                false
            );
        }

        if (showDebugLog &&
            isChoiceOpen)
        {
            Debug.Log(
                "EventRoomController: 이벤트 선택 종료, " +
                $"Completed={wasCompleted}, " +
                $"RoomId={activeEventRoomId}",
                this
            );
        }

        isChoiceOpen =
            false;

        activeEventRoomId =
            -1;

        currentChoices.Clear();
    }
}