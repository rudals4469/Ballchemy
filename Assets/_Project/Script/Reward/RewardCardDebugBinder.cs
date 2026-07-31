using UnityEngine;

public sealed class RewardCardDebugBinder :
    MonoBehaviour
{
    [SerializeField]
    private RewardCardUI rewardCard;

    [SerializeField]
    private RewardDefinition testReward;

    private void Start()
    {
        if (rewardCard == null)
        {
            Debug.LogError(
                "RewardCardDebugBinder: " +
                "Reward Card가 연결되지 않았습니다.",
                this
            );

            return;
        }

        if (testReward == null)
        {
            Debug.LogError(
                "RewardCardDebugBinder: " +
                "Test Reward가 연결되지 않았습니다.",
                this
            );

            return;
        }

        rewardCard.Bind(
            testReward
        );

        rewardCard.Selected +=
            HandleRewardSelected;
    }

    private void OnDestroy()
    {
        if (rewardCard != null)
        {
            rewardCard.Selected -=
                HandleRewardSelected;
        }
    }

    private void HandleRewardSelected(
        RewardCardUI selectedCard,
        RewardDefinition selectedReward)
    {
        Debug.Log(
            "RewardCardDebugBinder: " +
            $"{selectedReward.DisplayName} 카드가 " +
            "선택되었습니다.",
            selectedCard
        );
    }
}