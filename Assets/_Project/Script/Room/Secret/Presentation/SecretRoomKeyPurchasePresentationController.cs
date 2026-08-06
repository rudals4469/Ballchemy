using UnityEngine;

[DisallowMultipleComponent]
public sealed class
    SecretRoomKeyPurchasePresentationController :
        MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private ShopPurchaseController
        shopPurchaseController;

    [SerializeField]
    private SecretRoomKeyPickupPresenter
        pickupPresenter;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog = true;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        FindReferences();
    }

    private void FindReferences()
    {
        if (shopPurchaseController == null)
        {
            shopPurchaseController =
                FindFirstObjectByType<
                    ShopPurchaseController
                >();
        }

        if (pickupPresenter == null)
        {
            pickupPresenter =
                FindFirstObjectByType<
                    SecretRoomKeyPickupPresenter
                >(
                    FindObjectsInactive.Include
                );
        }
    }

    private void ValidateReferences()
    {
        if (shopPurchaseController == null)
        {
            Debug.LogError(
                "SecretRoomKeyPurchasePresentationController: " +
                "ShopPurchaseController가 연결되지 않았습니다.",
                this
            );
        }

        if (pickupPresenter == null)
        {
            Debug.LogError(
                "SecretRoomKeyPurchasePresentationController: " +
                "SecretRoomKeyPickupPresenter가 연결되지 않았습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (shopPurchaseController == null)
        {
            return;
        }

        shopPurchaseController.PurchaseSucceeded -=
            HandlePurchaseSucceeded;

        shopPurchaseController.PurchaseSucceeded +=
            HandlePurchaseSucceeded;
    }

    private void UnsubscribeEvents()
    {
        if (shopPurchaseController == null)
        {
            return;
        }

        shopPurchaseController.PurchaseSucceeded -=
            HandlePurchaseSucceeded;
    }

    private void HandlePurchaseSucceeded(
        int roomId,
        int inventorySlotIndex,
        ShopItemDefinition purchasedItem)
    {
        if (purchasedItem == null ||
            purchasedItem.EffectType !=
            ShopItemEffectType.GrantSecretRoomKey)
        {
            return;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "SecretRoomKeyPurchasePresentationController: " +
                "비밀문 공명석 구매 연출 시작. " +
                $"RoomId={roomId}, " +
                $"SlotIndex={inventorySlotIndex}, " +
                $"ItemId={purchasedItem.ItemId}",
                this
            );
        }

        pickupPresenter?.PlayPickup();
    }
}