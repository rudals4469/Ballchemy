using System.Collections;
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
    private float nodeSpacing = 52f;

    [Tooltip(
        "현재 방을 MapContent 부모 영역의 중앙에 " +
        "고정하도록 맵 전체를 이동합니다."
    )]
    [SerializeField]
    private bool centerOnCurrentRoom = true;

    [Header("Follow Movement")]

    [Tooltip(
        "방 이동 시 맵이 현재 방 중심으로 " +
        "부드럽게 이동할지 결정합니다."
    )]
    [SerializeField]
    private bool animateMapMovement = true;

    [Tooltip(
        "현재 방 중심으로 이동하는 데 걸리는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float mapMovementDuration = 0.15f;

    [Header("Room Colors")]

    [Tooltip(
        "처음부터 공개됐지만 아직 방문하지 않은 " +
        "특수방의 배경색입니다."
    )]
    [SerializeField]
    private Color unvisitedRoomColor =
        new Color(
            0.18f,
            0.18f,
            0.18f,
            1f
        );

    [Tooltip(
        "방문했지만 아직 클리어하지 않은 방의 색입니다."
    )]
    [SerializeField]
    private Color visitedRoomColor =
        new Color(
            0.38f,
            0.38f,
            0.38f,
            1f
        );

    [Tooltip(
        "클리어한 방의 밝은 색입니다."
    )]
    [SerializeField]
    private Color clearedRoomColor =
        new Color(
            0.82f,
            0.82f,
            0.82f,
            1f
        );

    [Tooltip(
        "현재 위치한 방의 배경색입니다."
    )]
    [SerializeField]
    private Color currentRoomColor =
        new Color(
            0.95f,
            0.72f,
            0.2f,
            1f
        );

    [SerializeField]
    private Color symbolColor =
        Color.white;

    [Header("Temporary Room Symbols")]

    [SerializeField]
    private string startRoomSymbol = "S";

    [SerializeField]
    private string normalRoomSymbol = "";

    [SerializeField]
    private string namedRoomSymbol = "★";

    [SerializeField]
    private string bossRoomSymbol = "B";

    [SerializeField]
    private string shopRoomSymbol = "$";

    [SerializeField]
    private string rewardRoomSymbol = "G";

    [SerializeField]
    private string eventRoomSymbol = "?";

    [Header("Optional Room Icons")]

    [Tooltip(
        "Sprite가 연결되어 있으면 텍스트 대신 " +
        "해당 Sprite를 표시합니다."
    )]
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

    private readonly Dictionary<
        int,
        MapRoomNodeUI
    > nodeByRoomId =
        new Dictionary<
            int,
            MapRoomNodeUI
        >();

    private StageMap displayedMap;

    private Coroutine mapMovementCoroutine;

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
        StopMapMovement();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        StopMapMovement();
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

        mapMovementDuration =
            Mathf.Max(
                mapMovementDuration,
                0f
            );

        startRoomSymbol =
            NormalizeSymbol(
                startRoomSymbol,
                "S"
            );

        namedRoomSymbol =
            NormalizeSymbol(
                namedRoomSymbol,
                "★"
            );

        bossRoomSymbol =
            NormalizeSymbol(
                bossRoomSymbol,
                "B"
            );

        shopRoomSymbol =
            NormalizeSymbol(
                shopRoomSymbol,
                "$"
            );

        rewardRoomSymbol =
            NormalizeSymbol(
                rewardRoomSymbol,
                "G"
            );

        eventRoomSymbol =
            NormalizeSymbol(
                eventRoomSymbol,
                "?"
            );
    }

    private static string NormalizeSymbol(
        string symbol,
        string fallback)
    {
        return string.IsNullOrWhiteSpace(
                symbol
            )
            ? fallback
            : symbol;
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
        FocusCurrentRoom(
            animateMapMovement
        );
    }

    private void
        HandleNavigationAvailabilityChanged()
    {
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
            FocusCurrentRoom(
                false
            );

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
                nodeRect.anchoredPosition =
                    new Vector2(
                        room.GridPosition.x *
                        nodeSpacing,
                        room.GridPosition.y *
                        nodeSpacing
                    );
            }

            nodeByRoomId.Add(
                room.RoomId,
                node
            );
        }

        RefreshAllNodes();

        /*
         * 생성 직후 시작방을 즉시 중앙에 놓습니다.
         * 첫 프레임에는 이동 연출을 사용하지 않습니다.
         */
        FocusCurrentRoom(
            false
        );

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
            KeyValuePair<
                int,
                MapRoomNodeUI
            > pair in nodeByRoomId)
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

            bool isVisited =
                navigator.IsRoomVisited(
                    room.RoomId
                );

            bool isCleared =
                IsRoomDisplayedAsCleared(
                    room
                );

            Sprite icon =
                ResolveRoomIcon(
                    room.RoomType
                );

            string symbol =
                icon == null
                    ? ResolveRoomSymbol(
                        room.RoomType
                    )
                    : string.Empty;

            node.SetDisplayState(
                isVisible,
                isCurrentRoom,
                isVisited,
                isCleared,
                unvisitedRoomColor,
                visitedRoomColor,
                clearedRoomColor,
                currentRoomColor,
                symbolColor,
                icon,
                symbol
            );
        }
    }

    public void FocusCurrentRoom(
        bool animate)
    {
        if (!centerOnCurrentRoom ||
            navigator == null ||
            mapContent == null)
        {
            return;
        }

        RoomNode currentRoom =
            navigator.CurrentRoom;

        if (currentRoom == null)
        {
            return;
        }

        Vector2 targetContentPosition =
            new Vector2(
                -currentRoom.GridPosition.x *
                nodeSpacing,
                -currentRoom.GridPosition.y *
                nodeSpacing
            );

        StopMapMovement();

        if (!animate ||
            !animateMapMovement ||
            mapMovementDuration <= 0f ||
            !isActiveAndEnabled)
        {
            mapContent.anchoredPosition =
                targetContentPosition;

            return;
        }

        mapMovementCoroutine =
            StartCoroutine(
                MoveMapContentRoutine(
                    targetContentPosition
                )
            );
    }

    private IEnumerator MoveMapContentRoutine(
        Vector2 targetPosition)
    {
        Vector2 startPosition =
            mapContent.anchoredPosition;

        float elapsedTime = 0f;

        while (elapsedTime <
               mapMovementDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float normalizedTime =
                mapMovementDuration > 0f
                    ? Mathf.Clamp01(
                        elapsedTime /
                        mapMovementDuration
                    )
                    : 1f;

            /*
             * 부드럽게 출발하고 멈추도록
             * SmoothStep 보간을 사용합니다.
             */
            float smoothedTime =
                normalizedTime *
                normalizedTime *
                (
                    3f -
                    2f *
                    normalizedTime
                );

            mapContent.anchoredPosition =
                Vector2.LerpUnclamped(
                    startPosition,
                    targetPosition,
                    smoothedTime
                );

            yield return null;
        }

        mapContent.anchoredPosition =
            targetPosition;

        mapMovementCoroutine =
            null;
    }

    private void StopMapMovement()
    {
        if (mapMovementCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            mapMovementCoroutine
        );

        mapMovementCoroutine =
            null;
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

    private string ResolveRoomSymbol(
        RoomType roomType)
    {
        switch (roomType)
        {
            case RoomType.Start:
                return startRoomSymbol;

            case RoomType.NormalCombat:
                return normalRoomSymbol;

            case RoomType.NamedCombat:
                return namedRoomSymbol;

            case RoomType.Boss:
                return bossRoomSymbol;

            case RoomType.Shop:
                return shopRoomSymbol;

            case RoomType.Reward:
                return rewardRoomSymbol;

            case RoomType.Event:
                return eventRoomSymbol;

            default:
                return string.Empty;
        }
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

    private void ClearGeneratedNodes()
    {
        StopMapMovement();

        foreach (
            KeyValuePair<
                int,
                MapRoomNodeUI
            > pair in nodeByRoomId)
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

        if (mapContent != null)
        {
            mapContent.anchoredPosition =
                Vector2.zero;
        }
    }
}