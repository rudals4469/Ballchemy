using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RewardSelectionUI :
    MonoBehaviour
{
    [Header("Panel")]

    [Tooltip(
        "보상 선택 UI 전체를 감싸는 오브젝트입니다. " +
        "현재 RewardSelectionPanel을 연결하면 됩니다."
    )]
    [SerializeField]
    private GameObject panelRoot;

    [Header("Cards")]

    [Tooltip(
        "보상 카드들을 표시할 순서대로 연결합니다."
    )]
    [SerializeField]
    private List<RewardCardUI> rewardCards =
        new List<RewardCardUI>();

    [Header("State")]

    [Tooltip(
        "Awake 시 보상 패널을 자동으로 숨길지 결정합니다."
    )]
    [SerializeField]
    private bool hideOnAwake = true;

    private bool isOpen;
    private bool hasSelection;

    public bool IsOpen =>
        isOpen;

    public bool HasSelection =>
        hasSelection;

    public int CardCount =>
        rewardCards.Count;

    public event Action<RewardDefinition>
        RewardSelected;

    private void Awake()
    {
        ValidateReferences();
        SubscribeCards();

        if (hideOnAwake)
        {
            Hide();
        }
        else
        {
            ClearCards();
        }
    }

    private void OnEnable()
    {
        SubscribeCards();
    }

    private void OnDisable()
    {
        UnsubscribeCards();
    }

    private void OnDestroy()
    {
        UnsubscribeCards();
    }

    private void OnValidate()
    {
        RemoveDuplicateAndNullCards();
    }

    public void ShowChoices(
        IReadOnlyList<RewardDefinition> choices)
    {
        if (choices == null ||
            choices.Count == 0)
        {
            Debug.LogWarning(
                "RewardSelectionUI: " +
                "표시할 보상 선택지가 없습니다.",
                this
            );

            Hide();

            return;
        }

        /*
         * 패널과 카드 오브젝트를 먼저 활성화합니다.
         *
         * 비활성화된 카드에 먼저 Bind한 뒤 패널을 켜면
         * 카드가 처음 활성화될 때 RewardCardUI.Awake()가
         * 실행되면서 Clear()되어 보상 연결이 사라질 수 있습니다.
         */
        SetPanelActive(
            true
        );

        isOpen =
            true;

        hasSelection =
            false;

        /*
         * 패널 활성화로 모든 RewardCardUI의 Awake가
         * 완료된 다음 카드를 초기화하고 보상을 연결합니다.
         */
        ClearCards();

        int visibleCardCount =
            Mathf.Min(
                rewardCards.Count,
                choices.Count
            );

        for (int i = 0;
             i < visibleCardCount;
             i++)
        {
            RewardCardUI card =
                rewardCards[i];

            RewardDefinition reward =
                choices[i];

            if (card == null)
            {
                continue;
            }

            card.Bind(
                reward
            );

            card.SetSelectionEnabled(
                true
            );
        }

        for (int i = visibleCardCount;
             i < rewardCards.Count;
             i++)
        {
            RewardCardUI card =
                rewardCards[i];

            if (card == null)
            {
                continue;
            }

            card.Clear();
        }

        if (choices.Count >
            rewardCards.Count)
        {
            Debug.LogWarning(
                "RewardSelectionUI: " +
                $"보상은 {choices.Count}개지만 " +
                $"연결된 카드는 {rewardCards.Count}개입니다. " +
                "초과 보상은 표시되지 않습니다.",
                this
            );
        }
    }

    public void SetCardsInteractable(
        bool shouldEnable)
    {
        for (int i = 0;
             i < rewardCards.Count;
             i++)
        {
            RewardCardUI card =
                rewardCards[i];

            if (card == null ||
                !card.HasReward)
            {
                continue;
            }

            card.SetSelectionEnabled(
                shouldEnable
            );
        }
    }

    public void Hide()
    {
        SetCardsInteractable(
            false
        );

        ClearCards();

        SetPanelActive(
            false
        );

        isOpen =
            false;

        hasSelection =
            false;
    }

    public void ClearCards()
    {
        for (int i = 0;
             i < rewardCards.Count;
             i++)
        {
            RewardCardUI card =
                rewardCards[i];

            if (card == null)
            {
                continue;
            }

            card.Clear();
        }
    }

    private void HandleCardSelected(
        RewardCardUI selectedCard,
        RewardDefinition selectedReward)
    {
        if (!isOpen ||
            hasSelection ||
            selectedCard == null ||
            selectedReward == null)
        {
            return;
        }

        hasSelection =
            true;

        SetCardsInteractable(
            false
        );

        RewardSelected?.Invoke(
            selectedReward
        );
    }

    private void SubscribeCards()
    {
        for (int i = 0;
             i < rewardCards.Count;
             i++)
        {
            RewardCardUI card =
                rewardCards[i];

            if (card == null)
            {
                continue;
            }

            card.Selected -=
                HandleCardSelected;

            card.Selected +=
                HandleCardSelected;
        }
    }

    private void UnsubscribeCards()
    {
        for (int i = 0;
             i < rewardCards.Count;
             i++)
        {
            RewardCardUI card =
                rewardCards[i];

            if (card == null)
            {
                continue;
            }

            card.Selected -=
                HandleCardSelected;
        }
    }

    private void SetPanelActive(
        bool shouldActivate)
    {
        GameObject target =
            panelRoot != null
                ? panelRoot
                : gameObject;

        if (target.activeSelf ==
            shouldActivate)
        {
            return;
        }

        target.SetActive(
            shouldActivate
        );
    }

    private void ValidateReferences()
    {
        RemoveDuplicateAndNullCards();

        if (rewardCards.Count == 0)
        {
            Debug.LogError(
                "RewardSelectionUI: " +
                "연결된 RewardCardUI가 없습니다.",
                this
            );

            return;
        }

        if (rewardCards.Count < 3)
        {
            Debug.LogWarning(
                "RewardSelectionUI: " +
                "현재 기본 보상 선택지는 3개입니다. " +
                $"연결된 카드는 {rewardCards.Count}개입니다.",
                this
            );
        }
    }

    private void RemoveDuplicateAndNullCards()
    {
        HashSet<RewardCardUI> uniqueCards =
            new HashSet<RewardCardUI>();

        for (int i = rewardCards.Count - 1;
             i >= 0;
             i--)
        {
            RewardCardUI card =
                rewardCards[i];

            if (card == null ||
                !uniqueCards.Add(
                    card
                ))
            {
                rewardCards.RemoveAt(
                    i
                );
            }
        }
    }
}