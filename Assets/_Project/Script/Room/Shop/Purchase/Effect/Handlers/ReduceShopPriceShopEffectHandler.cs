using UnityEngine;

[DisallowMultipleComponent]
public sealed class ReduceShopPriceShopEffectHandler :
    MonoBehaviour,
    IShopItemEffectHandler
{
    [Header("References")]

    [SerializeField]
    private ShopPriceDiscountState
        shopPriceDiscountState;

    public ShopItemEffectType EffectType =>
        ShopItemEffectType.ReduceShopPrice;

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
            item.EffectType == EffectType;
    }

    public ShopItemEffectApplyResult Validate(
        ShopPurchaseContext context)
    {
        if (context == null)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "상인의 계약서 구매 정보가 없습니다."
                );
        }

        ShopItemDefinition item =
            context.Item;

        if (item == null)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "상인의 계약서 상품 정보가 없습니다."
                );
        }

        if (!CanHandle(
                item
            ))
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "현재 핸들러가 처리할 수 없는 " +
                    "상품 효과입니다."
                );
        }

        if (item.Category !=
            ShopItemCategory.Special)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "ReduceShopPrice 효과의 " +
                    "상품 카테고리가 Special이 아닙니다."
                );
        }

        if (shopPriceDiscountState == null)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "상점 가격 할인 상태가 연결되지 않았습니다."
                );
        }

        float discountRatio =
            item.RatioValue;

        if (discountRatio <= 0f ||
            discountRatio > 1f)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "상점 가격 할인율은 0보다 크고 " +
                    "1 이하여야 합니다. " +
                    $"현재 값={discountRatio}"
                );
        }

        if (shopPriceDiscountState.HasDiscount)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "이미 상인의 계약서 효과가 적용되어 있습니다."
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

        float discountRatio =
            context.Item.RatioValue;

        bool applied =
            shopPriceDiscountState
                .TryApplyDiscount(
                    discountRatio
                );

        if (!applied)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "상점 가격 할인 적용에 실패했습니다."
                );
        }

        return ShopItemEffectApplyResult
            .Success(
                $"ShopPriceDiscount={discountRatio:P0}"
            );
    }

    public bool Rollback(
        ShopPurchaseContext context)
    {
        if (shopPriceDiscountState == null)
        {
            return false;
        }

        if (!shopPriceDiscountState.HasDiscount)
        {
            return true;
        }

        if (context == null ||
            context.Item == null)
        {
            return false;
        }

        return shopPriceDiscountState
            .TryRemoveDiscount(
                context.Item.RatioValue
            );
    }

    private void FindReferences()
    {
        if (shopPriceDiscountState != null)
        {
            return;
        }

        shopPriceDiscountState =
            FindFirstObjectByType<
                ShopPriceDiscountState
            >(
                FindObjectsInactive.Include
            );
    }

    private void ValidateReferences()
    {
        if (shopPriceDiscountState != null)
        {
            return;
        }

        Debug.LogError(
            "ReduceShopPriceShopEffectHandler: " +
            "ShopPriceDiscountState가 연결되지 않았습니다.",
            this
        );
    }
}