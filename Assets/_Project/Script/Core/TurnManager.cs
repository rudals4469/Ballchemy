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

public sealed class TurnManager :
    MonoBehaviour
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

    private bool isInputLocked;

    public TurnState CurrentState
    {
        get;
        private set;
    }

    public bool CanAim =>
        CurrentState ==
        TurnState.Aiming &&
        !isInputLocked;

    public bool CanRetreat =>
        CurrentState ==
        TurnState.Aiming &&
        !isInputLocked &&
        !IsGameOver;

    public bool IsGameOver =>
        CurrentState ==
        TurnState.GameOver;

    public bool IsInputLocked =>
        isInputLocked;

    public event Action<TurnState>
        StateChanged;

    public event Action<bool>
        InputLockChanged;

    public event Action
        GameOverStarted;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();

        CurrentState =
            TurnState.Aiming;

        isInputLocked =
            false;

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
                FindFirstObjectByType<
                    BlockGridManager
                >();
        }

        if (playerHealth == null)
        {
            playerHealth =
                FindFirstObjectByType<
                    PlayerHealth
                >();
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

    public void SetInputLocked(
        bool shouldLock)
    {
        if (isInputLocked ==
            shouldLock)
        {
            return;
        }

        isInputLocked =
            shouldLock;

        Debug.Log(
            "TurnManager: 입력 잠금 " +
            (
                isInputLocked
                    ? "활성화"
                    : "해제"
            ),
            this
        );

        InputLockChanged?.Invoke(
            isInputLocked
        );
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

        StopResolveCoroutine();

        resolveCoroutine =
            StartCoroutine(
                ResolveTurnRoutine()
            );
    }

    public void ResetToAiming(
        bool unlockInput = true)
    {
        if (IsGameOver)
        {
            return;
        }

        StopResolveCoroutine();

        ChangeState(
            TurnState.Aiming
        );

        if (unlockInput)
        {
            SetInputLocked(
                false
            );
        }

        Debug.Log(
            "TurnManager: 전투 전환 후 " +
            "조준 상태로 재설정했습니다.",
            this
        );
    }

    private IEnumerator ResolveTurnRoutine()
    {
        if (resolveStartDelay > 0f)
        {
            yield return
                new WaitForSeconds(
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

        if (blockGridManager != null &&
            blockGridManager.IsCurrentRoomCleared)
        {
            resolveCoroutine = null;

            ChangeState(
                TurnState.Aiming
            );

            SetInputLocked(
                true
            );

            Debug.Log(
                "TurnManager: 방 클리어로 " +
                "조준과 발사를 잠급니다.",
                this
            );

            yield break;
        }

        if (nextTurnDelay > 0f)
        {
            yield return
                new WaitForSeconds(
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

        StopResolveCoroutine();

        ChangeState(
            TurnState.GameOver
        );

        SetInputLocked(
            true
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
        if (CurrentState ==
            nextState)
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

    private void StopResolveCoroutine()
    {
        if (resolveCoroutine == null)
        {
            return;
        }

        StopCoroutine(
            resolveCoroutine
        );

        resolveCoroutine = null;
    }

    private void OnDestroy()
    {
        StopResolveCoroutine();

        if (playerHealth != null)
        {
            playerHealth.Died -=
                HandlePlayerDied;
        }
    }
}