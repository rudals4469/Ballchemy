using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EventSelectionUI :
    MonoBehaviour
{
    [Header("Panel")]

    [SerializeField]
    private GameObject panelRoot;

    [Header("Cards")]

    [SerializeField]
    private List<EventChoiceCardUI> cards =
        new List<EventChoiceCardUI>();

    [Header("State")]

    [SerializeField]
    private bool hideOnAwake = true;

    private bool isOpen;
    private bool hasSelection;

    public bool IsOpen =>
        isOpen;

    public event Action<EventChoiceData>
        ChoiceSelected;

    private void Awake()
    {
        RemoveDuplicateAndNullCards();
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
        RemoveDuplicateAndNullCards();
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
        IReadOnlyList<EventChoiceData> choices)
    {
        if (choices == null ||
            choices.Count == 0)
        {
            Debug.LogWarning(
                "EventSelectionUI: " +
                "표시할 선택지가 없습니다.",
                this
            );

            Hide();

            return;
        }

        RemoveDuplicateAndNullCards();

        SetPanelActive(
            true
        );

        isOpen =
            true;

        hasSelection =
            false;

        ClearCards();

        int visibleCount =
            Mathf.Min(
                cards.Count,
                choices.Count
            );

        for (int i = 0;
             i < visibleCount;
             i++)
        {
            EventChoiceCardUI card =
                cards[i];

            if (card == null)
            {
                continue;
            }

            card.Bind(
                choices[i]
            );
        }

        for (int i = visibleCount;
             i < cards.Count;
             i++)
        {
            EventChoiceCardUI card =
                cards[i];

            if (card == null)
            {
                continue;
            }

            card.Clear();
        }

        if (choices.Count >
            cards.Count)
        {
            Debug.LogWarning(
                "EventSelectionUI: " +
                $"선택지는 {choices.Count}개지만 " +
                $"연결된 카드는 {cards.Count}개입니다. " +
                "초과 선택지는 표시되지 않습니다.",
                this
            );
        }
    }

    public void Hide()
    {
        SetCardsInteractable(
            false
        );

        ClearCards();

        isOpen =
            false;

        hasSelection =
            false;

        SetPanelActive(
            false
        );
    }

    public void SetCardsInteractable(
        bool shouldEnable)
    {
        for (int i = 0;
             i < cards.Count;
             i++)
        {
            EventChoiceCardUI card =
                cards[i];

            if (card == null ||
                !card.HasChoice)
            {
                continue;
            }

            card.SetSelectionEnabled(
                shouldEnable
            );
        }
    }

    private void ClearCards()
    {
        for (int i = 0;
             i < cards.Count;
             i++)
        {
            EventChoiceCardUI card =
                cards[i];

            /*
             * Unity에서 Destroy된 객체는
             * card == null 검사로 제외해야 합니다.
             *
             * card?.Clear()는 파괴된 Unity 객체에서
             * MissingReferenceException을 발생시킬 수 있습니다.
             */
            if (card == null)
            {
                continue;
            }

            card.Clear();
        }
    }

    private void HandleCardSelected(
        EventChoiceCardUI selectedCard,
        EventChoiceData selectedChoice)
    {
        if (!isOpen ||
            hasSelection ||
            selectedCard == null ||
            selectedChoice == null)
        {
            return;
        }

        hasSelection =
            true;

        SetCardsInteractable(
            false
        );

        ChoiceSelected?.Invoke(
            selectedChoice
        );
    }

    private void SubscribeCards()
    {
        for (int i = 0;
             i < cards.Count;
             i++)
        {
            EventChoiceCardUI card =
                cards[i];

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
             i < cards.Count;
             i++)
        {
            EventChoiceCardUI card =
                cards[i];

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

        if (target == null ||
            target.activeSelf ==
            shouldActivate)
        {
            return;
        }

        target.SetActive(
            shouldActivate
        );
    }

    private void RemoveDuplicateAndNullCards()
    {
        if (cards == null)
        {
            cards =
                new List<EventChoiceCardUI>();

            return;
        }

        HashSet<EventChoiceCardUI> unique =
            new HashSet<EventChoiceCardUI>();

        for (int i = cards.Count - 1;
             i >= 0;
             i--)
        {
            EventChoiceCardUI card =
                cards[i];

            if (card == null ||
                !unique.Add(
                    card
                ))
            {
                cards.RemoveAt(
                    i
                );
            }
        }
    }

    private void ValidateReferences()
    {
        if (panelRoot == null)
        {
            Debug.LogWarning(
                "EventSelectionUI: " +
                "Panel Root가 연결되지 않았습니다. " +
                "현재 GameObject를 패널 루트로 사용합니다.",
                this
            );
        }

        if (cards.Count < 3)
        {
            Debug.LogWarning(
                "EventSelectionUI: 이벤트 카드가 " +
                $"{cards.Count}개 연결되어 있습니다. " +
                "기본 선택지는 3개입니다.",
                this
            );
        }
    }
}