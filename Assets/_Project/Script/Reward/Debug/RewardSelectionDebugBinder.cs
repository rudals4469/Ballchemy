using System.Collections.Generic;
using UnityEngine;

public sealed class RewardSelectionDebugBinder :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private RoomRewardGenerator
        rewardGenerator;

    [SerializeField]
    private RewardSelectionUI
        rewardSelectionUI;

    [SerializeField]
    private BallCollection
        ballCollection;

    [Header("Test Settings")]

    [SerializeField]
    private RewardTier testRewardTier =
        RewardTier.Tier1;

    [Tooltip(
        "체크하면 씬 시작 시 즉시 보상 선택지를 표시합니다."
    )]
    [SerializeField]
    private bool showOnStart = true;

    [Tooltip(
        "보상 적용에 성공하면 보상 선택 패널을 닫습니다."
    )]
    [SerializeField]
    private bool hideAfterApply = true;

    private void Awake()
    {
        FindReferences();
    }

    private void Start()
    {
        SubscribeSelection();

        if (showOnStart)
        {
            ShowTestChoices();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeSelection();
    }

    private void FindReferences()
    {
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

    [ContextMenu("Show Test Choices")]
    public void ShowTestChoices()
    {
        FindReferences();

        if (!ValidateReferences())
        {
            return;
        }

        List<RewardDefinition> choices =
            rewardGenerator.GenerateChoices(
                testRewardTier
            );

        if (choices == null ||
            choices.Count == 0)
        {
            Debug.LogWarning(
                "RewardSelectionDebugBinder: " +
                "생성된 보상 선택지가 없습니다.",
                this
            );

            return;
        }

        rewardSelectionUI.ShowChoices(
            choices
        );
    }

    [ContextMenu("Hide Test Choices")]
    public void HideTestChoices()
    {
        if (rewardSelectionUI == null)
        {
            return;
        }

        rewardSelectionUI.Hide();
    }

    private void HandleRewardSelected(
        RewardDefinition selectedReward)
    {
        if (selectedReward == null)
        {
            return;
        }

        if (ballCollection == null)
        {
            Debug.LogError(
                "RewardSelectionDebugBinder: " +
                "BallCollection이 없어 보상을 적용할 수 없습니다.",
                this
            );

            RestoreSelection();

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
                "RewardSelectionDebugBinder: " +
                $"{selectedReward.DisplayName} 보상 적용에 " +
                "실패했습니다.",
                this
            );

            RestoreSelection();

            return;
        }

        Debug.Log(
            "RewardSelectionDebugBinder: " +
            $"{selectedReward.DisplayName} 보상이 " +
            "실제로 적용되었습니다. " +
            $"현재 공 개수={ballCollection.Count}",
            this
        );

        if (hideAfterApply)
        {
            rewardSelectionUI.Hide();
        }
    }

    private void RestoreSelection()
    {
        if (rewardSelectionUI == null)
        {
            return;
        }

        rewardSelectionUI.SetCardsInteractable(
            true
        );
    }

    private void SubscribeSelection()
    {
        if (rewardSelectionUI == null)
        {
            return;
        }

        rewardSelectionUI.RewardSelected -=
            HandleRewardSelected;

        rewardSelectionUI.RewardSelected +=
            HandleRewardSelected;
    }

    private void UnsubscribeSelection()
    {
        if (rewardSelectionUI == null)
        {
            return;
        }

        rewardSelectionUI.RewardSelected -=
            HandleRewardSelected;
    }

    private bool ValidateReferences()
    {
        bool isValid =
            true;

        if (rewardGenerator == null)
        {
            Debug.LogError(
                "RewardSelectionDebugBinder: " +
                "RoomRewardGenerator가 연결되지 않았습니다.",
                this
            );

            isValid =
                false;
        }

        if (rewardSelectionUI == null)
        {
            Debug.LogError(
                "RewardSelectionDebugBinder: " +
                "RewardSelectionUI가 연결되지 않았습니다.",
                this
            );

            isValid =
                false;
        }

        if (ballCollection == null)
        {
            Debug.LogError(
                "RewardSelectionDebugBinder: " +
                "BallCollection이 연결되지 않았습니다.",
                this
            );

            isValid =
                false;
        }
        else if (!ballCollection.IsInitialized)
        {
            Debug.LogWarning(
                "RewardSelectionDebugBinder: " +
                "BallCollection이 아직 초기화되지 않았습니다. " +
                "보상 선택 시점에는 초기화가 끝나 있어야 합니다.",
                this
            );
        }

        if (testRewardTier ==
            RewardTier.None)
        {
            Debug.LogError(
                "RewardSelectionDebugBinder: " +
                "Test Reward Tier가 None입니다.",
                this
            );

            isValid =
                false;
        }

        return isValid;
    }
}