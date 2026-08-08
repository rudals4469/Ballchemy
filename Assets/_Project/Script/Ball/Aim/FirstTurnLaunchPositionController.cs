using System;
using System.Collections;
using System.Collections.Generic;
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

    [SerializeField]
    private BallCollection ballCollection;

    [Header("Selection Feedback")]

    [SerializeField, Range(0f, 0.5f)]
    private float pulseScaleAmount = 0.12f;

    [SerializeField, Min(0.1f)]
    private float pulseSpeed = 3f;

    [SerializeField, Range(1f, 2f)]
    private float confirmationPulseScale = 1.4f;

    [SerializeField, Min(0.05f)]
    private float confirmationPulseDuration = 0.22f;

    [Header("Selection Arrow")]

    [SerializeField]
    private Sprite selectionArrowSprite;

    [SerializeField]
    private Vector2 selectionArrowOffset =
        new Vector2(0f, 0.95f);

    [SerializeField]
    private Vector2 selectionArrowScale =
        Vector2.one;

    [SerializeField]
    private Color selectionArrowColor =
        Color.white;

    [SerializeField]
    private int selectionArrowSortingOrder = 30;

    private readonly List<BallVisualView>
        pulseTargets =
            new List<BallVisualView>();

    private readonly List<BallVisualView>
        confirmationPulseTargets =
            new List<BallVisualView>();

    private Coroutine confirmationPulseCoroutine;
    private SpriteRenderer selectionArrowRenderer;

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
        CreateSelectionArrowRenderer();
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

        if (ballCollection == null)
        {
            ballCollection =
                GetComponent<BallCollection>();
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

        ballCountView?.SetCountSuppressed(
            available
        );

        SetArrowVisible(available);

        if (available)
        {
            StopConfirmationPulse();
            RefreshPulseTargets();
        }
        else
        {
            ResetPulseTargets();
        }

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

    private void LateUpdate()
    {
        if (!isSelectionAvailable)
        {
            return;
        }

        if (pulseTargets.Count == 0)
        {
            RefreshPulseTargets();
        }

        float multiplier =
            1f +
            Mathf.Sin(
                Time.unscaledTime * pulseSpeed
            ) *
            pulseScaleAmount;

        for (int i = 0;
             i < pulseTargets.Count;
             i++)
        {
            pulseTargets[i]
                ?.SetAttentionScaleMultiplier(
                    multiplier
                );
        }
    }

    private void CreateSelectionArrowRenderer()
    {
        GameObject arrowObject =
            new GameObject(
                "First Launch Arrow Image"
            );
        arrowObject.transform.SetParent(
            transform,
            false
        );

        arrowObject.transform.localPosition =
            new Vector3(
                selectionArrowOffset.x,
                selectionArrowOffset.y,
                0f
            );
        arrowObject.transform.localScale =
            new Vector3(
                Mathf.Max(selectionArrowScale.x, 0.01f),
                Mathf.Max(selectionArrowScale.y, 0.01f),
                1f
            );

        selectionArrowRenderer =
            arrowObject.AddComponent<SpriteRenderer>();
        selectionArrowRenderer.sprite =
            selectionArrowSprite;
        selectionArrowRenderer.color =
            selectionArrowColor;
        selectionArrowRenderer.sortingOrder =
            selectionArrowSortingOrder;
        selectionArrowRenderer.enabled = false;
    }

    private void SetArrowVisible(
        bool visible)
    {
        if (selectionArrowRenderer != null)
        {
            selectionArrowRenderer.enabled =
                visible &&
                selectionArrowSprite != null;
        }
    }

    private void RefreshPulseTargets()
    {
        ResetPulseTargets();

        if (ballCollection == null)
        {
            return;
        }

        List<Ball> balls =
            ballCollection.CreateSnapshot();

        for (int i = 0;
             i < balls.Count;
             i++)
        {
            BallVisualView view =
                balls[i] != null
                    ? balls[i].GetComponent<
                        BallVisualView
                    >()
                    : null;

            if (view != null)
            {
                pulseTargets.Add(view);
            }
        }
    }

    private void ResetPulseTargets()
    {
        for (int i = 0;
             i < pulseTargets.Count;
             i++)
        {
            pulseTargets[i]
                ?.SetAttentionScaleMultiplier(1f);
        }

        pulseTargets.Clear();
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

        confirmationPulseTargets.Clear();
        confirmationPulseTargets.AddRange(
            pulseTargets
        );

        selectionCompletedFrame =
            Time.frameCount;

        SetSelectionAvailable(false);

        confirmationPulseCoroutine =
            StartCoroutine(
                PlayConfirmationPulseRoutine()
            );

        SelectionCompleted?.Invoke(
            selectedPosition
        );
    }

    private IEnumerator PlayConfirmationPulseRoutine()
    {
        float elapsed = 0f;

        while (elapsed <
               confirmationPulseDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    confirmationPulseDuration
                );

            float multiplier =
                Mathf.Lerp(
                    confirmationPulseScale,
                    1f,
                    progress
                );

            for (int i = 0;
                 i < confirmationPulseTargets.Count;
                 i++)
            {
                confirmationPulseTargets[i]
                    ?.SetAttentionScaleMultiplier(
                        multiplier
                    );
            }

            yield return null;
        }

        StopConfirmationPulse();
    }

    private void StopConfirmationPulse()
    {
        if (confirmationPulseCoroutine != null)
        {
            StopCoroutine(
                confirmationPulseCoroutine
            );
            confirmationPulseCoroutine = null;
        }

        for (int i = 0;
             i < confirmationPulseTargets.Count;
             i++)
        {
            confirmationPulseTargets[i]
                ?.SetAttentionScaleMultiplier(1f);
        }

        confirmationPulseTargets.Clear();
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
        StopConfirmationPulse();
    }

}
