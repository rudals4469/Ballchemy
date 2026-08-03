using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StageKeyRoomSelector :
    MonoBehaviour
{
    [Header("References")]

    [Tooltip(
        "현재 스테이지 맵을 생성하고 " +
        "MapGenerated 이벤트를 발생시키는 컴포넌트입니다."
    )]
    [SerializeField]
    private StageMapGenerator stageMapGenerator;

    [Tooltip(
        "현재 스테이지의 열쇠 방과 " +
        "열쇠 보유 상태를 관리하는 컴포넌트입니다."
    )]
    [SerializeField]
    private StageKeyState stageKeyState;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog = true;

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
         * 다른 컴포넌트가 Start 이전에 맵을 생성한 경우에만
         * 현재 맵을 처리합니다.
         *
         * Inspector에 남아 있는 비어 있는 직렬화 맵은
         * IsGeneratedMap()에서 제외됩니다.
         */
        TryAssignCurrentMapIfNeeded();
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
        if (stageMapGenerator == null)
        {
            stageMapGenerator =
                GetComponent<StageMapGenerator>();
        }

        if (stageKeyState == null)
        {
            stageKeyState =
                GetComponent<StageKeyState>();
        }

        if (stageMapGenerator == null &&
            Application.isPlaying)
        {
            stageMapGenerator =
                FindFirstObjectByType<
                    StageMapGenerator
                >();
        }

        if (stageKeyState == null &&
            Application.isPlaying)
        {
            stageKeyState =
                FindFirstObjectByType<
                    StageKeyState
                >();
        }
    }

    private void ValidateReferences()
    {
        if (stageMapGenerator == null)
        {
            Debug.LogError(
                "StageKeyRoomSelector: " +
                "StageMapGenerator가 연결되지 않았습니다.",
                this
            );
        }

        if (stageKeyState == null)
        {
            Debug.LogError(
                "StageKeyRoomSelector: " +
                "StageKeyState가 연결되지 않았습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (stageMapGenerator == null)
        {
            return;
        }

        stageMapGenerator.MapGenerated -=
            HandleMapGenerated;

        stageMapGenerator.MapGenerated +=
            HandleMapGenerated;
    }

    private void UnsubscribeEvents()
    {
        if (stageMapGenerator == null)
        {
            return;
        }

        stageMapGenerator.MapGenerated -=
            HandleMapGenerated;
    }

    private void HandleMapGenerated(
        StageMap generatedMap)
    {
        if (!IsGeneratedMap(
                generatedMap
            ))
        {
            Debug.LogError(
                "StageKeyRoomSelector: " +
                "MapGenerated 이벤트로 전달된 맵이 " +
                "유효한 생성 완료 상태가 아닙니다.",
                this
            );

            return;
        }

        AssignKeyRoom(
            generatedMap
        );
    }

    private void TryAssignCurrentMapIfNeeded()
    {
        if (stageMapGenerator == null ||
            stageKeyState == null ||
            stageKeyState.HasAssignedKeyRoom)
        {
            return;
        }

        StageMap currentMap =
            stageMapGenerator.CurrentMap;

        /*
         * StageMapGenerator의 Inspector 직렬화 필드에
         * 비어 있는 StageMap이 존재할 수 있습니다.
         *
         * StageNumber 0, RoomCount 0인 임시 데이터는
         * 실제 생성된 맵이 아니므로 처리하지 않습니다.
         */
        if (!IsGeneratedMap(
                currentMap
            ))
        {
            return;
        }

        AssignKeyRoom(
            currentMap
        );
    }

    public bool AssignKeyRoom(
        StageMap stageMap)
    {
        if (!IsGeneratedMap(
                stageMap
            ))
        {
            Debug.LogWarning(
                "StageKeyRoomSelector: " +
                "생성이 완료되지 않은 맵에는 " +
                "열쇠 방을 지정할 수 없습니다.",
                this
            );

            return false;
        }

        if (stageKeyState == null)
        {
            Debug.LogError(
                "StageKeyRoomSelector: " +
                "StageKeyState가 연결되지 않아 " +
                "열쇠 방을 지정할 수 없습니다.",
                this
            );

            return false;
        }

        stageKeyState.ResetForNewStage();

        List<RoomNode> candidates =
            CollectCandidates(
                stageMap
            );

        if (candidates.Count == 0)
        {
            Debug.LogError(
                "StageKeyRoomSelector: " +
                "생성 완료된 맵에 열쇠 방 후보인 " +
                "일반 전투방이 없습니다. " +
                $"Stage={stageMap.StageNumber}, " +
                $"RoomCount={stageMap.RoomCount}",
                this
            );

            return false;
        }

        SortRoomsByRoomId(
            candidates
        );

        int mapHash =
            CalculateStableMapHash(
                stageMap
            );

        int selectedIndex =
            GetPositiveModulo(
                mapHash,
                candidates.Count
            );

        RoomNode selectedRoom =
            candidates[selectedIndex];

        bool assigned =
            stageKeyState.TryAssignKeyRoom(
                selectedRoom.RoomId
            );

        if (!assigned)
        {
            Debug.LogError(
                "StageKeyRoomSelector: " +
                "StageKeyState에 열쇠 방을 " +
                "저장하지 못했습니다. " +
                $"RoomId={selectedRoom.RoomId}",
                this
            );

            return false;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "StageKeyRoomSelector: " +
                "맵 구조를 기준으로 열쇠 방을 지정했습니다. " +
                $"Stage={stageMap.StageNumber}, " +
                $"Map Hash={mapHash}, " +
                $"Candidate Count={candidates.Count}, " +
                $"RoomId={selectedRoom.RoomId}",
                this
            );
        }

        return true;
    }

    private static bool IsGeneratedMap(
        StageMap stageMap)
    {
        if (stageMap == null)
        {
            return false;
        }

        if (stageMap.StageNumber < 1 ||
            stageMap.RoomCount <= 0)
        {
            return false;
        }

        if (stageMap.StartRoom == null)
        {
            return false;
        }

        return true;
    }

    private static List<RoomNode>
        CollectCandidates(
            StageMap stageMap)
    {
        List<RoomNode> candidates =
            new List<RoomNode>();

        if (stageMap == null)
        {
            return candidates;
        }

        IReadOnlyList<RoomNode> rooms =
            stageMap.Rooms;

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

    private static void SortRoomsByRoomId(
        List<RoomNode> rooms)
    {
        if (rooms == null)
        {
            return;
        }

        rooms.Sort(
            CompareRoomsById
        );
    }

    private static int CompareRoomsById(
        RoomNode first,
        RoomNode second)
    {
        if (ReferenceEquals(
                first,
                second
            ))
        {
            return 0;
        }

        if (first == null)
        {
            return 1;
        }

        if (second == null)
        {
            return -1;
        }

        return first.RoomId.CompareTo(
            second.RoomId
        );
    }

    private static int CalculateStableMapHash(
        StageMap stageMap)
    {
        unchecked
        {
            const int offsetBasis =
                (int)2166136261;

            const int prime =
                16777619;

            int hash =
                offsetBasis;

            hash =
                AddHashValue(
                    hash,
                    stageMap.StageNumber,
                    prime
                );

            hash =
                AddHashValue(
                    hash,
                    stageMap.StartRoomId,
                    prime
                );

            hash =
                AddHashValue(
                    hash,
                    stageMap.BossRoomId,
                    prime
                );

            List<RoomNode> sortedRooms =
                new List<RoomNode>();

            IReadOnlyList<RoomNode> rooms =
                stageMap.Rooms;

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

                sortedRooms.Add(
                    room
                );
            }

            SortRoomsByRoomId(
                sortedRooms
            );

            for (int i = 0;
                 i < sortedRooms.Count;
                 i++)
            {
                RoomNode room =
                    sortedRooms[i];

                hash =
                    AddHashValue(
                        hash,
                        room.RoomId,
                        prime
                    );

                hash =
                    AddHashValue(
                        hash,
                        room.GridPosition.x,
                        prime
                    );

                hash =
                    AddHashValue(
                        hash,
                        room.GridPosition.y,
                        prime
                    );

                hash =
                    AddHashValue(
                        hash,
                        (int)room.RoomType,
                        prime
                    );

                hash =
                    AddHashValue(
                        hash,
                        room.ConnectionCount,
                        prime
                    );

                List<int> connectedRoomIds =
                    new List<int>();

                IReadOnlyList<int> connections =
                    room.ConnectedRoomIds;

                if (connections != null)
                {
                    for (int connectionIndex = 0;
                         connectionIndex <
                         connections.Count;
                         connectionIndex++)
                    {
                        connectedRoomIds.Add(
                            connections[
                                connectionIndex
                            ]
                        );
                    }
                }

                connectedRoomIds.Sort();

                for (int connectionIndex = 0;
                     connectionIndex <
                     connectedRoomIds.Count;
                     connectionIndex++)
                {
                    hash =
                        AddHashValue(
                            hash,
                            connectedRoomIds[
                                connectionIndex
                            ],
                            prime
                        );
                }
            }

            return hash;
        }
    }

    private static int AddHashValue(
        int currentHash,
        int value,
        int prime)
    {
        unchecked
        {
            currentHash ^=
                value;

            currentHash *=
                prime;

            return currentHash;
        }
    }

    private static int GetPositiveModulo(
        int value,
        int divisor)
    {
        if (divisor <= 0)
        {
            return 0;
        }

        int remainder =
            value % divisor;

        return remainder >= 0
            ? remainder
            : remainder + divisor;
    }
}