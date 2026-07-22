using System;
using System.Collections;
using UnityEngine;

public enum TurnState
{
    Aiming,
    BallMoving,
    Resolving,
    GameOver
}

public sealed class TurnManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BlockGridManager blockGridManager;

    [SerializeField]
    private PlayerHealth playerHealth;

    [Header("Resolve Timing")]
    [SerializeField, Min(0f)]
    private float resolveStartDelay = 0.1f;

    [SerializeField, Min(0f)]
    private float nextTurnDelay = 0.1f;

    private Coroutine resolveCoroutine;

    public TurnState CurrentState { get; private set; }

    public bool CanAim =>
        CurrentState == TurnState.Aiming;

    public bool IsGameOver =>
        CurrentState == TurnState.GameOver;

    public event Action<TurnState> StateChanged;
    public event Action GameOverStarted;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();

        CurrentState =
            TurnState.Aiming;

        SubscribePlayerHealth();

        Debug.Log(
            $"Turn State: {CurrentState}",
            this
        );
    }

    private void FindReferences()
    {
        if (blockGridManager == null)
        {
            blockGridManager =
                FindFirstObjectByType<BlockGridManager>();
        }

        if (playerHealth == null)
        {
            playerHealth =
                FindFirstObjectByType<PlayerHealth>();
        }
    }

    private void ValidateReferences()
    {
        if (blockGridManager == null)
        {
            Debug.LogWarning(
                "TurnManager: " +
                "BlockGridManager가 연결되지 않았습니다.",
                this
            );
        }

        if (playerHealth == null)
        {
            Debug.LogWarning(
                "TurnManager: " +
                "PlayerHealth가 연결되지 않았습니다.",
                this
            );
        }
    }

    private void SubscribePlayerHealth()
    {
        if (playerHealth == null)
        {
            return;
        }

        playerHealth.Died -=
            HandlePlayerDied;

        playerHealth.Died +=
            HandlePlayerDied;
    }

    public bool TryStartAttack()
    {
        if (!CanAim)
        {
            return false;
        }

        if (playerHealth != null &&
            playerHealth.IsDead)
        {
            StartGameOver();
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

        if (IsGameOver)
        {
            resolveCoroutine = null;
            yield break;
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

        // 적 공격 처리 중 체력이 0이 됐을 경우
        // 다음 조준 턴으로 넘어가지 않는다.
        if (IsGameOver ||
            (
                playerHealth != null &&
                playerHealth.IsDead
            ))
        {
            resolveCoroutine = null;
            StartGameOver();
            yield break;
        }

        if (nextTurnDelay > 0f)
        {
            yield return new WaitForSeconds(
                nextTurnDelay
            );
        }

        if (IsGameOver)
        {
            resolveCoroutine = null;
            yield break;
        }

        resolveCoroutine = null;

        CompleteTurn();
    }

    private void CompleteTurn()
    {
        if (IsGameOver)
        {
            return;
        }

        ChangeState(
            TurnState.Aiming
        );
    }

    private void HandlePlayerDied()
    {
        StartGameOver();
    }

    private void StartGameOver()
    {
        if (IsGameOver)
        {
            return;
        }

        ChangeState(
            TurnState.GameOver
        );

        Debug.Log(
            "TurnManager: 게임 오버",
            this
        );

        GameOverStarted?.Invoke();
    }

    private void ChangeState(
        TurnState nextState)
    {
        if (CurrentState == nextState)
        {
            return;
        }

        CurrentState =
            nextState;

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

        if (playerHealth != null)
        {
            playerHealth.Died -=
                HandlePlayerDied;
        }
    }
}