using UnityEngine;

public sealed class RuleAugmentDefinition : AugmentDefinition
{
    private AugmentBuildTag buildTag;
    private RuleAugmentEffectKind effectKind;
    private ElementType elementType;
    private int[] integerValues;
    private float[] floatValues;

    public AugmentBuildTag BuildTag => buildTag;
    public RuleAugmentEffectKind EffectKind => effectKind;
    public ElementType ElementType => elementType;

    public int GetInteger(int level)
    {
        if (integerValues == null || integerValues.Length == 0 || level <= 0) return 0;
        return integerValues[Mathf.Clamp(level - 1, 0, integerValues.Length - 1)];
    }

    public float GetFloat(int level)
    {
        if (floatValues == null || floatValues.Length == 0 || level <= 0) return 0f;
        return floatValues[Mathf.Clamp(level - 1, 0, floatValues.Length - 1)];
    }

    public void Configure(
        string id,
        string title,
        string body,
        AugmentValueTier tier,
        int maximumLevel,
        AugmentBuildTag tag,
        RuleAugmentEffectKind kind,
        ElementType element,
        int[] integers,
        float[] floats)
    {
        ConfigureBase(id, title, body, tier, maximumLevel);
        buildTag = tag;
        effectKind = kind;
        elementType = element;
        integerValues = integers != null ? (int[])integers.Clone() : new int[0];
        floatValues = floats != null ? (float[])floats.Clone() : new float[0];
        name = $"RuntimeAugment_{id}";
    }

    public override bool ApplyLevel(
        RunAugmentState runState,
        int previousLevel,
        int newLevel)
    {
        return runState != null && IsValidLevel(newLevel);
    }

    public override string GetLevelDescription(int level)
    {
        return IsValidLevel(level) ? Description : string.Empty;
    }
}
