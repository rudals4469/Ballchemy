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

    [SerializeField]
    private SecretRoomKeyState secretRoomKeyState;

    [SerializeField]
    private SecretRoomState secretRoomState;

    [SerializeField]
    private StageMapRevealState stageMapRevealState;

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

            case ShopItemEffectType.ReduceEnemyMaxHealth:
                return TryPurchaseReduceEnemyMaxHealth(
                    roomId,
                    inventorySlotIndex,
                    item
                );

            case ShopItemEffectType.ReduceEnemyAttackDamage:
                return TryPurchaseReduceEnemyAttackDamage(
                    roomId,
                    inventorySlotIndex,
                    item
                );

            case ShopItemEffectType.IncreaseEnemyAttackInterval:
                return TryPurchaseIncreaseEnemyAttackInterval(
                    roomId,
                    inventorySlotIndex,
                    item
                );

            case ShopItemEffectType.IncreaseGoldGain:
                return TryPurchaseIncreaseGoldGain(
                    roomId,
                    inventorySlotIndex,
                    item
                );

            case ShopItemEffectType.GrantSecretRoomKey:
                return TryPurchaseSecretRoomKey(
                    roomId,
                    inventorySlotIndex,
                    item
                );

            case ShopItemEffectType.RevealEntireStageMap:
                return TryPurchaseRevealEntireStageMap(
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

    private bool TryPurchaseSecretRoomKey(
        int roomId,
        int inventorySlotIndex,
        ShopItemDefinition item)
    {
        if (item == null)
        {
            return Fail(
                "비밀방 열쇠 상품 정보가 없습니다."
            );
        }

        if (item.Category !=
            ShopItemCategory.Special)
        {
            return Fail(
                "GrantSecretRoomKey 효과의 상품 카테고리가 " +
                "Special이 아닙니다."
            );
        }

        if (currencyState == null ||
            shopRoomState == null ||
            secretRoomKeyState == null ||
            secretRoomState == null)
        {
            return Fail(
                "비밀방 열쇠 구매에 필요한 런타임 참조가 없습니다."
            );
        }

        if (!secretRoomState.HasSecretRoom)
        {
            return Fail(
                "현재 스테이지에 생성된 비밀방이 없습니다."
            );
        }

        if (secretRoomState.IsSecretRoomUnlocked)
        {
            return Fail(
                "현재 스테이지의 비밀방이 이미 개방되어 있습니다."
            );
        }

        if (secretRoomKeyState.HasKey)
        {
            return Fail(
                "이미 비밀문 공명석을 보유하고 있습니다."
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

        bool keyAcquired =
            secretRoomKeyState.AcquireKey();

        if (!keyAcquired)
        {
            currencyState.TryAddGold(
                price
            );

            return Fail(
                "비밀문 공명석 지급에 실패하여 " +
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
            bool keyRolledBack =
                secretRoomKeyState.TryConsumeKey();

            currencyState.TryAddGold(
                price
            );

            if (!keyRolledBack)
            {
                Debug.LogError(
                    "ShopPurchaseController: " +
                    "비밀방 열쇠 구매 상태 기록 실패 후 " +
                    "열쇠 롤백에도 실패했습니다.",
                    this
                );
            }

            return Fail(
                "상품 구매 상태 기록에 실패하여 " +
                "열쇠와 골드를 되돌렸습니다."
            );
        }

        LogPurchaseSuccess(
            roomId,
            inventorySlotIndex,
            item,
            price,
            "SecretRoomKeyAcquired=True"
        );

        NotifyPurchaseSucceeded(
            roomId,
            inventorySlotIndex,
            item
        );

        return true;
    }

    private bool TryPurchaseRevealEntireStageMap(
        int roomId,
        int inventorySlotIndex,
        ShopItemDefinition item)
    {
        if (item == null)
        {
            return Fail(
                "전체 지도 공개 상품 정보가 없습니다."
            );
        }

        if (item.Category !=
            ShopItemCategory.Special)
        {
            return Fail(
                "RevealEntireStageMap 효과의 상품 카테고리가 " +
                "Special이 아닙니다."
            );
        }

        if (currencyState == null ||
            shopRoomState == null ||
            stageMapRevealState == null)
        {
            return Fail(
                "전체 지도 공개 구매에 필요한 런타임 참조가 없습니다."
            );
        }

        if (stageMapRevealState
                .IsEntireStageMapRevealed)
        {
            return Fail(
                "현재 스테이지의 지도가 이미 모두 공개되어 있습니다."
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

        bool mapRevealed =
            stageMapRevealState
                .RevealEntireStageMap();

        if (!mapRevealed)
        {
            currencyState.TryAddGold(
                price
            );

            return Fail(
                "전체 지도 공개 적용에 실패하여 " +
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
            stageMapRevealState.Clear();

            currencyState.TryAddGold(
                price
            );

            return Fail(
                "상품 구매 상태 기록에 실패하여 " +
                "지도 공개와 골드를 되돌렸습니다."
            );
        }

        LogPurchaseSuccess(
            roomId,
            inventorySlotIndex,
            item,
            price,
            "EntireStageMapRevealed=True"
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
        float ratio =
            item.RatioValue;

        return TryPurchaseRatioStageBuff(
            roomId,
            inventorySlotIndex,
            item,
            ratio,
            "직접 피해 증가율",
            () =>
                stageModifierState
                    .TryAddDirectDamageIncreaseRatio(
                        ratio
                    ),
            () =>
                stageModifierState
                    .TryRemoveDirectDamageIncreaseRatio(
                        ratio
                    ),
            $"DirectDamageIncrease={ratio:P0}"
        );
    }

    private bool TryPurchaseReduceEnemyMaxHealth(
        int roomId,
        int inventorySlotIndex,
        ShopItemDefinition item)
    {
        float ratio =
            item.RatioValue;

        return TryPurchaseRatioStageBuff(
            roomId,
            inventorySlotIndex,
            item,
            ratio,
            "적 최대 체력 감소율",
            () =>
                stageModifierState
                    .TryAddEnemyMaxHealthReductionRatio(
                        ratio
                    ),
            () =>
                stageModifierState
                    .TryRemoveEnemyMaxHealthReductionRatio(
                        ratio
                    ),
            $"EnemyMaxHealthReduction={ratio:P0}"
        );
    }

    private bool TryPurchaseReduceEnemyAttackDamage(
        int roomId,
        int inventorySlotIndex,
        ShopItemDefinition item)
    {
        float ratio =
            item.RatioValue;

        return TryPurchaseRatioStageBuff(
            roomId,
            inventorySlotIndex,
            item,
            ratio,
            "적 공격력 감소율",
            () =>
                stageModifierState
                    .TryAddEnemyAttackDamageReductionRatio(
                        ratio
                    ),
            () =>
                stageModifierState
                    .TryRemoveEnemyAttackDamageReductionRatio(
                        ratio
                    ),
            $"EnemyAttackDamageReduction={ratio:P0}"
        );
    }

    private bool TryPurchaseIncreaseGoldGain(
        int roomId,
        int inventorySlotIndex,
        ShopItemDefinition item)
    {
        float ratio =
            item.RatioValue;

        return TryPurchaseRatioStageBuff(
            roomId,
            inventorySlotIndex,
            item,
            ratio,
            "골드 획득 증가율",
            () =>
                stageModifierState
                    .TryAddGoldGainIncreaseRatio(
                        ratio
                    ),
            () =>
                stageModifierState
                    .TryRemoveGoldGainIncreaseRatio(
                        ratio
                    ),
            $"GoldGainIncrease={ratio:P0}"
        );
    }

    private bool TryPurchaseIncreaseEnemyAttackInterval(
        int roomId,
        int inventorySlotIndex,
        ShopItemDefinition item)
    {
        if (!ValidateStageBuffPurchase(
                roomId,
                inventorySlotIndex,
                item,
                out int price
            ))
        {
            return false;
        }

        int bonusTurns =
            item.IntegerValue;

        if (bonusTurns <= 0)
        {
            return Fail(
                "적 공격 주기 증가 턴이 0 이하입니다. " +
                $"ItemId={item.ItemId}, " +
                $"IntegerValue={bonusTurns}"
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

        bool applied =
            stageModifierState
                .TryAddEnemyAttackIntervalBonusTurns(
                    bonusTurns
                );

        if (!applied)
        {
            currencyState.TryAddGold(
                price
            );

            return Fail(
                "적 공격 주기 증가 효과 적용에 실패하여 " +
                "구매를 취소했습니다."
            );
        }

        if (!TryRecordStageBuffPurchase(
                roomId,
                inventorySlotIndex,
                price,
                () =>
                    stageModifierState
                        .TryRemoveEnemyAttackIntervalBonusTurns(
                            bonusTurns
                        )
            ))
        {
            return false;
        }

        LogPurchaseSuccess(
            roomId,
            inventorySlotIndex,
            item,
            price,
            $"EnemyAttackIntervalBonus={bonusTurns}턴"
        );

        NotifyPurchaseSucceeded(
            roomId,
            inventorySlotIndex,
            item
        );

        return true;
    }

    private bool TryPurchaseRatioStageBuff(
        int roomId,
        int inventorySlotIndex,
        ShopItemDefinition item,
        float ratio,
        string ratioDescription,
        Func<bool> applyModifier,
        Func<bool> rollbackModifier,
        string successDescription)
    {
        if (!ValidateStageBuffPurchase(
                roomId,
                inventorySlotIndex,
                item,
                out int price
            ))
        {
            return false;
        }

        if (ratio <= 0f)
        {
            return Fail(
                $"{ratioDescription}이 0 이하입니다. " +
                $"ItemId={item.ItemId}, " +
                $"RatioValue={ratio}"
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

        bool applied =
            applyModifier != null &&
            applyModifier.Invoke();

        if (!applied)
        {
            currencyState.TryAddGold(
                price
            );

            return Fail(
                $"{ratioDescription} 효과 적용에 실패하여 " +
                "구매를 취소했습니다."
            );
        }

        if (!TryRecordStageBuffPurchase(
                roomId,
                inventorySlotIndex,
                price,
                rollbackModifier
            ))
        {
            return false;
        }

        LogPurchaseSuccess(
            roomId,
            inventorySlotIndex,
            item,
            price,
            successDescription
        );

        NotifyPurchaseSucceeded(
            roomId,
            inventorySlotIndex,
            item
        );

        return true;
    }

    private bool TryRecordStageBuffPurchase(
        int roomId,
        int inventorySlotIndex,
        int price,
        Func<bool> rollbackModifier)
    {
        bool recorded =
            shopRoomState.TryMarkSlotPurchased(
                roomId,
                inventorySlotIndex
            );

        if (recorded)
        {
            return true;
        }

        rollbackModifier?.Invoke();

        currencyState.TryAddGold(
            price
        );

        return Fail(
            "상품 구매 상태 기록에 실패하여 " +
            "효과와 골드를 되돌렸습니다."
        );
    }

    private bool ValidateStageBuffPurchase(
        int roomId,
        int inventorySlotIndex,
        ShopItemDefinition item,
        out int price)
    {
        price =
            0;

        if (item == null)
        {
            return Fail(
                "스테이지 버프 상품 정보가 없습니다."
            );
        }

        if (item.Category !=
            ShopItemCategory.StageBuff)
        {
            return Fail(
                $"{item.EffectType} 효과의 상품 카테고리가 " +
                "StageBuff가 아닙니다."
            );
        }

        if (currencyState == null ||
            shopRoomState == null ||
            stageModifierState == null)
        {
            return Fail(
                "스테이지 버프 구매에 필요한 " +
                "런타임 참조가 없습니다."
            );
        }

        price =
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

        if (secretRoomKeyState == null)
        {
            secretRoomKeyState =
                FindFirstObjectByType<
                    SecretRoomKeyState
                >();
        }

        if (secretRoomState == null)
        {
            secretRoomState =
                FindFirstObjectByType<
                    SecretRoomState
                >();
        }

        if (stageMapRevealState == null)
        {
            stageMapRevealState =
                FindFirstObjectByType<
                    StageMapRevealState
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

        if (secretRoomKeyState == null)
        {
            Debug.LogError(
                "ShopPurchaseController: " +
                "SecretRoomKeyState가 연결되지 않았습니다. " +
                "비밀문 공명석 구매 효과를 적용할 수 없습니다.",
                this
            );
        }

        if (secretRoomState == null)
        {
            Debug.LogError(
                "ShopPurchaseController: " +
                "SecretRoomState가 연결되지 않았습니다. " +
                "비밀방 개방 상태를 확인할 수 없습니다.",
                this
            );
        }

        if (stageMapRevealState == null)
        {
            Debug.LogError(
                "ShopPurchaseController: " +
                "StageMapRevealState가 연결되지 않았습니다. " +
                "전체 지도 공개 효과를 적용할 수 없습니다.",
                this
            );
        }
    }
}