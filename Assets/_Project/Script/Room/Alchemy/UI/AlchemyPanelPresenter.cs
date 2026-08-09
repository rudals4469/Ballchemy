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

    private bool isSubscribed;

    private void Awake()
    {
        FindReferences();
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