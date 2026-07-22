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

        ChangeState(
            TurnState.BallMoving
        );

        return true;
    }

    public void NotifyAllBallsReturned()
    {
        if (CurrentState !=
            TurnState.BallMoving)
        {
            return;
        }

        ChangeState(
            TurnState.Resolving
        );

        if (resolveCoroutine != null)
        {
            StopCoroutine(
                resolveCoroutine
            );
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

        if (blockGridManager != null)
        {
            yield return
                blockGridManager
                    .AdvanceTurnRoutine();
        }
        else
        {
            Debug.LogWarning(
                "TurnManager: " +
                "BlockGridManager가 연결되지 않았습니다.",
                this
            );
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
        ChangeState(
            TurnState.Aiming
        );
    }

    private void ChangeState(
        TurnState nextState)
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

        StateChanged?.Invoke(
            nextState
        );
    }

    private void OnDestroy()
    {
        if (resolveCoroutine != null)
        {
            StopCoroutine(
                resolveCoroutine
            );
        }
    }
}