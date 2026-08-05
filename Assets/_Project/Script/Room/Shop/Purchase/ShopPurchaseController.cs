using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShopPurchaseController :
    MonoBehaviour
{
    [Header("Runtime References")]

    [SerializeField]
    private StageRoomNavigator roomNavigator;

    [SerializeField]
    private ShopRoomState shopRoomState;

    [SerializeField]
    private RunCurrencyState currencyState;

    [SerializeField]
    private PlayerHealth playerHealth;

    [SerializeField]
    private StageModifierState stageModifierState;

    [Header("Healing Price")]

    [Tooltip(
        "같은 상점에서 치료를 구매할 때마다 추가되는 가격입니다.\n" +
        "실제 가격 = 기본 가격 + 구매 횟수 × 증가 가격"
    )]
    [SerializeField, Min(0)]
    private int healingPriceIncreasePerPurchase =
        5;

    [Header("Healing")]

    [Tooltip(
        "치료 상품의 Ratio Value가 0 이하일 때 사용할 기본 회복 비율입니다.\n" +
        "0.25는 최대 체력의 25%입니다."
    )]
    [SerializeField, Min(0f)]
    private float fallbackHealingRatio =
        0.25f;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog =
        true;

    public event Action<
        int,
        int,
        ShopItemDefinition
    > PurchaseSucceeded;

    public event Action<string>
        PurchaseFailed;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void OnValidate()
    {
        healingPriceIncreasePerPurchase =
            Mathf.Max(
                healingPriceIncreasePerPurchase,
                0
            );

        fallbackHealingRatio =
            Mathf.Max(
                fallbackHealingRatio,
                0f
            );

        FindReferences();
    }

    public int GetCurrentPrice(
        int roomId,
        int inventorySlotIndex,
        ShopItemDefinition item)
    {
        if (item == null)
        {
            return 0;
        }

        int basePrice =
            Mathf.Max(
                item.BaseGoldPrice,
                0
            );

        if (item.Category !=
            ShopItemCategory.Healing)
        {
            return basePrice;
        }

        int purchaseCount =
            shopRoomState != null
                ? shopRoomState.GetHealingPurchaseCount(
                    roomId
                )
                : 0;

        return basePrice +
               purchaseCount *
               healingPriceIncreasePerPurchase;
    }

    public bool TryPurchase(
        int inventorySlotIndex,
        ShopItemDefinition item)
    {
        if (!TryGetCurrentShopRoom(
                out RoomNode currentRoom
            ))
        {
            return Fail(
                "현재 상점방에 있지 않습니다."
            );
        }

        if (item == null)
        {
            return Fail(
                "구매할 상품 정보가 없습니다."
            );
        }

        int roomId =
            currentRoom.RoomId;

        if (!IsValidInventorySlot(
                inventorySlotIndex
            ))
        {
            return Fail(
                $"잘못된 상점 슬롯입니다. " +
                $"SlotIndex={inventorySlotIndex}"
            );
        }

        if (shopRoomState == null)
        {
            return Fail(
                "상점 상태 참조가 없습니다."
            );
        }

        if (!shopRoomState.HasInventory(
                roomId
            ))
        {
            return Fail(
                $"현재 상점방의 재고 상태가 없습니다. " +
                $"RoomId={roomId}"
            );
        }

        string storedProductId =
            shopRoomState.GetProductId(
                roomId,
                inventorySlotIndex
            );

        if (!string.Equals(
                storedProductId,
                item.ItemId,
                StringComparison.Ordinal
            ))
        {
            return Fail(
                "선택한 상품과 상점 재고 정보가 일치하지 않습니다."
            );
        }

        if (!item.IsRepeatable &&
            shopRoomState.IsSlotPurchased(
                roomId,
                inventorySlotIndex
            ))
        {
            return Fail(
                "이미 구매한 상품입니다."
            );
        }

        switch (item.EffectType)
        {
            case ShopItemEffectType.RecoverHealth:
                return TryPurchaseHealing(
                    roomId,
                    inventorySlotIndex,
                    item
                );

            case ShopItemEffectType.IncreaseDirectDamage:
                return TryPurchaseIncreaseDirectDamage(
                    roomId,
                    inventorySlotIndex,
                    item
                );

            default:
                return Fail(
                    $"아직 구매 효과가 구현되지 않은 상품입니다. " +
                    $"ItemId={item.ItemId}, " +
                    $"EffectType={item.EffectType}"
                );
        }
    }

    private bool TryPurchaseHealing(
        int roomId,
        int inventorySlotIndex,
        ShopItemDefinition item)
    {
        if (item.Category !=
            ShopItemCategory.Healing)
        {
            return Fail(
                "RecoverHealth 효과의 상품 카테고리가 Healing이 아닙니다."
            );
        }

        if (playerHealth == null ||
            currencyState == null ||
            shopRoomState == null)
        {
            return Fail(
                "치료 구매에 필요한 런타임 참조가 없습니다."
            );
        }

        if (playerHealth.IsDead)
        {
            return Fail(
                "플레이어가 사망한 상태에서는 치료할 수 없습니다."
            );
        }

        if (playerHealth.CurrentHealth >=
            playerHealth.MaxHealth)
        {
            return Fail(
                "현재 체력이 이미 최대입니다."
            );
        }

        int price =
            GetCurrentPrice(
                roomId,
                inventorySlotIndex,
                item
            );

        if (!currencyState.CanAfford(
                price
            ))
        {
            return Fail(
                $"골드가 부족합니다. " +
                $"필요={price}G, " +
                $"보유={currencyState.CurrentGold}G"
            );
        }

        int healingAmount =
            CalculateHealingAmount(
                item
            );

        if (healingAmount <= 0)
        {
            return Fail(
                "계산된 회복량이 0 이하입니다."
            );
        }

        int healthBefore =
            playerHealth.CurrentHealth;

        if (!currencyState.TrySpendGold(
                price
            ))
        {
            return Fail(
                "골드 차감에 실패했습니다."
            );
        }

        playerHealth.Heal(
            healingAmount
        );

        int appliedHealing =
            playerHealth.CurrentHealth -
            healthBefore;

        if (appliedHealing <= 0)
        {
            currencyState.TryAddGold(
                price
            );

            return Fail(
                "체력이 회복되지 않아 구매를 취소했습니다."
            );
        }

        if (!shopRoomState
                .TryIncrementHealingPurchaseCount(
                    roomId
                ))
        {
            Debug.LogError(
                "ShopPurchaseController: " +
                "치료는 적용됐지만 구매 횟수 기록에 실패했습니다. " +
                $"RoomId={roomId}",
                this
            );
        }

        LogPurchaseSuccess(
            roomId,
            inventorySlotIndex,
            item,
            price,
            $"Healing={appliedHealing}, " +
            $"Health={playerHealth.CurrentHealth}/" +
            $"{playerHealth.MaxHealth}"
        );

        NotifyPurchaseSucceeded(
            roomId,
            inventorySlotIndex,
            item
        );

        return true;
    }

    private bool TryPurchaseIncreaseDirectDamage(
        int roomId,
        int inventorySlotIndex,
        ShopItemDefinition item)
    {
        if (item.Category !=
            ShopItemCategory.StageBuff)
        {
            return Fail(
                "IncreaseDirectDamage 효과의 상품 카테고리가 " +
                "StageBuff가 아닙니다."
            );
        }

        if (currencyState == null ||
            shopRoomState == null ||
            stageModifierState == null)
        {
            return Fail(
                "직접 피해 증가 상품 구매에 필요한 " +
                "런타임 참조가 없습니다."
            );
        }

        float increaseRatio =
            item.RatioValue;

        if (increaseRatio <= 0f)
        {
            return Fail(
                "직접 피해 증가율이 0 이하입니다. " +
                $"ItemId={item.ItemId}, " +
                $"RatioValue={increaseRatio}"
            );
        }

        int price =
            GetCurrentPrice(
                roomId,
                inventorySlotIndex,
                item
            );

        if (!currencyState.CanAfford(
                price
            ))
        {
            return Fail(
                $"골드가 부족합니다. " +
                $"필요={price}G, " +
                $"보유={currencyState.CurrentGold}G"
            );
        }

        if (!currencyState.TrySpendGold(
                price
            ))
        {
            return Fail(
                "골드 차감에 실패했습니다."
            );
        }

        bool modifierApplied =
            stageModifierState
                .TryAddDirectDamageIncreaseRatio(
                    increaseRatio
                );

        if (!modifierApplied)
        {
            currencyState.TryAddGold(
                price
            );

            return Fail(
                "직접 피해 증가 효과 적용에 실패하여 " +
                "구매를 취소했습니다."
            );
        }

        bool purchaseRecorded =
            shopRoomState.TryMarkSlotPurchased(
                roomId,
                inventorySlotIndex
            );

        if (!purchaseRecorded)
        {
            stageModifierState
                .TryRemoveDirectDamageIncreaseRatio(
                    increaseRatio
                );

            currencyState.TryAddGold(
                price
            );

            return Fail(
                "상품 구매 상태 기록에 실패하여 " +
                "효과와 골드를 되돌렸습니다."
            );
        }

        LogPurchaseSuccess(
            roomId,
            inventorySlotIndex,
            item,
            price,
            $"DirectDamageIncrease=" +
            $"{increaseRatio:P0}, " +
            $"TotalIncrease=" +
            $"{stageModifierState.DirectDamageIncreaseRatio:P0}"
        );

        NotifyPurchaseSucceeded(
            roomId,
            inventorySlotIndex,
            item
        );

        return true;
    }

    private int CalculateHealingAmount(
        ShopItemDefinition item)
    {
        if (item == null ||
            playerHealth == null)
        {
            return 0;
        }

        float healingRatio =
            item.RatioValue > 0f
                ? item.RatioValue
                : fallbackHealingRatio;

        return Mathf.Max(
            Mathf.CeilToInt(
                playerHealth.MaxHealth *
                healingRatio
            ),
            1
        );
    }

    private bool TryGetCurrentShopRoom(
        out RoomNode currentRoom)
    {
        currentRoom =
            roomNavigator != null
                ? roomNavigator.CurrentRoom
                : null;

        return currentRoom != null &&
               currentRoom.RoomType ==
               RoomType.Shop;
    }

    private static bool IsValidInventorySlot(
        int inventorySlotIndex)
    {
        return inventorySlotIndex >= 0 &&
               inventorySlotIndex <
               ShopRoomState.TotalSlotCount;
    }

    private void NotifyPurchaseSucceeded(
        int roomId,
        int inventorySlotIndex,
        ShopItemDefinition item)
    {
        PurchaseSucceeded?.Invoke(
            roomId,
            inventorySlotIndex,
            item
        );
    }

    private void LogPurchaseSuccess(
        int roomId,
        int inventorySlotIndex,
        ShopItemDefinition item,
        int price,
        string effectDescription)
    {
        if (!showDebugLog)
        {
            return;
        }

        string itemId =
            item != null
                ? item.ItemId
                : "None";

        Debug.Log(
            "ShopPurchaseController: " +
            "상품 구매 완료. " +
            $"RoomId={roomId}, " +
            $"SlotIndex={inventorySlotIndex}, " +
            $"ItemId={itemId}, " +
            $"Price={price}G, " +
            effectDescription,
            this
        );
    }

    private bool Fail(
        string message)
    {
        if (showDebugLog)
        {
            Debug.LogWarning(
                "ShopPurchaseController: " +
                message,
                this
            );
        }

        PurchaseFailed?.Invoke(
            message
        );

        return false;
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

        if (currencyState == null)
        {
            currencyState =
                FindFirstObjectByType<
                    RunCurrencyState
                >();
        }

        if (playerHealth == null)
        {
            playerHealth =
                FindFirstObjectByType<
                    PlayerHealth
                >();
        }

        if (stageModifierState == null)
        {
            stageModifierState =
                FindFirstObjectByType<
                    StageModifierState
                >();
        }
    }

    private void ValidateReferences()
    {
        if (roomNavigator == null)
        {
            Debug.LogError(
                "ShopPurchaseController: " +
                "StageRoomNavigator가 연결되지 않았습니다.",
                this
            );
        }

        if (shopRoomState == null)
        {
            Debug.LogError(
                "ShopPurchaseController: " +
                "ShopRoomState가 연결되지 않았습니다.",
                this
            );
        }

        if (currencyState == null)
        {
            Debug.LogError(
                "ShopPurchaseController: " +
                "RunCurrencyState가 연결되지 않았습니다.",
                this
            );
        }

        if (playerHealth == null)
        {
            Debug.LogError(
                "ShopPurchaseController: " +
                "PlayerHealth가 연결되지 않았습니다.",
                this
            );
        }

        if (stageModifierState == null)
        {
            Debug.LogError(
                "ShopPurchaseController: " +
                "StageModifierState가 연결되지 않았습니다.",
                this
            );
        }
    }
}