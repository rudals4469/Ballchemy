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

    [Header("Drag")]

    [SerializeField, Min(0.1f)]
    private float markerHitRadius = 0.75f;

    [Header("Marker")]

    [SerializeField]
    private Color markerColor =
        new Color(1f, 0.82f, 0.18f, 1f);

    [SerializeField, Min(0.01f)]
    private float markerWidth = 0.08f;

    [SerializeField, Min(0.1f)]
    private float markerHeight = 0.7f;

    [SerializeField]
    private int markerSortingOrder = 20;

    private Camera mainCamera;
    private LineRenderer marker;
    private bool isSelectionAvailable;
    private bool isDragging;

    public bool IsSelecting =>
        isSelectionAvailable;

    public event Action SelectionStarted;
    public event Action<Vector2> SelectionCompleted;

    private void Awake()
    {
        mainCamera = Camera.main;
        FindReferences();
        CreateMarker();
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
            isDragging = false;
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
        isDragging = false;

        if (marker != null)
        {
            marker.enabled = available;
        }

        if (available)
        {
            UpdateMarkerPosition();

            if (!wasAvailable)
            {
                SelectionStarted?.Invoke();
            }
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

        if (Input.GetMouseButtonDown(0))
        {
            TryBeginDrag(screenPosition, -1);
        }

        if (isDragging &&
            Input.GetMouseButton(0))
        {
            UpdateDrag(screenPosition);
        }

        if (isDragging &&
            Input.GetMouseButtonUp(0))
        {
            CompleteSelection(screenPosition);
        }
    }

    private void HandleTouch(
        Touch touch)
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
                TryBeginDrag(
                    touch.position,
                    touch.fingerId
                );
                break;

            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if (isDragging)
                {
                    UpdateDrag(touch.position);
                }
                break;

            case TouchPhase.Ended:
                if (isDragging)
                {
                    CompleteSelection(
                        touch.position
                    );
                }
                break;

            case TouchPhase.Canceled:
                isDragging = false;
                break;
        }
    }

    private void TryBeginDrag(
        Vector2 screenPosition,
        int pointerId)
    {
        if (IsPointerOverUi(pointerId))
        {
            return;
        }

        Vector2 worldPosition =
            ScreenToWorld(screenPosition);

        if (Vector2.Distance(
                worldPosition,
                ballLauncher.CurrentLaunchPosition) >
            markerHitRadius)
        {
            return;
        }

        isDragging = true;
        UpdateDrag(screenPosition);
    }

    private void UpdateDrag(
        Vector2 screenPosition)
    {
        Vector2 worldPosition =
            ScreenToWorld(screenPosition);

        if (ballLauncher.TrySetLaunchPositionX(
                worldPosition.x))
        {
            UpdateMarkerPosition();
        }
    }

    private void CompleteSelection(
        Vector2 screenPosition)
    {
        UpdateDrag(screenPosition);
        isDragging = false;

        Vector2 selectedPosition =
            ballLauncher.CurrentLaunchPosition;

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

        Vector3 worldPosition =
            mainCamera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    depth
                )
            );

        return worldPosition;
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

    private void CreateMarker()
    {
        GameObject markerObject =
            new GameObject(
                "First Launch Position Marker"
            );

        markerObject.transform.SetParent(
            transform,
            false
        );

        marker =
            markerObject.AddComponent<LineRenderer>();

        marker.useWorldSpace = true;
        marker.positionCount = 3;
        marker.startWidth = markerWidth;
        marker.endWidth = markerWidth;
        marker.startColor = markerColor;
        marker.endColor = markerColor;
        marker.sortingOrder = markerSortingOrder;
        marker.material =
            new Material(
                Shader.Find("Sprites/Default")
            );
    }

    private void UpdateMarkerPosition()
    {
        if (marker == null ||
            ballLauncher == null)
        {
            return;
        }

        Vector2 center =
            ballLauncher.CurrentLaunchPosition;
        float halfHeight =
            markerHeight * 0.5f;
        float halfWidth =
            markerHeight * 0.35f;

        marker.SetPosition(
            0,
            center +
            new Vector2(-halfWidth, halfHeight)
        );
        marker.SetPosition(
            1,
            center
        );
        marker.SetPosition(
            2,
            center +
            new Vector2(halfWidth, halfHeight)
        );
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

    private void OnDestroy()
    {
        if (marker != null &&
            marker.material != null)
        {
            Destroy(marker.material);
        }
    }
}
