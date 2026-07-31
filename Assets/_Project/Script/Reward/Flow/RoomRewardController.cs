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

    [Header("Navigation UI")]

    [Tooltip(
        "상하좌우 방 이동 버튼을 감싸는 " +
        "부모 오브젝트를 연결합니다. " +
        "보상 선택 중에는 이 오브젝트를 숨깁니다."
    )]
    [SerializeField]
    private GameObject navigationUiRoot;

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

            ReleaseRoomAfterReward();

            return;
        }

        pendingChoices.Clear();
        pendingChoices.AddRange(
            choices
        );

        isRewardPending =
            true;

        turnManager?.SetInputLocked(
            true
        );

        SetNavigationUiActive(
            false
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
        turnManager?.SetInputLocked(
            true
        );

        SetNavigationUiActive(
            false
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
        turnManager?.SetInputLocked(
            false
        );

        SetNavigationUiActive(
            true
        );
    }

    private void SetNavigationUiActive(
        bool shouldActivate)
    {
        if (navigationUiRoot == null ||
            navigationUiRoot.activeSelf ==
            shouldActivate)
        {
            return;
        }

        navigationUiRoot.SetActive(
            shouldActivate
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

        if (navigationUiRoot == null)
        {
            Debug.LogWarning(
                "RoomRewardController: " +
                "Navigation UI Root가 연결되지 않았습니다. " +
                "보상 선택 중 이동 화살표를 자동으로 " +
                "숨기지 못합니다.",
                this
            );
        }
    }
}