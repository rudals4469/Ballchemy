public interface IShopItemEffectHandler
{
    ShopItemEffectType EffectType
    {
        get;
    }

    bool CanHandle(
        ShopItemDefinition item
    );

    ShopItemEffectApplyResult Validate(
        ShopPurchaseContext context
    );

    ShopItemEffectApplyResult Apply(
        ShopPurchaseContext context
    );

    bool Rollback(
        ShopPurchaseContext context
    );
}