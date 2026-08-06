using UnityEngine;
using UnityEngine.UI;

public sealed class RoomNavigationUI :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private StageRoomNavigator navigator;

    [SerializeField]
    private RoomTransitionController
        transitionController;

    [Header("Direction Buttons")]

    [SerializeField]
    private Button upButton;

    [SerializeField]
    private Button rightButton;

    [SerializeField]
    private Button downButton;

    [SerializeField]
    private Button leftButton;

    [Header("Display")]

    [Tooltip(
        "활성화하면 현재 이동할 수 없는 방향의 버튼을 숨깁니다.\n" +
        "전투방에서는 방을 클리어한 뒤에만 이동 버튼이 표시됩니다.\n" +
        "해제하면 연결된 방향 버튼을 표시하되 상호작용만 막습니다."
    )]
    [SerializeField]
    private bool hideUnavailableButtons = true;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
        BindButtons();
    }

    private void OnEnable()
    {
        SubscribeEvents();
        Refresh();
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnbindButtons();
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

        if (transitionController == null)
        {
            transitionController =
                FindFirstObjectByType<
                    RoomTransitionController
                >();
        }
    }

    private void ValidateReferences()
    {
        if (navigator == null)
        {
            Debug.LogError(
                "RoomNavigationUI: " +
                "StageRoomNavigator가 연결되지 않았습니다.",
                this
            );
        }

        if (transitionController == null)
        {
            Debug.LogError(
                "RoomNavigationUI: " +
                "RoomTransitionController가 연결되지 않았습니다.",
                this
            );
        }

        if (upButton == null ||
            rightButton == null ||
            downButton == null ||
            leftButton == null)
        {
            Debug.LogError(
                "RoomNavigationUI: " +
                "상하좌우 버튼 중 연결되지 않은 버튼이 있습니다.",
                this
            );
        }
    }

    private void BindButtons()
    {
        UnbindButtons();

        upButton?.onClick.AddListener(
            HandleUpClicked
        );

        rightButton?.onClick.AddListener(
            HandleRightClicked
        );

        downButton?.onClick.AddListener(
            HandleDownClicked
        );

        leftButton?.onClick.AddListener(
            HandleLeftClicked
        );
    }

    private void UnbindButtons()
    {
        upButton?.onClick.RemoveListener(
            HandleUpClicked
        );

        rightButton?.onClick.RemoveListener(
            HandleRightClicked
        );

        downButton?.onClick.RemoveListener(
            HandleDownClicked
        );

        leftButton?.onClick.RemoveListener(
            HandleLeftClicked
        );
    }

    private void SubscribeEvents()
    {
        if (navigator != null)
        {
            navigator.MapInitialized -=
                HandleMapInitialized;

            navigator.MapInitialized +=
                HandleMapInitialized;

            navigator.RoomChanged -=
                HandleRoomChanged;

            navigator.RoomChanged +=
                HandleRoomChanged;

            navigator
                .NavigationAvailabilityChanged -=
                HandleNavigationAvailabilityChanged;

            navigator
                .NavigationAvailabilityChanged +=
                HandleNavigationAvailabilityChanged;
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
    }

    private void UnsubscribeEvents()
    {
        if (navigator != null)
        {
            navigator.MapInitialized -=
                HandleMapInitialized;

            navigator.RoomChanged -=
                HandleRoomChanged;

            navigator
                .NavigationAvailabilityChanged -=
                HandleNavigationAvailabilityChanged;
        }

        if (transitionController != null)
        {
            transitionController
                .TransitionStateChanged -=
                HandleTransitionStateChanged;
        }
    }

    public void Refresh()
    {
        RefreshButton(
            upButton,
            RoomDirection.Up
        );

        RefreshButton(
            rightButton,
            RoomDirection.Right
        );

        RefreshButton(
            downButton,
            RoomDirection.Down
        );

        RefreshButton(
            leftButton,
            RoomDirection.Left
        );
    }

    private void RefreshButton(
        Button button,
        RoomDirection direction)
    {
        if (button == null)
        {
            return;
        }

        bool hasConnectedRoom =
            navigator != null &&
            navigator.HasConnectedRoom(
                direction
            );

        bool canMove =
            transitionController != null
                ? transitionController
                    .CanRequestDirectionalMove(
                        direction
                    )
                : navigator != null &&
                  navigator.CanMove(
                      direction
                  );

        button.gameObject.SetActive(
            hideUnavailableButtons
                ? canMove
                : hasConnectedRoom
        );

        button.interactable =
            canMove;
    }

    private void HandleUpClicked()
    {
        transitionController
            ?.TryMoveFromDirectionButton(
                RoomDirection.Up
            );
    }

    private void HandleRightClicked()
    {
        transitionController
            ?.TryMoveFromDirectionButton(
                RoomDirection.Right
            );
    }

    private void HandleDownClicked()
    {
        transitionController
            ?.TryMoveFromDirectionButton(
                RoomDirection.Down
            );
    }

    private void HandleLeftClicked()
    {
        transitionController
            ?.TryMoveFromDirectionButton(
                RoomDirection.Left
            );
    }

    private void HandleMapInitialized(
        StageMap map)
    {
        Refresh();
    }

    private void HandleRoomChanged(
        RoomNode previousRoom,
        RoomNode currentRoom)
    {
        Refresh();
    }

    private void
        HandleNavigationAvailabilityChanged()
    {
        Refresh();
    }

    private void HandleTransitionStateChanged(
        bool isTransitioning)
    {
        Refresh();
    }
}