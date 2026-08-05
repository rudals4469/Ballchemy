using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ShopItemSlotPresenter :
    MonoBehaviour
{
    [Header("References")]

    [Tooltip(
        "카드 전체 클릭을 처리하는 루트 Button입니다."
    )]
    [SerializeField]
    private Button slotButton;

    [Tooltip(
        "상품 아이콘을 표시할 Image입니다."
    )]
    [SerializeField]
    private Image itemIcon;

    [Tooltip(
        "상품 이름을 표시할 TMP 텍스트입니다."
    )]
    [SerializeField]
    private TMP_Text itemNameText;

    [Tooltip(
        "상품 설명을 표시할 TMP 텍스트입니다."
    )]
    [SerializeField]
    private TMP_Text itemDescriptionText;

    [Tooltip(
        "상품 가격을 표시할 TMP 텍스트입니다."
    )]
    [SerializeField]
    private TMP_Text priceText;

    [Tooltip(
        "품절 상태에서 활성화할 루트 오브젝트입니다."
    )]
    [SerializeField]
    private GameObject soldOutRoot;

    [Header("Display")]

    [SerializeField]
    private string goldSuffix =
        "GOLD";

    [SerializeField]
    private bool useSpaceBeforeGoldSuffix =
        true;

    private ShopItemDefinition currentItem;
    private int currentInventorySlotIndex = -1;
    private int currentDisplayedPrice;
    private bool isSoldOut;

    public ShopItemDefinition CurrentItem =>
        currentItem;

    public int CurrentInventorySlotIndex =>
        currentInventorySlotIndex;

    public int CurrentDisplayedPrice =>
        currentDisplayedPrice;

    public bool IsSoldOut =>
        isSoldOut;

    public event Action<
        ShopItemSlotPresenter
    > Clicked;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void OnEnable()
    {
        SubscribeButton();
    }

    private void OnDisable()
    {
        UnsubscribeButton();
    }

    private void OnDestroy()
    {
        UnsubscribeButton();
    }

    private void OnValidate()
    {
        FindReferences();

        if (goldSuffix == null)
        {
            goldSuffix =
                string.Empty;
        }
    }

    public void Show(
        ShopItemDefinition item,
        int inventorySlotIndex,
        bool soldOut)
    {
        int displayedPrice =
            item != null
                ? item.BaseGoldPrice
                : 0;

        Show(
            item,
            inventorySlotIndex,
            soldOut,
            displayedPrice
        );
    }

    public void Show(
        ShopItemDefinition item,
        int inventorySlotIndex,
        bool soldOut,
        int displayedPrice)
    {
        currentItem =
            item;

        currentInventorySlotIndex =
            inventorySlotIndex;

        currentDisplayedPrice =
            Mathf.Max(
                displayedPrice,
                0
            );

        isSoldOut =
            soldOut;

        gameObject.SetActive(
            true
        );

        ApplyItemVisuals();
    }

    public void Hide()
    {
        currentItem =
            null;

        currentInventorySlotIndex =
            -1;

        currentDisplayedPrice =
            0;

        isSoldOut =
            false;

        gameObject.SetActive(
            false
        );
    }

    public void RefreshPrice(
        int displayedPrice)
    {
        currentDisplayedPrice =
            Mathf.Max(
                displayedPrice,
                0
            );

        ApplyPriceVisual();
    }

    public void RefreshSoldOut(
        bool soldOut)
    {
        isSoldOut =
            soldOut;

        ApplyInteractionState();
    }

    private void ApplyItemVisuals()
    {
        if (currentItem == null)
        {
            Hide();
            return;
        }

        if (itemIcon != null)
        {
            itemIcon.sprite =
                currentItem.Icon;

            itemIcon.enabled =
                currentItem.Icon != null;
        }

        if (itemNameText != null)
        {
            itemNameText.text =
                currentItem.DisplayName;
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text =
                currentItem.Description;
        }

        ApplyPriceVisual();
        ApplyInteractionState();
    }

    private void ApplyPriceVisual()
    {
        if (priceText == null)
        {
            return;
        }

        priceText.text =
            FormatGoldPrice(
                currentDisplayedPrice
            );
    }

    private void ApplyInteractionState()
    {
        if (soldOutRoot != null)
        {
            soldOutRoot.SetActive(
                isSoldOut
            );
        }

        if (slotButton != null)
        {
            slotButton.interactable =
                currentItem != null &&
                !isSoldOut;
        }
    }

    private string FormatGoldPrice(
        int price)
    {
        price =
            Mathf.Max(
                price,
                0
            );

        string normalizedSuffix =
            string.IsNullOrWhiteSpace(
                goldSuffix
            )
                ? string.Empty
                : goldSuffix.Trim();

        string separator =
            useSpaceBeforeGoldSuffix &&
            !string.IsNullOrEmpty(
                normalizedSuffix
            )
                ? " "
                : string.Empty;

        return $"{price}" +
               separator +
               normalizedSuffix;
    }

    private void SubscribeButton()
    {
        if (slotButton == null)
        {
            return;
        }

        slotButton.onClick.RemoveListener(
            HandleButtonClicked
        );

        slotButton.onClick.AddListener(
            HandleButtonClicked
        );
    }

    private void UnsubscribeButton()
    {
        if (slotButton == null)
        {
            return;
        }

        slotButton.onClick.RemoveListener(
            HandleButtonClicked
        );
    }

    private void HandleButtonClicked()
    {
        if (currentItem == null ||
            isSoldOut)
        {
            return;
        }

        Clicked?.Invoke(
            this
        );
    }

    private void FindReferences()
    {
        if (slotButton == null)
        {
            slotButton =
                GetComponent<Button>();
        }
    }

    private void ValidateReferences()
    {
        if (slotButton == null)
        {
            Debug.LogError(
                "ShopItemSlotPresenter: " +
                "루트 Button이 연결되지 않았습니다.",
                this
            );
        }

        if (itemNameText == null)
        {
            Debug.LogError(
                "ShopItemSlotPresenter: " +
                "Item Name Text가 연결되지 않았습니다.",
                this
            );
        }

        if (itemDescriptionText == null)
        {
            Debug.LogError(
                "ShopItemSlotPresenter: " +
                "Item Description Text가 연결되지 않았습니다.",
                this
            );
        }

        if (priceText == null)
        {
            Debug.LogError(
                "ShopItemSlotPresenter: " +
                "Price Text가 연결되지 않았습니다.",
                this
            );
        }
    }
}