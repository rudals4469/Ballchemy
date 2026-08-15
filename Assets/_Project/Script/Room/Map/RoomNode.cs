using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RoomNode
{
    [SerializeField]
    private int roomId;

    [SerializeField]
    private Vector2Int gridPosition;

    [SerializeField]
    private RoomType roomType =
        RoomType.NormalCombat;

    [SerializeField]
    private List<int> connectedRoomIds =
        new List<int>();

    public int RoomId =>
        roomId;

    public Vector2Int GridPosition =>
        gridPosition;

    public RoomType RoomType =>
        roomType;

    public IReadOnlyList<int>
        ConnectedRoomIds =>
            connectedRoomIds;

    public int ConnectionCount =>
        connectedRoomIds != null
            ? connectedRoomIds.Count
            : 0;

    public bool IsCombatRoom =>
        roomType ==
            RoomType.NormalCombat ||
        roomType ==
            RoomType.NamedCombat ||
        roomType ==
            RoomType.Boss;

    public bool IsSpecialRoom =>
        roomType ==
            RoomType.Shop ||
        roomType ==
            RoomType.Alchemy ||
        roomType ==
            RoomType.Event ||
        roomType ==
            RoomType.Augment ||
        roomType ==
            RoomType.Secret;

    public bool IsSecretRoom =>
        roomType ==
            RoomType.Secret;

    public RoomNode(
        int roomId,
        Vector2Int gridPosition,
        RoomType roomType)
    {
        this.roomId =
            Mathf.Max(
                roomId,
                0
            );

        this.gridPosition =
            gridPosition;

        this.roomType =
            roomType;

        connectedRoomIds =
            new List<int>();
    }

    public void SetRoomType(
        RoomType nextRoomType)
    {
        roomType =
            nextRoomType;
    }

    public bool HasConnection(
        int targetRoomId)
    {
        if (connectedRoomIds == null)
        {
            return false;
        }

        return connectedRoomIds.Contains(
            targetRoomId
        );
    }

    public bool AddConnection(
        int targetRoomId)
    {
        if (targetRoomId < 0 ||
            targetRoomId == roomId)
        {
            return false;
        }

        if (connectedRoomIds == null)
        {
            connectedRoomIds =
                new List<int>();
        }

        if (connectedRoomIds.Contains(
                targetRoomId))
        {
            return false;
        }

        connectedRoomIds.Add(
            targetRoomId
        );

        return true;
    }

    public bool RemoveConnection(
        int targetRoomId)
    {
        if (connectedRoomIds == null)
        {
            return false;
        }

        return connectedRoomIds.Remove(
            targetRoomId
        );
    }
}
