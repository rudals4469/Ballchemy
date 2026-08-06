using UnityEngine;

[DisallowMultipleComponent]
public sealed class
    UpgradeNextRewardTierShopEffectHandler :
        MonoBehaviour,
        IShopItemEffectHandler
{
    [Header("References")]

    [SerializeField]
    private RunRewardState runRewardState;

    public ShopItemEffectType EffectType =>
        ShopItemEffectType
            .UpgradeNextRewardTier;

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
                    "보상 등급 증가 구매 정보가 없습니다."
                );
        }

        ShopItemDefinition item =
            context.Item;

        if (item == null)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "보상 보증서 상품 정보가 없습니다."
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
                    "UpgradeNextRewardTier 효과의 " +
                    "상품 카테고리가 Special이 아닙니다."
                );
        }

        if (runRewardState == null)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "RunRewardState가 연결되지 않았습니다."
                );
        }

        if (runRewardState
                .HasPendingRewardUpgrade)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "이미 다음 전투방 보상 등급 증가가 " +
                    "예약되어 있습니다."
                );
        }

        int upgradeAmount =
            item.IntegerValue;

        if (upgradeAmount <= 0)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "보상 등급 증가 단계가 0 이하입니다. " +
                    $"IntegerValue={upgradeAmount}"
                );
        }

        return ShopItemEffectApplyResult
            .Success();
    }

    public ShopItemEffectApplyResult Apply(
        ShopPurchaseContext context)
    {
        ShopItemEffectApplyResult
            validationResult =
                Validate(
                    context
                );

        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }

        int upgradeAmount =
            context.Item.IntegerValue;

        bool reserved =
            runRewardState
                .AddPendingRewardTierIncrease(
                    upgradeAmount
                );

        if (!reserved)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "다음 전투방 보상 등급 증가 예약에 " +
                    "실패했습니다."
                );
        }

        return ShopItemEffectApplyResult
            .Success(
                "PendingRewardTierIncrease=" +
                runRewardState
                    .PendingRewardTierIncrease
            );
    }

    public bool Rollback(
        ShopPurchaseContext context)
    {
        if (runRewardState == null)
        {
            return false;
        }

        if (!runRewardState
                .HasPendingRewardUpgrade)
        {
            return true;
        }

        runRewardState
            .ClearPendingRewardUpgrade();

        return
            !runRewardState
                .HasPendingRewardUpgrade;
    }

    private void FindReferences()
    {
        if (runRewardState == null)
        {
            runRewardState =
                FindFirstObjectByType<
                    RunRewardState
                >();
        }
    }

    private void ValidateReferences()
    {
        if (runRewardState == null)
        {
            Debug.LogError(
                "UpgradeNextRewardTierShopEffectHandler: " +
                "RunRewardState가 연결되지 않았습니다.",
                this
            );
        }
    }
}