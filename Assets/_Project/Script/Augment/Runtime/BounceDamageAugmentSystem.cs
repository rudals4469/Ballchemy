using System.Collections.Generic;
using UnityEngine;

public static class BounceDamageAugmentSystem
{
    private static RunAugmentState
        cachedRunAugmentState;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration
    )]
    private static void ResetStaticState()
    {
        cachedRunAugmentState =
            null;
    }

    public static int CalculateDamageBonus(
        int bounceCount)
    {
        if (bounceCount <= 0)
        {
            return 0;
        }

        RunAugmentState runAugmentState =
            FindRunAugmentState();

        if (runAugmentState == null)
        {
            return 0;
        }

        IReadOnlyList<AugmentRuntimeEntry>
            activeAugments =
                runAugmentState.ActiveAugments;

        if (activeAugments == null)
        {
            return 0;
        }

        /*
         * 같은 종류의 증강 Definition은
         * 한 런에 하나만 존재하는 것을 기준으로 합니다.
         */
        for (int i = 0;
             i < activeAugments.Count;
             i++)
        {
            AugmentRuntimeEntry entry =
                activeAugments[i];

            if (entry == null ||
                entry.Level <= 0)
            {
                continue;
            }

            BounceDamageAugmentDefinition
                definition =
                    entry.Definition as
                        BounceDamageAugmentDefinition;

            if (definition == null)
            {
                continue;
            }

            return definition.CalculateDamageBonus(
                entry.Level,
                bounceCount
            );
        }

        return 0;
    }

    private static RunAugmentState
        FindRunAugmentState()
    {
        if (cachedRunAugmentState != null)
        {
            return cachedRunAugmentState;
        }

        cachedRunAugmentState =
            Object.FindFirstObjectByType<
                RunAugmentState
            >(
                FindObjectsInactive.Include
            );

        return cachedRunAugmentState;
    }
}