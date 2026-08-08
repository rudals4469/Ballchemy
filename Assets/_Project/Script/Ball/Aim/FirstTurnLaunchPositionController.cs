using System;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class FirstTurnLaunchPositionController :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private BallLauncher ballLauncher;

    [SerializeField]
    private BlockGridManager blockGridManager;

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private BallCountView ballCountView;

    private Camera mainCamera;
    private bool isSelectionAvailable;
    private int selectionCompletedFrame = -1;

    public bool IsSelecting =>
        isSelectionAvailable;

    public bool DidCompleteSelectionThisFrame =>
        selectionCompletedFrame ==
        Time.frameCount;

    public event Action SelectionStarted;
    public event Action<Vector2> SelectionCompleted;

    private void Awake()
    {
        mainCamera = Camera.main;
        FindReferences();
        SetSelectionAvailable(false);
    }

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void Start()
    {
        RefreshForCurrentRoom();
    }

    private void Update()
    {
        if (!CanHandleSelection())
        {
            return;
        }

        if (Input.touchCount > 0)
        {
            HandleTouch(Input.GetTouch(0));
            return;
        }

        HandleMouse();
    }

    private void FindReferences()
    {
        if (ballLauncher == null)
        {
            ballLauncher = GetComponent<BallLauncher>();
        }

        if (blockGridManager == null)
        {
            blockGridManager =
                FindFirstObjectByType<BlockGridManager>();
        }

        if (turnManager == null)
        {
            turnManager =
                FindFirstObjectByType<TurnManager>();
        }

        if (ballCountView == null)
        {
            ballCountView =
                GetComponentInChildren<BallCountView>(
                    true
                );
        }
    }

    private void SubscribeEvents()
    {
        if (blockGridManager == null)
        {
            return;
        }

        blockGridManager.RoomStateChanged -=
            HandleRoomStateChanged;
        blockGridManager.RoomStateChanged +=
            HandleRoomStateChanged;

        blockGridManager.RoomCombatReset -=
            HandleRoomCombatReset;
        blockGridManager.RoomCombatReset +=
            HandleRoomCombatReset;
    }

    private void HandleRoomStateChanged(
        RoomCombatState state)
    {
        SetSelectionAvailable(
            state == RoomCombatState.InCombat &&
            blockGridManager != null &&
            blockGridManager.CurrentTurn == 0
        );
    }

    private void HandleRoomCombatReset()
    {
        RefreshForCurrentRoom();
    }

    private void RefreshForCurrentRoom()
    {
        SetSelectionAvailable(
            blockGridManager != null &&
            blockGridManager.CurrentRoomState ==
                RoomCombatState.InCombat &&
            blockGridManager.CurrentTurn == 0
        );
    }

    private void SetSelectionAvailable(
        bool available)
    {
        bool wasAvailable =
            isSelectionAvailable;

        isSelectionAvailable = available;

        ballCountView?.SetExternallySuppressed(
            available
        );

        if (available &&
            !wasAvailable)
        {
            SelectionStarted?.Invoke();
        }
    }

    private bool CanHandleSelection()
    {
        return isSelectionAvailable &&
               ballLauncher != null &&
               mainCamera != null &&
               turnManager != null &&
               turnManager.CanAim &&
               blockGridManager != null &&
               blockGridManager.CurrentRoomState ==
                   RoomCombatState.InCombat &&
               blockGridManager.CurrentTurn == 0;
    }

    private void HandleMouse()
    {
        Vector2 screenPosition =
            Input.mousePosition;

        if (!IsValidScreenPosition(screenPosition) ||
            IsPointerOverUi(-1))
        {
            return;
        }

        FollowPointer(screenPosition);

        if (Input.GetMouseButtonDown(0))
        {
            CompleteSelection();
        }
    }

    private void HandleTouch(
        Touch touch)
    {
        if (IsPointerOverUi(touch.fingerId))
        {
            return;
        }

        switch (touch.phase)
        {
            case TouchPhase.Began:
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                FollowPointer(touch.position);
                break;

            case TouchPhase.Ended:
                FollowPointer(touch.position);
                CompleteSelection();
                break;
        }
    }

    private void FollowPointer(
        Vector2 screenPosition)
    {
        Vector2 worldPosition =
            ScreenToWorld(screenPosition);

        ballLauncher.TrySetLaunchPositionX(
            worldPosition.x
        );
    }

    private void CompleteSelection()
    {
        Vector2 selectedPosition =
            ballLauncher.CurrentLaunchPosition;

        selectionCompletedFrame =
            Time.frameCount;

        SetSelectionAvailable(false);
        SelectionCompleted?.Invoke(
            selectedPosition
        );
    }

    private Vector2 ScreenToWorld(
        Vector2 screenPosition)
    {
        float depth =
            Mathf.Abs(
                mainCamera.transform.position.z -
                transform.position.z
            );

        return mainCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                depth
            )
        );
    }

    private bool IsPointerOverUi(
        int pointerId)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return pointerId >= 0
            ? EventSystem.current
                .IsPointerOverGameObject(pointerId)
            : EventSystem.current
                .IsPointerOverGameObject();
    }

    private static bool IsValidScreenPosition(
        Vector2 screenPosition)
    {
        return screenPosition.x >= 0f &&
               screenPosition.x <= Screen.width &&
               screenPosition.y >= 0f &&
               screenPosition.y <= Screen.height;
    }

    private void OnDisable()
    {
        if (blockGridManager != null)
        {
            blockGridManager.RoomStateChanged -=
                HandleRoomStateChanged;
            blockGridManager.RoomCombatReset -=
                HandleRoomCombatReset;
        }

        SetSelectionAvailable(false);
    }
}
