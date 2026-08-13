using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ShopPanelPresenter :
    MonoBehaviour
{
    [Header("Panel")]

    [Tooltip(
        "실제로 표시하거나 숨길 상점 UI 오브젝트입니다.\n" +
        "Presenter가 붙은 오브젝트와 분리되어 있어야 합니다."
    )]
    [SerializeField]
    private GameObject panelRoot;

    [Tooltip(
        "상품 목록을 표시하는 Scroll Rect입니다."
    )]
    [SerializeField]
    private ScrollRect productScrollRect;

    [Header("Slots")]

    [SerializeField]
    private ShopItemSlotPresenter[] slots =
        new ShopItemSlotPresenter[
            ShopRoomState.TotalSlotCount
        ];

    [Header("Runtime References")]

    [SerializeField]
    private StageRoomNavigator roomNavigator;

    [SerializeField]
    private ShopRoomState shopRoomState;

    [SerializeField]
    private ShopItemCatalog itemCatalog;

    [SerializeField]
    private ShopPurchaseController purchaseController;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog =
        true;

    private Coroutine delayedRefreshCoroutine;
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
        SubscribeSlots();
        ScheduleRefresh();
    }

    private void Start()
    {
        ScheduleRefresh();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        UnsubscribeSlots();
        StopDelayedRefresh();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        UnsubscribeSlots();
        StopDelayedRefresh();
    }

    private void OnValidate()
    {
        FindReferences();
    }

    private void SubscribeEvents()
    {
        if (isSubscribed)
        {
            return;
        }

        if (roomNavigator != null)
        {
            roomNavigator.RoomChanged -=
                HandleRoomChanged;

            roomNavigator.RoomChanged +=
                HandleRoomChanged;
        }

        if (shopRoomState != null)
        {
            shopRoomState.ShopInventoryCreated -=
                HandleShopInventoryCreated;

            shopRoomState.ShopInventoryCreated +=
                HandleShopInventoryCreated;

            shopRoomState.ShopSlotPurchased -=
                HandleShopSlotPurchased;

            shopRoomState.ShopSlotPurchased +=
                HandleShopSlotPurchased;

            shopRoomState.HealingPurchaseCountChanged -=
                HandleHealingPurchaseCountChanged;

            shopRoomState.HealingPurchaseCountChanged +=
                HandleHealingPurchaseCountChanged;

            shopRoomState.StageStateReset -=
                HandleStageStateReset;

            shopRoomState.StageStateReset +=
                HandleStageStateReset;
        }

        if (purchaseController != null)
        {
            purchaseController.PurchaseSucceeded -=
                HandlePurchaseSucceeded;

            purchaseController.PurchaseSucceeded +=
                HandlePurchaseSucceeded;

            purchaseController.PurchaseFailed -=
                HandlePurchaseFailed;

            purchaseController.PurchaseFailed +=
                HandlePurchaseFailed;
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

        if (shopRoomState != null)
        {
            shopRoomState.ShopInventoryCreated -=
                HandleShopInventoryCreated;

            shopRoomState.ShopSlotPurchased -=
                HandleShopSlotPurchased;

            shopRoomState.HealingPurchaseCountChanged -=
                HandleHealingPurchaseCountChanged;

            shopRoomState.StageStateReset -=
                HandleStageStateReset;
        }

        if (purchaseController != null)
        {
            purchaseController.PurchaseSucceeded -=
                HandlePurchaseSucceeded;

            purchaseController.PurchaseFailed -=
                HandlePurchaseFailed;
        }

        isSubscribed =
            false;
    }

    private void SubscribeSlots()
    {
        if (slots == null)
        {
            return;
        }

        for (int i = 0;
             i < slots.Length;
             i++)
        {
            ShopItemSlotPresenter slot =
                slots[i];

            if (slot == null)
            {
                continue;
            }

            slot.Clicked -=
                HandleSlotClicked;

            slot.Clicked +=
                HandleSlotClicked;
        }
    }

    private void UnsubscribeSlots()
    {
        if (slots == null)
        {
            return;
        }

        for (int i = 0;
             i < slots.Length;
             i++)
        {
            ShopItemSlotPresenter slot =
                slots[i];

            if (slot == null)
            {
                continue;
            }

            slot.Clicked -=
                HandleSlotClicked;
        }
    }

    private void HandleRoomChanged(
        RoomNode previousRoom,
        RoomNode currentRoom)
    {
        if (!IsShopRoom(
                currentRoom
            ))
        {
            HideImmediately();
            return;
        }

        ScheduleRefresh();
    }

    private void HandleShopInventoryCreated(
        int roomId)
    {
        if (!IsCurrentShopRoom(
                roomId
            ))
        {
            return;
        }

        ScheduleRefresh();
    }

    private void HandleShopSlotPurchased(
        int roomId,
        int slotIndex)
    {
        if (!IsCurrentShopRoom(
                roomId
            ))
        {
            return;
        }

        RefreshInventory();
    }

    private void HandleHealingPurchaseCountChanged(
        int roomId,
        int purchaseCount)
    {
        if (!IsCurrentShopRoom(
                roomId
            ))
        {
            return;
        }

        RefreshInventory();
    }

    private void HandlePurchaseSucceeded(
        int roomId,
        int slotIndex,
        ShopItemDefinition item)
    {
        if (!IsCurrentShopRoom(
                roomId
            ))
        {
            return;
        }

        RefreshInventory();
    }

    private void HandlePurchaseFailed(
        string message)
    {
        if (!showDebugLog)
        {
            return;
        }

        Debug.LogWarning(
            "ShopPanelPresenter: " +
            $"구매 실패 - {message}",
            this
        );
    }

    private void HandleStageStateReset()
    {
        HideImmediately();
    }

    private void ScheduleRefresh()
    {
        StopDelayedRefresh();

        if (!isActiveAndEnabled)
        {
            return;
        }

        delayedRefreshCoroutine =
            StartCoroutine(
                DelayedRefreshRoutine()
            );
    }

    private IEnumerator DelayedRefreshRoutine()
    {
        yield return null;

        delayedRefreshCoroutine =
            null;

        RefreshForCurrentRoom();
    }

    private void RefreshForCurrentRoom()
    {
        if (roomNavigator == null)
        {
            HideImmediately();
            return;
        }

        RoomNode currentRoom =
            roomNavigator.CurrentRoom;

        if (!IsShopRoom(
                currentRoom
            ))
        {
            HideImmediately();
            return;
        }

        if (shopRoomState == null ||
            itemCatalog == null)
        {
            HideImmediately();
            return;
        }

        if (!shopRoomState.HasInventory(
                currentRoom.RoomId
            ))
        {
            Debug.LogWarning(
                "ShopPanelPresenter: " +
                "현재 상점방의 재고 상태가 아직 생성되지 않았습니다. " +
                $"RoomId={currentRoom.RoomId}",
                this
            );

            HideImmediately();
            return;
        }

        SetPanelActive(
            true
        );

        RefreshInventory();
        ResetScrollToTop();

        if (showDebugLog)
        {
            Debug.Log(
                "ShopPanelPresenter: " +
                "상점 패널 표시 완료. " +
                $"RoomId={currentRoom.RoomId}",
                this
            );
        }
    }

    private void RefreshInventory()
    {
        if (roomNavigator == null ||
            roomNavigator.CurrentRoom == null ||
            shopRoomState == null ||
            itemCatalog == null)
        {
            return;
        }

        RoomNode currentRoom =
            roomNavigator.CurrentRoom;

        if (!IsShopRoom(
                currentRoom
            ))
        {
            HideImmediately();
            return;
        }

        if (slots == null)
        {
            return;
        }

        for (int inventorySlotIndex = 0;
             inventorySlotIndex <
             ShopRoomState.TotalSlotCount;
             inventorySlotIndex++)
        {
            if (inventorySlotIndex >=
                slots.Length)
            {
                break;
            }

            ShopItemSlotPresenter slot =
                slots[
                    inventorySlotIndex
                ];

            if (slot == null)
            {
                continue;
            }

            string productId =
                shopRoomState.GetProductId(
                    currentRoom.RoomId,
                    inventorySlotIndex
                );

            if (string.IsNullOrWhiteSpace(
                    productId
                ))
            {
                slot.Hide();

                Debug.LogWarning(
                    "ShopPanelPresenter: " +
                    "상점 슬롯에 상품 ID가 없습니다. " +
                    $"RoomId={currentRoom.RoomId}, " +
                    $"SlotIndex={inventorySlotIndex}",
                    this
                );

                continue;
            }

            if (!itemCatalog.TryGetItem(
                    productId,
                    out ShopItemDefinition item
                ) ||
                item == null)
            {
                slot.Hide();

                Debug.LogWarning(
                    "ShopPanelPresenter: " +
                    "상품 정의를 찾지 못했습니다. " +
                    $"RoomId={currentRoom.RoomId}, " +
                    $"SlotIndex={inventorySlotIndex}, " +
                    $"ProductId={productId}",
                    this
                );

                continue;
            }

            bool soldOut =
                shopRoomState.IsSlotPurchased(
                    currentRoom.RoomId,
                    inventorySlotIndex
                );

            int displayedPrice =
                purchaseController != null
                    ? purchaseController.GetCurrentPrice(
                        currentRoom.RoomId,
                        inventorySlotIndex,
                        item
                    )
                    : item.BaseGoldPrice;

            slot.Show(
                item,
                inventorySlotIndex,
                soldOut,
                displayedPrice
            );
        }

        HideUnusedSlots();
    }

    private void HideUnusedSlots()
    {
        if (slots == null)
        {
            return;
        }

        for (int i =
                 ShopRoomState.TotalSlotCount;
             i < slots.Length;
             i++)
        {
            if (slots[i] == null)
            {
                continue;
            }

            slots[i].Hide();
        }
    }

    private void HandleSlotClicked(
        ShopItemSlotPresenter slot)
    {
        if (slot == null ||
            slot.CurrentItem == null)
        {
            return;
        }

        if (purchaseController == null)
        {
            Debug.LogError(
                "ShopPanelPresenter: " +
                "ShopPurchaseController가 연결되지 않아 " +
                "구매할 수 없습니다.",
                this
            );

            return;
        }

        purchaseController.TryPurchase(
            slot.CurrentInventorySlotIndex,
            slot.CurrentItem
        );
    }

    private bool IsCurrentShopRoom(
        int roomId)
    {
        if (roomNavigator == null ||
            roomNavigator.CurrentRoom == null)
        {
            return false;
        }

        RoomNode currentRoom =
            roomNavigator.CurrentRoom;

        return IsShopRoom(
                   currentRoom
               ) &&
               currentRoom.RoomId ==
               roomId;
    }

    private static bool IsShopRoom(
        RoomNode room)
    {
        return room != null &&
               room.RoomType ==
               RoomType.Shop;
    }

    private void ResetScrollToTop()
    {
        if (productScrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        productScrollRect.StopMovement();

        productScrollRect
                .verticalNormalizedPosition =
            1f;
    }

    private void HideImmediately()
    {
        StopDelayedRefresh();

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

        if (panelRoot.activeSelf != shouldActivate)
            panelRoot.SetActive(shouldActivate);

        if (shouldActivate)
            panelRoot.transform.SetAsLastSibling();
    }

    private void StopDelayedRefresh()
    {
        if (delayedRefreshCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            delayedRefreshCoroutine
        );

        delayedRefreshCoroutine =
            null;
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
                FindFirstObjectByType<
                    ShopRoomState
                >();
        }

        if (itemCatalog == null)
        {
            ShopInventoryGenerator generator =
                FindFirstObjectByType<
                    ShopInventoryGenerator
                >();

            if (generator != null)
            {
                itemCatalog =
                    generator.ItemCatalog;
            }
        }

        if (purchaseController == null)
        {
            purchaseController =
                FindFirstObjectByType<
                    ShopPurchaseController
                >();
        }

        if (productScrollRect == null &&
            panelRoot != null)
        {
            productScrollRect =
                panelRoot.GetComponentInChildren<
                    ScrollRect
                >(
                    true
                );
        }
    }

    private void ValidateReferences()
    {
        if (panelRoot == null)
        {
            Debug.LogError(
                "ShopPanelPresenter: " +
                "Panel Root가 연결되지 않았습니다.",
                this
            );
        }

        if (roomNavigator == null)
        {
            Debug.LogError(
                "ShopPanelPresenter: " +
                "StageRoomNavigator가 연결되지 않았습니다.",
                this
            );
        }

        if (shopRoomState == null)
        {
            Debug.LogError(
                "ShopPanelPresenter: " +
                "ShopRoomState가 연결되지 않았습니다.",
                this
            );
        }

        if (itemCatalog == null)
        {
            Debug.LogError(
                "ShopPanelPresenter: " +
                "ShopItemCatalog가 연결되지 않았습니다.",
                this
            );
        }

        if (purchaseController == null)
        {
            Debug.LogError(
                "ShopPanelPresenter: " +
                "ShopPurchaseController가 연결되지 않았습니다.",
                this
            );
        }

        if (slots == null ||
            slots.Length !=
            ShopRoomState.TotalSlotCount)
        {
            Debug.LogError(
                "ShopPanelPresenter: " +
                "상품 슬롯 배열은 정확히 " +
                $"{ShopRoomState.TotalSlotCount}개가 필요합니다.",
                this
            );
        }

        if (productScrollRect == null)
        {
            Debug.LogWarning(
                "ShopPanelPresenter: " +
                "Product Scroll Rect가 연결되지 않았습니다.",
                this
            );
        }
    }
}
