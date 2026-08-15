using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class AugmentRuleEntry
{
    [SerializeField] private string augmentId;
    [SerializeField] private string displayName;
    [TextArea(2, 4)] [SerializeField] private string description;
    [SerializeField] private AugmentValueTier valueTier = AugmentValueTier.Value1;
    [SerializeField, Min(1)] private int maxLevel = 3;
    [SerializeField] private AugmentBuildTag buildTag;
    [SerializeField] private RuleAugmentEffectKind effectKind;
    [SerializeField] private ElementType elementType;
    [SerializeField] private int selectionWeight = 1;
    [SerializeField] private int[] integerValues = new int[0];
    [SerializeField] private float[] floatValues = new float[0];

    public string AugmentId => augmentId;
    public string DisplayName => displayName;
    public string Description => description;
    public AugmentValueTier ValueTier => valueTier;
    public int MaxLevel => maxLevel;
    public AugmentBuildTag BuildTag => buildTag;
    public RuleAugmentEffectKind EffectKind => effectKind;
    public ElementType ElementType => elementType;
    public int SelectionWeight => selectionWeight;
    public int[] IntegerValues => integerValues;
    public float[] FloatValues => floatValues;
}

[CreateAssetMenu(fileName = "AugmentRuleCatalog", menuName = "Ballchemy/Augments/Rule Catalog")]
public sealed class AugmentRuleCatalog : ScriptableObject
{
    [SerializeField] private List<AugmentRuleEntry> entries =
        new List<AugmentRuleEntry>();

    private readonly List<AugmentRewardDefinition> runtimeRewards =
        new List<AugmentRewardDefinition>();
    private bool initialized;

    public IReadOnlyList<AugmentRuleEntry> Entries => entries;

    public void GetRuntimeRewards(List<AugmentRewardDefinition> results)
    {
        if (results == null) return;
        EnsureInitialized();
        results.AddRange(runtimeRewards);
    }

    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;
        runtimeRewards.Clear();

        if (entries == null) return;
        for (int i = 0; i < entries.Count; i++)
        {
            AugmentRuleEntry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.AugmentId)) continue;

            RuleAugmentDefinition definition = CreateInstance<RuleAugmentDefinition>();
            definition.hideFlags = HideFlags.HideAndDontSave;
            definition.Configure(
                entry.AugmentId, entry.DisplayName, entry.Description,
                entry.ValueTier, entry.MaxLevel, entry.BuildTag,
                entry.EffectKind, entry.ElementType,
                entry.IntegerValues, entry.FloatValues);

            AugmentRewardDefinition reward = CreateInstance<AugmentRewardDefinition>();
            reward.hideFlags = HideFlags.HideAndDontSave;
            reward.ConfigureRuntime(
                $"reward_{entry.AugmentId}", entry.DisplayName,
                entry.Description, definition, entry.SelectionWeight);
            runtimeRewards.Add(reward);
        }
    }

    private void OnDisable()
    {
        initialized = false;
        runtimeRewards.Clear();
    }
}
