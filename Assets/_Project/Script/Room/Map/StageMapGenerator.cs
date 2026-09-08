using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class StageMapGenerator :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private SecretRoomCoordinateSelector
        secretRoomCoordinateSelector;

    [SerializeField]
    private SecretRoomState secretRoomState;

    [Header("Stage")]

    [SerializeField, Min(1)]
    private int testStageNumber = 1;

    [Header("Room Count")]

    [Tooltip(
        "첫 번째 스테이지의 최소 방 개수입니다. " +
        "필수 방 구성을 위해 최소 9 이상이 필요합니다."
    )]
    [SerializeField, Min(9)]
    private int baseRoomCount = 9;

    [Tooltip(
        "스테이지가 증가할 때마다 추가되는 방 개수입니다."
    )]
    [SerializeField, Min(0)]
    private int additionalRoomsPerStage = 2;

    [SerializeField, Min(9)]
    private int maximumRoomCount = 20;

    [Header("Named Rooms")]

    [SerializeField, Min(1)]
    private int baseNamedRoomCount = 1;

    [SerializeField, Min(1)]
    private int stagesPerAdditionalNamedRoom = 2;

    [Header("Shape")]

    [Tooltip(
        "맵 중심에서 방이 생성될 수 있는 최대 거리입니다."
    )]
    [SerializeField, Min(2)]
    private int maximumDistanceFromCenter = 6;

    [Tooltip(
        "일반 방 하나가 가질 수 있는 최대 연결 수입니다. " +
        "3으로 설정하면 지나치게 복잡한 십자 교차로를 막습니다."
    )]
    [SerializeField, Range(2, 4)]
    private int maximumConnectionsPerRoom = 3;

    [Tooltip(
        "새 방을 생성할 때 기존 통로를 연장하기보다 " +
        "분기점에서 새 가지를 만들 확률입니다."
    )]
    [SerializeField, Range(0f, 1f)]
    private float branchPreference = 0.75f;

    [Tooltip(
        "보스·상점·연금술·이벤트·증강방에 필요한 막다른 방이 " +
        "부족할 때 맵 생성을 다시 시도하는 최대 횟수입니다."
    )]
    [SerializeField, Min(1)]
    private int maximumLayoutGenerationAttempts = 100;

    [Header("Secret Room")]

    [Tooltip(
        "활성화하면 기본 맵과 방 타입 배정이 완료된 뒤 " +
        "비밀방을 하나 추가합니다."
    )]
    [SerializeField]
    private bool generateSecretRoom = true;

    [Tooltip(
        "비밀방 생성에 실패해도 기본 맵 생성을 계속 진행합니다."
    )]
    [SerializeField]
    private bool allowMapWithoutSecretRoom = true;

    [Header("Random")]

    [Tooltip(
        "활성화하면 아래 고정 Seed를 사용합니다."
    )]
    [SerializeField]
    private bool useFixedSeed;

    [SerializeField]
    private int fixedSeed = 12345;

    [Header("Runtime Debug")]

    [SerializeField]
    private StageMap currentMap;

    private const int RequiredSpecialLeafCount = 5;

    private static readonly RoomDirection[]
        Directions =
        {
            RoomDirection.Up,
            RoomDirection.Right,
            RoomDirection.Down,
            RoomDirection.Left
        };

    public int TestStageNumber =>
        testStageNumber;

    public StageMap CurrentMap =>
        currentMap;

    public void RestoreMap(StageMap savedMap)
    {
        currentMap = savedMap;
        currentMap?.RebuildLookup();
        MapGenerated?.Invoke(currentMap);
    }

    public event Action<StageMap>
        MapGenerated;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
    }

    private void OnValidate()
    {
        FindReferences();
        NormalizeSettings();
    }

    private void FindReferences()
    {
        if (secretRoomCoordinateSelector == null)
        {
            secretRoomCoordinateSelector =
                GetComponent<
                    SecretRoomCoordinateSelector
                >();
        }

        if (secretRoomCoordinateSelector == null)
        {
            secretRoomCoordinateSelector =
                FindFirstObjectByType<
                    SecretRoomCoordinateSelector
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

    public StageMap GenerateTestStage()
    {
        return GenerateStage(
            testStageNumber
        );
    }

    public StageMap GenerateStage(
        int stageNumber)
    {
        FindReferences();
        NormalizeSettings();

        stageNumber =
            Mathf.Max(
                stageNumber,
                1
            );

        int targetRoomCount =
            CalculateRoomCount(
                stageNumber
            );

        int baseSeed =
            useFixedSeed
                ? fixedSeed
                : Environment.TickCount;

        StageMap generatedMap =
            null;

        StageMap bestFallbackMap =
            null;

        int bestFallbackLeafCount = -1;

        int successfulAttemptIndex = -1;

        for (int attemptIndex = 0;
             attemptIndex <
             maximumLayoutGenerationAttempts;
             attemptIndex++)
        {
            int attemptSeed =
                CreateAttemptSeed(
                    baseSeed,
                    attemptIndex
                );

            System.Random layoutRandom =
                new System.Random(
                    attemptSeed
                );

            StageMap attemptMap =
                GenerateLayout(
                    stageNumber,
                    targetRoomCount,
                    layoutRandom
                );

            if (attemptMap == null)
            {
                continue;
            }

            int leafCount =
                CountAvailableSpecialLeaves(
                    attemptMap
                );

            if (attemptMap.RoomCount ==
                    targetRoomCount &&
                leafCount >=
                    RequiredSpecialLeafCount)
            {
                generatedMap =
                    attemptMap;

                successfulAttemptIndex =
                    attemptIndex;

                break;
            }

            if (attemptMap.RoomCount >
                    (
                        bestFallbackMap != null
                            ? bestFallbackMap.RoomCount
                            : -1
                    ) ||
                (
                    bestFallbackMap != null &&
                    attemptMap.RoomCount ==
                    bestFallbackMap.RoomCount &&
                    leafCount >
                    bestFallbackLeafCount
                ))
            {
                bestFallbackMap =
                    attemptMap;

                bestFallbackLeafCount =
                    leafCount;
            }
        }

        if (generatedMap == null)
        {
            generatedMap =
                bestFallbackMap;

            Debug.LogError(
                "StageMapGenerator: " +
                "필수 특수방을 모두 막다른 방에 배치할 수 있는 " +
                "맵 생성에 실패했습니다. " +
                $"시도 횟수={maximumLayoutGenerationAttempts}, " +
                $"최고 막다른 방 수={bestFallbackLeafCount}",
                this
            );
        }

        if (generatedMap == null)
        {
            Debug.LogError(
                "StageMapGenerator: " +
                "맵 레이아웃을 생성하지 못했습니다.",
                this
            );

            ClearSecretRoomState();

            return null;
        }

        int typeAssignmentSeed =
            CreateAttemptSeed(
                baseSeed,
                Mathf.Max(
                    successfulAttemptIndex,
                    0
                ) +
                maximumLayoutGenerationAttempts
            );

        System.Random typeRandom =
            new System.Random(
                typeAssignmentSeed
            );

        bool assignedRoomTypes =
            AssignRoomTypes(
                generatedMap,
                stageNumber,
                typeRandom
            );

        if (!assignedRoomTypes)
        {
            Debug.LogError(
                "StageMapGenerator: " +
                "필수 방 타입을 모두 배치하지 못했습니다.",
                this
            );
        }

        bool secretRoomCreated =
            TryCreateSecretRoom(
                generatedMap,
                baseSeed,
                successfulAttemptIndex
            );

        if (generateSecretRoom &&
            !secretRoomCreated &&
            !allowMapWithoutSecretRoom)
        {
            Debug.LogError(
                "StageMapGenerator: " +
                "비밀방 생성이 필수이지만 비밀방을 " +
                "생성하지 못했습니다.",
                this
            );

            currentMap =
                null;

            ClearSecretRoomState();

            return null;
        }

        generatedMap.RebuildLookup();

        currentMap =
            generatedMap;

        if (currentMap.Validate(
                out string validationMessage))
        {
            Debug.Log(
                "StageMapGenerator: " +
                $"스테이지 {stageNumber} 맵 생성 완료. " +
                $"Base Seed={baseSeed}, " +
                $"Layout Attempt={successfulAttemptIndex + 1}, " +
                $"SecretRoomCreated={secretRoomCreated}, " +
                $"{validationMessage}",
                this
            );
        }
        else
        {
            Debug.LogError(
                "StageMapGenerator: 생성된 맵이 " +
                $"유효하지 않습니다. " +
                $"{validationMessage}",
                this
            );
        }

        LogRoomTypeCounts(
            currentMap
        );

        MapGenerated?.Invoke(
            currentMap
        );

        return currentMap;
    }

    private StageMap GenerateLayout(
        int stageNumber,
        int targetRoomCount,
        System.Random random)
    {
        if (random == null)
        {
            return null;
        }

        StageMap generatedMap =
            new StageMap(
                stageNumber
            );

        RoomNode startRoom =
            new RoomNode(
                0,
                Vector2Int.zero,
                RoomType.Start
            );

        if (!generatedMap.AddRoom(
                startRoom
            ))
        {
            return null;
        }

        generatedMap.SetStartRoom(
            startRoom.RoomId
        );

        int nextRoomId = 1;

        int failedAttempts = 0;

        int maximumPlacementAttempts =
            targetRoomCount *
            200;

        while (generatedMap.RoomCount <
                   targetRoomCount &&
               failedAttempts <
                   maximumPlacementAttempts)
        {
            RoomNode parentRoom =
                SelectExpansionRoom(
                    generatedMap,
                    random
                );

            if (parentRoom == null)
            {
                failedAttempts++;

                continue;
            }

            List<RoomDirection>
                availableDirections =
                    GetAvailableDirections(
                        generatedMap,
                        parentRoom
                    );

            if (availableDirections.Count ==
                0)
            {
                failedAttempts++;

                continue;
            }

            RoomDirection direction =
                availableDirections[
                    random.Next(
                        availableDirections.Count
                    )
                ];

            Vector2Int newPosition =
                parentRoom.GridPosition +
                RoomDirectionUtility.ToOffset(
                    direction
                );

            RoomNode newRoom =
                new RoomNode(
                    nextRoomId,
                    newPosition,
                    RoomType.NormalCombat
                );

            if (!generatedMap.AddRoom(
                    newRoom
                ))
            {
                failedAttempts++;

                continue;
            }

            bool connected =
                generatedMap.ConnectRooms(
                    parentRoom.RoomId,
                    newRoom.RoomId
                );

            if (!connected)
            {
                Debug.LogError(
                    "StageMapGenerator: " +
                    "새 방을 부모 방과 연결하지 못했습니다.",
                    this
                );

                return generatedMap;
            }

            nextRoomId++;

            failedAttempts = 0;
        }

        return generatedMap;
    }

    private bool TryCreateSecretRoom(
        StageMap map,
        int baseSeed,
        int successfulAttemptIndex)
    {
        ClearSecretRoomState();

        if (!generateSecretRoom)
        {
            return false;
        }

        if (map == null)
        {
            return false;
        }

        if (secretRoomCoordinateSelector == null)
        {
            Debug.LogError(
                "StageMapGenerator: " +
                "SecretRoomCoordinateSelector가 없어 " +
                "비밀방을 생성할 수 없습니다.",
                this
            );

            return false;
        }

        int secretRoomSeed =
            CreateAttemptSeed(
                baseSeed,
                maximumLayoutGenerationAttempts *
                    2 +
                Mathf.Max(
                    successfulAttemptIndex,
                    0
                ) +
                1
            );

        System.Random secretRoomRandom =
            new System.Random(
                secretRoomSeed
            );

        bool foundPosition =
            secretRoomCoordinateSelector
                .TryFindSecretRoomPosition(
                    map,
                    secretRoomRandom,
                    out Vector2Int secretRoomPosition
                );

        if (!foundPosition)
        {
            Debug.LogWarning(
                "StageMapGenerator: " +
                "비밀방 후보 좌표를 찾지 못했습니다.",
                this
            );

            return false;
        }

        List<RoomNode> entranceRooms =
            secretRoomCoordinateSelector
                .GetEntranceRooms(
                    map,
                    secretRoomPosition
                );

        if (entranceRooms == null ||
            entranceRooms.Count <= 0)
        {
            Debug.LogError(
                "StageMapGenerator: " +
                "비밀방 좌표를 찾았지만 연결할 입구 방이 없습니다. " +
                $"Position={secretRoomPosition}",
                this
            );

            return false;
        }

        int secretRoomId =
            FindNextRoomId(
                map
            );

        RoomNode secretRoom =
            new RoomNode(
                secretRoomId,
                secretRoomPosition,
                RoomType.Secret
            );

        if (!map.AddRoom(
                secretRoom
            ))
        {
            Debug.LogError(
                "StageMapGenerator: " +
                "비밀방을 StageMap에 추가하지 못했습니다. " +
                $"RoomId={secretRoomId}, " +
                $"Position={secretRoomPosition}",
                this
            );

            return false;
        }

        List<SecretRoomEntrance> entrances =
            new List<SecretRoomEntrance>();

        for (int i = 0;
             i < entranceRooms.Count;
             i++)
        {
            RoomNode entranceRoom =
                entranceRooms[i];

            if (entranceRoom == null)
            {
                continue;
            }

            bool connected =
                map.ConnectRooms(
                    entranceRoom.RoomId,
                    secretRoom.RoomId
                );

            if (!connected)
            {
                Debug.LogWarning(
                    "StageMapGenerator: " +
                    "비밀방 입구 연결에 실패했습니다. " +
                    $"EntranceRoomId={entranceRoom.RoomId}, " +
                    $"SecretRoomId={secretRoom.RoomId}",
                    this
                );

                continue;
            }

            if (!TryGetDirection(
                    entranceRoom.GridPosition,
                    secretRoom.GridPosition,
                    out RoomDirection direction))
            {
                Debug.LogError(
                    "StageMapGenerator: " +
                    "비밀방 입구 방향을 계산하지 못했습니다. " +
                    $"EntrancePosition=" +
                    $"{entranceRoom.GridPosition}, " +
                    $"SecretPosition={secretRoom.GridPosition}",
                    this
                );

                continue;
            }

            SecretRoomEntrance entrance =
                new SecretRoomEntrance(
                    secretRoom.RoomId,
                    entranceRoom.RoomId,
                    direction
                );

            entrances.Add(
                entrance
            );
        }

        if (entrances.Count <= 0)
        {
            Debug.LogError(
                "StageMapGenerator: " +
                "비밀방은 생성했지만 유효한 입구를 " +
                "하나도 만들지 못했습니다.",
                this
            );

            return false;
        }

        map.RebuildLookup();

        if (secretRoomState != null)
        {
            secretRoomState.Initialize(
                secretRoom.RoomId,
                entrances
            );
        }
        else
        {
            Debug.LogWarning(
                "StageMapGenerator: " +
                "SecretRoomState가 없어 생성된 비밀방의 " +
                "잠금 상태를 초기화하지 못했습니다.",
                this
            );
        }

        Debug.Log(
            "StageMapGenerator: " +
            "비밀방 생성 완료\n" +
            $"RoomId={secretRoom.RoomId}\n" +
            $"Position={secretRoom.GridPosition}\n" +
            $"EntranceCount={entrances.Count}\n" +
            $"Seed={secretRoomSeed}",
            this
        );

        return true;
    }

    private void ClearSecretRoomState()
    {
        if (secretRoomState == null)
        {
            return;
        }

        secretRoomState.Clear();
    }

    private static int FindNextRoomId(
        StageMap map)
    {
        if (map == null ||
            map.Rooms == null)
        {
            return 0;
        }

        int highestRoomId = -1;

        IReadOnlyList<RoomNode> rooms =
            map.Rooms;

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

            highestRoomId =
                Mathf.Max(
                    highestRoomId,
                    room.RoomId
                );
        }

        return highestRoomId + 1;
    }

    private static bool TryGetDirection(
        Vector2Int originPosition,
        Vector2Int targetPosition,
        out RoomDirection direction)
    {
        Vector2Int difference =
            targetPosition -
            originPosition;

        if (difference == Vector2Int.up)
        {
            direction =
                RoomDirection.Up;

            return true;
        }

        if (difference == Vector2Int.right)
        {
            direction =
                RoomDirection.Right;

            return true;
        }

        if (difference == Vector2Int.down)
        {
            direction =
                RoomDirection.Down;

            return true;
        }

        if (difference == Vector2Int.left)
        {
            direction =
                RoomDirection.Left;

            return true;
        }

        direction =
            default;

        return false;
    }

    private static int CreateAttemptSeed(
        int baseSeed,
        int attemptIndex)
    {
        unchecked
        {
            return
                baseSeed +
                attemptIndex *
                7919;
        }
    }

    private int CalculateRoomCount(
        int stageNumber)
    {
        int stageAdditionalRooms =
            Mathf.Max(
                stageNumber - 1,
                0
            ) *
            additionalRoomsPerStage;

        return Mathf.Clamp(
            baseRoomCount +
            stageAdditionalRooms,
                9,
            maximumRoomCount
        );
    }

    private RoomNode SelectExpansionRoom(
        StageMap map,
        System.Random random)
    {
        if (map == null ||
            random == null ||
            map.RoomCount <= 0)
        {
            return null;
        }

        List<RoomNode> candidates =
            new List<RoomNode>();

        IReadOnlyList<RoomNode> rooms =
            map.Rooms;

        for (int i = 0;
             i < rooms.Count;
             i++)
        {
            RoomNode room =
                rooms[i];

            if (room == null ||
                room.ConnectionCount >=
                maximumConnectionsPerRoom)
            {
                continue;
            }

            if (GetAvailableDirections(
                    map,
                    room
                ).Count == 0)
            {
                continue;
            }

            candidates.Add(
                room
            );
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        bool preferBranch =
            random.NextDouble() <=
            branchPreference;

        if (!preferBranch)
        {
            return candidates[
                random.Next(
                    candidates.Count
                )
            ];
        }

        int highestConnectionCount =
            int.MinValue;

        for (int i = 0;
             i < candidates.Count;
             i++)
        {
            RoomNode candidate =
                candidates[i];

            if (candidate == null)
            {
                continue;
            }

            highestConnectionCount =
                Mathf.Max(
                    highestConnectionCount,
                    candidate.ConnectionCount
                );
        }

        List<RoomNode> branchCandidates =
            new List<RoomNode>();

        for (int i = 0;
             i < candidates.Count;
             i++)
        {
            RoomNode candidate =
                candidates[i];

            if (candidate == null ||
                candidate.ConnectionCount !=
                highestConnectionCount)
            {
                continue;
            }

            branchCandidates.Add(
                candidate
            );
        }

        if (branchCandidates.Count == 0)
        {
            return candidates[
                random.Next(
                    candidates.Count
                )
            ];
        }

        return branchCandidates[
            random.Next(
                branchCandidates.Count
            )
        ];
    }

    private List<RoomDirection>
        GetAvailableDirections(
            StageMap map,
            RoomNode originRoom)
    {
        List<RoomDirection>
            availableDirections =
                new List<RoomDirection>();

        if (map == null ||
            originRoom == null ||
            originRoom.ConnectionCount >=
            maximumConnectionsPerRoom)
        {
            return availableDirections;
        }

        for (int i = 0;
             i < Directions.Length;
             i++)
        {
            RoomDirection direction =
                Directions[i];

            Vector2Int targetPosition =
                originRoom.GridPosition +
                RoomDirectionUtility.ToOffset(
                    direction
                );

            if (!IsInsideMapBounds(
                    targetPosition
                ))
            {
                continue;
            }

            if (map.ContainsPosition(
                    targetPosition
                ))
            {
                continue;
            }

            if (!HasOnlyExpectedAdjacentRoom(
                    map,
                    originRoom,
                    targetPosition
                ))
            {
                continue;
            }

            availableDirections.Add(
                direction
            );
        }

        return availableDirections;
    }

    private bool IsInsideMapBounds(
        Vector2Int position)
    {
        return
            Mathf.Abs(
                position.x
            ) <=
            maximumDistanceFromCenter &&
            Mathf.Abs(
                position.y
            ) <=
            maximumDistanceFromCenter;
    }

    private static bool
        HasOnlyExpectedAdjacentRoom(
            StageMap map,
            RoomNode expectedParent,
            Vector2Int targetPosition)
    {
        if (map == null ||
            expectedParent == null)
        {
            return false;
        }

        int adjacentRoomCount = 0;

        bool isParentAdjacent =
            false;

        for (int i = 0;
             i < Directions.Length;
             i++)
        {
            Vector2Int adjacentPosition =
                targetPosition +
                RoomDirectionUtility.ToOffset(
                    Directions[i]
                );

            RoomNode adjacentRoom =
                map.GetRoomAt(
                    adjacentPosition
                );

            if (adjacentRoom == null)
            {
                continue;
            }

            adjacentRoomCount++;

            if (adjacentRoom.RoomId ==
                expectedParent.RoomId)
            {
                isParentAdjacent =
                    true;
            }
        }

        return
            adjacentRoomCount == 1 &&
            isParentAdjacent;
    }

    private bool AssignRoomTypes(
        StageMap map,
        int stageNumber,
        System.Random random)
    {
        if (map == null ||
            random == null ||
            map.RoomCount <= 1)
        {
            return false;
        }

        RoomNode startRoom =
            map.StartRoom;

        Dictionary<int, int> distances =
            CalculateDistancesFromStart(
                map
            );

        RoomNode bossRoom =
            FindFarthestRoom(
                map,
                distances,
                startRoom != null
                    ? startRoom.RoomId
                    : -1,
                true
            );

        if (bossRoom == null)
        {
            Debug.LogError(
                "StageMapGenerator: " +
                "보스방으로 사용할 막다른 방이 없습니다.",
                this
            );

            return false;
        }

        map.SetBossRoom(
            bossRoom.RoomId
        );

        List<RoomNode> specialLeafRooms =
            BuildSpecialRoomCandidates(
                map,
                true
            );

        bool shopAssigned =
            AssignOneRoomType(
                specialLeafRooms,
                RoomType.Shop,
                random
            );

        bool alchemyAssigned =
            AssignOneRoomType(
                specialLeafRooms,
                RoomType.Alchemy,
                random
            );

        bool eventAssigned =
            AssignOneRoomType(
                specialLeafRooms,
                RoomType.Event,
                random
            );

        bool augmentAssigned =
            AssignOneRoomType(
                specialLeafRooms,
                RoomType.Augment,
                random
            );

        if (!shopAssigned ||
            !alchemyAssigned ||
            !eventAssigned ||
            !augmentAssigned)
        {
            Debug.LogError(
                "StageMapGenerator: " +
                "상점·연금술·이벤트·증강방을 모두 막다른 방에 " +
                "배치하지 못했습니다.",
                this
            );

            return false;
        }

        // 네임드는 별도 방이 아니라 일반 전투방의 블록으로 출현한다.
        // NamedCombat 방은 신규 맵에 배치하지 않는다.
        return true;
    }

    private Dictionary<int, int>
        CalculateDistancesFromStart(
            StageMap map)
    {
        Dictionary<int, int> distances =
            new Dictionary<int, int>();

        if (map == null ||
            map.StartRoom == null)
        {
            return distances;
        }

        Queue<int> pendingRoomIds =
            new Queue<int>();

        distances.Add(
            map.StartRoomId,
            0
        );

        pendingRoomIds.Enqueue(
            map.StartRoomId
        );

        while (pendingRoomIds.Count > 0)
        {
            int currentRoomId =
                pendingRoomIds.Dequeue();

            int currentDistance =
                distances[currentRoomId];

            List<RoomNode> connectedRooms =
                map.GetConnectedRooms(
                    currentRoomId
                );

            for (int i = 0;
                 i < connectedRooms.Count;
                 i++)
            {
                RoomNode connectedRoom =
                    connectedRooms[i];

                if (connectedRoom == null ||
                    distances.ContainsKey(
                        connectedRoom.RoomId))
                {
                    continue;
                }

                distances.Add(
                    connectedRoom.RoomId,
                    currentDistance + 1
                );

                pendingRoomIds.Enqueue(
                    connectedRoom.RoomId
                );
            }
        }

        return distances;
    }

    private RoomNode FindFarthestRoom(
        StageMap map,
        Dictionary<int, int> distances,
        int excludedRoomId,
        bool requireLeaf)
    {
        if (map == null ||
            distances == null)
        {
            return null;
        }

        RoomNode farthestRoom =
            null;

        int farthestDistance =
            int.MinValue;

        IReadOnlyList<RoomNode> rooms =
            map.Rooms;

        for (int i = 0;
             i < rooms.Count;
             i++)
        {
            RoomNode room =
                rooms[i];

            if (room == null ||
                room.RoomId ==
                excludedRoomId ||
                room.RoomType !=
                RoomType.NormalCombat)
            {
                continue;
            }

            if (requireLeaf &&
                room.ConnectionCount != 1)
            {
                continue;
            }

            if (!distances.TryGetValue(
                    room.RoomId,
                    out int distance))
            {
                continue;
            }

            if (distance <=
                farthestDistance)
            {
                continue;
            }

            farthestDistance =
                distance;

            farthestRoom =
                room;
        }

        return farthestRoom;
    }

    private List<RoomNode>
        BuildSpecialRoomCandidates(
            StageMap map,
            bool requireLeaf)
    {
        List<RoomNode> candidates =
            new List<RoomNode>();

        if (map == null)
        {
            return candidates;
        }

        IReadOnlyList<RoomNode> rooms =
            map.Rooms;

        for (int i = 0;
             i < rooms.Count;
             i++)
        {
            RoomNode room =
                rooms[i];

            if (room == null ||
                room.RoomType !=
                RoomType.NormalCombat)
            {
                continue;
            }

            if (requireLeaf &&
                room.ConnectionCount != 1)
            {
                continue;
            }

            candidates.Add(
                room
            );
        }

        return candidates;
    }

    private bool AssignOneRoomType(
        List<RoomNode> availableRooms,
        RoomType roomType,
        System.Random random)
    {
        if (availableRooms == null ||
            random == null ||
            availableRooms.Count == 0)
        {
            Debug.LogWarning(
                "StageMapGenerator: " +
                $"{roomType} 방을 배치할 " +
                "후보 방이 없습니다.",
                this
            );

            return false;
        }

        RoomNode selectedRoom =
            availableRooms[
                random.Next(
                    availableRooms.Count
                )
            ];

        if (selectedRoom == null)
        {
            return false;
        }

        selectedRoom.SetRoomType(
            roomType
        );

        availableRooms.Remove(
            selectedRoom
        );

        return true;
    }

    private int CalculateNamedRoomCount(
        int stageNumber,
        int availableRoomCount)
    {
        int additionalNamedRooms =
            Mathf.Max(
                stageNumber - 1,
                0
            ) /
            stagesPerAdditionalNamedRoom;

        int requestedNamedRooms =
            baseNamedRoomCount +
            additionalNamedRooms;

        return Mathf.Clamp(
            requestedNamedRooms,
            0,
            availableRoomCount
        );
    }

    private static int
        CountAvailableSpecialLeaves(
            StageMap map)
    {
        if (map == null)
        {
            return 0;
        }

        int count = 0;

        IReadOnlyList<RoomNode> rooms =
            map.Rooms;

        for (int i = 0;
             i < rooms.Count;
             i++)
        {
            RoomNode room =
                rooms[i];

            if (room == null ||
                room.RoomType !=
                RoomType.NormalCombat ||
                room.ConnectionCount != 1)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private void LogRoomTypeCounts(
        StageMap map)
    {
        if (map == null)
        {
            return;
        }

        Debug.Log(
            "StageMapGenerator: 방 구성\n" +
            $"Start={map.CountRoomsOfType(RoomType.Start)}, " +
            $"Normal={map.CountRoomsOfType(RoomType.NormalCombat)}, " +
            $"Named={map.CountRoomsOfType(RoomType.NamedCombat)}, " +
            $"Boss={map.CountRoomsOfType(RoomType.Boss)}, " +
            $"Shop={map.CountRoomsOfType(RoomType.Shop)}, " +
            $"Alchemy={map.CountRoomsOfType(RoomType.Alchemy)}, " +
            $"Event={map.CountRoomsOfType(RoomType.Event)}, " +
            $"Augment={map.CountRoomsOfType(RoomType.Augment)}, " +
            $"Secret={map.CountRoomsOfType(RoomType.Secret)}",
            this
        );
    }

    private void NormalizeSettings()
    {
        testStageNumber =
            Mathf.Max(
                testStageNumber,
                1
            );

        baseRoomCount =
            Mathf.Max(
                baseRoomCount,
                9
            );

        additionalRoomsPerStage =
            Mathf.Max(
                additionalRoomsPerStage,
                0
            );

        maximumRoomCount =
            Mathf.Max(
                maximumRoomCount,
                baseRoomCount
            );

        baseNamedRoomCount = 0;

        stagesPerAdditionalNamedRoom =
            Mathf.Max(
                stagesPerAdditionalNamedRoom,
                1
            );

        maximumDistanceFromCenter =
            Mathf.Max(
                maximumDistanceFromCenter,
                2
            );

        maximumConnectionsPerRoom =
            Mathf.Clamp(
                maximumConnectionsPerRoom,
                2,
                4
            );

        branchPreference =
            Mathf.Clamp01(
                branchPreference
            );

        maximumLayoutGenerationAttempts =
            Mathf.Max(
                maximumLayoutGenerationAttempts,
                1
            );
    }
}
