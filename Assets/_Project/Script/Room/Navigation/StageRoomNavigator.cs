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

    private StageMap currentMap;

    private RoomNode currentRoom;
    private RoomNode previousRoom;

    private readonly HashSet<int>
        visitedRoomIds =
            new HashSet<int>();

    private readonly HashSet<int>
        clearedCombatRoomIds =
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

            if (!currentRoom.IsCombatRoom)
            {
                return true;
            }

            if (IsRoomCleared(
                    currentRoom.RoomId))
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
            Debug.LogError(
                "StageRoomNavigator: " +
                "TurnManager를 찾지 못했습니다.",
                this
            );
        }

        if (blockGridManager == null)
        {
            Debug.LogError(
                "StageRoomNavigator: " +
                "BlockGridManager를 찾지 못했습니다.",
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
        clearedCombatRoomIds.Clear();

        visitedRoomIds.Add(
            currentRoom.RoomId
        );

        /*
         * 시작방은 전투가 없는 빈 방이다.
         */
        ConfigureCurrentRoomOnEntry();

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

        RoomNode startRoom =
            map.StartRoom;

        if (startRoom != null)
        {
            return startRoom;
        }

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

    public bool IsRoomCleared(
        int roomId)
    {
        return clearedCombatRoomIds.Contains(
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

        ConfigureCurrentRoomOnEntry();

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

    private void ConfigureCurrentRoomOnEntry()
    {
        if (currentRoom == null ||
            blockGridManager == null)
        {
            return;
        }

        if (currentRoom.RoomType ==
            RoomType.NormalCombat ||
            currentRoom.RoomType ==
            RoomType.NamedCombat)
        {
            if (IsRoomCleared(
                    currentRoom.RoomId))
            {
                blockGridManager
                    .PrepareEmptyRoom();

                turnManager?.ResetToAiming(
                    true
                );

                return;
            }

            bool started =
                blockGridManager
                    .StartRoomCombat(
                        currentRoom.RoomType
                    );

            if (!started)
            {
                Debug.LogError(
                    "StageRoomNavigator: " +
                    $"{GetRoomDescription(currentRoom)}의 " +
                    "전투를 시작하지 못했습니다.",
                    this
                );

                return;
            }

            turnManager?.ResetToAiming(
                true
            );

            return;
        }

        /*
         * 현재 구현 단계에서는 Start와 특수방을
         * 빈 방으로 처리한다.
         *
         * 보스방, 상점방, 보상방, 이벤트방의
         * 실제 동작은 각 구현 단계에서 연결한다.
         */
        blockGridManager.PrepareEmptyRoom();

        turnManager?.ResetToAiming(
            true
        );
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
        if (currentRoom != null &&
            currentRoom.IsCombatRoom)
        {
            clearedCombatRoomIds.Add(
                currentRoom.RoomId
            );

            Debug.Log(
                "StageRoomNavigator: " +
                $"방 {currentRoom.RoomId} 클리어 상태 저장",
                this
            );
        }

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