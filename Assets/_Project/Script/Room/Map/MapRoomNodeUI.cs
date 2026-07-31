using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Button))]
public sealed class MapRoomNodeUI :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private Image backgroundImage;

    [SerializeField]
    private Button roomButton;

    [SerializeField]
    private Image roomIcon;

    [SerializeField]
    private TMP_Text roomSymbolText;

    [SerializeField]
    private GameObject currentRoomOutline;

    private RoomNode room;

    private Action<RoomNode>
        clickedHandler;

    public RoomNode Room =>
        room;

    public int RoomId =>
        room != null
            ? room.RoomId
            : -1;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
        BindButton();
    }

    private void OnDestroy()
    {
        UnbindButton();

        clickedHandler =
            null;
    }

    private void OnValidate()
    {
        FindReferences();
    }

    private void FindReferences()
    {
        if (backgroundImage == null)
        {
            backgroundImage =
                GetComponent<Image>();
        }

        if (roomButton == null)
        {
            roomButton =
                GetComponent<Button>();
        }

        if (roomIcon == null)
        {
            Transform iconTransform =
                transform.Find(
                    "RoomIcon"
                );

            if (iconTransform != null)
            {
                roomIcon =
                    iconTransform.GetComponent<
                        Image
                    >();
            }
        }

        if (roomSymbolText == null)
        {
            Transform symbolTransform =
                transform.Find(
                    "RoomSymbolText"
                );

            if (symbolTransform != null)
            {
                roomSymbolText =
                    symbolTransform.GetComponent<
                        TMP_Text
                    >();
            }
        }

        if (currentRoomOutline == null)
        {
            Transform outlineTransform =
                transform.Find(
                    "CurrentRoomOutline"
                );

            if (outlineTransform != null)
            {
                currentRoomOutline =
                    outlineTransform.gameObject;
            }
        }
    }

    private void ValidateReferences()
    {
        if (backgroundImage == null)
        {
            Debug.LogError(
                "MapRoomNodeUI: " +
                "Background Image가 연결되지 않았습니다.",
                this
            );
        }

        if (roomButton == null)
        {
            Debug.LogError(
                "MapRoomNodeUI: " +
                "Room Button이 연결되지 않았습니다.",
                this
            );
        }

        if (roomSymbolText == null)
        {
            Debug.LogError(
                "MapRoomNodeUI: " +
                "Room Symbol Text가 연결되지 않았습니다.",
                this
            );
        }

        if (currentRoomOutline == null)
        {
            Debug.LogWarning(
                "MapRoomNodeUI: " +
                "Current Room Outline이 연결되지 않았습니다.",
                this
            );
        }
    }

    private void BindButton()
    {
        if (roomButton == null)
        {
            return;
        }

        roomButton.onClick.RemoveListener(
            HandleClicked
        );

        roomButton.onClick.AddListener(
            HandleClicked
        );
    }

    private void UnbindButton()
    {
        if (roomButton == null)
        {
            return;
        }

        roomButton.onClick.RemoveListener(
            HandleClicked
        );
    }

    public void Bind(
        RoomNode targetRoom,
        Action<RoomNode> onClicked)
    {
        room =
            targetRoom;

        clickedHandler =
            onClicked;

        name =
            room != null
                ? $"MapRoomNode_" +
                  $"{room.RoomId}_" +
                  $"{room.RoomType}"
                : "MapRoomNode_None";

        if (roomButton != null)
        {
            roomButton.interactable =
                false;
        }
    }

    public void SetDisplayState(
        bool isVisible,
        bool isCurrentRoom,
        bool isVisited,
        bool isCleared,
        bool canInteract,
        Color unvisitedColor,
        Color visitedColor,
        Color clearedColor,
        Color currentRoomColor,
        Color symbolColor,
        Sprite iconSprite,
        string symbol)
    {
        gameObject.SetActive(
            isVisible
        );

        if (!isVisible)
        {
            return;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color =
                ResolveBackgroundColor(
                    isCurrentRoom,
                    isVisited,
                    isCleared,
                    unvisitedColor,
                    visitedColor,
                    clearedColor,
                    currentRoomColor
                );
        }

        if (roomButton != null)
        {
            roomButton.interactable =
                canInteract;
        }

        ApplyIcon(
            iconSprite
        );

        ApplySymbol(
            symbol,
            symbolColor
        );

        if (currentRoomOutline != null)
        {
            currentRoomOutline.SetActive(
                isCurrentRoom
            );
        }
    }

    private void HandleClicked()
    {
        if (room == null ||
            roomButton == null ||
            !roomButton.interactable)
        {
            return;
        }

        clickedHandler?.Invoke(
            room
        );
    }

    private void ApplyIcon(
        Sprite iconSprite)
    {
        if (roomIcon == null)
        {
            return;
        }

        bool hasIcon =
            iconSprite != null;

        roomIcon.gameObject.SetActive(
            hasIcon
        );

        roomIcon.sprite =
            iconSprite;
    }

    private void ApplySymbol(
        string symbol,
        Color symbolColor)
    {
        if (roomSymbolText == null)
        {
            return;
        }

        bool hasSymbol =
            !string.IsNullOrWhiteSpace(
                symbol
            );

        roomSymbolText.gameObject.SetActive(
            hasSymbol
        );

        if (!hasSymbol)
        {
            roomSymbolText.text =
                string.Empty;

            return;
        }

        roomSymbolText.text =
            symbol;

        roomSymbolText.color =
            symbolColor;
    }

    private static Color
        ResolveBackgroundColor(
            bool isCurrentRoom,
            bool isVisited,
            bool isCleared,
            Color unvisitedColor,
            Color visitedColor,
            Color clearedColor,
            Color currentRoomColor)
    {
        if (isCurrentRoom)
        {
            return currentRoomColor;
        }

        if (isCleared)
        {
            return clearedColor;
        }

        if (isVisited)
        {
            return visitedColor;
        }

        return unvisitedColor;
    }
}