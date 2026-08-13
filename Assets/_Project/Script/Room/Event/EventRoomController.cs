using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EventRoomController :
    MonoBehaviour
{
    public event Action<UnknownEventDefinition> UnknownEventApplied;

    [Header("References")]

    [SerializeField]
    private StageRoomNavigator
        roomNavigator;

    [SerializeField]
    private TurnManager
        turnManager;

    [SerializeField]
    private PlayerHealth
        playerHealth;

    [SerializeField]
    private BallCollection
        ballCollection;

    [SerializeField]
    private RunRewardState
        runRewardState;

    [SerializeField]
    private EventRoomState
        eventRoomState;

    [SerializeField]
    private EventSelectionUI
        selectionUI;

    [SerializeField]
    private UnknownEventSlotPresenter
        unknownEventSlotPresenter;

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

    private readonly List<UnknownEventDefinition>
        applicableUnknownEvents =
            new List<UnknownEventDefinition>();

    private UnknownEventApplyContext
        unknownEventContext;

    private UnknownEventDefinition
        pendingUnknownEvent;

    private int activeEventRoomId =
        -1;

    private bool isChoiceOpen;
    private bool isUnknownSlotPlaying;
    private bool isBeingDestroyed;

    /*
     * 슬롯 시작 전에 TurnManager가 이미 잠겨 있었는지
     * 기억해 두었다가 슬롯 종료 시 원래 상태로 복구합니다.
     */
    private bool isUnknownSlotInputLockActive;
    private bool inputWasLockedBeforeUnknownSlot;

    private void Awake()
    {
        FindReferences();
        CreateUnknownEventContext();
        ValidateReferences();
    }

    private void OnEnable()
    {
        isBeingDestroyed =
            false;

        FindReferences();
        CreateUnknownEventContext();
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();

        RestoreUnknownSlotInputLock();

        if (unknownEventSlotPresenter != null)
        {
            unknownEventSlotPresenter
                .CancelAndHide();
        }

        isChoiceOpen =
            false;

        isUnknownSlotPlaying =
            false;

        activeEventRoomId =
            -1;

        pendingUnknownEvent =
            null;

        currentChoices.Clear();
        applicableUnknownEvents.Clear();
    }

    private void OnDestroy()
    {
        isBeingDestroyed =
            true;

        UnsubscribeEvents();

        /*
         * OnDestroy 시점에는 TurnManager가 먼저 파괴됐을
         * 가능성이 있으므로 Unity 참조에 접근하지 않습니다.
         */
        isUnknownSlotInputLockActive =
            false;
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

        if (turnManager == null)
        {
            turnManager =
                FindFirstObjectByType<
                    TurnManager
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

        if (runRewardState == null)
        {
            runRewardState =
                FindFirstObjectByType<
                    RunRewardState
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

        if (unknownEventSlotPresenter == null)
        {
            unknownEventSlotPresenter =
                FindFirstObjectByType<
                    UnknownEventSlotPresenter
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
                playerHealth,
                runRewardState
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

        if (turnManager == null)
        {
            Debug.LogWarning(
                "EventRoomController: " +
                "TurnManager가 연결되지 않았습니다. " +
                "??? 슬롯 중 조준 잠금이 적용되지 않습니다.",
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

        if (runRewardState == null)
        {
            Debug.LogError(
                "EventRoomController: " +
                "RunRewardState가 연결되지 않았습니다. " +
                "다음 전투방 보상 등급 증가 결과가 " +
                "적용되지 않습니다.",
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

        if (unknownEventSlotPresenter == null)
        {
            Debug.LogError(
                "EventRoomController: " +
                "UnknownEventSlotPresenter가 연결되지 않았습니다.",
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
        if (isBeingDestroyed)
        {
            return;
        }

        RestoreUnknownSlotInputLock();

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
        if (isBeingDestroyed ||
            currentRoom == null ||
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
        if (isBeingDestroyed ||
            selectionUI == null ||
            playerHealth == null ||
            roomNavigator == null)
        {
            return;
        }

        FindReferences();
        CreateUnknownEventContext();

        RestoreUnknownSlotInputLock();

        if (unknownEventSlotPresenter != null)
        {
            unknownEventSlotPresenter
                .CancelAndHide();
        }

        pendingUnknownEvent =
            null;

        applicableUnknownEvents.Clear();

        isUnknownSlotPlaying =
            false;

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
        if (isBeingDestroyed ||
            !isChoiceOpen ||
            isUnknownSlotPlaying ||
            selectedChoice == null ||
            playerHealth == null)
        {
            return;
        }

        if (selectedChoice.ChoiceType ==
            EventChoiceType.Unknown)
        {
            bool started =
                TryStartUnknownEventSlot();

            if (!started)
            {
                RestoreChoiceSelection();
            }

            return;
        }

        bool applied =
            ApplyImmediateChoice(
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

            RestoreChoiceSelection();

            return;
        }

        CompleteEventRoomChoice();
    }

    private bool ApplyImmediateChoice(
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

            default:
                return false;
        }
    }

    private bool TryStartUnknownEventSlot()
    {
        if (isBeingDestroyed ||
            unknownEventPool == null ||
            unknownEventContext == null ||
            unknownEventSlotPresenter == null)
        {
            return false;
        }

        FindReferences();
        CreateUnknownEventContext();

        int candidateCount =
            unknownEventPool.GetApplicableEvents(
                unknownEventContext,
                applicableUnknownEvents
            );

        if (candidateCount <= 0)
        {
            Debug.LogWarning(
                "EventRoomController: " +
                "슬롯에 표시할 적용 가능한 " +
                "비밀 이벤트가 없습니다.",
                this
            );

            return false;
        }

        bool drewEvent =
            unknownEventPool.TryDraw(
                unknownEventContext,
                out pendingUnknownEvent
            );

        if (!drewEvent ||
            pendingUnknownEvent == null)
        {
            Debug.LogWarning(
                "EventRoomController: " +
                "비밀 이벤트 당첨 결과를 추첨하지 못했습니다.",
                this
            );

            return false;
        }

        isUnknownSlotPlaying =
            true;

        /*
         * 슬롯 연출 중 조준, 발사, 후퇴를 잠급니다.
         */
        ApplyUnknownSlotInputLock();

        selectionUI.SetPanelVisible(false);

        bool started =
            unknownEventSlotPresenter.Play(
                applicableUnknownEvents,
                pendingUnknownEvent,
                ApplyPendingUnknownEvent,
                HandleUnknownSlotCompleted
            );

        if (!started)
        {
            isUnknownSlotPlaying =
                false;

            pendingUnknownEvent =
                null;

            RestoreUnknownSlotInputLock();

            return false;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "EventRoomController: " +
                $"??? 슬롯 시작, 후보={candidateCount}개",
                this
            );
        }

        return true;
    }

    private UnknownEventResult
        ApplyPendingUnknownEvent()
    {
        if (pendingUnknownEvent == null ||
            unknownEventContext == null)
        {
            return new UnknownEventResult(
                pendingUnknownEvent,
                false,
                "적용할 비밀 이벤트가 없습니다."
            );
        }

        UnknownEventDefinition selectedEvent =
            pendingUnknownEvent;

        UnknownEventResult result =
            selectedEvent.Apply(
                unknownEventContext
            );

        if (result == null)
        {
            return new UnknownEventResult(
                selectedEvent,
                false,
                "비밀 이벤트 결과가 생성되지 않았습니다."
            );
        }

        if (result.WasApplied)
        {
            UnknownEventApplied?.Invoke(selectedEvent);
        }

        if (showDebugLog)
        {
            Debug.Log(
                "EventRoomController: ??? 결과 — " +
                $"{selectedEvent.DisplayName}\n" +
                $"{result.ResultText}",
                this
            );
        }

        return result;
    }

    private void HandleUnknownSlotCompleted(
        bool wasApplied)
    {
        if (isBeingDestroyed)
        {
            return;
        }

        RestoreUnknownSlotInputLock();

        isUnknownSlotPlaying =
            false;

        pendingUnknownEvent =
            null;

        applicableUnknownEvents.Clear();

        if (!wasApplied)
        {
            Debug.LogWarning(
                "EventRoomController: " +
                "비밀 이벤트 슬롯 결과 적용에 실패했습니다.",
                this
            );

            RestoreChoiceSelection();

            return;
        }

        CompleteEventRoomChoice();
    }

    private void RestoreChoiceSelection()
    {
        if (isBeingDestroyed ||
            !isChoiceOpen)
        {
            return;
        }

        RestoreUnknownSlotInputLock();

        isUnknownSlotPlaying =
            false;

        pendingUnknownEvent =
            null;

        applicableUnknownEvents.Clear();

        BuildChoices();

        if (selectionUI != null)
        {
            selectionUI.ShowChoices(
                currentChoices
            );
        }

        if (roomNavigator != null)
        {
            roomNavigator.SetNavigationLocked(
                true
            );
        }
    }

    private void CompleteEventRoomChoice()
    {
        RestoreUnknownSlotInputLock();

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

    private void CloseSelection(
        bool wasCompleted)
    {
        if (isBeingDestroyed)
        {
            return;
        }

        RestoreUnknownSlotInputLock();

        if (selectionUI != null && !wasCompleted)
        {
            selectionUI.Hide();
        }
        else if (selectionUI != null)
        {
            selectionUI.SetPanelVisible(true);
        }

        if (unknownEventSlotPresenter != null)
        {
            unknownEventSlotPresenter
                .CancelAndHide();
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

        isUnknownSlotPlaying =
            false;

        activeEventRoomId =
            -1;

        pendingUnknownEvent =
            null;

        currentChoices.Clear();
        applicableUnknownEvents.Clear();
    }

    private void ApplyUnknownSlotInputLock()
    {
        if (isUnknownSlotInputLockActive)
        {
            return;
        }

        if (turnManager == null)
        {
            turnManager =
                FindFirstObjectByType<
                    TurnManager
                >();
        }

        if (turnManager == null)
        {
            return;
        }

        inputWasLockedBeforeUnknownSlot =
            turnManager.IsInputLocked;

        turnManager.SetInputLocked(
            true
        );

        isUnknownSlotInputLockActive =
            true;

        if (showDebugLog)
        {
            Debug.Log(
                "EventRoomController: " +
                "??? 슬롯 중 조준과 발사를 잠급니다.",
                this
            );
        }
    }

    private void RestoreUnknownSlotInputLock()
    {
        if (!isUnknownSlotInputLockActive)
        {
            return;
        }

        isUnknownSlotInputLockActive =
            false;

        if (turnManager == null)
        {
            return;
        }

        turnManager.SetInputLocked(
            inputWasLockedBeforeUnknownSlot
        );

        if (showDebugLog)
        {
            Debug.Log(
                "EventRoomController: " +
                "??? 슬롯 입력 잠금을 원래 상태로 복구했습니다.",
                this
            );
        }
    }
}
