public sealed class ShopPurchaseContext
{
    public int RoomId
    {
        get;
    }

    public int InventorySlotIndex
    {
        get;
    }

    public int Price
    {
        get;
    }

    public ShopItemDefinition Item
    {
        get;
    }

    public ShopPurchaseContext(
        int roomId,
        int inventorySlotIndex,
        int price,
        ShopItemDefinition item)
    {
        RoomId =
            roomId;

        InventorySlotIndex =
            inventorySlotIndex;

        Price =
            price;

        Item =
            item;
    }
}