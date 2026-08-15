using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RoomPanelVisibilityCoordinator : MonoBehaviour
{
    [SerializeField] private StageRoomNavigator roomNavigator;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject alchemyPanel;
    [SerializeField] private GameObject eventSelectionPanel;
    [SerializeField] private GameObject unknownEventPanel;
    [SerializeField] private GameObject secretPanel;
    [SerializeField] private GameObject rewardSelectionPanel;

    private Coroutine refreshRoutine;

    private void Awake()
    {
        if (roomNavigator == null)
            roomNavigator = FindFirstObjectByType<StageRoomNavigator>();
    }

    private void OnEnable()
    {
        if (roomNavigator != null)
        {
            roomNavigator.RoomChanged -= HandleRoomChanged;
            roomNavigator.RoomChanged += HandleRoomChanged;
        }
    }

    private void Start()
    {
        ScheduleRefresh();
    }

    private void OnDisable()
    {
        if (roomNavigator != null)
            roomNavigator.RoomChanged -= HandleRoomChanged;
    }

    private void HandleRoomChanged(RoomNode previousRoom, RoomNode currentRoom)
    {
        ScheduleRefresh();
    }

    private void ScheduleRefresh()
    {
        if (refreshRoutine != null)
            StopCoroutine(refreshRoutine);
        refreshRoutine = StartCoroutine(RefreshAtEndOfFrame());
    }

    private IEnumerator RefreshAtEndOfFrame()
    {
        yield return null;
        refreshRoutine = null;

        RoomNode room = roomNavigator != null
            ? roomNavigator.CurrentRoom
            : null;
        RoomType roomType = room != null
            ? room.RoomType
            : RoomType.Start;

        SetRoomPanelActive(shopPanel, roomType == RoomType.Shop);
        SetRoomPanelActive(alchemyPanel, roomType == RoomType.Alchemy);
        SetRoomPanelActive(eventSelectionPanel, roomType == RoomType.Event);
        SetRoomPanelActive(secretPanel, roomType == RoomType.Secret);

        EnforcePanel(unknownEventPanel, roomType == RoomType.Event);

        bool isRewardRoom = roomType == RoomType.NormalCombat ||
            roomType == RoomType.NamedCombat ||
            roomType == RoomType.Boss ||
            roomType == RoomType.Augment;
        EnforcePanel(rewardSelectionPanel, isRewardRoom);
    }

    private static void SetRoomPanelActive(
        GameObject panel,
        bool shouldActivate)
    {
        if (panel == null)
            return;

        if (panel.activeSelf != shouldActivate)
            panel.SetActive(shouldActivate);

        if (shouldActivate)
            panel.transform.SetAsLastSibling();
    }

    private static void EnforcePanel(GameObject panel, bool allowedInRoom)
    {
        if (panel == null)
            return;

        if (!allowedInRoom)
        {
            if (panel.activeSelf)
                panel.SetActive(false);
            return;
        }

        if (panel.activeSelf)
            panel.transform.SetAsLastSibling();
    }
}
