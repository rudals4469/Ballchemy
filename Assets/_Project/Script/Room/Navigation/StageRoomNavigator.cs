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

    [SerializeField]
    private BallCollection ballCollection;

    [SerializeField]
    private StageKeyState stageKeyState;

    [SerializeField]
    private SecretRoomState secretRoomState;

    private StageMap currentMap;

    private RoomNode currentRoom;
    private RoomNode previousRoom;

    private readonly HashSet<int>
        visitedRoomIds =
            new HashSet<int>();

    private readonly HashSet<int>
        clearedCombatRoomIds =
            new HashSet<int>();

    /*
     * 보상 선택, 방 전환 연출 등 외부 시스템이
     * 방 이동 자체를 잠글 때 사용합니다.
     */
    private bool isNavigationLocked;

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

    public bool IsNavigationLocked =>
        isNavigationLocked;

    public bool CanNavigate
    {
        get
        {
            if (isNavigationLocked)
            {
                return false;
            }

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

    public event Action<RoomNode>
        CombatRoomCleared;

    /*
     * 열쇠가 없어 이벤트방 입장이 거부됐을 때 발생합니다.
     * 이후 안내 문구나 흔들림 연출을 연결할 수 있습니다.
     */
    public event Action<RoomNode>
        EventRoomEntryBlocked;

    public event Action
        NavigationAvailabilityChanged;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();

        isNavigationLocked =
            false;
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

        if (ballCollection == null)
        {
            ballCollection =
                FindFirstObjectByType<
                    BallCollection
                >();
        }

        if (stageKeyState == null)
        {
            stageKeyState =
                FindFirstObjectByType<
                    StageKeyState
                >();
        }

        if (secretRoomState == null)
        {
            secretRoomState =
                FindFirstObjectByType<
                    SecretRoomState
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

        if (ballCollection == null)
        {
            Debug.LogError(
                "StageRoomNavigator: " +
                "BallCollection을 찾지 못했습니다.",
                this
            );
        }

        if (stageKeyState == null)
        {
            Debug.LogError(
                "StageRoomNavigator: " +
                "StageKeyState를 찾지 못했습니다. " +
                "이벤트방 입장 조건을 검사할 수 없습니다.",
                this
            );
        }

        if (secretRoomState == null)
        {
            Debug.LogError(
                "StageRoomNavigator: " +
                "SecretRoomState를 찾지 못했습니다. " +
                "잠긴 비밀방의 이동을 제어할 수 없습니다.",
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

        if (secretRoomState != null)
        {
            secretRoomState.StateChanged -=
                HandleSecretRoomStateChanged;

            secretRoomState.StateChanged +=
                HandleSecretRoomStateChanged;
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

        if (secretRoomState != null)
        {
            secretRoomState.StateChanged -=
                HandleSecretRoomStateChanged;
        }
    }

    public void SetNavigationLocked(
        bool shouldLock)
    {
        if (isNavigationLocked ==
            shouldLock)
        {
            return;
        }

        isNavigationLocked =
            shouldLock;

        Debug.Log(
            "StageRoomNavigator: 방 이동 잠금 " +
            (
                isNavigationLocked
                    ? "활성화"
                    : "해제"
            ),
            this
        );

        NavigationAvailabilityChanged
            ?.Invoke();
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

        blockGridManager
            ?.ClearRoomWaveSnapshots();

        currentRoom =
            startRoom;

        previousRoom =
            null;

        isNavigationLocked =
            false;

        visitedRoomIds.Clear();
        clearedCombatRoomIds.Clear();

        visitedRoomIds.Add(
            currentRoom.RoomId
        );

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

        RoomNode targetRoom =
            GetConnectedRoom(
                direction
            );

        return targetRoom != null &&
               CanEnterRoom(
                   targetRoom
               );
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

        if (!CanExposeConnection(
                currentRoom,
                targetRoom
            ))
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
                (
                    isNavigationLocked
                        ? "방 이동이 외부 시스템에 의해 잠겨 있습니다."
                        : "현재 상태에서는 일반 방 이동을 할 수 없습니다."
                ),
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
                $"{direction} 방향에 현재 이용 가능한 " +
                "연결 방이 없습니다.",
                this
            );

            return false;
        }

        if (!CanEnterRoom(
                targetRoom
            ))
        {
            HandleBlockedEventRoomEntry(
                targetRoom
            );

            return false;
        }

        return MoveToAdjacentRoom(
            targetRoom
        );
    }

    public bool TryMoveToPreviousRoom()
    {
        if (isNavigationLocked)
        {
            Debug.LogWarning(
                "StageRoomNavigator: " +
                "방 이동이 외부 시스템에 의해 잠겨 있어 " +
                "직전 방으로 이동할 수 없습니다.",
                this
            );

            return false;
        }

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

        if (!CanExposeConnection(
                currentRoom,
                previousRoom
            ))
        {
            Debug.LogWarning(
                "StageRoomNavigator: " +
                "직전 방으로 이어지는 비밀방 입구가 " +
                "아직 개방되지 않았습니다.",
                this
            );

            return false;
        }

        if (!CanEnterRoom(
                previousRoom
            ))
        {
            HandleBlockedEventRoomEntry(
                previousRoom
            );

            return false;
        }

        return MoveToAdjacentRoom(
            previousRoom
        );
    }

    public bool CanFastTravelToRoom(
        int targetRoomId)
    {
        if (!CanNavigate ||
            currentMap == null ||
            currentRoom == null)
        {
            return false;
        }

        if (targetRoomId ==
            currentRoom.RoomId)
        {
            return false;
        }

        RoomNode targetRoom =
            FindRoomById(
                targetRoomId
            );

        if (targetRoom == null ||
            !IsRoomVisited(
                targetRoom.RoomId))
        {
            return false;
        }

        if (!CanUseRoomForFastTravel(
                targetRoom
            ))
        {
            return false;
        }

        return HasSafeFastTravelPath(
            currentRoom.RoomId,
            targetRoom.RoomId
        );
    }

    public bool TryFastTravelToRoom(
        int targetRoomId)
    {
        if (!CanFastTravelToRoom(
                targetRoomId
            ))
        {
            return false;
        }

        RoomNode targetRoom =
            FindRoomById(
                targetRoomId
            );

        if (targetRoom == null)
        {
            return false;
        }

        return MoveToRoomDirect(
            targetRoom
        );
    }

    private bool MoveToAdjacentRoom(
        RoomNode targetRoom)
    {
        if (isNavigationLocked)
        {
            return false;
        }

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

        if (!CanExposeConnection(
                currentRoom,
                targetRoom
            ))
        {
            return false;
        }

        if (!CanEnterRoom(
                targetRoom
            ))
        {
            return false;
        }

        return CompleteRoomMove(
            targetRoom
        );
    }

    private bool MoveToRoomDirect(
        RoomNode targetRoom)
    {
        if (isNavigationLocked)
        {
            return false;
        }

        if (targetRoom == null ||
            currentRoom == null ||
            targetRoom.RoomId ==
            currentRoom.RoomId)
        {
            return false;
        }

        /*
         * 빠른 이동 대상은 이미 방문한 방만 허용되므로
         * 이벤트방 열쇠를 다시 요구하지 않습니다.
         */
        return CompleteRoomMove(
            targetRoom
        );
    }

    private bool CompleteRoomMove(
        RoomNode targetRoom)
    {
        if (targetRoom == null ||
            currentRoom == null)
        {
            return false;
        }

        bool isFirstEventRoomEntry =
            targetRoom.RoomType ==
                RoomType.Event &&
            !IsRoomVisited(
                targetRoom.RoomId
            );

        if (isFirstEventRoomEntry)
        {
            if (stageKeyState == null ||
                !stageKeyState.TryConsumeKey())
            {
                Debug.LogError(
                    "StageRoomNavigator: " +
                    "이벤트방 입장 직전 스테이지 열쇠를 " +
                    "소비하지 못해 이동을 취소했습니다.",
                    this
                );

                return false;
            }
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

        if (currentRoom.RoomType ==
            RoomType.Secret)
        {
            secretRoomState
                ?.MarkSecretRoomEntered(
                    currentRoom.RoomId
                );
        }

        ConfigureCurrentRoomOnEntry();

        Debug.Log(
            "StageRoomNavigator: 방 이동, " +
            $"{GetRoomDescription(departedRoom)} -> " +
            $"{GetRoomDescription(currentRoom)}",
            this
        );

        if (isFirstEventRoomEntry)
        {
            Debug.Log(
                "StageRoomNavigator: " +
                $"이벤트방 최초 입장, " +
                $"RoomId={currentRoom.RoomId}, " +
                "스테이지 열쇠 소비 완료",
                this
            );
        }

        RoomChanged?.Invoke(
            departedRoom,
            currentRoom
        );

        NavigationAvailabilityChanged
            ?.Invoke();

        return true;
    }

    /*
     * 비밀방이 포함되지 않은 연결은 항상 노출합니다.
     *
     * 일반방 -> 비밀방:
     * 연결된 일반방 ID와 비밀방 ID로 입구 상태를 검사합니다.
     *
     * 비밀방 -> 일반방:
     * 일반방 ID와 현재 비밀방 ID로 같은 입구 상태를 검사합니다.
     */
    private bool CanExposeConnection(
        RoomNode sourceRoom,
        RoomNode targetRoom)
    {
        if (sourceRoom == null ||
            targetRoom == null)
        {
            return false;
        }

        bool sourceIsSecret =
            sourceRoom.RoomType ==
            RoomType.Secret;

        bool targetIsSecret =
            targetRoom.RoomType ==
            RoomType.Secret;

        if (!sourceIsSecret &&
            !targetIsSecret)
        {
            return true;
        }

        if (secretRoomState == null ||
            !secretRoomState.IsSecretRoomUnlocked)
        {
            return false;
        }

        if (targetIsSecret)
        {
            return secretRoomState.CanUseEntrance(
                sourceRoom.RoomId,
                targetRoom.RoomId
            );
        }

        if (sourceIsSecret)
        {
            return secretRoomState.CanUseEntrance(
                targetRoom.RoomId,
                sourceRoom.RoomId
            );
        }

        return false;
    }

    /*
     * 이벤트방은 최초 입장 시 스테이지 열쇠를 요구합니다.
     * 비밀방은 SecretRoomState에서 개방된 뒤에만 입장할 수 있습니다.
     */
    private bool CanEnterRoom(
        RoomNode targetRoom)
    {
        if (targetRoom == null)
        {
            return false;
        }

        if (targetRoom.RoomType ==
            RoomType.Secret)
        {
            return
                secretRoomState != null &&
                secretRoomState
                    .IsSecretRoomUnlocked;
        }

        if (targetRoom.RoomType !=
            RoomType.Event)
        {
            return true;
        }

        if (IsRoomVisited(
                targetRoom.RoomId
            ))
        {
            return true;
        }

        return stageKeyState != null &&
               stageKeyState.HasKey;
    }

    private void HandleBlockedEventRoomEntry(
        RoomNode eventRoom)
    {
        if (eventRoom == null ||
            eventRoom.RoomType !=
            RoomType.Event)
        {
            return;
        }

        Debug.LogWarning(
            "StageRoomNavigator: " +
            "이벤트방에 입장하려면 " +
            "스테이지 열쇠가 필요합니다. " +
            $"RoomId={eventRoom.RoomId}",
            this
        );

        EventRoomEntryBlocked?.Invoke(
            eventRoom
        );

        NavigationAvailabilityChanged
            ?.Invoke();
    }

    private bool HasSafeFastTravelPath(
        int startRoomId,
        int targetRoomId)
    {
        if (currentMap == null)
        {
            return false;
        }

        Queue<int> pendingRoomIds =
            new Queue<int>();

        HashSet<int> checkedRoomIds =
            new HashSet<int>();

        pendingRoomIds.Enqueue(
            startRoomId
        );

        checkedRoomIds.Add(
            startRoomId
        );

        while (pendingRoomIds.Count > 0)
        {
            int roomId =
                pendingRoomIds.Dequeue();

            if (roomId ==
                targetRoomId)
            {
                return true;
            }

            RoomNode currentPathRoom =
                currentMap.GetRoomById(
                    roomId
                );

            if (currentPathRoom == null)
            {
                continue;
            }

            List<RoomNode> connectedRooms =
                currentMap.GetConnectedRooms(
                    roomId
                );

            if (connectedRooms == null)
            {
                continue;
            }

            for (int i = 0;
                 i < connectedRooms.Count;
                 i++)
            {
                RoomNode connectedRoom =
                    connectedRooms[i];

                if (connectedRoom == null ||
                    checkedRoomIds.Contains(
                        connectedRoom.RoomId))
                {
                    continue;
                }

                if (!CanExposeConnection(
                        currentPathRoom,
                        connectedRoom
                    ))
                {
                    continue;
                }

                if (!CanUseRoomForFastTravel(
                        connectedRoom))
                {
                    continue;
                }

                checkedRoomIds.Add(
                    connectedRoom.RoomId
                );

                pendingRoomIds.Enqueue(
                    connectedRoom.RoomId
                );
            }
        }

        return false;
    }

    private bool CanUseRoomForFastTravel(
        RoomNode room)
    {
        if (room == null ||
            !IsRoomVisited(
                room.RoomId))
        {
            return false;
        }

        if (room.RoomType ==
            RoomType.Start)
        {
            return true;
        }

        if (room.RoomType ==
            RoomType.Secret)
        {
            return
                secretRoomState != null &&
                secretRoomState
                    .IsSecretRoomUnlocked;
        }

        if (room.IsCombatRoom)
        {
            return IsRoomCleared(
                room.RoomId
            );
        }

        return true;
    }

    private RoomNode FindRoomById(
        int roomId)
    {
        if (currentMap == null ||
            currentMap.Rooms == null)
        {
            return null;
        }

        IReadOnlyList<RoomNode> rooms =
            currentMap.Rooms;

        for (int i = 0;
             i < rooms.Count;
             i++)
        {
            RoomNode room =
                rooms[i];

            if (room == null ||
                room.RoomId !=
                roomId)
            {
                continue;
            }

            return room;
        }

        return null;
    }

    private void ConfigureCurrentRoomOnEntry()
    {
        if (currentRoom == null ||
            blockGridManager == null)
        {
            return;
        }

        bool isSupportedCombatRoom =
            currentRoom.RoomType ==
                RoomType.NormalCombat ||
            currentRoom.RoomType ==
                RoomType.NamedCombat;

        if (isSupportedCombatRoom)
        {
            if (IsRoomCleared(
                    currentRoom.RoomId))
            {
                ballCollection
                    ?.SetBallsVisible(
                        false
                    );

                blockGridManager
                    .PrepareEmptyRoom();

                turnManager?.ResetToAiming(
                    true
                );

                return;
            }

            ballCollection
                ?.SetBallsVisible(
                    true
                );

            bool started =
                blockGridManager
                    .StartRoomCombat(
                        currentRoom.RoomType,
                        currentRoom.RoomId
                    );

            if (!started)
            {
                ballCollection
                    ?.SetBallsVisible(
                        false
                    );

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

        ballCollection
            ?.SetBallsVisible(
                false
            );

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

    private void HandleSecretRoomStateChanged()
    {
        NavigationAvailabilityChanged
            ?.Invoke();
    }

    private void HandleRoomCleared()
    {
        if (currentRoom != null &&
            currentRoom.IsCombatRoom)
        {
            bool wasNewlyCleared =
                clearedCombatRoomIds.Add(
                    currentRoom.RoomId
                );

            ballCollection
                ?.SetBallsVisible(
                    false
                );

            if (wasNewlyCleared)
            {
                Debug.Log(
                    "StageRoomNavigator: " +
                    $"방 {currentRoom.RoomId} 클리어 상태 저장",
                    this
                );

                CombatRoomCleared?.Invoke(
                    currentRoom
                );
            }
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
