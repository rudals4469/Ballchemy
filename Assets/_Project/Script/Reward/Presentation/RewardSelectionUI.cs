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

    [Header("Augment State")]

    [Tooltip(
        "증강 카드의 현재 및 다음 레벨을 " +
        "표시할 때 사용하는 런 상태입니다."
    )]
    [SerializeField]
    private RunAugmentState runAugmentState;

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
        FindReferences();
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
        FindReferences();
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

    private void FindReferences()
    {
        if (runAugmentState != null)
        {
            return;
        }

        runAugmentState =
            FindFirstObjectByType<
                RunAugmentState
            >(
                FindObjectsInactive.Include
            );
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

        FindReferences();

        SetPanelActive(
            true
        );

        isOpen =
            true;

        hasSelection =
            false;

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
                reward,
                runAugmentState
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

        for (int i = 0; i < rewardCards.Count; i++)
        {
            RewardCardUI card = rewardCards[i];
            if (card != null && card.HasReward)
                card.ShowSelectionResult(card == selectedCard);
        }

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

        if (runAugmentState == null)
        {
            Debug.LogWarning(
                "RewardSelectionUI: " +
                "RunAugmentState가 연결되지 않았습니다. " +
                "증강 카드는 표시되지만 현재 레벨을 " +
                "정확히 반영하지 못할 수 있습니다.",
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
