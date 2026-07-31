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
     * 보상 선택, 연출, 맵 전환 등 외부 시스템이
     * 방 이동 자체를 잠글 때 사용합니다.
     *
     * UI를 숨기는 것과 별개로 TryMove와
     * MoveToRoom 실행을 시스템 수준에서 막습니다.
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

        /*
         * RoomNavigationUI는 이 이벤트를 받아
         * CanMove 결과에 따라 화살표를 갱신합니다.
         *
         * 따라서 잠금 중에는 버튼이 숨고,
         * 잠금 해제 후에만 연결된 방향 버튼이 나타납니다.
         */
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
        /*
         * 후퇴는 일반 CanNavigate 조건을 사용하지 않습니다.
         * 미클리어 전투 중에도 허용되어야 하기 때문입니다.
         *
         * 단, 보상 선택처럼 외부 이동 잠금이 걸린 동안에는
         * 후퇴를 포함한 모든 방 이동을 차단합니다.
         */
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

        return MoveToRoom(
            previousRoom
        );
    }

    private bool MoveToRoom(
        RoomNode targetRoom)
    {
        /*
         * 모든 이동 경로의 마지막 진입점에서도
         * 외부 잠금을 검사합니다.
         *
         * 이후 맵 노드 클릭 이동 같은 새로운 호출처가
         * 추가되어도 잠금을 우회할 수 없습니다.
         */
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
                /*
                 * 이미 클리어한 전투방은 빈 이동 경로로
                 * 사용하므로 공을 표시하지 않습니다.
                 */
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

            /*
             * 아직 클리어하지 않은 전투방에서만
             * 공을 화면에 표시하고 전투를 시작합니다.
             */
            ballCollection
                ?.SetBallsVisible(
                    true
                );

            bool started =
                blockGridManager
                    .StartRoomCombat(
                        currentRoom.RoomType
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

        /*
         * 시작방과 현재 구현되지 않은 특수방에서는
         * 공 보유 데이터는 유지하되 화면에는 표시하지 않습니다.
         */
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

    private void HandleRoomCleared()
    {
        if (currentRoom != null &&
            currentRoom.IsCombatRoom)
        {
            clearedCombatRoomIds.Add(
                currentRoom.RoomId
            );

            /*
             * 전투가 끝난 즉시 공을 숨깁니다.
             *
             * 이후 보상으로 새 공을 받아도
             * BallCollection의 현재 표시 상태를 따라
             * 숨김 상태로 생성됩니다.
             */
            ballCollection
                ?.SetBallsVisible(
                    false
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