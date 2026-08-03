using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StageKeyState :
    MonoBehaviour
{
    private const int InvalidRoomId = -1;

    [Header("Runtime State")]

    [Tooltip(
        "현재 스테이지에서 열쇠가 숨겨진 " +
        "일반 전투방의 RoomId입니다. " +
        "-1이면 아직 지정되지 않은 상태입니다."
    )]
    [SerializeField]
    private int keyRoomId =
        InvalidRoomId;

    [Tooltip(
        "현재 스테이지의 열쇠를 " +
        "획득했는지 여부입니다."
    )]
    [SerializeField]
    private bool isKeyAcquired;

    [Tooltip(
        "획득한 열쇠가 이벤트방 입장 등에 의해 " +
        "소비되었는지 여부입니다."
    )]
    [SerializeField]
    private bool isKeyConsumed;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog = true;

    public int KeyRoomId =>
        keyRoomId;

    public bool HasAssignedKeyRoom =>
        keyRoomId >= 0;

    public bool IsKeyAcquired =>
        isKeyAcquired;

    public bool IsKeyConsumed =>
        isKeyConsumed;

    public bool HasKey =>
        isKeyAcquired &&
        !isKeyConsumed;

    public event Action StateChanged;

    public event Action<int>
        KeyRoomAssigned;

    public event Action<int>
        KeyAcquired;

    public event Action
        KeyConsumed;

    public event Action
        StageStateReset;

    private void OnValidate()
    {
        if (keyRoomId < InvalidRoomId)
        {
            keyRoomId =
                InvalidRoomId;
        }

        if (!isKeyAcquired)
        {
            isKeyConsumed =
                false;
        }
    }

    public void ResetForNewStage()
    {
        keyRoomId =
            InvalidRoomId;

        isKeyAcquired =
            false;

        isKeyConsumed =
            false;

        StageStateReset?.Invoke();
        StateChanged?.Invoke();

        if (showDebugLog)
        {
            Debug.Log(
                "StageKeyState: " +
                "새 스테이지 열쇠 상태를 초기화했습니다.",
                this
            );
        }
    }

    public bool TryAssignKeyRoom(
        int roomId)
    {
        if (roomId < 0)
        {
            Debug.LogWarning(
                "StageKeyState: " +
                $"유효하지 않은 RoomId입니다. " +
                $"RoomId={roomId}",
                this
            );

            return false;
        }

        if (HasAssignedKeyRoom)
        {
            Debug.LogWarning(
                "StageKeyState: " +
                "현재 스테이지의 열쇠 방이 " +
                "이미 지정되어 있습니다. " +
                $"현재 RoomId={keyRoomId}, " +
                $"요청 RoomId={roomId}",
                this
            );

            return false;
        }

        if (isKeyAcquired ||
            isKeyConsumed)
        {
            Debug.LogWarning(
                "StageKeyState: " +
                "이미 열쇠 진행 상태가 시작되어 " +
                "열쇠 방을 새로 지정할 수 없습니다.",
                this
            );

            return false;
        }

        keyRoomId =
            roomId;

        KeyRoomAssigned?.Invoke(
            keyRoomId
        );

        StateChanged?.Invoke();

        if (showDebugLog)
        {
            Debug.Log(
                "StageKeyState: " +
                $"열쇠 방을 지정했습니다. " +
                $"RoomId={keyRoomId}",
                this
            );
        }

        return true;
    }

    public bool IsKeyRoom(
        int roomId)
    {
        return HasAssignedKeyRoom &&
               roomId == keyRoomId;
    }

    public bool TryAcquireKey(
        int clearedRoomId)
    {
        if (!HasAssignedKeyRoom)
        {
            Debug.LogWarning(
                "StageKeyState: " +
                "열쇠 방이 지정되지 않아 " +
                "열쇠를 획득할 수 없습니다.",
                this
            );

            return false;
        }

        if (!IsKeyRoom(
                clearedRoomId
            ))
        {
            return false;
        }

        if (isKeyAcquired)
        {
            return false;
        }

        isKeyAcquired =
            true;

        isKeyConsumed =
            false;

        KeyAcquired?.Invoke(
            clearedRoomId
        );

        StateChanged?.Invoke();

        if (showDebugLog)
        {
            Debug.Log(
                "StageKeyState: " +
                $"스테이지 열쇠를 획득했습니다. " +
                $"RoomId={clearedRoomId}",
                this
            );
        }

        return true;
    }

    public bool TryConsumeKey()
    {
        if (!HasKey)
        {
            return false;
        }

        isKeyConsumed =
            true;

        KeyConsumed?.Invoke();
        StateChanged?.Invoke();

        if (showDebugLog)
        {
            Debug.Log(
                "StageKeyState: " +
                "스테이지 열쇠를 소비했습니다.",
                this
            );
        }

        return true;
    }
}