using UnityEngine;

[DisallowMultipleComponent]
public sealed class RefreshCurrentShopInventoryShopEffectHandler :
    MonoBehaviour,
    IShopItemEffectHandler
{
    [Header("References")]

    [SerializeField]
    private ShopInventoryGenerator
        inventoryGenerator;

    [SerializeField]
    private ShopRoomState
        shopRoomState;

    private int lastAppliedRoomId =
        -1;

    private int lastAppliedSlotIndex =
        -1;

    private string[] lastProductIds;

    public ShopItemEffectType EffectType =>
        ShopItemEffectType.RefreshCurrentShopInventory;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void OnValidate()
    {
        FindReferences();
    }

    public bool CanHandle(
        ShopItemDefinition item)
    {
        return
            item != null &&
            item.EffectType ==
            EffectType;
    }

    public ShopItemEffectApplyResult Validate(
        ShopPurchaseContext context)
    {
        if (context == null)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "재고 갱신권 구매 정보가 없습니다."
                );
        }

        ShopItemDefinition item =
            context.Item;

        if (item == null)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "재고 갱신권 상품 정보가 없습니다."
                );
        }

        if (!CanHandle(
                item
            ))
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "현재 핸들러가 처리할 수 없는 상품 효과입니다."
                );
        }

        if (item.Category !=
            ShopItemCategory.Special)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "RefreshCurrentShopInventory 효과의 " +
                    "상품 카테고리가 Special이 아닙니다."
                );
        }

        if (inventoryGenerator == null ||
            shopRoomState == null)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "재고 갱신에 필요한 참조가 연결되지 않았습니다."
                );
        }

        if (!shopRoomState.HasInventory(
                context.RoomId
            ))
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "현재 상점 재고 상태가 없습니다."
                );
        }

        bool hasRerollableSlot =
            false;

        for (int slotIndex = 0;
             slotIndex <
             ShopRoomState.TotalSlotCount;
             slotIndex++)
        {
            if (slotIndex ==
                context.InventorySlotIndex)
            {
                continue;
            }

            if (shopRoomState.IsSlotPurchased(
                    context.RoomId,
                    slotIndex
                ))
            {
                continue;
            }

            hasRerollableSlot =
                true;

            break;
        }

        if (!hasRerollableSlot)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "갱신할 미구매 상품이 없습니다."
                );
        }

        return ShopItemEffectApplyResult
            .Success();
    }

    public ShopItemEffectApplyResult Apply(
        ShopPurchaseContext context)
    {
        ShopItemEffectApplyResult validationResult =
            Validate(
                context
            );

        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }

        CaptureSnapshot(
            context
        );

        bool rerolled =
            inventoryGenerator
                .TryRerollUnpurchasedInventory(
                    context.RoomId,
                    context.InventorySlotIndex,
                    context.Item.ItemId
                );

        if (!rerolled)
        {
            ClearSnapshot();

            return ShopItemEffectApplyResult
                .Failure(
                    "상점 재고 갱신에 실패했습니다."
                );
        }

        return ShopItemEffectApplyResult
            .Success(
                "ShopInventoryRefreshed=True"
            );
    }

    public bool Rollback(
        ShopPurchaseContext context)
    {
        if (context == null ||
            shopRoomState == null ||
            lastProductIds == null ||
            lastProductIds.Length !=
            ShopRoomState.TotalSlotCount)
        {
            return false;
        }

        if (context.RoomId !=
                lastAppliedRoomId ||
            context.InventorySlotIndex !=
                lastAppliedSlotIndex)
        {
            return false;
        }

        bool allRestored =
            true;

        for (int slotIndex = 0;
             slotIndex <
             lastProductIds.Length;
             slotIndex++)
        {
            string productId =
                lastProductIds[
                    slotIndex
                ];

            if (string.IsNullOrWhiteSpace(
                    productId
                ))
            {
                continue;
            }

            allRestored &=
                shopRoomState
                    .TryReplaceProductId(
                        context.RoomId,
                        slotIndex,
                        productId
                    );
        }

        ClearSnapshot();

        return allRestored;
    }

    private void CaptureSnapshot(
        ShopPurchaseContext context)
    {
        lastAppliedRoomId =
            context.RoomId;

        lastAppliedSlotIndex =
            context.InventorySlotIndex;

        lastProductIds =
            new string[
                ShopRoomState.TotalSlotCount
            ];

        for (int slotIndex = 0;
             slotIndex <
             lastProductIds.Length;
             slotIndex++)
        {
            lastProductIds[
                slotIndex
            ] =
                shopRoomState.GetProductId(
                    context.RoomId,
                    slotIndex
                );
        }
    }

    private void ClearSnapshot()
    {
        lastAppliedRoomId =
            -1;

        lastAppliedSlotIndex =
            -1;

        lastProductIds =
            null;
    }

    private void FindReferences()
    {
        if (inventoryGenerator == null)
        {
            inventoryGenerator =
                FindFirstObjectByType<
                    ShopInventoryGenerator
                >(
                    FindObjectsInactive.Include
                );
        }

        if (shopRoomState == null)
        {
            shopRoomState =
                FindFirstObjectByType<
                    ShopRoomState
                >(
                    FindObjectsInactive.Include
                );
        }
    }

    private void ValidateReferences()
    {
        if (inventoryGenerator == null)
        {
            Debug.LogError(
                "RefreshCurrentShopInventoryShopEffectHandler: " +
                "ShopInventoryGenerator가 연결되지 않았습니다.",
                this
            );
        }

        if (shopRoomState == null)
        {
            Debug.LogError(
                "RefreshCurrentShopInventoryShopEffectHandler: " +
                "ShopRoomState가 연결되지 않았습니다.",
                this
            );
        }
    }
}