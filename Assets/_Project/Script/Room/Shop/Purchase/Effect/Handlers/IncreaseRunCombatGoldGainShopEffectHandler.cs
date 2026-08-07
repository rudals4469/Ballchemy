using UnityEngine;

[DisallowMultipleComponent]
public sealed class IncreaseRunCombatGoldGainShopEffectHandler :
    MonoBehaviour,
    IShopItemEffectHandler
{
    [Header("References")]

    [SerializeField]
    private RunCombatGoldGainState
        runCombatGoldGainState;

    public ShopItemEffectType EffectType =>
        ShopItemEffectType
            .IncreaseRunCombatGoldGain;

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
                    "런 전체 전투방 골드 증가 구매 정보가 없습니다."
                );
        }

        ShopItemDefinition item =
            context.Item;

        if (item == null)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "런 전체 전투방 골드 증가 상품 정보가 없습니다."
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
                    "IncreaseRunCombatGoldGain 효과의 " +
                    "상품 카테고리가 Special이 아닙니다."
                );
        }

        if (runCombatGoldGainState == null)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "런 전체 전투방 골드 증가 상태가 " +
                    "연결되지 않았습니다."
                );
        }

        if (item.RatioValue <= 0f)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "런 전체 전투방 골드 증가율은 " +
                    "0보다 커야 합니다. " +
                    $"현재 값={item.RatioValue}"
                );
        }

        if (runCombatGoldGainState.IsActive)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "이미 런 전체 전투방 골드 증가 " +
                    "효과가 적용되어 있습니다."
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

        float increaseRatio =
            context.Item.RatioValue;

        bool applied =
            runCombatGoldGainState
                .TryActivate(
                    increaseRatio
                );

        if (!applied)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "런 전체 전투방 골드 증가 효과 " +
                    "적용에 실패했습니다."
                );
        }

        return ShopItemEffectApplyResult
            .Success(
                $"RunCombatGoldGain={increaseRatio:P0}"
            );
    }

    public bool Rollback(
        ShopPurchaseContext context)
    {
        if (runCombatGoldGainState == null)
        {
            return false;
        }

        if (!runCombatGoldGainState.IsActive)
        {
            return true;
        }

        if (context == null ||
            context.Item == null)
        {
            return false;
        }

        return runCombatGoldGainState
            .TryDeactivate(
                context.Item.RatioValue
            );
    }

    private void FindReferences()
    {
        if (runCombatGoldGainState != null)
        {
            return;
        }

        runCombatGoldGainState =
            FindFirstObjectByType<
                RunCombatGoldGainState
            >(
                FindObjectsInactive.Include
            );
    }

    private void ValidateReferences()
    {
        if (runCombatGoldGainState != null)
        {
            return;
        }

        Debug.LogError(
            "IncreaseRunCombatGoldGainShopEffectHandler: " +
            "RunCombatGoldGainState가 연결되지 않았습니다.",
            this
        );
    }
}