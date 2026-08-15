using System.Collections.Generic;
using System.Text;
using UnityEngine;

[RequireComponent(
    typeof(StageMapGenerator)
)]
public sealed class StageMapDebugView :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private StageMapGenerator mapGenerator;

    [Header("Generation")]
    [SerializeField]
    private bool generateOnStart = true;

    [Header("Scene Gizmo")]
    [SerializeField]
    private bool drawGizmos = true;

    [SerializeField, Min(0.1f)]
    private float roomSpacing = 1.5f;

    [SerializeField, Min(0.05f)]
    private float roomSize = 0.45f;

    [SerializeField]
    private bool drawRoomIds = true;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
    }

    private void Start()
    {
        if (!generateOnStart)
        {
            return;
        }

        GenerateAndLogMap();
    }

    private void OnValidate()
    {
        FindReferences();
        NormalizeSettings();
    }

    private void FindReferences()
    {
        if (mapGenerator == null)
        {
            mapGenerator =
                GetComponent<
                    StageMapGenerator
                >();
        }
    }

    private void NormalizeSettings()
    {
        roomSpacing =
            Mathf.Max(
                roomSpacing,
                0.1f
            );

        roomSize =
            Mathf.Max(
                roomSize,
                0.05f
            );
    }

    [ContextMenu("Generate And Log Map")]
    public void GenerateAndLogMap()
    {
        if (mapGenerator == null)
        {
            Debug.LogError(
                "StageMapDebugView: " +
                "StageMapGenerator를 찾지 못했습니다.",
                this
            );

            return;
        }

        StageMap map =
            mapGenerator.GenerateTestStage();

        if (map == null)
        {
            Debug.LogError(
                "StageMapDebugView: " +
                "맵 생성에 실패했습니다.",
                this
            );

            return;
        }

        LogMap(
            map
        );
    }

    private void LogMap(
        StageMap map)
    {
        if (!TryGetRooms(
                map,
                out IReadOnlyList<RoomNode> rooms))
        {
            Debug.LogWarning(
                "StageMapDebugView: " +
                "출력할 방 데이터가 없습니다.",
                this
            );

            return;
        }

        StringBuilder builder =
            new StringBuilder();

        builder.AppendLine(
            $"=== Stage {map.StageNumber} Map ==="
        );

        builder.AppendLine(
            $"Room Count: {rooms.Count}"
        );

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

            builder.Append(
                $"Room {room.RoomId} | "
            );

            builder.Append(
                $"{room.RoomType} | "
            );

            builder.Append(
                $"Position={room.GridPosition} | "
            );

            builder.Append(
                "Connections="
            );

            IReadOnlyList<int> connections =
                room.ConnectedRoomIds;

            if (connections != null)
            {
                for (int connectionIndex = 0;
                     connectionIndex <
                     connections.Count;
                     connectionIndex++)
                {
                    builder.Append(
                        connections[
                            connectionIndex
                        ]
                    );

                    if (connectionIndex <
                        connections.Count - 1)
                    {
                        builder.Append(
                            ", "
                        );
                    }
                }
            }

            builder.AppendLine();
        }

        Debug.Log(
            builder.ToString(),
            this
        );
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
        {
            return;
        }

        FindReferences();

        if (mapGenerator == null)
        {
            return;
        }

        StageMap map =
            mapGenerator.CurrentMap;

        if (!TryGetRooms(
                map,
                out IReadOnlyList<RoomNode> rooms))
        {
            return;
        }

        DrawConnections(
            map,
            rooms
        );

        DrawRooms(
            rooms
        );
    }

    private bool TryGetRooms(
        StageMap map,
        out IReadOnlyList<RoomNode> rooms)
    {
        rooms = null;

        if (map == null)
        {
            return false;
        }

        rooms =
            map.Rooms;

        if (rooms == null ||
            rooms.Count == 0)
        {
            return false;
        }

        return true;
    }

    private void DrawConnections(
        StageMap map,
        IReadOnlyList<RoomNode> rooms)
    {
        if (map == null ||
            rooms == null)
        {
            return;
        }

        Gizmos.color =
            Color.white;

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

            Vector3 roomPosition =
                GetWorldPosition(
                    room.GridPosition
                );

            IReadOnlyList<int> connections =
                room.ConnectedRoomIds;

            if (connections == null)
            {
                continue;
            }

            for (int connectionIndex = 0;
                 connectionIndex <
                 connections.Count;
                 connectionIndex++)
            {
                int connectedRoomId =
                    connections[
                        connectionIndex
                    ];

                /*
                 * 양방향 연결이므로 선은 한 번만 그린다.
                 */
                if (connectedRoomId <
                    room.RoomId)
                {
                    continue;
                }

                RoomNode connectedRoom =
                    map.GetRoomById(
                        connectedRoomId
                    );

                if (connectedRoom == null)
                {
                    continue;
                }

                Vector3 connectedPosition =
                    GetWorldPosition(
                        connectedRoom.GridPosition
                    );

                Gizmos.DrawLine(
                    roomPosition,
                    connectedPosition
                );
            }
        }
    }

    private void DrawRooms(
        IReadOnlyList<RoomNode> rooms)
    {
        if (rooms == null)
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

            Gizmos.color =
                GetRoomColor(
                    room.RoomType
                );

            Vector3 position =
                GetWorldPosition(
                    room.GridPosition
                );

            Gizmos.DrawCube(
                position,
                Vector3.one *
                roomSize
            );

#if UNITY_EDITOR
            if (drawRoomIds)
            {
                UnityEditor.Handles.Label(
                    position +
                    Vector3.up *
                    roomSize,
                    $"{room.RoomId}\n" +
                    $"{room.RoomType}"
                );
            }
#endif
        }
    }

    private Vector3 GetWorldPosition(
        Vector2Int gridPosition)
    {
        return transform.position +
               new Vector3(
                   gridPosition.x *
                   roomSpacing,
                   gridPosition.y *
                   roomSpacing,
                   0f
               );
    }

    private Color GetRoomColor(
        RoomType roomType)
    {
        switch (roomType)
        {
            case RoomType.Start:
                return Color.green;

            case RoomType.NormalCombat:
                return Color.gray;

            case RoomType.NamedCombat:
                return new Color(
                    1f,
                    0.5f,
                    0f
                );

            case RoomType.Boss:
                return Color.red;

            case RoomType.Shop:
                return Color.cyan;

            case RoomType.Alchemy:
                return Color.yellow;

            case RoomType.Event:
                return Color.magenta;

            case RoomType.Augment:
                return new Color(0.55f, 0.3f, 1f);

            default:
                return Color.white;
        }
    }
}
