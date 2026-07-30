using UnityEngine;
using UnityEngine.UI;

public sealed class RoomNavigationUI :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private StageRoomNavigator navigator;

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

        if (upButton == null)
        {
            Debug.LogError(
                "RoomNavigationUI: " +
                "Up Button이 연결되지 않았습니다.",
                this
            );
        }

        if (rightButton == null)
        {
            Debug.LogError(
                "RoomNavigationUI: " +
                "Right Button이 연결되지 않았습니다.",
                this
            );
        }

        if (downButton == null)
        {
            Debug.LogError(
                "RoomNavigationUI: " +
                "Down Button이 연결되지 않았습니다.",
                this
            );
        }

        if (leftButton == null)
        {
            Debug.LogError(
                "RoomNavigationUI: " +
                "Left Button이 연결되지 않았습니다.",
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
        if (navigator == null)
        {
            return;
        }

        navigator.MapInitialized -=
            HandleMapInitialized;

        navigator.MapInitialized +=
            HandleMapInitialized;

        navigator.RoomChanged -=
            HandleRoomChanged;

        navigator.RoomChanged +=
            HandleRoomChanged;

        navigator.NavigationAvailabilityChanged -=
            HandleNavigationAvailabilityChanged;

        navigator.NavigationAvailabilityChanged +=
            HandleNavigationAvailabilityChanged;
    }

    private void UnsubscribeEvents()
    {
        if (navigator == null)
        {
            return;
        }

        navigator.MapInitialized -=
            HandleMapInitialized;

        navigator.RoomChanged -=
            HandleRoomChanged;

        navigator.NavigationAvailabilityChanged -=
            HandleNavigationAvailabilityChanged;
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
            navigator != null &&
            navigator.CanMove(
                direction
            );

        /*
         * 숨김 모드에서는 실제 이동 가능한 경우에만
         * 버튼을 표시합니다.
         *
         * 따라서 미클리어 전투방에서는 모든 버튼이
         * 사라지고, 클리어 이벤트가 발생하면 연결된
         * 방향 버튼이 다시 표시됩니다.
         */
        if (hideUnavailableButtons)
        {
            button.gameObject.SetActive(
                canMove
            );
        }
        else
        {
            button.gameObject.SetActive(
                hasConnectedRoom
            );
        }

        button.interactable =
            canMove;
    }

    private void HandleUpClicked()
    {
        navigator?.TryMove(
            RoomDirection.Up
        );
    }

    private void HandleRightClicked()
    {
        navigator?.TryMove(
            RoomDirection.Right
        );
    }

    private void HandleDownClicked()
    {
        navigator?.TryMove(
            RoomDirection.Down
        );
    }

    private void HandleLeftClicked()
    {
        navigator?.TryMove(
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
}