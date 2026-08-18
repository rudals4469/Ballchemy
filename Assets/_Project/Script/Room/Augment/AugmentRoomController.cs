using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AugmentRoomController : MonoBehaviour
{
    private const int ChoiceCount = 3;

    [SerializeField] private StageRoomNavigator roomNavigator;
    [SerializeField] private StageMapGenerator mapGenerator;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private RoomRewardGenerator rewardGenerator;
    [SerializeField] private RewardSelectionUI rewardSelectionUI;
    [SerializeField] private BallCollection ballCollection;
    [SerializeField] private RunAugmentState runAugmentState;

    private sealed class CompletedSelection
    {
        public readonly List<RewardDefinition> Choices;
        public readonly RewardDefinition Selected;

        public CompletedSelection(
            IEnumerable<RewardDefinition> choices,
            RewardDefinition selected)
        {
            Choices = new List<RewardDefinition>(choices);
            Selected = selected;
        }
    }

    private readonly List<RewardDefinition> pendingChoices =
        new List<RewardDefinition>();
    private readonly Dictionary<int, CompletedSelection> completed =
        new Dictionary<int, CompletedSelection>();
    private int pendingRoomId = -1;

    private void Awake() => FindReferences();

    private void OnEnable()
    {
        FindReferences();
        if (roomNavigator != null)
        {
            roomNavigator.RoomChanged -= HandleRoomChanged;
            roomNavigator.RoomChanged += HandleRoomChanged;
        }
        if (rewardSelectionUI != null)
        {
            rewardSelectionUI.RewardSelected -= HandleRewardSelected;
            rewardSelectionUI.RewardSelected += HandleRewardSelected;
        }
        if (mapGenerator != null)
        {
            mapGenerator.MapGenerated -= HandleMapGenerated;
            mapGenerator.MapGenerated += HandleMapGenerated;
        }
    }

    private void Start()
    {
        HandleRoomChanged(null, roomNavigator != null ? roomNavigator.CurrentRoom : null);
    }

    private void OnDisable()
    {
        if (roomNavigator != null)
            roomNavigator.RoomChanged -= HandleRoomChanged;
        if (rewardSelectionUI != null)
            rewardSelectionUI.RewardSelected -= HandleRewardSelected;
        if (mapGenerator != null)
            mapGenerator.MapGenerated -= HandleMapGenerated;
        ReleasePendingState();
    }

    private void HandleMapGenerated(StageMap map)
    {
        completed.Clear();
        ReleasePendingState();
    }

    private void HandleRoomChanged(RoomNode previousRoom, RoomNode currentRoom)
    {
        if (currentRoom == null || currentRoom.RoomType != RoomType.Augment)
        {
            if (pendingRoomId >= 0)
                ReleasePendingState();
            return;
        }

        if (completed.TryGetValue(currentRoom.RoomId, out CompletedSelection result))
        {
            pendingRoomId = -1;
            pendingChoices.Clear();
            roomNavigator.SetNavigationLocked(false);
            turnManager?.SetInputLocked(false);
            rewardSelectionUI.ShowCompletedChoices(result.Choices, result.Selected);
            return;
        }

        OpenSelection(currentRoom);
    }

    private void OpenSelection(RoomNode room)
    {
        if (room == null || rewardGenerator == null || rewardSelectionUI == null ||
            runAugmentState == null)
        {
            Debug.LogError(
                "AugmentRoomController: 증강 선택에 필요한 참조가 없습니다.",
                this);
            roomNavigator?.SetNavigationLocked(false);
            turnManager?.SetInputLocked(false);
            return;
        }

        RewardApplyContext context = new RewardApplyContext(
            ballCollection, runAugmentState);
        List<RewardDefinition> generated = rewardGenerator.GenerateAugmentChoices(
            AugmentRewardSource.AugmentRoom, ChoiceCount, context);

        if (generated == null || generated.Count == 0)
        {
            Debug.LogWarning("AugmentRoomController: 증강 후보를 생성하지 못했습니다.", this);
            roomNavigator.SetNavigationLocked(false);
            turnManager?.SetInputLocked(false);
            rewardSelectionUI.Hide();
            return;
        }

        pendingRoomId = room.RoomId;
        pendingChoices.Clear();
        pendingChoices.AddRange(generated);
        roomNavigator.SetNavigationLocked(true);
        turnManager?.SetInputLocked(true);
        rewardSelectionUI.ShowChoices(pendingChoices);
    }

    private void HandleRewardSelected(RewardDefinition selectedReward)
    {
        if (pendingRoomId < 0 || selectedReward == null ||
            roomNavigator == null || roomNavigator.CurrentRoom == null ||
            roomNavigator.CurrentRoom.RoomType != RoomType.Augment)
            return;

        RewardApplyContext context = new RewardApplyContext(
            ballCollection, runAugmentState);
        if (!selectedReward.CanApply(context) || !selectedReward.Apply(context))
        {
            // ShowChoices가 RewardSelectionUI의 hasSelection까지 초기화합니다.
            // SetCardsInteractable만 호출하면 카드는 보여도 재선택 이벤트가
            // 차단된 상태로 남습니다.
            rewardSelectionUI.ShowChoices(pendingChoices);
            return;
        }

        completed[pendingRoomId] = new CompletedSelection(
            pendingChoices, selectedReward);
        pendingRoomId = -1;
        pendingChoices.Clear();
        roomNavigator.SetNavigationLocked(false);
        turnManager?.SetInputLocked(false);
    }

    private void ReleasePendingState()
    {
        pendingRoomId = -1;
        pendingChoices.Clear();
        roomNavigator?.SetNavigationLocked(false);
        turnManager?.SetInputLocked(false);
        rewardSelectionUI?.Hide();
    }

    private void FindReferences()
    {
        if (roomNavigator == null) roomNavigator = FindFirstObjectByType<StageRoomNavigator>();
        if (mapGenerator == null) mapGenerator = FindFirstObjectByType<StageMapGenerator>();
        if (turnManager == null) turnManager = FindFirstObjectByType<TurnManager>();
        if (rewardGenerator == null) rewardGenerator = FindFirstObjectByType<RoomRewardGenerator>();
        if (rewardSelectionUI == null) rewardSelectionUI = FindFirstObjectByType<RewardSelectionUI>(FindObjectsInactive.Include);
        if (ballCollection == null) ballCollection = FindFirstObjectByType<BallCollection>();
        if (runAugmentState == null) runAugmentState = FindFirstObjectByType<RunAugmentState>();
    }
}
