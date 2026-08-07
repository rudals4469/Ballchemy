using UnityEngine;

[DisallowMultipleComponent]
public sealed class AmplifyFutureStageBuffShopEffectHandler :
    MonoBehaviour,
    IShopItemEffectHandler
{
    [Header("References")]

    [SerializeField]
    private StageBuffAmplificationState
        stageBuffAmplificationState;

    public ShopItemEffectType EffectType =>
        ShopItemEffectType
            .AmplifyFutureStageBuffs;

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
                    "연금 촉매 구매 정보가 없습니다."
                );
        }

        ShopItemDefinition item =
            context.Item;

        if (item == null)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "연금 촉매 상품 정보가 없습니다."
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
                    "AmplifyFutureStageBuffs 효과의 " +
                    "상품 카테고리가 Special이 아닙니다."
                );
        }

        if (stageBuffAmplificationState == null)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "스테이지 버프 증폭 상태가 " +
                    "연결되지 않았습니다."
                );
        }

        if (item.RatioValue <= 0f)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "스테이지 버프 증폭률은 " +
                    "0보다 커야 합니다. " +
                    $"현재 값={item.RatioValue}"
                );
        }

        if (stageBuffAmplificationState.IsActive)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "이미 연금 촉매 효과가 " +
                    "적용되어 있습니다."
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

        float amplificationRatio =
            context.Item.RatioValue;

        bool applied =
            stageBuffAmplificationState
                .TryActivate(
                    amplificationRatio
                );

        if (!applied)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "연금 촉매 효과 적용에 실패했습니다."
                );
        }

        return ShopItemEffectApplyResult
            .Success(
                $"StageBuffAmplification=" +
                $"{amplificationRatio:P0}"
            );
    }

    public bool Rollback(
        ShopPurchaseContext context)
    {
        if (stageBuffAmplificationState == null)
        {
            return false;
        }

        if (!stageBuffAmplificationState.IsActive)
        {
            return true;
        }

        if (context == null ||
            context.Item == null)
        {
            return false;
        }

        return stageBuffAmplificationState
            .TryDeactivate(
                context.Item.RatioValue
            );
    }

    private void FindReferences()
    {
        if (stageBuffAmplificationState != null)
        {
            return;
        }

        stageBuffAmplificationState =
            FindFirstObjectByType<
                StageBuffAmplificationState
            >(
                FindObjectsInactive.Include
            );
    }

    private void ValidateReferences()
    {
        if (stageBuffAmplificationState != null)
        {
            return;
        }

        Debug.LogError(
            "AmplifyFutureStageBuffShopEffectHandler: " +
            "StageBuffAmplificationState가 연결되지 않았습니다.",
            this
        );
    }
}