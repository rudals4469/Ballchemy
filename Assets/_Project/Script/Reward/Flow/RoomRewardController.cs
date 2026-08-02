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

        if (ballCollection == null)
        {
            Debug.LogError(
                "RoomRewardController: " +
                "BallCollection이 없어 보상 선택지를 " +
                "생성할 수 없습니다.",
                this
            );

            ReleaseRoomAfterReward();

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

        /*
         * 보상 생성 시점에도 현재 BallCollection을 전달합니다.
         *
         * 이를 통해 승급 가능한 공이 없는 경우,
         * 1성 기본 공이 3개 미만인 경우처럼
         * 현재 상태에서 적용할 수 없는 보상을
         * 선택지 후보에서 제외합니다.
         */
        RewardApplyContext applyContext =
            new RewardApplyContext(
                ballCollection
            );

        List<RewardDefinition> choices =
            rewardGenerator.GenerateChoices(
                rewardTier,
                applyContext
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

        /*
         * 선택지를 생성한 뒤 공 상태가 바뀌었을 가능성에
         * 대비해 적용 직전에도 조건을 다시 검사합니다.
         */
        if (!selectedReward.CanApply(
                applyContext
            ))
        {
            Debug.LogWarning(
                "RoomRewardController: " +
                $"{selectedReward.DisplayName} 보상은 " +
                "현재 상태에서 적용할 수 없습니다.",
                this
            );

            RestorePendingChoices();

            return;
        }

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
        roomNavigator?.SetNavigationLocked(
            true
        );

        turnManager?.SetInputLocked(
            true
        );

        if (pendingChoices.Count > 0 &&
            rewardSelectionUI != null)
        {
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