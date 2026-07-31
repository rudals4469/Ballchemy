using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StageMapUI :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private StageRoomNavigator navigator;

    [SerializeField]
    private RectTransform mapContent;

    [SerializeField]
    private MapRoomNodeUI roomNodePrefab;

    [Header("Layout")]

    [Tooltip(
        "맵 좌표 한 칸당 UI에서 떨어질 거리입니다."
    )]
    [SerializeField, Min(1f)]
    private float nodeSpacing = 36f;

    [Tooltip(
        "생성된 전체 맵의 중심을 MapContent 중앙에 " +
        "맞출지 결정합니다."
    )]
    [SerializeField]
    private bool centerGeneratedMap = true;

    [Header("Room Colors")]

    [SerializeField]
    private Color hiddenRoomColor =
        new Color(
            0.08f,
            0.08f,
            0.08f,
            1f
        );

    [Tooltip(
        "처음부터 공개됐지만 아직 방문하지 않은 " +
        "특수방에 사용하는 색입니다."
    )]
    [SerializeField]
    private Color undiscoveredRoomColor =
        new Color(
            0.2f,
            0.2f,
            0.2f,
            1f
        );

    [Tooltip(
        "방문했지만 아직 클리어하지 않은 방의 색입니다."
    )]
    [SerializeField]
    private Color discoveredRoomColor =
        new Color(
            0.35f,
            0.35f,
            0.35f,
            1f
        );

    [Tooltip(
        "클리어한 방의 밝은 색입니다."
    )]
    [SerializeField]
    private Color clearedRoomColor =
        new Color(
            0.8f,
            0.8f,
            0.8f,
            1f
        );

    [Tooltip(
        "현재 위치한 방의 배경색입니다."
    )]
    [SerializeField]
    private Color currentRoomColor =
        new Color(
            1f,
            0.8f,
            0.25f,
            1f
        );

    [Header("Room Type Icons")]

    [SerializeField]
    private Sprite startRoomIcon;

    [SerializeField]
    private Sprite namedRoomIcon;

    [SerializeField]
    private Sprite bossRoomIcon;

    [SerializeField]
    private Sprite shopRoomIcon;

    [SerializeField]
    private Sprite rewardRoomIcon;

    [SerializeField]
    private Sprite eventRoomIcon;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog;

    private readonly Dictionary<int, MapRoomNodeUI>
        nodeByRoomId =
            new Dictionary<int, MapRoomNodeUI>();

    private StageMap displayedMap;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
        ValidateReferences();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();

        TryBuildFromCurrentMap();
    }

    private void Start()
    {
        TryBuildFromCurrentMap();
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
        NormalizeSettings();
    }

    private void FindReferences()
    {
        if (navigator == null)
        {
            navigator =
                FindFirstObjectByType<
                    StageRoomNavigator
                >();
        }

        if (mapContent == null)
        {
            mapContent =
                transform as
                    RectTransform;
        }
    }

    private void NormalizeSettings()
    {
        nodeSpacing =
            Mathf.Max(
                nodeSpacing,
                1f
            );
    }

    private void ValidateReferences()
    {
        if (navigator == null)
        {
            Debug.LogError(
                "StageMapUI: " +
                "StageRoomNavigator가 연결되지 않았습니다.",
                this
            );
        }

        if (mapContent == null)
        {
            Debug.LogError(
                "StageMapUI: " +
                "Map Content가 연결되지 않았습니다.",
                this
            );
        }

        if (roomNodePrefab == null)
        {
            Debug.LogError(
                "StageMapUI: " +
                "Room Node Prefab이 연결되지 않았습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (navigator == null)
        {
            return;
        }

        navigator.MapInitialized -=
            HandleMapInitialized;

        navigator.MapInitialized +=
            HandleMapInitialized;

        navigator.RoomChanged -=
            HandleRoomChanged;

        navigator.RoomChanged +=
            HandleRoomChanged;

        navigator
            .NavigationAvailabilityChanged -=
            HandleNavigationAvailabilityChanged;

        navigator
            .NavigationAvailabilityChanged +=
            HandleNavigationAvailabilityChanged;
    }

    private void UnsubscribeEvents()
    {
        if (navigator == null)
        {
            return;
        }

        navigator.MapInitialized -=
            HandleMapInitialized;

        navigator.RoomChanged -=
            HandleRoomChanged;

        navigator
            .NavigationAvailabilityChanged -=
            HandleNavigationAvailabilityChanged;
    }

    private void TryBuildFromCurrentMap()
    {
        if (navigator == null ||
            navigator.CurrentMap == null)
        {
            return;
        }

        BuildMap(
            navigator.CurrentMap
        );
    }

    private void HandleMapInitialized(
        StageMap initializedMap)
    {
        BuildMap(
            initializedMap
        );
    }

    private void HandleRoomChanged(
        RoomNode previousRoom,
        RoomNode currentRoom)
    {
        RefreshAllNodes();
    }

    private void
        HandleNavigationAvailabilityChanged()
    {
        /*
         * 이 이벤트는 방 클리어 때도 호출되므로
         * 클리어 방의 밝기 갱신에 사용할 수 있습니다.
         */
        RefreshAllNodes();
    }

    public void BuildMap(
        StageMap map)
    {
        if (map == null ||
            mapContent == null ||
            roomNodePrefab == null)
        {
            return;
        }

        if (displayedMap == map &&
            nodeByRoomId.Count ==
            map.RoomCount)
        {
            RefreshAllNodes();

            return;
        }

        ClearGeneratedNodes();

        displayedMap =
            map;

        IReadOnlyList<RoomNode> rooms =
            map.Rooms;

        if (rooms == null ||
            rooms.Count == 0)
        {
            return;
        }

        Vector2 mapCenter =
            centerGeneratedMap
                ? CalculateMapCenter(
                    rooms
                )
                : Vector2.zero;

        for (int i = 0;
             i < rooms.Count;
             i++)
        {
            RoomNode room =
                rooms[i];

            if (room == null)
            {
                continue;
            }

            MapRoomNodeUI node =
                Instantiate(
                    roomNodePrefab,
                    mapContent
                );

            if (node == null)
            {
                continue;
            }

            node.Bind(
                room
            );

            RectTransform nodeRect =
                node.transform as
                    RectTransform;

            if (nodeRect != null)
            {
                Vector2 roomPosition =
                    new Vector2(
                        room.GridPosition.x,
                        room.GridPosition.y
                    );

                nodeRect.anchoredPosition =
                    (
                        roomPosition -
                        mapCenter
                    ) *
                    nodeSpacing;
            }

            nodeByRoomId.Add(
                room.RoomId,
                node
            );
        }

        RefreshAllNodes();

        if (showDebugLog)
        {
            Debug.Log(
                "StageMapUI: " +
                $"스테이지 {map.StageNumber}의 " +
                $"맵 노드 {nodeByRoomId.Count}개 생성 완료",
                this
            );
        }
    }

    public void RefreshAllNodes()
    {
        if (navigator == null ||
            displayedMap == null)
        {
            return;
        }

        foreach (
            KeyValuePair<int, MapRoomNodeUI>
                pair in nodeByRoomId)
        {
            MapRoomNodeUI node =
                pair.Value;

            if (node == null ||
                node.Room == null)
            {
                continue;
            }

            RoomNode room =
                node.Room;

            bool isVisible =
                ShouldShowRoom(
                    room
                );

            bool isCurrentRoom =
                navigator.CurrentRoomId ==
                room.RoomId;

            bool isCleared =
                IsRoomDisplayedAsCleared(
                    room
                );

            Sprite icon =
                ResolveRoomIcon(
                    room.RoomType
                );

            node.SetDisplayState(
                isVisible,
                isCurrentRoom,
                isCleared,
                hiddenRoomColor,
                undiscoveredRoomColor,
                discoveredRoomColor,
                clearedRoomColor,
                currentRoomColor,
                icon
            );
        }
    }

    private bool ShouldShowRoom(
        RoomNode room)
    {
        if (room == null ||
            navigator == null)
        {
            return false;
        }

        switch (room.RoomType)
        {
            case RoomType.Start:
            case RoomType.Boss:
            case RoomType.Shop:
            case RoomType.Reward:
            case RoomType.Event:
                return true;

            case RoomType.NormalCombat:
            case RoomType.NamedCombat:
                return navigator.IsRoomVisited(
                    room.RoomId
                );

            default:
                return false;
        }
    }

    private bool IsRoomDisplayedAsCleared(
        RoomNode room)
    {
        if (room == null ||
            navigator == null)
        {
            return false;
        }

        /*
         * 시작방은 전투가 없는 이동 거점이므로
         * 처음부터 밝은 방으로 표시합니다.
         */
        if (room.RoomType ==
            RoomType.Start)
        {
            return true;
        }

        if (!room.IsCombatRoom)
        {
            return false;
        }

        return navigator.IsRoomCleared(
            room.RoomId
        );
    }

    private Sprite ResolveRoomIcon(
        RoomType roomType)
    {
        switch (roomType)
        {
            case RoomType.Start:
                return startRoomIcon;

            case RoomType.NamedCombat:
                return namedRoomIcon;

            case RoomType.Boss:
                return bossRoomIcon;

            case RoomType.Shop:
                return shopRoomIcon;

            case RoomType.Reward:
                return rewardRoomIcon;

            case RoomType.Event:
                return eventRoomIcon;

            case RoomType.NormalCombat:
            default:
                return null;
        }
    }

    private static Vector2 CalculateMapCenter(
        IReadOnlyList<RoomNode> rooms)
    {
        if (rooms == null ||
            rooms.Count == 0)
        {
            return Vector2.zero;
        }

        int minimumX =
            int.MaxValue;

        int maximumX =
            int.MinValue;

        int minimumY =
            int.MaxValue;

        int maximumY =
            int.MinValue;

        for (int i = 0;
             i < rooms.Count;
             i++)
        {
            RoomNode room =
                rooms[i];

            if (room == null)
            {
                continue;
            }

            minimumX =
                Mathf.Min(
                    minimumX,
                    room.GridPosition.x
                );

            maximumX =
                Mathf.Max(
                    maximumX,
                    room.GridPosition.x
                );

            minimumY =
                Mathf.Min(
                    minimumY,
                    room.GridPosition.y
                );

            maximumY =
                Mathf.Max(
                    maximumY,
                    room.GridPosition.y
                );
        }

        if (minimumX ==
                int.MaxValue ||
            minimumY ==
                int.MaxValue)
        {
            return Vector2.zero;
        }

        return new Vector2(
            (
                minimumX +
                maximumX
            ) *
            0.5f,
            (
                minimumY +
                maximumY
            ) *
            0.5f
        );
    }

    private void ClearGeneratedNodes()
    {
        foreach (
            KeyValuePair<int, MapRoomNodeUI>
                pair in nodeByRoomId)
        {
            MapRoomNodeUI node =
                pair.Value;

            if (node == null)
            {
                continue;
            }

            node.gameObject.SetActive(
                false
            );

            Destroy(
                node.gameObject
            );
        }

        nodeByRoomId.Clear();

        displayedMap =
            null;
    }
}