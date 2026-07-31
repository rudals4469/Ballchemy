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

    [Header("Test Settings")]

    [SerializeField]
    private RewardTier testRewardTier =
        RewardTier.Tier1;

    [Tooltip(
        "체크하면 씬 시작 시 즉시 보상 선택지를 표시합니다."
    )]
    [SerializeField]
    private bool showOnStart = true;

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

    [ContextMenu("Show Test Choices")]
    public void ShowTestChoices()
    {
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

        Debug.Log(
            "RewardSelectionDebugBinder: " +
            $"{selectedReward.DisplayName} 보상이 " +
            "선택되었습니다.",
            this
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