using System;
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

    [SerializeField]
    private RunAugmentState
        runAugmentState;

    [SerializeField]
    private RunRewardState
        runRewardState;

    [Header("Debug")]

    [Tooltip(
        "None이면 방 타입에 맞는 정상 보상 등급을 사용합니다.\n" +
        "Tier3로 설정하면 일반 또는 네임드 전투방에서도 " +
        "Tier3 증강 보상을 테스트할 수 있습니다.\n" +
        "테스트 후 반드시 None으로 되돌립니다."
    )]
    [SerializeField]
    private RewardTier
        debugRewardTierOverride =
            RewardTier.None;

    [SerializeField]
    private bool showDebugLog = true;

    private readonly List<RewardDefinition>
        pendingChoices =
            new List<RewardDefinition>();

    private sealed class CompletedRewardSelection
    {
        public readonly List<RewardDefinition> Choices;
        public readonly RewardDefinition SelectedReward;

        public CompletedRewardSelection(
            IEnumerable<RewardDefinition> choices,
            RewardDefinition selectedReward)
        {
            Choices = new List<RewardDefinition>(choices);
            SelectedReward = selectedReward;
        }
    }

    private readonly Dictionary<int, CompletedRewardSelection>
        completedSelections =
            new Dictionary<int, CompletedRewardSelection>();

    private bool isRewardPending;
    private Action endlessRewardCompleted;

    public bool IsRewardPending =>
        isRewardPending;

    public bool TryOpenEndlessAugmentReward(Action completed = null)
    {
        if (isRewardPending || rewardGenerator == null ||
            rewardSelectionUI == null || ballCollection == null)
        {
            return false;
        }

        List<RewardDefinition> choices = rewardGenerator.GenerateAugmentChoices(
            AugmentRewardSource.Boss,
            rewardGenerator.ChoiceCount,
            CreateApplyContext());

        if (choices == null || choices.Count == 0)
        {
            return false;
        }

        pendingChoices.Clear();
        pendingChoices.AddRange(choices);
        endlessRewardCompleted = completed;
        isRewardPending = true;
        roomNavigator?.SetNavigationLocked(true);
        turnManager?.SetInputLocked(true);
        rewardSelectionUI.ShowChoices(pendingChoices);
        return true;
    }

    /*
     * 보상 선택 완료 후 다른 연출이 잠금을 이어받을 때 사용합니다.
     *
     * 반환값:
     * true  = 외부 시스템이 입력/이동 잠금을 이어받음
     * false = RoomRewardController가 잠금을 해제함
     */
    public event Func<RoomNode, bool>
        RewardCompletionHandoffRequested;

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

        if (runAugmentState == null)
        {
            runAugmentState =
                FindFirstObjectByType<
                    RunAugmentState
                >();
        }

        if (runRewardState == null)
        {
            runRewardState =
                FindFirstObjectByType<
                    RunRewardState
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

        if (roomNavigator != null)
        {
            roomNavigator.RoomChanged -= HandleRoomChanged;
            roomNavigator.RoomChanged += HandleRoomChanged;
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

        if (roomNavigator != null)
        {
            roomNavigator.RoomChanged -= HandleRoomChanged;
        }
    }

    private void HandleRoomChanged(RoomNode previousRoom, RoomNode currentRoom)
    {
        if (isRewardPending)
            return;

        // AugmentRoomController가 같은 공용 보상 UI를 소유합니다.
        // 여기서 Hide()를 호출하면 방 입장 직후 표시한 증강 카드가
        // 이벤트 구독 순서에 따라 다시 사라질 수 있습니다.
        if (currentRoom != null &&
            currentRoom.RoomType == RoomType.Augment)
        {
            return;
        }

        if (currentRoom != null &&
            completedSelections.TryGetValue(
                currentRoom.RoomId,
                out CompletedRewardSelection completed))
        {
            rewardSelectionUI?.ShowCompletedChoices(
                completed.Choices,
                completed.SelectedReward);
            return;
        }

        rewardSelectionUI?.Hide();
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

        RewardTier baseRewardTier =
            ResolveRewardTier(
                currentRoom.RoomType
            );

        if (debugRewardTierOverride !=
            RewardTier.None)
        {
            baseRewardTier =
                debugRewardTierOverride;
        }

        if (baseRewardTier ==
            RewardTier.None)
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

        bool isAugmentReward =
            currentRoom.RoomType == RoomType.Boss;

        RewardTier finalRewardTier =
            isAugmentReward
                ? baseRewardTier
                : ResolveFinalRewardTier(
                    baseRewardTier
                );

        RewardApplyContext applyContext =
            CreateApplyContext();

        List<RewardDefinition> choices =
            isAugmentReward
                ? rewardGenerator.GenerateAugmentChoices(
                    AugmentRewardSource.Boss,
                    rewardGenerator.ChoiceCount,
                    applyContext)
                : rewardGenerator.GenerateChoices(
                    finalRewardTier,
                    applyContext
                );

        if (choices == null ||
            choices.Count == 0)
        {
            Debug.LogWarning(
                "RoomRewardController: " +
                $"{currentRoom.RoomType} 방의 " +
                $"{finalRewardTier} 보상 선택지를 " +
                "생성하지 못했습니다.",
                this
            );

            ReleaseRoomAfterReward();

            return;
        }

        bool consumedRewardUpgrade =
            !isAugmentReward &&
            ConsumePendingRewardUpgradeIfNeeded();

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
                $"{currentRoom.RoomType} 클리어 보상, " +
                $"기본 등급={baseRewardTier}, " +
                $"최종 등급={finalRewardTier}, " +
                $"승급 예약 소비={consumedRewardUpgrade}, " +
                $"선택지={pendingChoices.Count}개",
                this
            );
        }
    }

    private RewardTier ResolveFinalRewardTier(
        RewardTier baseRewardTier)
    {
        if (runRewardState == null ||
            !runRewardState
                .HasPendingRewardUpgrade)
        {
            return baseRewardTier;
        }

        return runRewardState
            .ApplyPendingUpgrade(
                baseRewardTier
            );
    }

    private bool
        ConsumePendingRewardUpgradeIfNeeded()
    {
        if (runRewardState == null ||
            !runRewardState
                .HasPendingRewardUpgrade)
        {
            return false;
        }

        return runRewardState
            .ConsumePendingRewardUpgrade();
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
            CreateApplyContext();

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

        CompleteRewardSelection(selectedReward);
    }

    private RewardApplyContext CreateApplyContext()
    {
        return new RewardApplyContext(
            ballCollection,
            runAugmentState
        );
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
                   RoomType.NamedCombat ||
               currentRoom.RoomType ==
                   RoomType.Boss;
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

    private void CompleteRewardSelection(
        RewardDefinition selectedReward)
    {
        if (endlessRewardCompleted != null)
        {
            Action completed = endlessRewardCompleted;
            endlessRewardCompleted = null;
            pendingChoices.Clear();
            isRewardPending = false;
            rewardSelectionUI?.Hide();
            ReleaseRoomAfterReward();
            completed.Invoke();
            return;
        }

        RoomNode completedRoom =
            roomNavigator != null
                ? roomNavigator.CurrentRoom
                : null;

        if (completedRoom != null && selectedReward != null)
        {
            completedSelections[completedRoom.RoomId] =
                new CompletedRewardSelection(
                    pendingChoices,
                    selectedReward);
        }

        pendingChoices.Clear();

        isRewardPending =
            false;

        bool wasLockHandedOff =
            TryHandOffRewardCompletion(
                completedRoom
            );

        if (wasLockHandedOff)
        {
            if (showDebugLog)
            {
                Debug.Log(
                    "RoomRewardController: " +
                    "보상 완료 후 입력 잠금을 " +
                    "외부 연출 시스템에 인계했습니다.",
                    this
                );
            }

            return;
        }

        ReleaseRoomAfterReward();
    }

    private bool TryHandOffRewardCompletion(
        RoomNode completedRoom)
    {
        if (RewardCompletionHandoffRequested ==
            null)
        {
            return false;
        }

        Delegate[] handlers =
            RewardCompletionHandoffRequested
                .GetInvocationList();

        for (int i = 0;
             i < handlers.Length;
             i++)
        {
            if (!(handlers[i] is
                Func<RoomNode, bool> handler))
            {
                continue;
            }

            try
            {
                bool accepted =
                    handler.Invoke(
                        completedRoom
                    );

                if (accepted)
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception,
                    this
                );
            }
        }

        return false;
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

        if (runAugmentState == null)
        {
            Debug.LogWarning(
                "RoomRewardController: " +
                "RunAugmentState가 연결되지 않았습니다. " +
                "공 보상은 동작하지만 증강 보상은 " +
                "생성되지 않습니다.",
                this
            );
        }

        if (runRewardState == null)
        {
            Debug.LogError(
                "RoomRewardController: " +
                "RunRewardState가 연결되지 않았습니다. " +
                "다음 전투방 보상 등급 증가 효과가 " +
                "적용되지 않습니다.",
                this
            );
        }
    }
}
