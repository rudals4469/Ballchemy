using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EventRoomState :
    MonoBehaviour
{
    private readonly HashSet<int>
        usedEventRoomIds =
            new HashSet<int>();

    public event Action<int>
        EventRoomUsed;

    public event Action
        StageStateReset;

    public bool IsEventRoomUsed(
        int roomId)
    {
        return usedEventRoomIds.Contains(
            roomId
        );
    }

    public bool TryMarkEventRoomUsed(
        int roomId)
    {
        if (roomId < 0)
        {
            return false;
        }

        bool added =
            usedEventRoomIds.Add(
                roomId
            );

        if (!added)
        {
            return false;
        }

        Debug.Log(
            "EventRoomState: 이벤트방 이용 완료, " +
            $"RoomId={roomId}",
            this
        );

        EventRoomUsed?.Invoke(
            roomId
        );

        return true;
    }

    public void ResetForNewStage()
    {
        usedEventRoomIds.Clear();

        Debug.Log(
            "EventRoomState: 새 스테이지 상태 초기화",
            this
        );

        StageStateReset?.Invoke();
    }
}