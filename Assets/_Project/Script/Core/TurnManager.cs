using System;
using UnityEngine;

public enum TurnState
{
    Aiming,
    BallMoving,
    Resolving
}

public sealed class TurnManager : MonoBehaviour
{
    public TurnState CurrentState { get; private set; }

    public bool CanAim =>
        CurrentState == TurnState.Aiming;

    public event Action<TurnState> StateChanged;

    private void Awake()
    {
        ChangeState(TurnState.Aiming);
    }

    public bool TryStartAttack()
    {
        if (!CanAim)
        {
            return false;
        }

        ChangeState(TurnState.BallMoving);

        return true;
    }

    public void NotifyAllBallsReturned()
    {
        if (CurrentState != TurnState.BallMoving)
        {
            return;
        }

        ChangeState(TurnState.Resolving);

        ResolveTurn();
    }

    private void ResolveTurn()
    {
        // 이후 이곳에 다음 순서가 추가된다.
        //
        // 1. 살아남은 블록의 공격
        // 2. 플레이어 HP 감소
        // 3. 블록 한 칸 하강
        // 4. 새로운 블록 줄 생성
        // 5. 패배 조건 확인

        CompleteTurn();
    }

    private void CompleteTurn()
    {
        ChangeState(TurnState.Aiming);
    }

    private void ChangeState(TurnState nextState)
    {
        if (CurrentState == nextState)
        {
            return;
        }

        CurrentState = nextState;

        Debug.Log($"Turn State: {nextState}");

        StateChanged?.Invoke(nextState);
    }
}