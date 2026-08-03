using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "UnknownEventPool",
    menuName =
        "Ballchemy/Events/Unknown/Event Pool"
)]
public sealed class UnknownEventPool :
    ScriptableObject
{
    [Header("Events")]

    [SerializeField]
    private List<UnknownEventDefinition> events =
        new List<UnknownEventDefinition>();

    private readonly List<UnknownEventDefinition>
        validCandidates =
            new List<UnknownEventDefinition>();

    public int EventCount =>
        events != null
            ? events.Count
            : 0;

    public bool HasApplicableEvent(
        UnknownEventApplyContext context)
    {
        CollectApplicableEvents(
            context
        );

        return validCandidates.Count > 0;
    }

    /*
     * 현재 상태에서 실제 적용 가능한 이벤트를
     * 전달받은 results 목록에 복사합니다.
     *
     * 슬롯머신은 이 목록의 표시 문구만 순환하며,
     * 실제 당첨 결과는 TryDraw()의 가중치 추첨을
     * 그대로 사용합니다.
     */
    public int GetApplicableEvents(
        UnknownEventApplyContext context,
        List<UnknownEventDefinition> results)
    {
        if (results == null)
        {
            return 0;
        }

        results.Clear();

        CollectApplicableEvents(
            context
        );

        for (int i = 0;
             i < validCandidates.Count;
             i++)
        {
            UnknownEventDefinition candidate =
                validCandidates[i];

            if (candidate == null)
            {
                continue;
            }

            results.Add(
                candidate
            );
        }

        return results.Count;
    }

    public bool TryDraw(
        UnknownEventApplyContext context,
        out UnknownEventDefinition selectedEvent)
    {
        selectedEvent =
            null;

        CollectApplicableEvents(
            context
        );

        if (validCandidates.Count == 0)
        {
            return false;
        }

        int totalWeight = 0;

        for (int i = 0;
             i < validCandidates.Count;
             i++)
        {
            UnknownEventDefinition candidate =
                validCandidates[i];

            if (candidate == null)
            {
                continue;
            }

            totalWeight +=
                Mathf.Max(
                    candidate.SelectionWeight,
                    0
                );
        }

        if (totalWeight <= 0)
        {
            return false;
        }

        int randomValue =
            Random.Range(
                0,
                totalWeight
            );

        int accumulatedWeight = 0;

        for (int i = 0;
             i < validCandidates.Count;
             i++)
        {
            UnknownEventDefinition candidate =
                validCandidates[i];

            if (candidate == null)
            {
                continue;
            }

            accumulatedWeight +=
                Mathf.Max(
                    candidate.SelectionWeight,
                    0
                );

            if (randomValue >=
                accumulatedWeight)
            {
                continue;
            }

            selectedEvent =
                candidate;

            return true;
        }

        selectedEvent =
            validCandidates[
                validCandidates.Count - 1
            ];

        return selectedEvent != null;
    }

    private void CollectApplicableEvents(
        UnknownEventApplyContext context)
    {
        validCandidates.Clear();

        if (events == null ||
            context == null)
        {
            return;
        }

        HashSet<UnknownEventDefinition> unique =
            new HashSet<UnknownEventDefinition>();

        for (int i = 0;
             i < events.Count;
             i++)
        {
            UnknownEventDefinition candidate =
                events[i];

            if (candidate == null ||
                !unique.Add(
                    candidate
                ) ||
                !candidate.CanBeSelected)
            {
                continue;
            }

            if (!candidate.CanApply(
                    context
                ))
            {
                continue;
            }

            validCandidates.Add(
                candidate
            );
        }
    }

    private void OnValidate()
    {
        if (events == null)
        {
            events =
                new List<UnknownEventDefinition>();

            return;
        }

        HashSet<UnknownEventDefinition> unique =
            new HashSet<UnknownEventDefinition>();

        for (int i = events.Count - 1;
             i >= 0;
             i--)
        {
            UnknownEventDefinition candidate =
                events[i];

            if (candidate == null ||
                !unique.Add(
                    candidate
                ))
            {
                events.RemoveAt(
                    i
                );
            }
        }
    }
}