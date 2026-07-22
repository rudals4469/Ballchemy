using System;
using System.Collections;
using UnityEngine;

public enum TurnState
{
    Aiming,
    BallMoving,
    Resolving
}

public sealed class TurnManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BlockGridManager blockGridManager;

    [Header("Resolve Timing")]
    [SerializeField, Min(0f)]
    private float resolveStartDelay = 0.1f;

    [SerializeField, Min(0f)]
    private float nextTurnDelay = 0.1f;

    private Coroutine resolveCoroutine;

    public TurnState CurrentState { get; private set; }

    public bool CanAim =>
        CurrentState == TurnState.Aiming;

    public event Action<TurnState> StateChanged;

    private void Awake()
    {
        if (blockGridManager == null)
        {
            blockGridManager =
                FindFirstObjectByType<BlockGridManager>();
        }

        CurrentState = TurnState.Aiming;

        Debug.Log(
            $"Turn State: {CurrentState}",
            this
        );
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

        if (resolveCoroutine != null)
        {
            StopCoroutine(resolveCoroutine);
        }

        resolveCoroutine = StartCoroutine(
            ResolveTurnRoutine()
        );
    }

    private IEnumerator ResolveTurnRoutine()
    {
        if (resolveStartDelay > 0f)
        {
            yield return new WaitForSeconds(
                resolveStartDelay
            );
        }

        // 추후 이 앞부분에 살아남은 블록의 공격이 들어간다.
        //
        // 1. 살아남은 블록 공격
        // 2. 플레이어 HP 감소
        // 3. 블록 하강
        // 4. 새로운 줄 생성

        if (blockGridManager != null)
        {
            yield return
                blockGridManager.AdvanceTurnRoutine();
        }

        if (nextTurnDelay > 0f)
        {
            yield return new WaitForSeconds(
                nextTurnDelay
            );
        }

        resolveCoroutine = null;

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

        Debug.Log(
            $"Turn State: {CurrentState}",
            this
        );

        StateChanged?.Invoke(nextState);
    }

    private void OnDestroy()
    {
        if (resolveCoroutine != null)
        {
            StopCoroutine(resolveCoroutine);
        }
    }
}