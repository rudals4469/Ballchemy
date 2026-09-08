using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class AugmentRuntimeEntry
{
    [SerializeField]
    private AugmentDefinition definition;

    [SerializeField, Min(1)]
    private int level = 1;

    public AugmentDefinition Definition =>
        definition;

    public int Level =>
        level;

    public AugmentRuntimeEntry(
        AugmentDefinition definition,
        int level)
    {
        this.definition =
            definition;

        this.level =
            Mathf.Max(
                level,
                1
            );
    }

    public void SetLevel(
        int newLevel)
    {
        level =
            Mathf.Max(
                newLevel,
                1
            );
    }
}

[DisallowMultipleComponent]
public sealed class RunAugmentState :
    MonoBehaviour
{
    [Header("References")]

    [Tooltip(
        "모든 공이 공유하는 현재 런의 " +
        "BallRuntimeStats입니다."
    )]
    [SerializeField]
    private BallRuntimeStats ballRuntimeStats;

    [Header("Runtime Augments")]

    [Tooltip(
        "현재 런에서 획득한 증강과 레벨입니다. " +
        "플레이 중 확인하기 위한 런타임 목록입니다."
    )]
    [SerializeField]
    private List<AugmentRuntimeEntry>
        activeAugments =
            new List<AugmentRuntimeEntry>();

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog = true;

    public BallRuntimeStats BallRuntimeStats =>
        ballRuntimeStats;

    public IReadOnlyList<AugmentRuntimeEntry>
        ActiveAugments =>
            activeAugments;

    public event Action<
        AugmentDefinition,
        int,
        int
    > AugmentLevelChanged;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
        RemoveInvalidAndDuplicateEntries();
    }

    private void OnValidate()
    {
        FindReferences();
        RemoveInvalidAndDuplicateEntries();
    }

    private void FindReferences()
    {
        if (ballRuntimeStats != null)
        {
            return;
        }

        ballRuntimeStats =
            GetComponent<
                BallRuntimeStats
            >();

        if (ballRuntimeStats == null &&
            Application.isPlaying)
        {
            ballRuntimeStats =
                FindFirstObjectByType<
                    BallRuntimeStats
                >();
        }
    }

    private void ValidateReferences()
    {
        if (ballRuntimeStats != null)
        {
            return;
        }

        Debug.LogError(
            "RunAugmentState: " +
            "BallRuntimeStats가 연결되지 않았습니다.",
            this
        );
    }

    public int GetLevel(
        AugmentDefinition definition)
    {
        AugmentRuntimeEntry entry =
            FindEntry(
                definition
            );

        return entry != null
            ? entry.Level
            : 0;
    }

    public bool HasAugment(
        AugmentDefinition definition)
    {
        return GetLevel(definition) > 0;
    }

    public bool IsAtMaxLevel(
        AugmentDefinition definition)
    {
        if (definition == null)
        {
            return false;
        }

        return GetLevel(definition) >=
               definition.MaxLevel;
    }

    public bool CanIncreaseLevel(
        AugmentDefinition definition)
    {
        if (definition == null ||
            ballRuntimeStats == null)
        {
            return false;
        }

        int currentLevel =
            GetLevel(
                definition
            );

        return currentLevel <
               definition.MaxLevel;
    }

    public bool TryIncreaseLevel(
        AugmentDefinition definition)
    {
        if (!CanIncreaseLevel(
                definition
            ))
        {
            return false;
        }

        int previousLevel =
            GetLevel(
                definition
            );

        int newLevel =
            previousLevel + 1;

        if (!definition.ApplyLevel(
                this,
                previousLevel,
                newLevel
            ))
        {
            return false;
        }

        AugmentRuntimeEntry entry =
            FindEntry(
                definition
            );

        if (entry == null)
        {
            entry =
                new AugmentRuntimeEntry(
                    definition,
                    newLevel
                );

            activeAugments.Add(
                entry
            );
        }
        else
        {
            entry.SetLevel(
                newLevel
            );
        }

        AugmentLevelChanged?.Invoke(
            definition,
            previousLevel,
            newLevel
        );

        if (showDebugLog)
        {
            Debug.Log(
                "RunAugmentState: " +
                $"{definition.DisplayName} " +
                $"Lv.{previousLevel} → Lv.{newLevel}",
                this
            );
        }

        return true;
    }

    public void RestoreAugments(IReadOnlyList<AugmentDefinition> definitions,
        IReadOnlyList<int> levels)
    {
        activeAugments.Clear();
        ballRuntimeStats?.ResetRunBonuses();
        if (definitions == null || levels == null)
            return;

        int count = Mathf.Min(definitions.Count, levels.Count);
        for (int i = 0; i < count; i++)
        {
            AugmentDefinition definition = definitions[i];
            int targetLevel = definition != null
                ? Mathf.Clamp(levels[i], 0, definition.MaxLevel)
                : 0;
            for (int level = 0; level < targetLevel; level++)
                TryIncreaseLevel(definition);
        }
    }

    private AugmentRuntimeEntry FindEntry(
        AugmentDefinition definition)
    {
        if (definition == null ||
            activeAugments == null)
        {
            return null;
        }

        for (int i = 0;
             i < activeAugments.Count;
             i++)
        {
            AugmentRuntimeEntry entry =
                activeAugments[i];

            if (entry == null ||
                entry.Definition != definition)
            {
                continue;
            }

            return entry;
        }

        return null;
    }

    private void RemoveInvalidAndDuplicateEntries()
    {
        if (activeAugments == null)
        {
            activeAugments =
                new List<AugmentRuntimeEntry>();

            return;
        }

        HashSet<AugmentDefinition>
            registeredDefinitions =
                new HashSet<AugmentDefinition>();

        for (int i =
                 activeAugments.Count - 1;
             i >= 0;
             i--)
        {
            AugmentRuntimeEntry entry =
                activeAugments[i];

            if (entry == null ||
                entry.Definition == null ||
                !registeredDefinitions.Add(
                    entry.Definition
                ))
            {
                activeAugments.RemoveAt(
                    i
                );

                continue;
            }

            int clampedLevel =
                Mathf.Clamp(
                    entry.Level,
                    1,
                    entry.Definition.MaxLevel
                );

            entry.SetLevel(
                clampedLevel
            );
        }
    }
}
