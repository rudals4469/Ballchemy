using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RoomGoldTransactionState :
    MonoBehaviour
{
    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog = true;

    private int activeRoomId = -1;
    private int currentRoomEarnedGold;

    public int ActiveRoomId =>
        activeRoomId;

    public int CurrentRoomEarnedGold =>
        currentRoomEarnedGold;

    public bool HasActiveTransaction =>
        activeRoomId >= 0;

    public event Action<int>
        TransactionStarted;

    public event Action<int, int>
        EarnedGoldChanged;

    public event Action<int, int>
        TransactionConfirmed;

    public event Action<int, int>
        TransactionRollbackRequested;

    public event Action
        TransactionCleared;

    public bool BeginTransaction(
        int roomId)
    {
        if (roomId < 0)
        {
            return false;
        }

        if (HasActiveTransaction)
        {
            if (activeRoomId ==
                roomId)
            {
                return true;
            }

            Debug.LogWarning(
                "RoomGoldTransactionState: " +
                "기존 미확정 거래가 남아 있어 " +
                "새 방 거래를 시작하지 않습니다. " +
                $"기존 RoomId={activeRoomId}, " +
                $"요청 RoomId={roomId}, " +
                $"미확정 골드={currentRoomEarnedGold}G",
                this
            );

            return false;
        }

        activeRoomId =
            roomId;

        currentRoomEarnedGold =
            0;

        if (showDebugLog)
        {
            Debug.Log(
                "RoomGoldTransactionState: " +
                $"방 골드 거래 시작, RoomId={roomId}",
                this
            );
        }

        TransactionStarted?.Invoke(
            roomId
        );

        EarnedGoldChanged?.Invoke(
            activeRoomId,
            currentRoomEarnedGold
        );

        return true;
    }

    public bool TryRecordEarnedGold(
        int roomId,
        int amount)
    {
        if (amount <= 0 ||
            !HasActiveTransaction ||
            activeRoomId !=
            roomId)
        {
            return false;
        }

        currentRoomEarnedGold +=
            amount;

        if (showDebugLog)
        {
            Debug.Log(
                "RoomGoldTransactionState: " +
                $"Room {roomId} 미확정 골드 " +
                $"+{amount}G, " +
                $"누적={currentRoomEarnedGold}G",
                this
            );
        }

        EarnedGoldChanged?.Invoke(
            activeRoomId,
            currentRoomEarnedGold
        );

        return true;
    }

    public bool TryConfirmTransaction(
        int roomId)
    {
        if (!HasActiveTransaction ||
            activeRoomId !=
            roomId)
        {
            return false;
        }

        int confirmedRoomId =
            activeRoomId;

        int confirmedGold =
            currentRoomEarnedGold;

        if (showDebugLog)
        {
            Debug.Log(
                "RoomGoldTransactionState: " +
                $"방 골드 확정, " +
                $"RoomId={confirmedRoomId}, " +
                $"Gold={confirmedGold}G",
                this
            );
        }

        ClearRuntimeState();

        TransactionConfirmed?.Invoke(
            confirmedRoomId,
            confirmedGold
        );

        TransactionCleared?.Invoke();

        return true;
    }

    /*
     * 후퇴 시 회수할 방 번호와 골드 수치를 반환하고
     * 내부 거래 상태를 제거합니다.
     */
    public bool TryConsumeRollback(
        out int roomId,
        out int rollbackGold)
    {
        if (!HasActiveTransaction)
        {
            roomId =
                -1;

            rollbackGold =
                0;

            return false;
        }

        roomId =
            activeRoomId;

        rollbackGold =
            currentRoomEarnedGold;

        if (showDebugLog)
        {
            Debug.Log(
                "RoomGoldTransactionState: " +
                $"방 골드 회수 요청, " +
                $"RoomId={roomId}, " +
                $"Gold={rollbackGold}G",
                this
            );
        }

        ClearRuntimeState();

        TransactionRollbackRequested
            ?.Invoke(
                roomId,
                rollbackGold
            );

        TransactionCleared?.Invoke();

        return true;
    }

    public void ResetWithoutRollback()
    {
        if (!HasActiveTransaction &&
            currentRoomEarnedGold <= 0)
        {
            return;
        }

        if (showDebugLog)
        {
            Debug.LogWarning(
                "RoomGoldTransactionState: " +
                "골드 회수 없이 거래 상태를 초기화합니다. " +
                $"RoomId={activeRoomId}, " +
                $"Gold={currentRoomEarnedGold}G",
                this
            );
        }

        ClearRuntimeState();

        TransactionCleared?.Invoke();
    }

    private void ClearRuntimeState()
    {
        activeRoomId =
            -1;

        currentRoomEarnedGold =
            0;
    }
}