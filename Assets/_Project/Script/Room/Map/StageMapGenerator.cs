using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class StageMapGenerator :
    MonoBehaviour
{
    [Header("Stage")]

    [SerializeField, Min(1)]
    private int testStageNumber = 1;

    [Header("Room Count")]

    [Tooltip(
        "첫 번째 스테이지의 최소 방 개수입니다. " +
        "필수 방 구성을 위해 최소 7 이상을 권장합니다."
    )]
    [SerializeField, Min(7)]
    private int baseRoomCount = 9;

    [Tooltip(
        "스테이지가 증가할 때마다 추가되는 방 개수입니다."
    )]
    [SerializeField, Min(0)]
    private int additionalRoomsPerStage = 2;

    [SerializeField, Min(7)]
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
        "보스·상점·보상·이벤트방에 필요한 막다른 방이 " +
        "부족할 때 맵 생성을 다시 시도하는 최대 횟수입니다."
    )]
    [SerializeField, Min(1)]
    private int maximumLayoutGenerationAttempts = 100;

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

    private const int RequiredSpecialLeafCount = 4;

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

    public event Action<StageMap>
        MapGenerated;

    private void Awake()
    {
        NormalizeSettings();
    }

    private void OnValidate()
    {
        NormalizeSettings();
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

            /*
             * 새 방은 생성 기준이 된 부모 방 하나와만
             * 연결합니다.
             *
             * 후보 위치를 선택할 때 다른 방과의 인접을
             * 이미 차단했으므로 맵 전체는 순환 없는
             * 트리 구조를 유지합니다.
             */
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
            7,
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

        /*
         * 후보 위치는 부모 방 하나하고만
         * 상하좌우로 맞닿아 있어야 합니다.
         *
         * 이 규칙으로 다음을 동시에 막습니다.
         *
         * - 연결되지 않은 방끼리 바로 붙는 구조
         * - 2x2 형태의 밀집 배치
         * - 의도하지 않은 순환 경로
         */
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

        bool rewardAssigned =
            AssignOneRoomType(
                specialLeafRooms,
                RoomType.Reward,
                random
            );

        bool eventAssigned =
            AssignOneRoomType(
                specialLeafRooms,
                RoomType.Event,
                random
            );

        if (!shopAssigned ||
            !rewardAssigned ||
            !eventAssigned)
        {
            Debug.LogError(
                "StageMapGenerator: " +
                "상점·보상·이벤트방을 모두 막다른 방에 " +
                "배치하지 못했습니다.",
                this
            );

            return false;
        }

        List<RoomNode> namedCandidates =
            BuildSpecialRoomCandidates(
                map,
                false
            );

        int namedRoomCount =
            CalculateNamedRoomCount(
                stageNumber,
                namedCandidates.Count
            );

        for (int i = 0;
             i < namedRoomCount;
             i++)
        {
            if (!AssignOneRoomType(
                    namedCandidates,
                    RoomType.NamedCombat,
                    random
                ))
            {
                break;
            }
        }

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
            $"Reward={map.CountRoomsOfType(RoomType.Reward)}, " +
            $"Event={map.CountRoomsOfType(RoomType.Event)}",
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
                7
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

        baseNamedRoomCount =
            Mathf.Max(
                baseNamedRoomCount,
                1
            );

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