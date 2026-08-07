using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShopInventoryGenerator :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private ShopItemCatalog itemCatalog;

    [SerializeField]
    private ShopRoomState shopRoomState;

    [Header("Random")]

    [Tooltip(
        "체크하면 UnityEngine.Random을 사용합니다."
    )]
    [SerializeField]
    private bool useRandomSelection =
        true;

    [Tooltip(
        "랜덤 선택을 끄면 이 시드로 고정 결과를 생성합니다."
    )]
    [SerializeField]
    private int fixedSeed =
        12345;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog =
        true;

    public ShopItemCatalog ItemCatalog =>
        itemCatalog;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void OnValidate()
    {
        FindReferences();
    }

    public bool TryGenerateInventory(
        int roomId)
    {
        if (roomId < 0 ||
            itemCatalog == null ||
            shopRoomState == null)
        {
            return false;
        }

        if (!shopRoomState.TryGetInventory(
                roomId,
                out ShopInventoryState inventory
            ))
        {
            Debug.LogError(
                "ShopInventoryGenerator: " +
                "재고 상태가 생성되지 않았습니다. " +
                $"RoomId={roomId}",
                this
            );

            return false;
        }

        if (HasGeneratedProducts(
                inventory
            ))
        {
            if (showDebugLog)
            {
                Debug.Log(
                    "ShopInventoryGenerator: " +
                    "기존 상품 구성을 유지합니다. " +
                    $"RoomId={roomId}",
                    this
                );
            }

            return true;
        }

        if (!itemCatalog.ValidateCatalog(
                out string catalogError
            ))
        {
            Debug.LogError(
                "ShopInventoryGenerator: " +
                catalogError,
                itemCatalog
            );

            return false;
        }

        Random.State previousRandomState =
            Random.state;

        if (!useRandomSelection)
        {
            Random.InitState(
                fixedSeed +
                roomId
            );
        }

        bool succeeded =
            TryFillInventory(
                roomId
            );

        if (!useRandomSelection)
        {
            Random.state =
                previousRandomState;
        }

        return succeeded;
    }

    public bool TryRerollUnpurchasedInventory(
        int roomId,
        int excludedSlotIndex,
        string excludedProductId)
    {
        if (roomId < 0 ||
            excludedSlotIndex < 0 ||
            excludedSlotIndex >=
            ShopRoomState.TotalSlotCount ||
            itemCatalog == null ||
            shopRoomState == null)
        {
            return false;
        }

        if (!shopRoomState.TryGetInventory(
                roomId,
                out ShopInventoryState inventory
            ) ||
            inventory == null)
        {
            return false;
        }

        Random.State previousRandomState =
            Random.state;

        if (!useRandomSelection)
        {
            Random.InitState(
                fixedSeed +
                roomId +
                100000
            );
        }

        bool succeeded =
            TryBuildAndApplyReroll(
                roomId,
                inventory,
                excludedSlotIndex,
                excludedProductId
            );

        if (!useRandomSelection)
        {
            Random.state =
                previousRandomState;
        }

        return succeeded;
    }

    private bool TryFillInventory(
        int roomId)
    {
        ShopItemDefinition healingItem =
            SelectSingleItem(
                ShopItemCategory.Healing
            );

        List<ShopItemDefinition> stageBuffs =
            SelectItems(
                ShopItemCategory.StageBuff,
                2
            );

        List<ShopItemDefinition> specialItems =
            SelectItems(
                ShopItemCategory.Special,
                3
            );

        if (healingItem == null ||
            stageBuffs.Count < 2 ||
            specialItems.Count < 3)
        {
            Debug.LogError(
                "ShopInventoryGenerator: " +
                "필요한 수량만큼 상품을 추첨하지 못했습니다.",
                this
            );

            return false;
        }

        bool allStored =
            true;

        allStored &=
            StoreProduct(
                roomId,
                ShopRoomState.HealingSlotIndex,
                healingItem
            );

        allStored &=
            StoreProduct(
                roomId,
                ShopRoomState.FirstStageBuffSlotIndex,
                stageBuffs[0]
            );

        allStored &=
            StoreProduct(
                roomId,
                ShopRoomState.SecondStageBuffSlotIndex,
                stageBuffs[1]
            );

        allStored &=
            StoreProduct(
                roomId,
                ShopRoomState.FirstSpecialSlotIndex,
                specialItems[0]
            );

        allStored &=
            StoreProduct(
                roomId,
                ShopRoomState.SecondSpecialSlotIndex,
                specialItems[1]
            );

        allStored &=
            StoreProduct(
                roomId,
                ShopRoomState.ThirdSpecialSlotIndex,
                specialItems[2]
            );

        if (!allStored)
        {
            Debug.LogError(
                "ShopInventoryGenerator: " +
                "일부 상품 ID를 재고에 저장하지 못했습니다.",
                this
            );

            return false;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "ShopInventoryGenerator: " +
                $"상점 상품 6개 생성 완료. RoomId={roomId}\n" +
                $"치료={healingItem.ItemId}\n" +
                $"버프={stageBuffs[0].ItemId}, " +
                $"{stageBuffs[1].ItemId}\n" +
                $"특수={specialItems[0].ItemId}, " +
                $"{specialItems[1].ItemId}, " +
                $"{specialItems[2].ItemId}",
                this
            );
        }

        return true;
    }

    private bool TryBuildAndApplyReroll(
        int roomId,
        ShopInventoryState inventory,
        int excludedSlotIndex,
        string excludedProductId)
    {
        Dictionary<
            int,
            ShopItemDefinition
        > replacements =
            new Dictionary<
                int,
                ShopItemDefinition
            >();

        HashSet<string> reservedProductIds =
            new HashSet<string>();

        /*
         * 갱신하지 않을 슬롯의 상품은
         * 중복 방지 대상으로 먼저 예약합니다.
         */
        for (int slotIndex = 0;
             slotIndex < inventory.SlotCount;
             slotIndex++)
        {
            bool shouldKeep =
                slotIndex ==
                excludedSlotIndex ||
                inventory.IsSlotPurchased(
                    slotIndex
                );

            if (!shouldKeep)
            {
                continue;
            }

            AddReservedProductId(
                inventory.GetProductId(
                    slotIndex
                ),
                reservedProductIds
            );
        }

        for (int slotIndex = 0;
             slotIndex < inventory.SlotCount;
             slotIndex++)
        {
            if (slotIndex ==
                excludedSlotIndex)
            {
                continue;
            }

            if (inventory.IsSlotPurchased(
                    slotIndex
                ))
            {
                continue;
            }

            ShopItemCategory category =
                GetCategoryForSlot(
                    slotIndex
                );

            string currentProductId =
                inventory.GetProductId(
                    slotIndex
                );

            List<ShopItemDefinition> candidates =
                BuildRerollCandidates(
                    category,
                    reservedProductIds,
                    excludedProductId
                );

            /*
             * 가능하면 기존 상품과 같은 상품이
             * 다시 뽑히지 않도록 합니다.
             *
             * 단 해당 카테고리에 대체 후보가 없다면
             * 기존 상품도 허용합니다.
             */
            if (candidates.Count > 1)
            {
                candidates.RemoveAll(
                    item =>
                        item != null &&
                        item.ItemId ==
                        currentProductId
                );
            }

            if (candidates.Count <= 0)
            {
                candidates =
                    BuildRerollCandidates(
                        category,
                        reservedProductIds,
                        excludedProductId
                    );
            }

            ShopItemDefinition selected =
                SelectWeightedItem(
                    candidates
                );

            if (selected == null)
            {
                Debug.LogError(
                    "ShopInventoryGenerator: " +
                    "재고 갱신 상품 추첨에 실패했습니다. " +
                    $"RoomId={roomId}, " +
                    $"SlotIndex={slotIndex}",
                    this
                );

                return false;
            }

            replacements.Add(
                slotIndex,
                selected
            );

            if (!selected.AllowDuplicateInSameShop)
            {
                reservedProductIds.Add(
                    selected.ItemId
                );
            }
        }

        if (replacements.Count <= 0)
        {
            return false;
        }

        Dictionary<int, string>
            previousProductIds =
                new Dictionary<int, string>();

        foreach (
            KeyValuePair<
                int,
                ShopItemDefinition
            > pair in replacements)
        {
            previousProductIds.Add(
                pair.Key,
                inventory.GetProductId(
                    pair.Key
                )
            );

            bool replaced =
                shopRoomState.TryReplaceProductId(
                    roomId,
                    pair.Key,
                    pair.Value.ItemId
                );

            if (replaced)
            {
                continue;
            }

            foreach (
                KeyValuePair<
                    int,
                    string
                > previousPair
                in previousProductIds)
            {
                if (string.IsNullOrWhiteSpace(
                        previousPair.Value
                    ))
                {
                    continue;
                }

                shopRoomState.TryReplaceProductId(
                    roomId,
                    previousPair.Key,
                    previousPair.Value
                );
            }

            return false;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "ShopInventoryGenerator: " +
                "미구매 상품 재고 갱신 완료. " +
                $"RoomId={roomId}, " +
                $"ChangedSlots={replacements.Count}",
                this
            );
        }

        return true;
    }

    private List<ShopItemDefinition>
        BuildRerollCandidates(
            ShopItemCategory category,
            HashSet<string> reservedProductIds,
            string excludedProductId)
    {
        List<ShopItemDefinition> source =
            itemCatalog.GetSelectableItems(
                category
            );

        List<ShopItemDefinition> results =
            new List<ShopItemDefinition>();

        for (int i = 0;
             i < source.Count;
             i++)
        {
            ShopItemDefinition candidate =
                source[i];

            if (candidate == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(
                    excludedProductId
                ) &&
                candidate.ItemId ==
                excludedProductId)
            {
                continue;
            }

            if (!candidate.AllowDuplicateInSameShop &&
                reservedProductIds.Contains(
                    candidate.ItemId
                ))
            {
                continue;
            }

            results.Add(
                candidate
            );
        }

        return results;
    }

    private void AddReservedProductId(
        string productId,
        HashSet<string> reservedProductIds)
    {
        if (string.IsNullOrWhiteSpace(
                productId
            ) ||
            reservedProductIds == null ||
            itemCatalog == null)
        {
            return;
        }

        if (!itemCatalog.TryGetItem(
                productId,
                out ShopItemDefinition item
            ) ||
            item == null)
        {
            return;
        }

        if (item.AllowDuplicateInSameShop)
        {
            return;
        }

        reservedProductIds.Add(
            item.ItemId
        );
    }

    private static ShopItemCategory
        GetCategoryForSlot(
            int slotIndex)
    {
        if (slotIndex ==
            ShopRoomState.HealingSlotIndex)
        {
            return ShopItemCategory.Healing;
        }

        if (slotIndex ==
                ShopRoomState.FirstStageBuffSlotIndex ||
            slotIndex ==
                ShopRoomState.SecondStageBuffSlotIndex)
        {
            return ShopItemCategory.StageBuff;
        }

        return ShopItemCategory.Special;
    }

    private ShopItemDefinition SelectSingleItem(
        ShopItemCategory category)
    {
        List<ShopItemDefinition> candidates =
            itemCatalog.GetSelectableItems(
                category
            );

        return SelectWeightedItem(
            candidates
        );
    }

    private List<ShopItemDefinition> SelectItems(
        ShopItemCategory category,
        int count)
    {
        List<ShopItemDefinition> results =
            new List<ShopItemDefinition>();

        List<ShopItemDefinition> candidates =
            itemCatalog.GetSelectableItems(
                category
            );

        while (results.Count < count &&
               candidates.Count > 0)
        {
            ShopItemDefinition selected =
                SelectWeightedItem(
                    candidates
                );

            if (selected == null)
            {
                break;
            }

            results.Add(
                selected
            );

            if (!selected.AllowDuplicateInSameShop)
            {
                candidates.Remove(
                    selected
                );
            }
        }

        return results;
    }

    private static ShopItemDefinition
        SelectWeightedItem(
            List<ShopItemDefinition> candidates)
    {
        if (candidates == null ||
            candidates.Count <= 0)
        {
            return null;
        }

        int totalWeight =
            0;

        for (int i = 0;
             i < candidates.Count;
             i++)
        {
            ShopItemDefinition candidate =
                candidates[i];

            if (candidate == null)
            {
                continue;
            }

            totalWeight +=
                Mathf.Max(
                    candidate.SelectionWeight,
                    0
                );
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        int roll =
            Random.Range(
                0,
                totalWeight
            );

        int accumulatedWeight =
            0;

        for (int i = 0;
             i < candidates.Count;
             i++)
        {
            ShopItemDefinition candidate =
                candidates[i];

            if (candidate == null)
            {
                continue;
            }

            accumulatedWeight +=
                Mathf.Max(
                    candidate.SelectionWeight,
                    0
                );

            if (roll <
                accumulatedWeight)
            {
                return candidate;
            }
        }

        return candidates[
            candidates.Count - 1
        ];
    }

    private bool StoreProduct(
        int roomId,
        int slotIndex,
        ShopItemDefinition item)
    {
        if (item == null)
        {
            return false;
        }

        return shopRoomState.TrySetProductId(
            roomId,
            slotIndex,
            item.ItemId
        );
    }

    private static bool HasGeneratedProducts(
        ShopInventoryState inventory)
    {
        if (inventory == null)
        {
            return false;
        }

        for (int i = 0;
             i < inventory.SlotCount;
             i++)
        {
            if (string.IsNullOrWhiteSpace(
                    inventory.GetProductId(
                        i
                    )
                ))
            {
                return false;
            }
        }

        return true;
    }

    private void FindReferences()
    {
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
    }

    private void ValidateReferences()
    {
        if (itemCatalog == null)
        {
            Debug.LogError(
                "ShopInventoryGenerator: " +
                "ShopItemCatalog가 연결되지 않았습니다.",
                this
            );
        }

        if (shopRoomState == null)
        {
            Debug.LogError(
                "ShopInventoryGenerator: " +
                "ShopRoomState가 연결되지 않았습니다.",
                this
            );
        }
    }
}