using UnityEngine;

[DisallowMultipleComponent]
public sealed class
    GrantSecretRoomKeyShopEffectHandler :
        MonoBehaviour,
        IShopItemEffectHandler
{
    [Header("References")]

    [SerializeField]
    private SecretRoomKeyState
        secretRoomKeyState;

    [SerializeField]
    private SecretRoomState
        secretRoomState;

    public ShopItemEffectType EffectType =>
        ShopItemEffectType
            .GrantSecretRoomKey;

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
                    "비밀문 공명석 구매 정보가 없습니다."
                );
        }

        ShopItemDefinition item =
            context.Item;

        if (item == null)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "비밀문 공명석 상품 정보가 없습니다."
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
                    "GrantSecretRoomKey 효과의 " +
                    "상품 카테고리가 Special이 아닙니다."
                );
        }

        if (secretRoomKeyState == null ||
            secretRoomState == null)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "비밀문 공명석 구매에 필요한 " +
                    "상태 참조가 없습니다."
                );
        }

        if (!secretRoomState.HasSecretRoom)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "현재 스테이지에 생성된 " +
                    "비밀방이 없습니다."
                );
        }

        if (secretRoomState
                .IsSecretRoomUnlocked)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "현재 스테이지의 비밀방이 " +
                    "이미 개방되어 있습니다."
                );
        }

        if (secretRoomKeyState.HasKey)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "이미 비밀문 공명석을 " +
                    "보유하고 있습니다."
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

        bool acquired =
            secretRoomKeyState.AcquireKey();

        if (!acquired)
        {
            return ShopItemEffectApplyResult
                .Failure(
                    "비밀문 공명석 지급에 실패했습니다."
                );
        }

        return ShopItemEffectApplyResult
            .Success(
                "SecretRoomKeyAcquired=True"
            );
    }

    public bool Rollback(
        ShopPurchaseContext context)
    {
        if (secretRoomKeyState == null)
        {
            return false;
        }

        if (!secretRoomKeyState.HasKey)
        {
            return true;
        }

        bool consumed =
            secretRoomKeyState.TryConsumeKey();

        return
            consumed &&
            !secretRoomKeyState.HasKey;
    }

    private void FindReferences()
    {
        if (secretRoomKeyState == null)
        {
            secretRoomKeyState =
                FindFirstObjectByType<
                    SecretRoomKeyState
                >();
        }

        if (secretRoomState == null)
        {
            secretRoomState =
                FindFirstObjectByType<
                    SecretRoomState
                >();
        }
    }

    private void ValidateReferences()
    {
        if (secretRoomKeyState == null)
        {
            Debug.LogError(
                "GrantSecretRoomKeyShopEffectHandler: " +
                "SecretRoomKeyState가 연결되지 않았습니다.",
                this
            );
        }

        if (secretRoomState == null)
        {
            Debug.LogError(
                "GrantSecretRoomKeyShopEffectHandler: " +
                "SecretRoomState가 연결되지 않았습니다.",
                this
            );
        }
    }
}