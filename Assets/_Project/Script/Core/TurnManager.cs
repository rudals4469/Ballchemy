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

    public void NotifyBallReturned()
    {
        if (CurrentState != TurnState.BallMoving)
        {
            return;
        }

        ChangeState(TurnState.Resolving);

        // 추후 이 사이에 블록 공격, 하강, 생성이 들어간다.
        CompleteTurn();
    }

    private void CompleteTurn()
    {
        ChangeState(TurnState.Aiming);
    }

    private void ChangeState(TurnState nextState)
    {
        CurrentState = nextState;
        StateChanged?.Invoke(nextState);

        Debug.Log($"Turn State: {nextState}");
    }
}