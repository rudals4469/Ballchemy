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
        "새 방을 생성할 때 기존 방 중 연결 수가 적은 방을 " +
        "우선할 확률입니다."
    )]
    [SerializeField, Range(0f, 1f)]
    private float branchPreference = 0.75f;

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

        int seed =
            useFixedSeed
                ? fixedSeed
                : Environment.TickCount;

        System.Random random =
            new System.Random(
                seed
            );

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

        generatedMap.AddRoom(
            startRoom
        );

        generatedMap.SetStartRoom(
            startRoom.RoomId
        );

        int nextRoomId = 1;
        int failedAttempts = 0;
        int maximumAttempts =
            targetRoomCount *
            100;

        while (generatedMap.RoomCount <
                   targetRoomCount &&
               failedAttempts <
                   maximumAttempts)
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
                RoomDirectionUtility
                    .ToOffset(
                        direction
                    );

            RoomNode newRoom =
                new RoomNode(
                    nextRoomId,
                    newPosition,
                    RoomType.NormalCombat
                );

            bool roomAdded =
                generatedMap.AddRoom(
                    newRoom
                );

            if (!roomAdded)
            {
                failedAttempts++;

                continue;
            }

            generatedMap.ConnectRooms(
                parentRoom.RoomId,
                newRoom.RoomId
            );

            /*
             * 새 방이 이미 존재하는 다른 방과도
             * 상하좌우로 맞닿아 있다면 연결한다.
             *
             * 이렇게 하면 일부 맵에 순환 경로가 생긴다.
             */
            ConnectAdjacentRooms(
                generatedMap,
                newRoom
            );

            nextRoomId++;
            failedAttempts = 0;
        }

        if (generatedMap.RoomCount <
            targetRoomCount)
        {
            Debug.LogWarning(
                "StageMapGenerator: 목표 방 수를 " +
                "모두 생성하지 못했습니다. " +
                $"목표={targetRoomCount}, " +
                $"실제={generatedMap.RoomCount}",
                this
            );
        }

        AssignRoomTypes(
            generatedMap,
            stageNumber,
            random
        );

        generatedMap.RebuildLookup();

        currentMap =
            generatedMap;

        if (currentMap.Validate(
                out string validationMessage))
        {
            Debug.Log(
                "StageMapGenerator: " +
                $"스테이지 {stageNumber} 맵 생성 완료. " +
                $"Seed={seed}, " +
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
            map.RoomCount <= 0)
        {
            return null;
        }

        List<RoomNode> candidates =
            new List<RoomNode>();

        int lowestConnectionCount =
            int.MaxValue;

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

            if (GetAvailableDirections(
                    map,
                    room
                ).Count == 0)
            {
                continue;
            }

            if (room.ConnectionCount <
                lowestConnectionCount)
            {
                lowestConnectionCount =
                    room.ConnectionCount;
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

        List<RoomNode> branchCandidates =
            new List<RoomNode>();

        for (int i = 0;
             i < candidates.Count;
             i++)
        {
            RoomNode candidate =
                candidates[i];

            if (candidate.ConnectionCount !=
                lowestConnectionCount)
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
            originRoom == null)
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
                RoomDirectionUtility
                    .ToOffset(
                        direction
                    );

            if (Mathf.Abs(
                    targetPosition.x
                ) >
                maximumDistanceFromCenter ||
                Mathf.Abs(
                    targetPosition.y
                ) >
                maximumDistanceFromCenter)
            {
                continue;
            }

            if (map.ContainsPosition(
                    targetPosition))
            {
                continue;
            }

            availableDirections.Add(
                direction
            );
        }

        return availableDirections;
    }

    private void ConnectAdjacentRooms(
        StageMap map,
        RoomNode newRoom)
    {
        if (map == null ||
            newRoom == null)
        {
            return;
        }

        for (int i = 0;
             i < Directions.Length;
             i++)
        {
            Vector2Int adjacentPosition =
                newRoom.GridPosition +
                RoomDirectionUtility
                    .ToOffset(
                        Directions[i]
                    );

            RoomNode adjacentRoom =
                map.GetRoomAt(
                    adjacentPosition
                );

            if (adjacentRoom == null ||
                adjacentRoom.RoomId ==
                newRoom.RoomId)
            {
                continue;
            }

            map.ConnectRooms(
                newRoom.RoomId,
                adjacentRoom.RoomId
            );
        }
    }

    private void AssignRoomTypes(
        StageMap map,
        int stageNumber,
        System.Random random)
    {
        if (map == null ||
            map.RoomCount <= 1)
        {
            return;
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
            bossRoom =
                FindFarthestRoom(
                    map,
                    distances,
                    startRoom != null
                        ? startRoom.RoomId
                        : -1,
                    false
                );
        }

        if (bossRoom != null)
        {
            map.SetBossRoom(
                bossRoom.RoomId
            );
        }

        List<RoomNode> availableRooms =
            BuildSpecialRoomCandidates(
                map
            );

        AssignOneRoomType(
            availableRooms,
            RoomType.Shop,
            random,
            true
        );

        AssignOneRoomType(
            availableRooms,
            RoomType.Reward,
            random,
            true
        );

        AssignOneRoomType(
            availableRooms,
            RoomType.Event,
            random,
            true
        );

        int namedRoomCount =
            CalculateNamedRoomCount(
                stageNumber,
                availableRooms.Count
            );

        for (int i = 0;
             i < namedRoomCount;
             i++)
        {
            AssignOneRoomType(
                availableRooms,
                RoomType.NamedCombat,
                random,
                false
            );
        }
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
                excludedRoomId)
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
        StageMap map)
    {
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
                room.RoomType !=
                RoomType.NormalCombat)
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
        System.Random random,
        bool preferLeaf)
    {
        if (availableRooms == null ||
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

        List<RoomNode> preferredRooms =
            new List<RoomNode>();

        if (preferLeaf)
        {
            for (int i = 0;
                 i < availableRooms.Count;
                 i++)
            {
                RoomNode room =
                    availableRooms[i];

                if (room == null ||
                    room.ConnectionCount != 1)
                {
                    continue;
                }

                preferredRooms.Add(
                    room
                );
            }
        }

        List<RoomNode> source =
            preferredRooms.Count > 0
                ? preferredRooms
                : availableRooms;

        RoomNode selectedRoom =
            source[
                random.Next(
                    source.Count
                )
            ];

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

        branchPreference =
            Mathf.Clamp01(
                branchPreference
            );
    }
}