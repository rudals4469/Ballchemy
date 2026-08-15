using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class AugmentTierWeights
{
    [SerializeField, Min(0)] private int value1Weight = 1;
    [SerializeField, Min(0)] private int value2Weight = 1;
    [SerializeField, Min(0)] private int value3Weight = 1;

    public int GetWeight(AugmentValueTier tier)
    {
        switch (tier)
        {
            case AugmentValueTier.Value1: return value1Weight;
            case AugmentValueTier.Value2: return value2Weight;
            case AugmentValueTier.Value3: return value3Weight;
            default: return 0;
        }
    }
}

[Serializable]
public sealed class AugmentRewardSourceSettings
{
    [SerializeField] private AugmentRewardSource source;
    [SerializeField] private AugmentTierWeights tierWeights =
        new AugmentTierWeights();
    [SerializeField, Min(1)] private int minimumStage = 1;

    public AugmentRewardSource Source => source;
    public AugmentTierWeights TierWeights => tierWeights;
    public int MinimumStage => minimumStage;
}

[CreateAssetMenu(
    fileName = "AugmentRewardSettings",
    menuName = "Ballchemy/Augments/Reward Settings")]
public sealed class AugmentRewardSettings : ScriptableObject
{
    [SerializeField]
    private List<AugmentRewardSourceSettings> sources =
        new List<AugmentRewardSourceSettings>();

    public bool TryGet(
        AugmentRewardSource source,
        out AugmentRewardSourceSettings settings)
    {
        if (sources != null)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i] != null && sources[i].Source == source)
                {
                    settings = sources[i];
                    return true;
                }
            }
        }

        settings = null;
        return false;
    }
}
