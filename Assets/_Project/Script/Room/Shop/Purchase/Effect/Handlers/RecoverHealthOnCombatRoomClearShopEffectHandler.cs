using UnityEngine;

[DisallowMultipleComponent]
public sealed class RecoverHealthOnCombatRoomClearShopEffectHandler :
    MonoBehaviour,
    IShopItemEffectHandler
{
    [Header("References")]

    [SerializeField]
    private RoomClearHealingState
        roomClearHealingState;

    public ShopItemEffectType EffectType =>
        ShopItemEffectType
            .RecoverHealthOnCombatRoomClear;

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
                    "생존자의 문장 구매 정보가 없습니다."
                );
        }

        ShopItemDefinition item =
            context.Item;

        if (item == null)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "생존자의 문장 상품 정보가 없습니다."
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
                    "RecoverHealthOnCombatRoomClear 효과의 " +
                    "상품 카테고리가 Special이 아닙니다."
                );
        }

        if (roomClearHealingState == null)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "전투방 클리어 회복 상태가 " +
                    "연결되지 않았습니다."
                );
        }

        if (item.IntegerValue <= 0)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "전투방 클리어 회복량은 " +
                    "1 이상이어야 합니다. " +
                    $"현재 값={item.IntegerValue}"
                );
        }

        if (roomClearHealingState.IsActive)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "이미 생존자의 문장 효과가 " +
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

        int healingAmount =
            context.Item.IntegerValue;

        bool applied =
            roomClearHealingState
                .TryActivate(
                    healingAmount
                );

        if (!applied)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "생존자의 문장 효과 적용에 " +
                    "실패했습니다."
                );
        }

        return ShopItemEffectApplyResult
            .Success(
                $"RoomClearHealing={healingAmount}"
            );
    }

    public bool Rollback(
        ShopPurchaseContext context)
    {
        if (roomClearHealingState == null)
        {
            return false;
        }

        if (!roomClearHealingState.IsActive)
        {
            return true;
        }

        if (context == null ||
            context.Item == null)
        {
            return false;
        }

        return roomClearHealingState
            .TryDeactivate(
                context.Item.IntegerValue
            );
    }

    private void FindReferences()
    {
        if (roomClearHealingState != null)
        {
            return;
        }

        roomClearHealingState =
            FindFirstObjectByType<
                RoomClearHealingState
            >(
                FindObjectsInactive.Include
            );
    }

    private void ValidateReferences()
    {
        if (roomClearHealingState != null)
        {
            return;
        }

        Debug.LogError(
            "RecoverHealthOnCombatRoomClearShopEffectHandler: " +
            "RoomClearHealingState가 연결되지 않았습니다.",
            this
        );
    }
}