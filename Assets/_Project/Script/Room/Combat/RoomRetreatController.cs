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

    [SerializeField]
    private StageRoomNavigator navigator;

    [SerializeField]
    private RoomTransitionController
        transitionController;

    [SerializeField]
    private RetreatCostDiscountState
        retreatCostDiscountState;

    [Header("Retreat Cost")]

    [Tooltip(
        "후퇴 시 최대 체력에서 차감할 기본 비율입니다. " +
        "0.1은 최대 체력의 10%입니다."
    )]
    [SerializeField, Range(0.01f, 1f)]
    private float retreatCostRatio = 0.1f;

    [Tooltip(
        "후퇴 비용으로 플레이어가 사망하는 것을 " +
        "허용할지 결정합니다."
    )]
    [SerializeField]
    private bool allowLethalRetreatCost;

    public int BaseRetreatCost
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

    public float RetreatCostDiscountRatio =>
        retreatCostDiscountState != null
            ? retreatCostDiscountState
                .DiscountRatio
            : 0f;

    public int RetreatCost
    {
        get
        {
            int baseCost =
                BaseRetreatCost;

            if (baseCost <= 0)
            {
                return 0;
            }

            float remainingRatio =
                1f -
                Mathf.Clamp01(
                    RetreatCostDiscountRatio
                );

            /*
             * 할인율이 100%라면 비용 0을 허용합니다.
             */
            return Mathf.Max(
                0,
                Mathf.CeilToInt(
                    baseCost *
                    remainingRatio
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

            if (cost <= 0)
            {
                return true;
            }

            if (allowLethalRetreatCost)
            {
                return
                    playerHealth.CurrentHealth >=
                    cost;
            }

            return
                playerHealth.CurrentHealth >
                cost;
        }
    }

    public bool CanRetreat =>
        transitionController != null &&
        !transitionController.IsTransitioning &&
        CanExecuteRetreat(
            true
        );

    public event Action<int>
        RetreatCompleted;

    /*
     * 기존 외부 참조와의 컴파일 호환을 위해
     * 이벤트 선언은 유지합니다.
     *
     * 이동 중복을 막기 위해 더 이상 발생시키지 않습니다.
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

        if (navigator == null)
        {
            navigator =
                FindFirstObjectByType<
                    StageRoomNavigator
                >();
        }

        if (transitionController == null)
        {
            transitionController =
                FindFirstObjectByType<
                    RoomTransitionController
                >();
        }

        if (retreatCostDiscountState == null)
        {
            retreatCostDiscountState =
                FindFirstObjectByType<
                    RetreatCostDiscountState
                >(
                    FindObjectsInactive.Include
                );
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

        if (navigator == null)
        {
            Debug.LogError(
                "RoomRetreatController: " +
                "StageRoomNavigator를 찾지 못했습니다.",
                this
            );
        }

        if (transitionController == null)
        {
            Debug.LogError(
                "RoomRetreatController: " +
                "RoomTransitionController를 찾지 못했습니다.",
                this
            );
        }

        if (retreatCostDiscountState == null)
        {
            Debug.LogError(
                "RoomRetreatController: " +
                "RetreatCostDiscountState가 " +
                "연결되지 않았습니다.",
                this
            );
        }
    }

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

        return transitionController
            .TryRetreat(
                this
            );
    }

    /*
     * RoomTransitionController가 화면을 완전히
     * 암전한 상태에서만 호출합니다.
     */
    public bool ExecuteRetreatDuringFade()
    {
        /*
         * 전환 컨트롤러가 이미 입력을 잠갔으므로
         * 입력 및 네비게이션 잠금 조건은 제외하고
         * 실제 후퇴 가능 조건을 다시 검사합니다.
         */
        if (!CanExecuteRetreat(
                false
            ))
        {
            return false;
        }

        int baseCost =
            BaseRetreatCost;

        int appliedCost =
            RetreatCost;

        float discountRatio =
            RetreatCostDiscountRatio;

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

            return false;
        }

        /*
         * 현재 미클리어 방을 떠나기 전에
         * 최초 생성 상태로 복원합니다.
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

            return false;
        }

        /*
         * 실제 직전 방 이동 성공 여부를 먼저 확인합니다.
         * 이동 실패 시 체력은 차감되지 않습니다.
         */
        bool roomMoveSucceeded =
            navigator
                .TryMoveToPreviousRoom();

        if (!roomMoveSucceeded)
        {
            Debug.LogWarning(
                "RoomRetreatController: " +
                "직전 방 이동에 실패했습니다. " +
                "후퇴 비용은 차감하지 않습니다.",
                this
            );

            return false;
        }

        if (appliedCost > 0)
        {
            playerHealth.TakeDamage(
                appliedCost
            );
        }

        /*
         * 할인 상태는 런 지속형이므로
         * 후퇴 성공 후에도 소비하거나 초기화하지 않습니다.
         */
        RetreatCompleted?.Invoke(
            appliedCost
        );

        Debug.Log(
            "RoomRetreatController: 후퇴 완료, " +
            $"기본 비용={baseCost}, " +
            $"할인율={discountRatio:P0}, " +
            $"실제 체력 비용={appliedCost}, " +
            $"남은 체력=" +
            $"{playerHealth.CurrentHealth}/" +
            $"{playerHealth.MaxHealth}",
            this
        );

        return true;
    }

    private bool CanExecuteRetreat(
        bool includeInputLocks)
    {
        if (turnManager == null ||
            blockGridManager == null ||
            playerHealth == null ||
            ballLauncher == null ||
            navigator == null)
        {
            return false;
        }

        if (navigator.PreviousRoom == null)
        {
            return false;
        }

        if (turnManager.IsGameOver ||
            playerHealth.IsDead)
        {
            return false;
        }

        if (includeInputLocks)
        {
            if (navigator.IsNavigationLocked ||
                turnManager.IsInputLocked)
            {
                return false;
            }

            if (turnManager.CurrentState !=
                TurnState.Aiming)
            {
                return false;
            }
        }

        if (blockGridManager.CurrentRoomState !=
            RoomCombatState.InCombat)
        {
            return false;
        }

        if (!blockGridManager
                .CanResetCurrentRoomCombat)
        {
            return false;
        }

        if (ballLauncher.IsAttackInProgress)
        {
            return false;
        }

        return HasEnoughHealth;
    }

    private void LogRetreatFailure()
    {
        if (turnManager == null ||
            blockGridManager == null ||
            playerHealth == null ||
            ballLauncher == null ||
            navigator == null ||
            transitionController == null)
        {
            Debug.LogWarning(
                "RoomRetreatController: " +
                "필수 참조가 연결되지 않아 " +
                "후퇴할 수 없습니다.",
                this
            );

            return;
        }

        if (transitionController.IsTransitioning)
        {
            Debug.LogWarning(
                "RoomRetreatController: " +
                "방 전환 중에는 후퇴할 수 없습니다.",
                this
            );

            return;
        }

        if (navigator.PreviousRoom == null)
        {
            Debug.LogWarning(
                "RoomRetreatController: " +
                "이동할 직전 방이 없습니다.",
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
                "공격 또는 턴 처리 중에는 후퇴할 수 없습니다.",
                this
            );

            return;
        }

        if (turnManager.IsInputLocked ||
            navigator.IsNavigationLocked)
        {
            Debug.LogWarning(
                "RoomRetreatController: " +
                "입력 또는 이동이 잠겨 있습니다.",
                this
            );

            return;
        }

        if (ballLauncher.IsAttackInProgress)
        {
            Debug.LogWarning(
                "RoomRetreatController: " +
                "공 공격 진행 중에는 후퇴할 수 없습니다.",
                this
            );

            return;
        }

        if (blockGridManager.CurrentRoomState !=
            RoomCombatState.InCombat)
        {
            Debug.LogWarning(
                "RoomRetreatController: " +
                "미클리어 전투방에서만 후퇴할 수 있습니다.",
                this
            );

            return;
        }

        if (!blockGridManager
                .CanResetCurrentRoomCombat)
        {
            Debug.LogWarning(
                "RoomRetreatController: " +
                "현재 방 최초 배치가 저장되지 않아 " +
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
                $"기본 비용={BaseRetreatCost}, " +
                $"할인율={RetreatCostDiscountRatio:P0}, " +
                $"실제 필요 체력={RetreatCost}",
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