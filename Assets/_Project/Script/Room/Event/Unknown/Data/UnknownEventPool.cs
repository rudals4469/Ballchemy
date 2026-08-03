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