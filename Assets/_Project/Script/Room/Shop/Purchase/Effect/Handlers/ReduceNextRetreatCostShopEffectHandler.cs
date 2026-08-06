using UnityEngine;

[DisallowMultipleComponent]
public sealed class ReduceNextRetreatCostShopEffectHandler :
    MonoBehaviour,
    IShopItemEffectHandler
{
    [Header("References")]

    [SerializeField]
    private RetreatCostDiscountState
        retreatCostDiscountState;

    public ShopItemEffectType EffectType =>
        ShopItemEffectType.ReduceNextRetreatCost;

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
                    "후퇴 허가증 구매 정보가 없습니다."
                );
        }

        ShopItemDefinition item =
            context.Item;

        if (item == null)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "후퇴 허가증 상품 정보가 없습니다."
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
                    "ReduceNextRetreatCost 효과의 " +
                    "상품 카테고리가 Special이 아닙니다."
                );
        }

        if (retreatCostDiscountState == null)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "후퇴 비용 할인 상태가 연결되지 않았습니다."
                );
        }

        float discountRatio =
            item.RatioValue;

        if (discountRatio <= 0f ||
            discountRatio > 1f)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "후퇴 비용 할인율은 0보다 크고 " +
                    "1 이하여야 합니다. " +
                    $"현재 값={discountRatio}"
                );
        }

        if (retreatCostDiscountState.HasDiscount)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "이미 후퇴 허가증 효과가 적용되어 있습니다."
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
            retreatCostDiscountState
                .TryApplyDiscount(
                    discountRatio
                );

        if (!applied)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "후퇴 비용 할인 적용에 실패했습니다."
                );
        }

        return ShopItemEffectApplyResult
            .Success(
                $"RetreatCostDiscount={discountRatio:P0}"
            );
    }

    public bool Rollback(
        ShopPurchaseContext context)
    {
        if (retreatCostDiscountState == null)
        {
            return false;
        }

        if (!retreatCostDiscountState.HasDiscount)
        {
            return true;
        }

        if (context == null ||
            context.Item == null)
        {
            return false;
        }

        return retreatCostDiscountState
            .TryRemoveDiscount(
                context.Item.RatioValue
            );
    }

    private void FindReferences()
    {
        if (retreatCostDiscountState != null)
        {
            return;
        }

        retreatCostDiscountState =
            FindFirstObjectByType<
                RetreatCostDiscountState
            >(
                FindObjectsInactive.Include
            );
    }

    private void ValidateReferences()
    {
        if (retreatCostDiscountState != null)
        {
            return;
        }

        Debug.LogError(
            "ReduceNextRetreatCostShopEffectHandler: " +
            "RetreatCostDiscountState가 연결되지 않았습니다.",
            this
        );
    }
}