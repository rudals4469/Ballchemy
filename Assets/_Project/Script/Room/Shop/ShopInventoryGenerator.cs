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