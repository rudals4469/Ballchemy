using System.Collections.Generic;
using UnityEngine;

public static class AugmentCombatModifiers
{
    private static RunAugmentState cachedState;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => cachedState = null;

    public static int GetPoisonMaximumStackBonus() => GetValue<PoisonMaximumStackAugmentDefinition>((d, l) => d.GetBonus(l));
    public static int GetConductionTargetBonus() => GetValue<ConductionTargetAugmentDefinition>((d, l) => d.GetBonus(l));
    public static int GetOverconductionTargetBonus() => GetValue<OverconductionAugmentDefinition>((d, l) => d.GetExtraTargets(l));
    public static bool TryGetPoisonContagion(out PoisonContagionAugmentDefinition definition, out int level) => TryGet(out definition, out level);
    public static bool TryGetPoisonCollapse(out PoisonCollapseAugmentDefinition definition, out int level) => TryGet(out definition, out level);

    private static int GetValue<T>(System.Func<T, int, int> resolver) where T : AugmentDefinition
    {
        return TryGet(out T definition, out int level) ? resolver(definition, level) : 0;
    }

    private static bool TryGet<T>(out T definition, out int level) where T : AugmentDefinition
    {
        definition = null;
        level = 0;
        if (cachedState == null) cachedState = Object.FindFirstObjectByType<RunAugmentState>(FindObjectsInactive.Include);
        IReadOnlyList<AugmentRuntimeEntry> entries = cachedState != null ? cachedState.ActiveAugments : null;
        if (entries == null) return false;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i]?.Definition is T match && entries[i].Level > 0)
            {
                definition = match;
                level = entries[i].Level;
                return true;
            }
        }
        return false;
    }
}
