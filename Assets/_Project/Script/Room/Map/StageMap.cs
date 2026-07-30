using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StageMap
{
    [SerializeField, Min(1)]
    private int stageNumber = 1;

    [SerializeField]
    private int startRoomId = -1;

    [SerializeField]
    private int bossRoomId = -1;

    [SerializeField]
    private List<RoomNode> rooms =
        new List<RoomNode>();

    /*
     * 조회 성능을 위한 런타임 캐시입니다.
     *
     * Unity 직렬화 대상이 아니므로 에디터 재컴파일,
     * 플레이 모드 전환, 역직렬화 이후 null일 수 있습니다.
     * 모든 접근 전에 EnsureLookupCollections()로 복구합니다.
     */
    [NonSerialized]
    private Dictionary<int, RoomNode>
        roomById;

    [NonSerialized]
    private Dictionary<Vector2Int, RoomNode>
        roomByPosition;

    public int StageNumber =>
        stageNumber;

    public int StartRoomId =>
        startRoomId;

    public int BossRoomId =>
        bossRoomId;

    public int RoomCount =>
        rooms != null
            ? rooms.Count
            : 0;

    public IReadOnlyList<RoomNode> Rooms
    {
        get
        {
            EnsureRoomList();

            return rooms;
        }
    }

    public RoomNode StartRoom =>
        GetRoomById(
            startRoomId
        );

    public RoomNode BossRoom =>
        GetRoomById(
            bossRoomId
        );

    public StageMap(
        int stageNumber)
    {
        this.stageNumber =
            Mathf.Max(
                stageNumber,
                1
            );

        startRoomId = -1;
        bossRoomId = -1;

        rooms =
            new List<RoomNode>();

        InitializeLookupCollections();

        RebuildLookup();
    }

    public void Clear()
    {
        EnsureRoomList();

        rooms.Clear();

        startRoomId = -1;
        bossRoomId = -1;

        RebuildLookup();
    }

    public bool AddRoom(
        RoomNode room)
    {
        if (room == null)
        {
            return false;
        }

        EnsureLookup();

        if (roomById.ContainsKey(
                room.RoomId))
        {
            Debug.LogWarning(
                "StageMap: 중복된 Room ID를 " +
                "추가할 수 없습니다. " +
                $"Room ID={room.RoomId}"
            );

            return false;
        }

        if (roomByPosition.ContainsKey(
                room.GridPosition))
        {
            Debug.LogWarning(
                "StageMap: 동일한 좌표에 방을 " +
                "추가할 수 없습니다. " +
                $"좌표={room.GridPosition}"
            );

            return false;
        }

        rooms.Add(
            room
        );

        roomById.Add(
            room.RoomId,
            room
        );

        roomByPosition.Add(
            room.GridPosition,
            room
        );

        return true;
    }

    public RoomNode GetRoomById(
        int roomId)
    {
        EnsureLookup();

        roomById.TryGetValue(
            roomId,
            out RoomNode room
        );

        return room;
    }

    public RoomNode GetRoomAt(
        Vector2Int gridPosition)
    {
        EnsureLookup();

        roomByPosition.TryGetValue(
            gridPosition,
            out RoomNode room
        );

        return room;
    }

    public bool ContainsPosition(
        Vector2Int gridPosition)
    {
        EnsureLookup();

        return roomByPosition.ContainsKey(
            gridPosition
        );
    }

    public void SetStartRoom(
        int roomId)
    {
        RoomNode room =
            GetRoomById(
                roomId
            );

        if (room == null)
        {
            Debug.LogWarning(
                "StageMap: 존재하지 않는 방을 " +
                "시작방으로 설정할 수 없습니다. " +
                $"Room ID={roomId}"
            );

            return;
        }

        startRoomId =
            roomId;

        room.SetRoomType(
            RoomType.Start
        );
    }

    public void SetBossRoom(
        int roomId)
    {
        RoomNode room =
            GetRoomById(
                roomId
            );

        if (room == null)
        {
            Debug.LogWarning(
                "StageMap: 존재하지 않는 방을 " +
                "보스방으로 설정할 수 없습니다. " +
                $"Room ID={roomId}"
            );

            return;
        }

        bossRoomId =
            roomId;

        room.SetRoomType(
            RoomType.Boss
        );
    }

    public bool ConnectRooms(
        int firstRoomId,
        int secondRoomId)
    {
        RoomNode firstRoom =
            GetRoomById(
                firstRoomId
            );

        RoomNode secondRoom =
            GetRoomById(
                secondRoomId
            );

        if (firstRoom == null ||
            secondRoom == null ||
            firstRoom == secondRoom)
        {
            return false;
        }

        Vector2Int difference =
            secondRoom.GridPosition -
            firstRoom.GridPosition;

        int manhattanDistance =
            Mathf.Abs(
                difference.x
            ) +
            Mathf.Abs(
                difference.y
            );

        if (manhattanDistance != 1)
        {
            Debug.LogWarning(
                "StageMap: 상하좌우로 인접하지 않은 " +
                "방은 연결할 수 없습니다. " +
                $"{firstRoom.GridPosition} -> " +
                $"{secondRoom.GridPosition}"
            );

            return false;
        }

        bool firstAdded =
            firstRoom.AddConnection(
                secondRoomId
            );

        bool secondAdded =
            secondRoom.AddConnection(
                firstRoomId
            );

        return firstAdded ||
               secondAdded;
    }

    public List<RoomNode> GetConnectedRooms(
        int roomId)
    {
        List<RoomNode> connectedRooms =
            new List<RoomNode>();

        RoomNode room =
            GetRoomById(
                roomId
            );

        if (room == null)
        {
            return connectedRooms;
        }

        IReadOnlyList<int> connectedIds =
            room.ConnectedRoomIds;

        if (connectedIds == null)
        {
            return connectedRooms;
        }

        for (int i = 0;
             i < connectedIds.Count;
             i++)
        {
            RoomNode connectedRoom =
                GetRoomById(
                    connectedIds[i]
                );

            if (connectedRoom == null)
            {
                continue;
            }

            connectedRooms.Add(
                connectedRoom
            );
        }

        return connectedRooms;
    }

    public bool Validate(
        out string validationMessage)
    {
        EnsureLookup();

        if (RoomCount <= 0)
        {
            validationMessage =
                "생성된 방이 없습니다.";

            return false;
        }

        if (StartRoom == null)
        {
            validationMessage =
                "시작방이 없습니다.";

            return false;
        }

        if (BossRoom == null)
        {
            validationMessage =
                "보스방이 없습니다.";

            return false;
        }

        HashSet<int> visitedRoomIds =
            new HashSet<int>();

        Queue<int> pendingRoomIds =
            new Queue<int>();

        visitedRoomIds.Add(
            startRoomId
        );

        pendingRoomIds.Enqueue(
            startRoomId
        );

        while (pendingRoomIds.Count > 0)
        {
            int currentRoomId =
                pendingRoomIds.Dequeue();

            RoomNode currentRoom =
                GetRoomById(
                    currentRoomId
                );

            if (currentRoom == null)
            {
                continue;
            }

            IReadOnlyList<int> connections =
                currentRoom.ConnectedRoomIds;

            if (connections == null)
            {
                continue;
            }

            for (int i = 0;
                 i < connections.Count;
                 i++)
            {
                int connectedRoomId =
                    connections[i];

                if (visitedRoomIds.Contains(
                        connectedRoomId))
                {
                    continue;
                }

                RoomNode connectedRoom =
                    GetRoomById(
                        connectedRoomId
                    );

                if (connectedRoom == null)
                {
                    validationMessage =
                        "존재하지 않는 Room ID와 " +
                        "연결된 방이 있습니다. " +
                        $"Room ID={connectedRoomId}";

                    return false;
                }

                visitedRoomIds.Add(
                    connectedRoomId
                );

                pendingRoomIds.Enqueue(
                    connectedRoomId
                );
            }
        }

        if (visitedRoomIds.Count !=
            RoomCount)
        {
            validationMessage =
                "시작방에서 도달할 수 없는 " +
                "분리된 방이 있습니다.";

            return false;
        }

        validationMessage =
            $"유효한 맵입니다. 방 수={RoomCount}";

        return true;
    }

    public int CountRoomsOfType(
        RoomType roomType)
    {
        EnsureRoomList();

        int count = 0;

        for (int i = 0;
             i < rooms.Count;
             i++)
        {
            RoomNode room =
                rooms[i];

            if (room == null ||
                room.RoomType != roomType)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    public void RebuildLookup()
    {
        EnsureRoomList();
        EnsureLookupCollections();

        roomById.Clear();
        roomByPosition.Clear();

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

            if (!roomById.ContainsKey(
                    room.RoomId))
            {
                roomById.Add(
                    room.RoomId,
                    room
                );
            }

            if (!roomByPosition.ContainsKey(
                    room.GridPosition))
            {
                roomByPosition.Add(
                    room.GridPosition,
                    room
                );
            }
        }
    }

    private void EnsureLookup()
    {
        EnsureRoomList();
        EnsureLookupCollections();

        if (roomById.Count !=
                rooms.Count ||
            roomByPosition.Count !=
                rooms.Count)
        {
            RebuildLookup();

            return;
        }

        /*
         * 개수는 같지만 캐시 내용이 오래된 경우도
         * 확인합니다. Unity 역직렬화 이후 다른 방 목록을
         * 참조하는 캐시가 남는 상황을 방지합니다.
         */
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

            if (!roomById.TryGetValue(
                    room.RoomId,
                    out RoomNode cachedById) ||
                cachedById != room)
            {
                RebuildLookup();

                return;
            }

            if (!roomByPosition.TryGetValue(
                    room.GridPosition,
                    out RoomNode cachedByPosition) ||
                cachedByPosition != room)
            {
                RebuildLookup();

                return;
            }
        }
    }

    private void EnsureRoomList()
    {
        if (rooms != null)
        {
            return;
        }

        rooms =
            new List<RoomNode>();
    }

    private void EnsureLookupCollections()
    {
        if (roomById == null)
        {
            roomById =
                new Dictionary<int, RoomNode>();
        }

        if (roomByPosition == null)
        {
            roomByPosition =
                new Dictionary<
                    Vector2Int,
                    RoomNode
                >();
        }
    }

    private void InitializeLookupCollections()
    {
        roomById =
            new Dictionary<int, RoomNode>();

        roomByPosition =
            new Dictionary<
                Vector2Int,
                RoomNode
            >();
    }
}