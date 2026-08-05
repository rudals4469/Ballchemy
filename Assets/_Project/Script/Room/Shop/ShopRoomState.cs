using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShopRoomState :
    MonoBehaviour
{
    public const int HealingSlotIndex =
        0;

    public const int FirstStageBuffSlotIndex =
        1;

    public const int SecondStageBuffSlotIndex =
        2;

    public const int FirstSpecialSlotIndex =
        3;

    public const int SecondSpecialSlotIndex =
        4;

    public const int ThirdSpecialSlotIndex =
        5;

    public const int TotalSlotCount =
        6;

    private readonly Dictionary<
        int,
        ShopInventoryState
    > inventoriesByRoomId =
        new Dictionary<
            int,
            ShopInventoryState
        >();

    public event Action<int>
        ShopInventoryCreated;

    public event Action<
        int,
        int
    > ShopSlotPurchased;

    public event Action<
        int,
        int
    > HealingPurchaseCountChanged;

    public event Action
        StageStateReset;

    public bool HasInventory(
        int roomId)
    {
        if (roomId < 0)
        {
            return false;
        }

        return inventoriesByRoomId
            .ContainsKey(
                roomId
            );
    }

    public bool TryCreateInventory(
        int roomId)
    {
        if (roomId < 0 ||
            inventoriesByRoomId.ContainsKey(
                roomId
            ))
        {
            return false;
        }

        ShopInventoryState inventory =
            new ShopInventoryState(
                roomId,
                TotalSlotCount
            );

        inventoriesByRoomId.Add(
            roomId,
            inventory
        );

        Debug.Log(
            "ShopRoomState: " +
            $"상점 재고 상태 생성, RoomId={roomId}",
            this
        );

        ShopInventoryCreated?.Invoke(
            roomId
        );

        return true;
    }

    public bool TryGetInventory(
        int roomId,
        out ShopInventoryState inventory)
    {
        if (roomId < 0)
        {
            inventory =
                null;

            return false;
        }

        return inventoriesByRoomId.TryGetValue(
            roomId,
            out inventory
        );
    }

    public bool IsSlotPurchased(
        int roomId,
        int slotIndex)
    {
        if (!TryGetInventory(
                roomId,
                out ShopInventoryState inventory))
        {
            return false;
        }

        return inventory.IsSlotPurchased(
            slotIndex
        );
    }

    public bool TryMarkSlotPurchased(
        int roomId,
        int slotIndex)
    {
        if (!TryGetInventory(
                roomId,
                out ShopInventoryState inventory))
        {
            return false;
        }

        bool wasMarked =
            inventory.TryMarkSlotPurchased(
                slotIndex
            );

        if (!wasMarked)
        {
            return false;
        }

        Debug.Log(
            "ShopRoomState: " +
            $"상품 구매 완료, " +
            $"RoomId={roomId}, " +
            $"SlotIndex={slotIndex}",
            this
        );

        ShopSlotPurchased?.Invoke(
            roomId,
            slotIndex
        );

        return true;
    }

    public int GetHealingPurchaseCount(
        int roomId)
    {
        if (!TryGetInventory(
                roomId,
                out ShopInventoryState inventory))
        {
            return 0;
        }

        return inventory
            .HealingPurchaseCount;
    }

    public bool TryIncrementHealingPurchaseCount(
        int roomId)
    {
        if (!TryGetInventory(
                roomId,
                out ShopInventoryState inventory))
        {
            return false;
        }

        int nextCount =
            inventory.IncrementHealingPurchaseCount();

        Debug.Log(
            "ShopRoomState: " +
            $"치료 구매 횟수 증가, " +
            $"RoomId={roomId}, " +
            $"Count={nextCount}",
            this
        );

        HealingPurchaseCountChanged?.Invoke(
            roomId,
            nextCount
        );

        return true;
    }

    public bool TrySetProductId(
        int roomId,
        int slotIndex,
        string productId)
    {
        if (!TryGetInventory(
                roomId,
                out ShopInventoryState inventory))
        {
            return false;
        }

        return inventory.TrySetProductId(
            slotIndex,
            productId
        );
    }

    public string GetProductId(
        int roomId,
        int slotIndex)
    {
        if (!TryGetInventory(
                roomId,
                out ShopInventoryState inventory))
        {
            return string.Empty;
        }

        return inventory.GetProductId(
            slotIndex
        );
    }

    public void ResetForNewStage()
    {
        inventoriesByRoomId.Clear();

        Debug.Log(
            "ShopRoomState: " +
            "새 스테이지 상점 상태 초기화",
            this
        );

        StageStateReset?.Invoke();
    }
}

public sealed class ShopInventoryState
{
    private readonly bool[]
        purchasedSlots;

    private readonly string[]
        productIds;

    public int RoomId
    {
        get;
    }

    public int SlotCount =>
        purchasedSlots.Length;

    public int HealingPurchaseCount
    {
        get;
        private set;
    }

    public ShopInventoryState(
        int roomId,
        int slotCount)
    {
        RoomId =
            Mathf.Max(
                roomId,
                0
            );

        slotCount =
            Mathf.Max(
                slotCount,
                1
            );

        purchasedSlots =
            new bool[
                slotCount
            ];

        productIds =
            new string[
                slotCount
            ];

        HealingPurchaseCount =
            0;
    }

    public bool IsValidSlotIndex(
        int slotIndex)
    {
        return slotIndex >= 0 &&
               slotIndex <
               purchasedSlots.Length;
    }

    public bool IsSlotPurchased(
        int slotIndex)
    {
        if (!IsValidSlotIndex(
                slotIndex))
        {
            return false;
        }

        return purchasedSlots[
            slotIndex
        ];
    }

    public bool TryMarkSlotPurchased(
        int slotIndex)
    {
        if (!IsValidSlotIndex(
                slotIndex) ||
            purchasedSlots[
                slotIndex
            ])
        {
            return false;
        }

        purchasedSlots[
            slotIndex
        ] =
            true;

        return true;
    }

    public int IncrementHealingPurchaseCount()
    {
        HealingPurchaseCount++;

        return HealingPurchaseCount;
    }

    public bool TrySetProductId(
        int slotIndex,
        string productId)
    {
        if (!IsValidSlotIndex(
                slotIndex) ||
            string.IsNullOrWhiteSpace(
                productId
            ))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(
                productIds[
                    slotIndex
                ]))
        {
            return false;
        }

        productIds[
            slotIndex
        ] =
            productId.Trim();

        return true;
    }

    public string GetProductId(
        int slotIndex)
    {
        if (!IsValidSlotIndex(
                slotIndex))
        {
            return string.Empty;
        }

        return productIds[
                   slotIndex
               ] ??
               string.Empty;
    }
}