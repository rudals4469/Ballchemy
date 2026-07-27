using UnityEngine;

public static class ElementReactionResolver
{
    public static ElementReactionResult
        ApplyElementAndResolve(
            BlockElementStatus status,
            ElementType appliedElement,
            int requestedStackAmount,
            int directDamage,
            float electrocutionDamageMultiplier,
            float thermalShockDamageMultiplier)
    {
        if (status == null)
        {
            return default;
        }

        requestedStackAmount =
            Mathf.Max(
                requestedStackAmount,
                0
            );

        directDamage =
            Mathf.Max(
                directDamage,
                1
            );

        electrocutionDamageMultiplier =
            Mathf.Max(
                electrocutionDamageMultiplier,
                0f
            );

        thermalShockDamageMultiplier =
            Mathf.Max(
                thermalShockDamageMultiplier,
                0f
            );

        int wetStackBefore =
            status.WetStack;

        int chargeStackBefore =
            status.ChargeStack;

        int burnStackBefore =
            status.BurnStack;

        int frostStackBefore =
            status.FrostStack;

        int resolvedWetStack =
            wetStackBefore;

        int resolvedChargeStack =
            chargeStackBefore;

        int resolvedBurnStack =
            burnStackBefore;

        int resolvedFrostStack =
            frostStackBefore;

        int appliedElementStack = 0;

        switch (appliedElement)
        {
            case ElementType.Water:
            {
                int previousStack =
                    resolvedWetStack;

                resolvedWetStack =
                    Mathf.Clamp(
                        resolvedWetStack +
                        requestedStackAmount,
                        0,
                        status.MaximumStack
                    );

                appliedElementStack =
                    resolvedWetStack -
                    previousStack;

                break;
            }

            case ElementType.Electric:
            {
                int previousStack =
                    resolvedChargeStack;

                resolvedChargeStack =
                    Mathf.Clamp(
                        resolvedChargeStack +
                        requestedStackAmount,
                        0,
                        status.MaximumStack
                    );

                appliedElementStack =
                    resolvedChargeStack -
                    previousStack;

                break;
            }

            case ElementType.Fire:
            {
                int previousStack =
                    resolvedBurnStack;

                resolvedBurnStack =
                    Mathf.Clamp(
                        resolvedBurnStack +
                        requestedStackAmount,
                        0,
                        status.MaximumStack
                    );

                appliedElementStack =
                    resolvedBurnStack -
                    previousStack;

                break;
            }

            case ElementType.Ice:
            {
                int previousStack =
                    resolvedFrostStack;

                resolvedFrostStack =
                    Mathf.Clamp(
                        resolvedFrostStack +
                        requestedStackAmount,
                        0,
                        status.MaximumStack
                    );

                appliedElementStack =
                    resolvedFrostStack -
                    previousStack;

                break;
            }

            default:
            {
                return new ElementReactionResult(
                    appliedElement,
                    wetStackBefore,
                    chargeStackBefore,
                    burnStackBefore,
                    frostStackBefore,
                    wetStackBefore,
                    chargeStackBefore,
                    burnStackBefore,
                    frostStackBefore,
                    0,
                    ElementReactionKind.None,
                    0,
                    0,
                    0
                );
            }
        }

        ElementReactionKind reactionKind =
            ElementReactionKind.None;

        int reactionCount = 0;
        float reactionDamageMultiplier = 0f;

        switch (appliedElement)
        {
            case ElementType.Water:
            case ElementType.Electric:
            {
                reactionCount =
                    Mathf.Min(
                        resolvedWetStack,
                        resolvedChargeStack
                    );

                if (reactionCount > 0)
                {
                    resolvedWetStack -=
                        reactionCount;

                    resolvedChargeStack -=
                        reactionCount;

                    reactionKind =
                        ElementReactionKind
                            .Electrocution;

                    reactionDamageMultiplier =
                        electrocutionDamageMultiplier;
                }

                break;
            }

            case ElementType.Fire:
            case ElementType.Ice:
            {
                reactionCount =
                    Mathf.Min(
                        resolvedBurnStack,
                        resolvedFrostStack
                    );

                if (reactionCount > 0)
                {
                    resolvedBurnStack -=
                        reactionCount;

                    resolvedFrostStack -=
                        reactionCount;

                    reactionKind =
                        ElementReactionKind
                            .ThermalShock;

                    reactionDamageMultiplier =
                        thermalShockDamageMultiplier;
                }

                break;
            }
        }

        int damagePerReaction = 0;
        int totalDamage = 0;

        if (reactionCount > 0)
        {
            damagePerReaction =
                Mathf.Max(
                    Mathf.FloorToInt(
                        directDamage *
                        reactionDamageMultiplier +
                        0.5f
                    ),
                    1
                );

            totalDamage =
                reactionCount *
                damagePerReaction;
        }

        status.ApplyResolvedStacks(
            resolvedWetStack,
            resolvedChargeStack,
            resolvedBurnStack,
            resolvedFrostStack
        );

        return new ElementReactionResult(
            appliedElement,
            wetStackBefore,
            chargeStackBefore,
            burnStackBefore,
            frostStackBefore,
            resolvedWetStack,
            resolvedChargeStack,
            resolvedBurnStack,
            resolvedFrostStack,
            appliedElementStack,
            reactionKind,
            reactionCount,
            damagePerReaction,
            totalDamage
        );
    }
}