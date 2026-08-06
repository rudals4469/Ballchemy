using UnityEngine;

[DisallowMultipleComponent]
public sealed class
    RevealEntireStageMapShopEffectHandler :
        MonoBehaviour,
        IShopItemEffectHandler
{
    [Header("References")]

    [SerializeField]
    private StageMapRevealState
        stageMapRevealState;

    public ShopItemEffectType EffectType =>
        ShopItemEffectType
            .RevealEntireStageMap;

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
                    "지도 공개 구매 정보가 없습니다."
                );
        }

        ShopItemDefinition item =
            context.Item;

        if (item == null)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "전체 지도 공개 상품 정보가 없습니다."
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
                    "RevealEntireStageMap 효과의 " +
                    "상품 카테고리가 Special이 아닙니다."
                );
        }

        if (stageMapRevealState == null)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "StageMapRevealState가 연결되지 않았습니다."
                );
        }

        if (stageMapRevealState
                .IsEntireStageMapRevealed)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "현재 스테이지의 지도가 " +
                    "이미 모두 공개되어 있습니다."
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

        bool revealed =
            stageMapRevealState
                .RevealEntireStageMap();

        if (!revealed)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "전체 지도 공개 효과 적용에 " +
                    "실패했습니다."
                );
        }

        return ShopItemEffectApplyResult
            .Success(
                "EntireStageMapRevealed=True"
            );
    }

    public bool Rollback(
        ShopPurchaseContext context)
    {
        if (stageMapRevealState == null)
        {
            return false;
        }

        if (!stageMapRevealState
                .IsEntireStageMapRevealed)
        {
            return true;
        }

        stageMapRevealState.Clear();

        return
            !stageMapRevealState
                .IsEntireStageMapRevealed;
    }

    private void FindReferences()
    {
        if (stageMapRevealState == null)
        {
            stageMapRevealState =
                FindFirstObjectByType<
                    StageMapRevealState
                >();
        }
    }

    private void ValidateReferences()
    {
        if (stageMapRevealState == null)
        {
            Debug.LogError(
                "RevealEntireStageMapShopEffectHandler: " +
                "StageMapRevealState가 연결되지 않았습니다.",
                this
            );
        }
    }
}