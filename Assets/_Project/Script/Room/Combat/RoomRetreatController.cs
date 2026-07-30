using System;
using UnityEngine;

public sealed class RoomRetreatController :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private BlockGridManager blockGridManager;

    [SerializeField]
    private PlayerHealth playerHealth;

    [SerializeField]
    private BallLauncher ballLauncher;

    [Header("Retreat Cost")]
    [Tooltip(
        "후퇴 시 최대 체력에서 차감할 비율입니다. " +
        "0.1은 최대 체력의 10%입니다."
    )]
    [SerializeField, Range(0.01f, 1f)]
    private float retreatCostRatio = 0.1f;

    [Tooltip(
        "후퇴 비용으로 플레이어가 사망하는 것을 허용할지 " +
        "결정합니다. 현재는 false를 권장합니다."
    )]
    [SerializeField]
    private bool allowLethalRetreatCost;

    public int RetreatCost
    {
        get
        {
            if (playerHealth == null)
            {
                return 0;
            }

            return Mathf.Max(
                1,
                Mathf.CeilToInt(
                    playerHealth.MaxHealth *
                    retreatCostRatio
                )
            );
        }
    }

    public bool HasEnoughHealth
    {
        get
        {
            if (playerHealth == null ||
                playerHealth.IsDead)
            {
                return false;
            }

            int cost =
                RetreatCost;

            if (allowLethalRetreatCost)
            {
                return playerHealth.CurrentHealth >=
                       cost;
            }

            return playerHealth.CurrentHealth >
                   cost;
        }
    }

    public bool CanRetreat =>
        turnManager != null &&
        blockGridManager != null &&
        playerHealth != null &&
        ballLauncher != null &&
        turnManager.CanRetreat &&
        blockGridManager.CurrentRoomState ==
        RoomCombatState.InCombat &&
        blockGridManager.CanResetCurrentRoomCombat &&
        !ballLauncher.IsAttackInProgress &&
        HasEnoughHealth;

    public event Action<int>
        RetreatCompleted;

    /*
     * 실제 방 이동 시스템이 추가되면
     * 이 이벤트를 구독해 직전 방으로 이동한다.
     */
    public event Action
        PreviousRoomMoveRequested;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
        ValidateReferences();
    }

    private void OnValidate()
    {
        FindReferences();
        NormalizeSettings();
    }

    private void FindReferences()
    {
        if (turnManager == null)
        {
            turnManager =
                FindFirstObjectByType<
                    TurnManager
                >();
        }

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

        if (ballLauncher == null)
        {
            ballLauncher =
                FindFirstObjectByType<
                    BallLauncher
                >();
        }
    }

    private void NormalizeSettings()
    {
        retreatCostRatio =
            Mathf.Clamp(
                retreatCostRatio,
                0.01f,
                1f
            );
    }

    private void ValidateReferences()
    {
        if (turnManager == null)
        {
            Debug.LogError(
                "RoomRetreatController: " +
                "TurnManager를 찾지 못했습니다.",
                this
            );
        }

        if (blockGridManager == null)
        {
            Debug.LogError(
                "RoomRetreatController: " +
                "BlockGridManager를 찾지 못했습니다.",
                this
            );
        }

        if (playerHealth == null)
        {
            Debug.LogError(
                "RoomRetreatController: " +
                "PlayerHealth를 찾지 못했습니다.",
                this
            );
        }

        if (ballLauncher == null)
        {
            Debug.LogError(
                "RoomRetreatController: " +
                "BallLauncher를 찾지 못했습니다.",
                this
            );
        }
    }

    /*
     * Unity UI Button의 OnClick에서 연결하기 위한
     * void 반환형 공개 메서드다.
     */
    public void RequestRetreat()
    {
        TryRetreat();
    }

    public bool TryRetreat()
    {
        if (!CanRetreat)
        {
            LogRetreatFailure();

            return false;
        }

        int cost =
            RetreatCost;

        /*
         * 초기화 도중 조준 클릭이 들어오는 것을 막는다.
         */
        turnManager.SetInputLocked(
            true
        );

        /*
         * 먼저 공과 발사 지점을 중앙으로 복구한다.
         *
         * 현재 후퇴는 Aiming 상태에서만 허용되므로
         * 진행 중인 공은 존재하지 않아야 한다.
         */
        bool launchPositionReset =
            ballLauncher
                .TryResetLaunchPositionToCenter();

        if (!launchPositionReset)
        {
            Debug.LogWarning(
                "RoomRetreatController: " +
                "공 발사 위치를 중앙으로 " +
                "초기화하지 못했습니다.",
                this
            );

            turnManager.SetInputLocked(
                false
            );

            return false;
        }

        /*
         * 저장된 최초 생성 요청을 이용해
         * 현재 미클리어 방을 최초 상태로 복원한다.
         */
        bool roomResetSucceeded =
            blockGridManager
                .TryResetCurrentRoomCombat();

        if (!roomResetSucceeded)
        {
            Debug.LogWarning(
                "RoomRetreatController: " +
                "현재 방 전투를 초기화하지 못했습니다.",
                this
            );

            turnManager.SetInputLocked(
                false
            );

            return false;
        }

        /*
         * 방 초기화까지 성공한 뒤에만
         * 체력 비용을 지불한다.
         */
        playerHealth.TakeDamage(
            cost
        );

        if (playerHealth.IsDead)
        {
            Debug.LogWarning(
                "RoomRetreatController: " +
                "후퇴 비용으로 플레이어가 사망했습니다.",
                this
            );

            return false;
        }

        /*
         * 전투 상태를 다시 조준 가능 상태로 만든다.
         */
        turnManager.ResetToAiming(
            true
        );

        RetreatCompleted?.Invoke(
            cost
        );

        /*
         * 아직 지도와 방 이동 시스템이 없으므로
         * 실제 이동 대신 요청 이벤트만 발생시킨다.
         */
        PreviousRoomMoveRequested?.Invoke();

        Debug.Log(
            "RoomRetreatController: 후퇴 완료, " +
            $"체력 비용={cost}, " +
            $"남은 체력=" +
            $"{playerHealth.CurrentHealth}/" +
            $"{playerHealth.MaxHealth}",
            this
        );

        return true;
    }

    private void LogRetreatFailure()
    {
        if (turnManager == null ||
            blockGridManager == null ||
            playerHealth == null ||
            ballLauncher == null)
        {
            Debug.LogWarning(
                "RoomRetreatController: " +
                "필수 참조가 연결되지 않아 " +
                "후퇴할 수 없습니다.",
                this
            );

            return;
        }

        if (turnManager.IsGameOver ||
            playerHealth.IsDead)
        {
            Debug.LogWarning(
                "RoomRetreatController: " +
                "게임 오버 상태에서는 후퇴할 수 없습니다.",
                this
            );

            return;
        }

        if (turnManager.CurrentState !=
            TurnState.Aiming)
        {
            Debug.LogWarning(
                "RoomRetreatController: " +
                "공격 또는 턴 처리 중에는 후퇴할 수 없습니다. " +
                $"현재 턴 상태={turnManager.CurrentState}",
                this
            );

            return;
        }

        if (turnManager.IsInputLocked)
        {
            Debug.LogWarning(
                "RoomRetreatController: " +
                "입력이 잠긴 상태에서는 후퇴할 수 없습니다.",
                this
            );

            return;
        }

        if (ballLauncher.IsAttackInProgress)
        {
            Debug.LogWarning(
                "RoomRetreatController: " +
                "공 공격이 진행 중이라 후퇴할 수 없습니다.",
                this
            );

            return;
        }

        if (blockGridManager.IsBossEncounterActive)
        {
            Debug.LogWarning(
                "RoomRetreatController: " +
                "현재 단계에서는 보스전 후퇴를 지원하지 않습니다.",
                this
            );

            return;
        }

        if (blockGridManager.IsCurrentRoomCleared ||
            blockGridManager.CurrentRoomState ==
            RoomCombatState.Cleared)
        {
            Debug.LogWarning(
                "RoomRetreatController: " +
                "이미 클리어한 방에서는 후퇴할 수 없습니다.",
                this
            );

            return;
        }

        if (!blockGridManager
                .CanResetCurrentRoomCombat)
        {
            Debug.LogWarning(
                "RoomRetreatController: " +
                "현재 방의 최초 배치가 저장되지 않아 " +
                "후퇴할 수 없습니다.",
                this
            );

            return;
        }

        if (!HasEnoughHealth)
        {
            Debug.LogWarning(
                "RoomRetreatController: " +
                "후퇴에 필요한 체력이 부족합니다. " +
                $"현재 체력={playerHealth.CurrentHealth}, " +
                $"필요 체력={RetreatCost}, " +
                $"치명적 비용 허용={allowLethalRetreatCost}",
                this
            );

            return;
        }

        Debug.LogWarning(
            "RoomRetreatController: " +
            "현재 상태에서는 후퇴할 수 없습니다.",
            this
        );
    }
}