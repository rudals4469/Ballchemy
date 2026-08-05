using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShopRoomController :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private StageRoomNavigator roomNavigator;

    [SerializeField]
    private ShopRoomState shopRoomState;

    [SerializeField]
    private ShopInventoryGenerator
        inventoryGenerator;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog =
        true;

    private bool isSubscribed;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();
    }

    private void Start()
    {
        TryPrepareCurrentRoom();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
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

        if (shopRoomState == null)
        {
            shopRoomState =
                GetComponent<
                    ShopRoomState
                >();
        }

        if (shopRoomState == null)
        {
            shopRoomState =
                FindFirstObjectByType<
                    ShopRoomState
                >();
        }

        if (inventoryGenerator == null)
        {
            inventoryGenerator =
                GetComponent<
                    ShopInventoryGenerator
                >();
        }

        if (inventoryGenerator == null)
        {
            inventoryGenerator =
                FindFirstObjectByType<
                    ShopInventoryGenerator
                >();
        }
    }

    private void ValidateReferences()
    {
        if (roomNavigator == null)
        {
            Debug.LogError(
                "ShopRoomController: " +
                "StageRoomNavigator가 연결되지 않았습니다.",
                this
            );
        }

        if (shopRoomState == null)
        {
            Debug.LogError(
                "ShopRoomController: " +
                "ShopRoomState가 연결되지 않았습니다.",
                this
            );
        }

        if (inventoryGenerator == null)
        {
            Debug.LogError(
                "ShopRoomController: " +
                "ShopInventoryGenerator가 연결되지 않았습니다.",
                this
            );
        }
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

        roomNavigator.MapInitialized -=
            HandleMapInitialized;

        roomNavigator.MapInitialized +=
            HandleMapInitialized;

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

            roomNavigator.MapInitialized -=
                HandleMapInitialized;
        }

        isSubscribed =
            false;
    }

    private void HandleMapInitialized(
        StageMap stageMap)
    {
        if (shopRoomState == null)
        {
            return;
        }

        shopRoomState.ResetForNewStage();

        TryPrepareCurrentRoom();
    }

    private void HandleRoomChanged(
        RoomNode previousRoom,
        RoomNode currentRoom)
    {
        TryPrepareShopRoom(
            currentRoom
        );
    }

    private void TryPrepareCurrentRoom()
    {
        if (roomNavigator == null)
        {
            return;
        }

        TryPrepareShopRoom(
            roomNavigator.CurrentRoom
        );
    }

    private void TryPrepareShopRoom(
        RoomNode room)
    {
        if (room == null ||
            room.RoomType != RoomType.Shop ||
            shopRoomState == null ||
            inventoryGenerator == null)
        {
            return;
        }

        bool alreadyCreated =
            shopRoomState.HasInventory(
                room.RoomId
            );

        if (!alreadyCreated)
        {
            bool created =
                shopRoomState.TryCreateInventory(
                    room.RoomId
                );

            if (!created)
            {
                Debug.LogWarning(
                    "ShopRoomController: " +
                    "상점 재고 상태를 생성하지 못했습니다. " +
                    $"RoomId={room.RoomId}",
                    this
                );

                return;
            }
        }

        bool generated =
            inventoryGenerator
                .TryGenerateInventory(
                    room.RoomId
                );

        if (!generated)
        {
            Debug.LogError(
                "ShopRoomController: " +
                "상점 상품 생성에 실패했습니다. " +
                $"RoomId={room.RoomId}",
                this
            );

            return;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "ShopRoomController: " +
                (
                    alreadyCreated
                        ? "기존 상점 재고를 사용합니다. "
                        : "상점 재고를 처음 생성했습니다. "
                ) +
                $"RoomId={room.RoomId}",
                this
            );
        }
    }
}