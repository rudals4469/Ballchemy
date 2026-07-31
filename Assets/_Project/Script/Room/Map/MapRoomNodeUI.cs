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
    private GameObject currentRoomOutline;

    private RoomNode room;

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
                "Button이 연결되지 않았습니다.",
                this
            );
        }

        if (roomIcon == null)
        {
            Debug.LogWarning(
                "MapRoomNodeUI: " +
                "Room Icon이 연결되지 않았습니다.",
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

    public void Bind(
        RoomNode targetRoom)
    {
        room =
            targetRoom;

        name =
            room != null
                ? $"MapRoomNode_{room.RoomId}_{room.RoomType}"
                : "MapRoomNode_None";

        /*
         * 맵 노드 클릭 이동은 다음 구현 단계에서
         * 연결합니다.
         */
        if (roomButton != null)
        {
            roomButton.interactable =
                false;
        }
    }

    public void SetDisplayState(
        bool isVisible,
        bool isCurrentRoom,
        bool isCleared,
        Color hiddenColor,
        Color undiscoveredColor,
        Color discoveredColor,
        Color clearedColor,
        Color currentRoomColor,
        Sprite iconSprite)
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
                    isCleared,
                    undiscoveredColor,
                    discoveredColor,
                    clearedColor,
                    currentRoomColor
                );
        }

        if (roomIcon != null)
        {
            bool hasIcon =
                iconSprite != null;

            roomIcon.gameObject.SetActive(
                hasIcon
            );

            roomIcon.sprite =
                iconSprite;
        }

        if (currentRoomOutline != null)
        {
            currentRoomOutline.SetActive(
                isCurrentRoom
            );
        }
    }

    private Color ResolveBackgroundColor(
        bool isCurrentRoom,
        bool isCleared,
        Color undiscoveredColor,
        Color discoveredColor,
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

        /*
         * 현재 단계에서 UI에 표시되는 일반·네임드 방은
         * 이미 방문한 방입니다.
         *
         * 특수방은 처음부터 공개되므로 아직 방문하지
         * 않았을 수 있습니다.
         */
        bool isVisited =
            room != null &&
            room.RoomType !=
                RoomType.Start;

        return isVisited
            ? discoveredColor
            : undiscoveredColor;
    }
}