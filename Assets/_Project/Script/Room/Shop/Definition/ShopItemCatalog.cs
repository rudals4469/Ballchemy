using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ShopItemCatalog",
    menuName = "Ballchemy/Shop/Shop Item Catalog"
)]
public sealed class ShopItemCatalog :
    ScriptableObject
{
    [Header("Items")]

    [Tooltip(
        "상점에서 등장할 수 있는 모든 상품 정의입니다."
    )]
    [SerializeField]
    private List<ShopItemDefinition> items =
        new List<ShopItemDefinition>();

    private readonly Dictionary<
        string,
        ShopItemDefinition
    > itemsById =
        new Dictionary<
            string,
            ShopItemDefinition
        >();

    private bool isCacheBuilt;

    public IReadOnlyList<ShopItemDefinition>
        Items =>
            items;

    private void OnEnable()
    {
        RebuildCache();
    }

    private void OnValidate()
    {
        RemoveNullEntries();
        RebuildCache();
    }

    public bool TryGetItem(
        string itemId,
        out ShopItemDefinition item)
    {
        EnsureCache();

        if (string.IsNullOrWhiteSpace(
                itemId
            ))
        {
            item = null;

            return false;
        }

        return itemsById.TryGetValue(
            itemId.Trim(),
            out item
        );
    }

    public List<ShopItemDefinition>
        GetSelectableItems(
            ShopItemCategory category)
    {
        List<ShopItemDefinition> results =
            new List<ShopItemDefinition>();

        for (int i = 0;
             i < items.Count;
             i++)
        {
            ShopItemDefinition item =
                items[i];

            if (item == null ||
                item.Category != category ||
                !item.IsValidForSelection)
            {
                continue;
            }

            results.Add(
                item
            );
        }

        return results;
    }

    public bool ValidateCatalog(
        out string errorMessage)
    {
        EnsureCache();

        int healingCount =
            CountSelectableItems(
                ShopItemCategory.Healing
            );

        int stageBuffCount =
            CountSelectableItems(
                ShopItemCategory.StageBuff
            );

        int specialCount =
            CountSelectableItems(
                ShopItemCategory.Special
            );

        if (healingCount < 1)
        {
            errorMessage =
                "선택 가능한 치료 상품이 최소 1개 필요합니다.";

            return false;
        }

        if (stageBuffCount < 2)
        {
            errorMessage =
                "선택 가능한 스테이지 버프가 최소 2개 필요합니다.";

            return false;
        }

        if (specialCount < 3)
        {
            errorMessage =
                "선택 가능한 특수 상품이 최소 3개 필요합니다.";

            return false;
        }

        errorMessage =
            string.Empty;

        return true;
    }

    private int CountSelectableItems(
        ShopItemCategory category)
    {
        int count =
            0;

        for (int i = 0;
             i < items.Count;
             i++)
        {
            ShopItemDefinition item =
                items[i];

            if (item == null ||
                item.Category != category ||
                !item.IsValidForSelection)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private void EnsureCache()
    {
        if (isCacheBuilt)
        {
            return;
        }

        RebuildCache();
    }

    private void RebuildCache()
    {
        itemsById.Clear();

        for (int i = 0;
             i < items.Count;
             i++)
        {
            ShopItemDefinition item =
                items[i];

            if (item == null ||
                string.IsNullOrWhiteSpace(
                    item.ItemId
                ))
            {
                continue;
            }

            string normalizedId =
                item.ItemId.Trim();

            if (itemsById.ContainsKey(
                    normalizedId
                ))
            {
                Debug.LogError(
                    "ShopItemCatalog: " +
                    $"중복 ItemId가 있습니다. " +
                    $"ItemId={normalizedId}, " +
                    $"Asset={item.name}",
                    this
                );

                continue;
            }

            itemsById.Add(
                normalizedId,
                item
            );
        }

        isCacheBuilt =
            true;
    }

    private void RemoveNullEntries()
    {
        items.RemoveAll(
            item =>
                item == null
        );
    }
}