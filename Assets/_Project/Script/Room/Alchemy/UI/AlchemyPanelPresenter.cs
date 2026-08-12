using UnityEngine;

[DisallowMultipleComponent]
public sealed class AlchemyPanelPresenter :
    MonoBehaviour
{
    [Header("Panel")]

    [Tooltip(
        "실제로 표시하거나 숨길 연금술 UI 오브젝트입니다.\n" +
        "Presenter가 붙은 오브젝트와 분리되어 있어야 합니다."
    )]
    [SerializeField]
    private GameObject panelRoot;

    [Header("Runtime References")]

    [SerializeField]
    private StageRoomNavigator roomNavigator;

    [SerializeField]
    private BallCollection ballCollection;

    private bool isSubscribed;
    private AlchemyBallSelectionPanel selectionPanel;
    private readonly AlchemyBallSelectionModel ballSelectionModel =
        new AlchemyBallSelectionModel();

    public AlchemyBallSelectionModel BallSelectionModel =>
        ballSelectionModel;

    private void Awake()
    {
        FindReferences();
        EnsureSelectionPanel();
        ValidateReferences();
        HideImmediately();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();
        RefreshForCurrentRoom();
    }

    private void Start()
    {
        RefreshForCurrentRoom();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        FindReferences();
    }

    private void SubscribeEvents()
    {
        if (isSubscribed ||
            roomNavigator == null)
        {
            return;
        }

        roomNavigator.RoomChanged -=
            HandleRoomChanged;

        roomNavigator.RoomChanged +=
            HandleRoomChanged;

        if (ballCollection != null)
        {
            ballCollection.BallCountChanged += HandleBallCountChanged;
            ballCollection.BallDefinitionsReplaced +=
                HandleBallDefinitionsReplaced;
        }

        isSubscribed =
            true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (roomNavigator != null)
        {
            roomNavigator.RoomChanged -=
                HandleRoomChanged;
        }

        if (ballCollection != null)
        {
            ballCollection.BallCountChanged -= HandleBallCountChanged;
            ballCollection.BallDefinitionsReplaced -=
                HandleBallDefinitionsReplaced;
        }

        isSubscribed =
            false;
    }

    private void HandleRoomChanged(
        RoomNode previousRoom,
        RoomNode currentRoom)
    {
        SetPanelActive(
            IsAlchemyRoom(
                currentRoom
            )
        );

        if (IsAlchemyRoom(currentRoom))
        {
            ballSelectionModel.Refresh(ballCollection);
        }
    }

    private void HandleBallCountChanged(int count)
    {
        if (panelRoot != null && panelRoot.activeSelf)
        {
            ballSelectionModel.Refresh(ballCollection);
        }
    }

    private void HandleBallDefinitionsReplaced(int count)
    {
        if (panelRoot != null && panelRoot.activeSelf)
        {
            ballSelectionModel.Refresh(ballCollection);
        }
    }

    private void RefreshForCurrentRoom()
    {
        if (roomNavigator == null)
        {
            HideImmediately();

            return;
        }

        SetPanelActive(
            IsAlchemyRoom(
                roomNavigator.CurrentRoom
            )
        );
    }

    private static bool IsAlchemyRoom(
        RoomNode room)
    {
        return room != null &&
               room.RoomType ==
               RoomType.Alchemy;
    }

    private void HideImmediately()
    {
        SetPanelActive(
            false
        );
    }

    private void SetPanelActive(
        bool shouldActivate)
    {
        if (panelRoot == null)
        {
            return;
        }

        if (panelRoot.activeSelf ==
            shouldActivate)
        {
            return;
        }

        panelRoot.SetActive(
            shouldActivate
        );

        if (shouldActivate)
        {
            ballSelectionModel.Refresh(ballCollection);
        }
        else
        {
            ballSelectionModel.ClearSelection();
        }
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

        if (ballCollection == null)
        {
            ballCollection =
                FindFirstObjectByType<BallCollection>();
        }
    }

    private void EnsureSelectionPanel()
    {
        if (panelRoot == null)
        {
            return;
        }

        selectionPanel =
            panelRoot.GetComponentInChildren<
                AlchemyBallSelectionPanel>(true);

        if (selectionPanel != null)
        {
            selectionPanel.Initialize(ballSelectionModel);
        }
        else
        {
            Debug.LogError(
                "AlchemyPanelPresenter: " +
                "Hierarchy에 AlchemyBallSelectionPanel이 없습니다.",
                this);
        }
    }

    private void ValidateReferences()
    {
        if (panelRoot == null)
        {
            Debug.LogError(
                "AlchemyPanelPresenter: " +
                "Panel Root가 연결되지 않았습니다.",
                this
            );
        }

        if (roomNavigator == null)
        {
            Debug.LogError(
                "AlchemyPanelPresenter: " +
                "StageRoomNavigator가 연결되지 않았습니다.",
                this
            );
        }
    }
}
