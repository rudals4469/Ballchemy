using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RoomRewardController :
    MonoBehaviour
{
    [Header("Room References")]

    [SerializeField]
    private BlockGridManager
        blockGridManager;

    [SerializeField]
    private StageRoomNavigator
        roomNavigator;

    [SerializeField]
    private TurnManager
        turnManager;

    [Header("Reward References")]

    [SerializeField]
    private RoomRewardGenerator
        rewardGenerator;

    [SerializeField]
    private RewardSelectionUI
        rewardSelectionUI;

    [SerializeField]
    private BallCollection
        ballCollection;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog = true;

    private readonly List<RewardDefinition>
        pendingChoices =
            new List<RewardDefinition>();

    private bool isRewardPending;

    public bool IsRewardPending =>
        isRewardPending;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        ReleaseLocksOnDisable();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        FindReferences();
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

        if (roomNavigator == null)
        {
            roomNavigator =
                FindFirstObjectByType<
                    StageRoomNavigator
                >();
        }

        if (turnManager == null)
        {
            turnManager =
                FindFirstObjectByType<
                    TurnManager
                >();
        }

        if (rewardGenerator == null)
        {
            rewardGenerator =
                FindFirstObjectByType<
                    RoomRewardGenerator
                >();
        }

        if (rewardSelectionUI == null)
        {
            rewardSelectionUI =
                FindFirstObjectByType<
                    RewardSelectionUI
                >(
                    FindObjectsInactive.Include
                );
        }

        if (ballCollection == null)
        {
            ballCollection =
                FindFirstObjectByType<
                    BallCollection
                >();
        }
    }

    private void SubscribeEvents()
    {
        if (blockGridManager != null)
        {
            blockGridManager.RoomCleared -=
                HandleRoomCleared;

            blockGridManager.RoomCleared +=
                HandleRoomCleared;
        }

        if (rewardSelectionUI != null)
        {
            rewardSelectionUI.RewardSelected -=
                HandleRewardSelected;

            rewardSelectionUI.RewardSelected +=
                HandleRewardSelected;
        }
    }

    private void UnsubscribeEvents()
    {
        if (blockGridManager != null)
        {
            blockGridManager.RoomCleared -=
                HandleRoomCleared;
        }

        if (rewardSelectionUI != null)
        {
            rewardSelectionUI.RewardSelected -=
                HandleRewardSelected;
        }
    }

    private void HandleRoomCleared()
    {
        if (isRewardPending)
        {
            return;
        }

        if (!CanOpenRoomReward())
        {
            return;
        }

        RoomNode currentRoom =
            roomNavigator.CurrentRoom;

        RewardTier rewardTier =
            ResolveRewardTier(
                currentRoom.RoomType
            );

        if (rewardTier == RewardTier.None)
        {
            if (showDebugLog)
            {
                Debug.Log(
                    "RoomRewardController: " +
                    $"{currentRoom.RoomType} 방은 " +
                    "현재 보상 대상이 아닙니다.",
                    this
                );
            }

            return;
        }

        List<RewardDefinition> choices =
            rewardGenerator.GenerateChoices(
                rewardTier
            );

        if (choices == null ||
            choices.Count == 0)
        {
            Debug.LogWarning(
                "RoomRewardController: " +
                $"{currentRoom.RoomType} 방의 " +
                $"{rewardTier} 보상 선택지를 " +
                "생성하지 못했습니다.",
                this
            );

            /*
             * 보상을 생성하지 못했다면 플레이어가
             * 방에 갇히지 않도록 잠금을 남기지 않습니다.
             */
            ReleaseRoomAfterReward();

            return;
        }

        pendingChoices.Clear();
        pendingChoices.AddRange(
            choices
        );

        isRewardPending =
            true;

        /*
         * 먼저 네비게이터 자체를 잠급니다.
         *
         * RoomNavigationUI는 CanMove가 false가 되면서
         * 화살표를 숨깁니다. 이후 맵 노드 클릭 이동이
         * 추가되어도 같은 잠금으로 차단됩니다.
         */
        roomNavigator.SetNavigationLocked(
            true
        );

        turnManager?.SetInputLocked(
            true
        );

        rewardSelectionUI.ShowChoices(
            pendingChoices
        );

        if (showDebugLog)
        {
            Debug.Log(
                "RoomRewardController: " +
                $"Room {currentRoom.RoomId}, " +
                $"{currentRoom.RoomType} 클리어 보상 " +
                $"{rewardTier} 선택지 " +
                $"{pendingChoices.Count}개 표시",
                this
            );
        }
    }

    private void HandleRewardSelected(
        RewardDefinition selectedReward)
    {
        if (!isRewardPending ||
            selectedReward == null)
        {
            return;
        }

        if (ballCollection == null)
        {
            Debug.LogError(
                "RoomRewardController: " +
                "BallCollection이 없어 " +
                "보상을 적용할 수 없습니다.",
                this
            );

            RestorePendingChoices();

            return;
        }

        RewardApplyContext applyContext =
            new RewardApplyContext(
                ballCollection
            );

        bool applied =
            selectedReward.Apply(
                applyContext
            );

        if (!applied)
        {
            Debug.LogWarning(
                "RoomRewardController: " +
                $"{selectedReward.DisplayName} 보상 " +
                "적용에 실패했습니다.",
                this
            );

            RestorePendingChoices();

            return;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "RoomRewardController: " +
                $"{selectedReward.DisplayName} 보상 적용 완료, " +
                $"현재 공 개수={ballCollection.Count}",
                this
            );
        }

        CompleteRewardSelection();
    }

    private bool CanOpenRoomReward()
    {
        if (roomNavigator == null ||
            rewardGenerator == null ||
            rewardSelectionUI == null)
        {
            Debug.LogError(
                "RoomRewardController: " +
                "보상 실행에 필요한 참조가 없습니다.",
                this
            );

            return false;
        }

        RoomNode currentRoom =
            roomNavigator.CurrentRoom;

        if (currentRoom == null ||
            !currentRoom.IsCombatRoom)
        {
            return false;
        }

        /*
         * 현재 구현 단계에서 BlockGridManager의
         * 일반 방 클리어 이벤트는 NormalCombat과
         * NamedCombat에서 사용됩니다.
         *
         * 보스 보상은 보스 클리어 흐름을 연결하는
         * 단계에서 같은 컨트롤러로 확장합니다.
         */
        return currentRoom.RoomType ==
                   RoomType.NormalCombat ||
               currentRoom.RoomType ==
                   RoomType.NamedCombat;
    }

    private static RewardTier ResolveRewardTier(
        RoomType roomType)
    {
        switch (roomType)
        {
            case RoomType.NormalCombat:
                return RewardTier.Tier1;

            case RoomType.NamedCombat:
                return RewardTier.Tier2;

            case RoomType.Boss:
                return RewardTier.Tier3;

            default:
                return RewardTier.None;
        }
    }

    private void RestorePendingChoices()
    {
        /*
         * 적용 실패 시 보상 선택을 다시 열어야 하므로
         * 입력과 이동 잠금을 유지합니다.
         */
        roomNavigator?.SetNavigationLocked(
            true
        );

        turnManager?.SetInputLocked(
            true
        );

        if (pendingChoices.Count > 0 &&
            rewardSelectionUI != null)
        {
            /*
             * ShowChoices를 다시 호출하면
             * RewardSelectionUI의 선택 완료 상태도
             * 함께 초기화됩니다.
             */
            rewardSelectionUI.ShowChoices(
                pendingChoices
            );
        }
    }

    private void CompleteRewardSelection()
    {
        rewardSelectionUI?.Hide();

        pendingChoices.Clear();

        isRewardPending =
            false;

        ReleaseRoomAfterReward();
    }

    private void ReleaseRoomAfterReward()
    {
        /*
         * 네비게이터 잠금을 마지막에 해제합니다.
         *
         * 먼저 TurnManager 입력 잠금을 풀어도
         * 네비게이터 잠금이 유지되므로 화살표는
         * 아직 나타나지 않습니다.
         *
         * 마지막 SetNavigationLocked(false)가
         * NavigationAvailabilityChanged를 발생시키며
         * 이때 이동 가능한 화살표가 표시됩니다.
         */
        turnManager?.SetInputLocked(
            false
        );

        roomNavigator?.SetNavigationLocked(
            false
        );
    }

    private void ReleaseLocksOnDisable()
    {
        if (!isRewardPending)
        {
            return;
        }

        isRewardPending =
            false;

        pendingChoices.Clear();

        turnManager?.SetInputLocked(
            false
        );

        roomNavigator?.SetNavigationLocked(
            false
        );
    }

    private void ValidateReferences()
    {
        if (blockGridManager == null)
        {
            Debug.LogError(
                "RoomRewardController: " +
                "BlockGridManager가 연결되지 않았습니다.",
                this
            );
        }

        if (roomNavigator == null)
        {
            Debug.LogError(
                "RoomRewardController: " +
                "StageRoomNavigator가 연결되지 않았습니다.",
                this
            );
        }

        if (turnManager == null)
        {
            Debug.LogError(
                "RoomRewardController: " +
                "TurnManager가 연결되지 않았습니다.",
                this
            );
        }

        if (rewardGenerator == null)
        {
            Debug.LogError(
                "RoomRewardController: " +
                "RoomRewardGenerator가 연결되지 않았습니다.",
                this
            );
        }

        if (rewardSelectionUI == null)
        {
            Debug.LogError(
                "RoomRewardController: " +
                "RewardSelectionUI가 연결되지 않았습니다.",
                this
            );
        }

        if (ballCollection == null)
        {
            Debug.LogError(
                "RoomRewardController: " +
                "BallCollection이 연결되지 않았습니다.",
                this
            );
        }
    }
}