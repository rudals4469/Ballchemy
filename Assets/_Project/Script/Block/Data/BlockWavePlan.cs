using UnityEngine;

public sealed class BlockWavePlan
{
    public int WaveIndex { get; }

    public int WaveNumber =>
        WaveIndex + 1;

    public int RequiredRowCount { get; }

    public bool IsNamedWave { get; }

    public BlockDefinition FeaturedDefinition
    {
        get;
    }

    public bool HasFeaturedBlock =>
        FeaturedDefinition != null;

    public BlockWavePlan(
        int waveIndex,
        int requiredRowCount,
        bool isNamedWave,
        BlockDefinition featuredDefinition)
    {
        WaveIndex =
            Mathf.Max(
                waveIndex,
                0
            );

        RequiredRowCount =
            Mathf.Max(
                requiredRowCount,
                1
            );

        IsNamedWave =
            isNamedWave;

        FeaturedDefinition =
            featuredDefinition;
    }
}