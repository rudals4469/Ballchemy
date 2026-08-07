using UnityEngine;

public enum ShopItemCategory
{
    Healing,
    StageBuff,
    Special
}

public enum ShopItemEffectType
{
    None,

    /*
     * 치료
     */
    RecoverHealth,

    /*
     * 스테이지 한정 버프
     */
    IncreaseDirectDamage,
    ReduceEnemyMaxHealth,
    ReduceEnemyAttackDamage,
    IncreaseEnemyAttackInterval,
    IncreaseBossDamage,
    IncreaseGoldGain,

    /*
     * 특수 상품
     */
    GrantSecretRoomKey,
    RevealEntireStageMap,
    GrantStageRevive,
    ExchangeMaxHealthForGold,
    UpgradeNextRewardTier,
    ReduceNegativeSpecialBlockChance,
    GrantNextRewardReroll,
    ReduceNextRetreatCost,
    ReduceShopPrice,
    RefreshCurrentShopInventory,
    RecoverHealthOnCombatRoomClear
}

[CreateAssetMenu(
    fileName = "ShopItem_New",
    menuName = "Ballchemy/Shop/Shop Item Definition"
)]
public sealed class ShopItemDefinition :
    ScriptableObject
{
    [Header("Identity")]

    [Tooltip(
        "저장과 상점 재고 식별에 사용하는 고유 ID입니다.\n" +
        "다른 상점 상품과 중복되면 안 됩니다."
    )]
    [SerializeField]
    private string itemId;

    [Tooltip(
        "상점 UI에 표시할 상품 이름입니다."
    )]
    [SerializeField]
    private string displayName;

    [Tooltip(
        "상점 UI에 표시할 상품 설명입니다."
    )]
    [SerializeField, TextArea(2, 5)]
    private string description;

    [Tooltip(
        "상점 UI에 표시할 상품 아이콘입니다."
    )]
    [SerializeField]
    private Sprite icon;

    [Header("Classification")]

    [Tooltip(
        "치료, 스테이지 버프, 특수 상품 중 " +
        "어느 슬롯 풀에 포함될지 결정합니다."
    )]
    [SerializeField]
    private ShopItemCategory category =
        ShopItemCategory.Special;

    [Tooltip(
        "구매 시 실행할 상품 효과 종류입니다."
    )]
    [SerializeField]
    private ShopItemEffectType effectType =
        ShopItemEffectType.None;

    [Header("Selection")]

    [Tooltip(
        "상점 재고 후보에 포함할지 결정합니다."
    )]
    [SerializeField]
    private bool canBeSelected =
        true;

    [Tooltip(
        "같은 카테고리 안에서 상품이 선택될 상대 가중치입니다."
    )]
    [SerializeField, Min(0)]
    private int selectionWeight =
        100;

    [Tooltip(
        "같은 상점 재고 안에 동일 상품이 두 번 등장할 수 있는지 결정합니다."
    )]
    [SerializeField]
    private bool allowDuplicateInSameShop;

    [Header("Purchase")]

    [Tooltip(
        "상품의 기본 골드 가격입니다.\n" +
        "치료 상품의 반복 구매 가격은 이 값을 기준으로 계산합니다."
    )]
    [SerializeField, Min(0)]
    private int baseGoldPrice =
        10;

    [Tooltip(
        "한 상점에서 반복 구매할 수 있는 상품인지 결정합니다.\n" +
        "현재는 치료 상품에만 사용하는 것을 권장합니다."
    )]
    [SerializeField]
    private bool isRepeatable;

    [Tooltip(
        "구매 후 상품 슬롯을 품절 처리할지 결정합니다."
    )]
    [SerializeField]
    private bool becomesSoldOutAfterPurchase =
        true;

    [Header("Effect Values")]

    [Tooltip(
        "효과에서 사용하는 정수 값입니다.\n" +
        "턴 증가, 고정 골드, 횟수 등에 사용합니다."
    )]
    [SerializeField]
    private int integerValue;

    [Tooltip(
        "효과에서 사용하는 비율 값입니다.\n" +
        "0.2는 20%를 의미합니다."
    )]
    [SerializeField]
    private float ratioValue;

    [Tooltip(
        "효과에서 사용하는 보조 비율 값입니다.\n" +
        "두 개의 비율이 필요한 상품에서 사용합니다."
    )]
    [SerializeField]
    private float secondaryRatioValue;

    public string ItemId =>
        itemId;

    public string DisplayName =>
        displayName;

    public string Description =>
        description;

    public Sprite Icon =>
        icon;

    public ShopItemCategory Category =>
        category;

    public ShopItemEffectType EffectType =>
        effectType;

    public bool CanBeSelected =>
        canBeSelected;

    public int SelectionWeight =>
        selectionWeight;

    public bool AllowDuplicateInSameShop =>
        allowDuplicateInSameShop;

    public int BaseGoldPrice =>
        baseGoldPrice;

    public bool IsRepeatable =>
        isRepeatable;

    public bool BecomesSoldOutAfterPurchase =>
        becomesSoldOutAfterPurchase;

    public int IntegerValue =>
        integerValue;

    public float RatioValue =>
        ratioValue;

    public float SecondaryRatioValue =>
        secondaryRatioValue;

    public bool IsValidForSelection =>
        canBeSelected &&
        selectionWeight > 0 &&
        !string.IsNullOrWhiteSpace(
            itemId
        );

    private void OnValidate()
    {
        itemId =
            NormalizeText(
                itemId
            );

        displayName =
            NormalizeText(
                displayName
            );

        selectionWeight =
            Mathf.Max(
                selectionWeight,
                0
            );

        baseGoldPrice =
            Mathf.Max(
                baseGoldPrice,
                0
            );

        integerValue =
            Mathf.Max(
                integerValue,
                0
            );

        ratioValue =
            Mathf.Max(
                ratioValue,
                0f
            );

        secondaryRatioValue =
            Mathf.Max(
                secondaryRatioValue,
                0f
            );

        ApplyCategoryDefaults();
    }

    private void ApplyCategoryDefaults()
    {
        switch (category)
        {
            case ShopItemCategory.Healing:
                isRepeatable =
                    true;

                becomesSoldOutAfterPurchase =
                    false;

                break;

            case ShopItemCategory.StageBuff:
            case ShopItemCategory.Special:
                isRepeatable =
                    false;

                becomesSoldOutAfterPurchase =
                    true;

                break;
        }
    }

    private static string NormalizeText(
        string value)
    {
        return string.IsNullOrWhiteSpace(
            value
        )
            ? string.Empty
            : value.Trim();
    }
}