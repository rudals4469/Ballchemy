using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class StageRoomNavigator :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private StageMapGenerator mapGenerator;

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private BlockGridManager blockGridManager;

    [SerializeField]
    private BallLauncher ballLauncher;

    [Header("Temporary Navigation Test")]
    [Tooltip(
        "현재는 방별 전투 연결 전 단계입니다.\n" +
        "활성화하면 전투 상태와 관계없이 방 이동 UI를 테스트할 수 있습니다.\n" +
        "실제 방 전투 연결 단계에서 제거할 예정입니다."
    )]
    [SerializeField]
    private bool enableNavigationTestMode = true;

    private StageMap currentMap;

    private RoomNode currentRoom;
    private RoomNode previousRoom;

    private readonly HashSet<int>
        visitedRoomIds =
            new HashSet<int>();

    public StageMap CurrentMap =>
        currentMap;

    public RoomNode CurrentRoom =>
        currentRoom;

    public RoomNode PreviousRoom =>
        previousRoom;

    public int CurrentRoomId =>
        currentRoom != null
            ? currentRoom.RoomId
            : -1;

    public int PreviousRoomId =>
        previousRoom != null
            ? previousRoom.RoomId
            : -1;

    public bool HasCurrentRoom =>
        currentRoom != null;

    public bool CanNavigate
    {
        get
        {
            if (currentMap == null ||
                currentRoom == null)
            {
                return false;
            }

            if (enableNavigationTestMode)
            {
                return true;
            }

            if (turnManager == null ||
                blockGridManager == null ||
                ballLauncher == null)
            {
                return false;
            }

            if (turnManager.IsGameOver ||
                ballLauncher.IsAttackInProgress)
            {
                return false;
            }

            if (turnManager.CurrentState !=
                TurnState.Aiming)
            {
                return false;
            }

            if (currentRoom.RoomType ==
                RoomType.Start)
            {
                return true;
            }

            return blockGridManager
                .CurrentRoomState ==
                RoomCombatState.Cleared;
        }
    }

    public event Action<StageMap>
        MapInitialized;

    public event Action<
        RoomNode,
        RoomNode
    > RoomChanged;

    public event Action
        NavigationAvailabilityChanged;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void Start()
    {
        /*
         * StageMapDebugView와 Start 실행 순서가
         * 보장되지 않으므로, 실제 방이 들어 있는 맵만
         * 초기화합니다.
         *
         * 아직 생성되지 않았다면 MapGenerated 이벤트를
         * 기다립니다.
         */
        TryInitializeFromCurrentMap();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        FindReferences();
    }

    private void FindReferences()
    {
        if (mapGenerator == null)
        {
            mapGenerator =
                FindFirstObjectByType<
                    StageMapGenerator
                >();
        }

        if (turnManager == null)
        {
            turnManager =
                FindFirstObjectByType<
                    TurnManager
                >();
        }

        if (blockGridManager == null)
        {
            blockGridManager =
                FindFirstObjectByType<
                    BlockGridManager
                >();
        }

        if (ballLauncher == null)
        {
            ballLauncher =
                FindFirstObjectByType<
                    BallLauncher
                >();
        }
    }

    private void ValidateReferences()
    {
        if (mapGenerator == null)
        {
            Debug.LogError(
                "StageRoomNavigator: " +
                "StageMapGenerator를 찾지 못했습니다.",
                this
            );
        }

        if (turnManager == null)
        {
            Debug.LogWarning(
                "StageRoomNavigator: " +
                "TurnManager를 찾지 못했습니다. " +
                "테스트 모드를 해제하면 이동할 수 없습니다.",
                this
            );
        }

        if (blockGridManager == null)
        {
            Debug.LogWarning(
                "StageRoomNavigator: " +
                "BlockGridManager를 찾지 못했습니다. " +
                "테스트 모드를 해제하면 이동할 수 없습니다.",
                this
            );
        }

        if (ballLauncher == null)
        {
            Debug.LogWarning(
                "StageRoomNavigator: " +
                "BallLauncher를 찾지 못했습니다. " +
                "방 이동 시 발사 위치를 중앙으로 " +
                "초기화할 수 없습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (mapGenerator != null)
        {
            mapGenerator.MapGenerated -=
                HandleMapGenerated;

            mapGenerator.MapGenerated +=
                HandleMapGenerated;
        }

        if (turnManager != null)
        {
            turnManager.StateChanged -=
                HandleTurnStateChanged;

            turnManager.StateChanged +=
                HandleTurnStateChanged;

            turnManager.InputLockChanged -=
                HandleInputLockChanged;

            turnManager.InputLockChanged +=
                HandleInputLockChanged;
        }

        if (blockGridManager != null)
        {
            blockGridManager.RoomStateChanged -=
                HandleRoomStateChanged;

            blockGridManager.RoomStateChanged +=
                HandleRoomStateChanged;

            blockGridManager.RoomCleared -=
                HandleRoomCleared;

            blockGridManager.RoomCleared +=
                HandleRoomCleared;
        }
    }

    private void UnsubscribeEvents()
    {
        if (mapGenerator != null)
        {
            mapGenerator.MapGenerated -=
                HandleMapGenerated;
        }

        if (turnManager != null)
        {
            turnManager.StateChanged -=
                HandleTurnStateChanged;

            turnManager.InputLockChanged -=
                HandleInputLockChanged;
        }

        if (blockGridManager != null)
        {
            blockGridManager.RoomStateChanged -=
                HandleRoomStateChanged;

            blockGridManager.RoomCleared -=
                HandleRoomCleared;
        }
    }

    private void TryInitializeFromCurrentMap()
    {
        if (mapGenerator == null)
        {
            return;
        }

        StageMap generatedMap =
            mapGenerator.CurrentMap;

        /*
         * Inspector에 남아 있는 빈 직렬화 데이터는
         * 유효한 런타임 맵으로 처리하지 않습니다.
         */
        if (generatedMap == null ||
            generatedMap.RoomCount <= 0)
        {
            return;
        }

        InitializeMap(
            generatedMap
        );
    }

    private void HandleMapGenerated(
        StageMap generatedMap)
    {
        InitializeMap(
            generatedMap
        );
    }

    private void InitializeMap(
        StageMap generatedMap)
    {
        if (generatedMap == null ||
            generatedMap.RoomCount <= 0)
        {
            return;
        }

        RoomNode startRoom =
            FindStartRoom(
                generatedMap
            );

        if (startRoom == null)
        {
            Debug.LogError(
                "StageRoomNavigator: " +
                "방이 생성됐지만 Start 타입의 방을 " +
                "찾지 못했습니다.",
                this
            );

            return;
        }

        /*
         * 동일한 맵과 동일한 시작방으로 이미 초기화된
         * 경우 중복 이벤트를 발생시키지 않습니다.
         */
        if (currentMap == generatedMap &&
            currentRoom != null &&
            currentRoom.RoomId ==
            startRoom.RoomId)
        {
            return;
        }

        currentMap =
            generatedMap;

        currentRoom =
            startRoom;

        previousRoom =
            null;

        visitedRoomIds.Clear();

        visitedRoomIds.Add(
            currentRoom.RoomId
        );

        Debug.Log(
            "StageRoomNavigator: 맵 이동 상태 초기화, " +
            $"현재 방={GetRoomDescription(currentRoom)}",
            this
        );

        MapInitialized?.Invoke(
            currentMap
        );

        RoomChanged?.Invoke(
            null,
            currentRoom
        );

        NavigationAvailabilityChanged
            ?.Invoke();
    }

    private RoomNode FindStartRoom(
        StageMap map)
    {
        if (map == null ||
            map.RoomCount <= 0)
        {
            return null;
        }

        /*
         * 정상적인 생성 경로에서는 StartRoom 프로퍼티가
         * 바로 반환됩니다.
         */
        RoomNode startRoom =
            map.StartRoom;

        if (startRoom != null)
        {
            return startRoom;
        }

        /*
         * 이전 직렬화 데이터 때문에 startRoomId가
         * 유실된 경우를 대비해 타입으로 한 번 더 찾습니다.
         */
        IReadOnlyList<RoomNode> rooms =
            map.Rooms;

        if (rooms == null)
        {
            return null;
        }

        for (int i = 0;
             i < rooms.Count;
             i++)
        {
            RoomNode room =
                rooms[i];

            if (room == null ||
                room.RoomType !=
                RoomType.Start)
            {
                continue;
            }

            return room;
        }

        return null;
    }

    public bool IsRoomVisited(
        int roomId)
    {
        return visitedRoomIds.Contains(
            roomId
        );
    }

    public bool HasConnectedRoom(
        RoomDirection direction)
    {
        return GetConnectedRoom(
                   direction
               ) != null;
    }

    public bool CanMove(
        RoomDirection direction)
    {
        if (!CanNavigate)
        {
            return false;
        }

        return GetConnectedRoom(
                   direction
               ) != null;
    }

    public RoomNode GetConnectedRoom(
        RoomDirection direction)
    {
        if (currentMap == null ||
            currentRoom == null)
        {
            return null;
        }

        Vector2Int targetPosition =
            currentRoom.GridPosition +
            RoomDirectionUtility.ToOffset(
                direction
            );

        RoomNode targetRoom =
            currentMap.GetRoomAt(
                targetPosition
            );

        if (targetRoom == null)
        {
            return null;
        }

        if (!currentRoom.HasConnection(
                targetRoom.RoomId))
        {
            return null;
        }

        return targetRoom;
    }

    public bool TryMove(
        RoomDirection direction)
    {
        if (!CanNavigate)
        {
            Debug.LogWarning(
                "StageRoomNavigator: " +
                "현재 상태에서는 일반 방 이동을 " +
                "할 수 없습니다.",
                this
            );

            return false;
        }

        RoomNode targetRoom =
            GetConnectedRoom(
                direction
            );

        if (targetRoom == null)
        {
            Debug.LogWarning(
                "StageRoomNavigator: " +
                $"{direction} 방향에 연결된 방이 없습니다.",
                this
            );

            return false;
        }

        return MoveToRoom(
            targetRoom
        );
    }

    public bool TryMoveToPreviousRoom()
    {
        if (currentMap == null ||
            currentRoom == null ||
            previousRoom == null)
        {
            return false;
        }

        if (!currentRoom.HasConnection(
                previousRoom.RoomId))
        {
            Debug.LogWarning(
                "StageRoomNavigator: " +
                "직전 방이 현재 방과 연결되어 있지 않습니다.",
                this
            );

            return false;
        }

        return MoveToRoom(
            previousRoom
        );
    }

    private bool MoveToRoom(
        RoomNode targetRoom)
    {
        if (targetRoom == null ||
            currentRoom == null ||
            targetRoom.RoomId ==
            currentRoom.RoomId)
        {
            return false;
        }

        if (!currentRoom.HasConnection(
                targetRoom.RoomId))
        {
            return false;
        }

        if (ballLauncher != null &&
            !ballLauncher.IsAttackInProgress)
        {
            ballLauncher
                .TryResetLaunchPositionToCenter();
        }

        RoomNode departedRoom =
            currentRoom;

        previousRoom =
            departedRoom;

        currentRoom =
            targetRoom;

        visitedRoomIds.Add(
            currentRoom.RoomId
        );

        Debug.Log(
            "StageRoomNavigator: 방 이동, " +
            $"{GetRoomDescription(departedRoom)} -> " +
            $"{GetRoomDescription(currentRoom)}",
            this
        );

        RoomChanged?.Invoke(
            departedRoom,
            currentRoom
        );

        NavigationAvailabilityChanged
            ?.Invoke();

        return true;
    }

    public void MoveUp()
    {
        TryMove(
            RoomDirection.Up
        );
    }

    public void MoveRight()
    {
        TryMove(
            RoomDirection.Right
        );
    }

    public void MoveDown()
    {
        TryMove(
            RoomDirection.Down
        );
    }

    public void MoveLeft()
    {
        TryMove(
            RoomDirection.Left
        );
    }

    private void HandleTurnStateChanged(
        TurnState turnState)
    {
        NavigationAvailabilityChanged
            ?.Invoke();
    }

    private void HandleInputLockChanged(
        bool isLocked)
    {
        NavigationAvailabilityChanged
            ?.Invoke();
    }

    private void HandleRoomStateChanged(
        RoomCombatState roomState)
    {
        NavigationAvailabilityChanged
            ?.Invoke();
    }

    private void HandleRoomCleared()
    {
        NavigationAvailabilityChanged
            ?.Invoke();
    }

    private static string GetRoomDescription(
        RoomNode room)
    {
        if (room == null)
        {
            return "None";
        }

        return
            $"Room {room.RoomId} " +
            $"({room.RoomType}, " +
            $"{room.GridPosition})";
    }
}