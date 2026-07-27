using UnityEngine;

public static class ElementReactionResolver
{
    public static ElementReactionResult
        ApplyElementAndResolve(
            BlockElementStatus status,
            ElementType appliedElement,
            int requestedStackAmount,
            int directDamage,
            float electrocutionDamageMultiplier)
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

        int wetStackBefore =
            status.WetStack;

        int chargeStackBefore =
            status.ChargeStack;

        int resolvedWetStack =
            wetStackBefore;

        int resolvedChargeStack =
            chargeStackBefore;

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

            default:
            {
                /*
                 * 불과 얼음은 이후 단계에서 구현합니다.
                 */
                return new ElementReactionResult(
                    wetStackBefore,
                    chargeStackBefore,
                    wetStackBefore,
                    chargeStackBefore,
                    0,
                    0,
                    0,
                    0
                );
            }
        }

        int reactionCount =
            Mathf.Min(
                resolvedWetStack,
                resolvedChargeStack
            );

        resolvedWetStack -=
            reactionCount;

        resolvedChargeStack -=
            reactionCount;

        int damagePerReaction = 0;
        int totalDamage = 0;

        if (reactionCount > 0)
        {
            /*
             * 일반적인 반올림 방식으로 계산합니다.
             *
             * 직접 피해 3 × 0.5 = 1.5
             * → 감전 1회당 2 피해
             */
            damagePerReaction =
                Mathf.Max(
                    Mathf.FloorToInt(
                        directDamage *
                        electrocutionDamageMultiplier +
                        0.5f
                    ),
                    1
                );

            totalDamage =
                reactionCount *
                damagePerReaction;
        }

        /*
         * 스택 추가와 반응 소비가 모두 끝난
         * 최종 상태만 한 번에 반영합니다.
         */
        status.ApplyResolvedStacks(
            resolvedWetStack,
            resolvedChargeStack
        );

        return new ElementReactionResult(
            wetStackBefore,
            chargeStackBefore,
            resolvedWetStack,
            resolvedChargeStack,
            appliedElementStack,
            reactionCount,
            damagePerReaction,
            totalDamage
        );
    }
}