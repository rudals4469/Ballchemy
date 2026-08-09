using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("Stage Progression")]
    [SerializeField]
    private Button nextStageButton;

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
        CreateNextStageButtonIfNeeded();
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

        nextStageButton?.onClick.AddListener(
            HandleNextStageClicked
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

        nextStageButton?.onClick.RemoveListener(
            HandleNextStageClicked
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
        bool showNextStage =
            navigator != null &&
            navigator.CanAdvanceToNextStage;

        if (nextStageButton != null)
        {
            nextStageButton.gameObject.SetActive(showNextStage);
            nextStageButton.interactable = showNextStage;
        }

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

    private void HandleNextStageClicked()
    {
        if (navigator != null &&
            navigator.TryAdvanceToNextStage())
        {
            Refresh();
        }
    }

    private void CreateNextStageButtonIfNeeded()
    {
        if (nextStageButton != null || upButton == null)
        {
            return;
        }

        nextStageButton = Instantiate(
            upButton,
            upButton.transform.parent
        );

        nextStageButton.name = "NextStageButton";
        nextStageButton.onClick.RemoveAllListeners();

        RectTransform sourceRect =
            upButton.transform as RectTransform;
        RectTransform targetRect =
            nextStageButton.transform as RectTransform;

        if (sourceRect != null && targetRect != null)
        {
            Vector2 sourceSize = sourceRect.rect.size;
            targetRect.anchorMin = new Vector2(0.5f, 0.5f);
            targetRect.anchorMax = new Vector2(0.5f, 0.5f);
            targetRect.anchoredPosition = Vector2.zero;
            targetRect.sizeDelta = new Vector2(
                sourceSize.x > 0f
                    ? sourceSize.x * 2f
                    : 160f,
                sourceSize.y > 0f
                    ? sourceSize.y * 2f
                    : 80f
            );
            targetRect.localScale = Vector3.one;
        }

        TMP_Text label =
            nextStageButton.GetComponentInChildren<TMP_Text>(true);

        if (label != null)
        {
            label.text = "다음 스테이지";
        }

        Image image = nextStageButton.targetGraphic as Image;
        if (image != null)
        {
            image.color = new Color(0.35f, 0.85f, 0.45f, 1f);
        }

        nextStageButton.gameObject.SetActive(false);
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
