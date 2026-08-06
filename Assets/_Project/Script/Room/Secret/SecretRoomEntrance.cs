using System;
using UnityEngine;

[Serializable]
public sealed class SecretRoomEntrance
{
    [SerializeField]
    private int secretRoomId = -1;

    [SerializeField]
    private int connectedRoomId = -1;

    [SerializeField]
    private RoomDirection directionFromConnectedRoom;

    [SerializeField]
    private bool isDiscovered;

    [SerializeField]
    private bool isUnlocked;

    public int SecretRoomId =>
        secretRoomId;

    public int ConnectedRoomId =>
        connectedRoomId;

    public RoomDirection
        DirectionFromConnectedRoom =>
        directionFromConnectedRoom;

    public bool IsDiscovered =>
        isDiscovered;

    public bool IsUnlocked =>
        isUnlocked;

    public SecretRoomEntrance(
        int secretRoomId,
        int connectedRoomId,
        RoomDirection directionFromConnectedRoom)
    {
        this.secretRoomId =
            Mathf.Max(
                secretRoomId,
                0
            );

        this.connectedRoomId =
            Mathf.Max(
                connectedRoomId,
                0
            );

        this.directionFromConnectedRoom =
            directionFromConnectedRoom;

        isDiscovered =
            false;

        isUnlocked =
            false;
    }

    public bool Matches(
        int sourceRoomId,
        int targetSecretRoomId)
    {
        return
            connectedRoomId ==
            sourceRoomId &&
            secretRoomId ==
            targetSecretRoomId;
    }

    public void Discover()
    {
        isDiscovered =
            true;
    }

    public void Unlock()
    {
        isDiscovered =
            true;

        isUnlocked =
            true;
    }

    public void ResetRuntimeState()
    {
        isDiscovered =
            false;

        isUnlocked =
            false;
    }
}