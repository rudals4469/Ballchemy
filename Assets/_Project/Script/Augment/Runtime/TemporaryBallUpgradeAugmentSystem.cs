using System.Collections.Generic;
using UnityEngine;

public readonly struct
    TemporaryBallUpgradeResult
{
    public static TemporaryBallUpgradeResult
        NotActivated =>
            new TemporaryBallUpgradeResult(
                false,
                null,
                null,
                0,
                0,
                false
            );

    public bool WasActivated
    {
        get;
    }

    public BallDefinition OriginalDefinition
    {
        get;
    }

    public BallDefinition TemporaryDefinition
    {
        get;
    }

    public int AppliedUpgradeStepCount
    {
        get;
    }

    public int MaximumGradeDamageBonus
    {
        get;
    }

    public bool UsedMaximumGradeBonus
    {
        get;
    }

    public bool HasTemporaryDefinition =>
        WasActivated &&
        TemporaryDefinition != null &&
        TemporaryDefinition != OriginalDefinition;

    public TemporaryBallUpgradeResult(
        bool wasActivated,
        BallDefinition originalDefinition,
        BallDefinition temporaryDefinition,
        int appliedUpgradeStepCount,
        int maximumGradeDamageBonus,
        bool usedMaximumGradeBonus)
    {
        WasActivated =
            wasActivated;

        OriginalDefinition =
            originalDefinition;

        TemporaryDefinition =
            temporaryDefinition;

        AppliedUpgradeStepCount =
            Mathf.Max(
                appliedUpgradeStepCount,
                0
            );

        MaximumGradeDamageBonus =
            Mathf.Max(
                maximumGradeDamageBonus,
                0
            );

        UsedMaximumGradeBonus =
            usedMaximumGradeBonus;
    }
}

public static class
    TemporaryBallUpgradeAugmentSystem
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

    public static TemporaryBallUpgradeResult
        RollForBall(
            Ball ball)
    {
        if (ball == null ||
            ball.Definition == null)
        {
            return
                TemporaryBallUpgradeResult
                    .NotActivated;
        }

        if (!TryFindActiveAugment(
                out TemporaryBallUpgradeAugmentDefinition
                    definition,
                out int level
            ))
        {
            return
                TemporaryBallUpgradeResult
                    .NotActivated;
        }

        float activationChance =
            definition.GetActivationChance(
                level
            );

        if (activationChance <= 0f ||
            Random.value > activationChance)
        {
            return
                TemporaryBallUpgradeResult
                    .NotActivated;
        }

        BallDefinition originalDefinition =
            ball.Definition;

        int requestedUpgradeSteps =
            definition.GetUpgradeStepCount(
                level
            );

        BallDefinition temporaryDefinition =
            ResolveTemporaryDefinition(
                originalDefinition,
                requestedUpgradeSteps,
                out int appliedUpgradeSteps
            );

        /*
         * 한 단계도 승급할 수 없었다면 현재 공은
         * 최종 등급으로 판단하고 직접 피해 보너스를 줍니다.
         */
        bool usedMaximumGradeBonus =
            appliedUpgradeSteps <= 0;

        int maximumGradeDamageBonus =
            usedMaximumGradeBonus
                ? definition
                    .MaximumGradeDamageBonus
                : 0;

        return new TemporaryBallUpgradeResult(
            true,
            originalDefinition,
            temporaryDefinition,
            appliedUpgradeSteps,
            maximumGradeDamageBonus,
            usedMaximumGradeBonus
        );
    }

    private static BallDefinition
        ResolveTemporaryDefinition(
            BallDefinition originalDefinition,
            int requestedUpgradeSteps,
            out int appliedUpgradeSteps)
    {
        appliedUpgradeSteps = 0;

        if (originalDefinition == null ||
            requestedUpgradeSteps <= 0)
        {
            return originalDefinition;
        }

        BallDefinition currentDefinition =
            originalDefinition;

        for (int i = 0;
             i < requestedUpgradeSteps;
             i++)
        {
            if (currentDefinition == null ||
                !currentDefinition.CanUpgrade ||
                currentDefinition.NextStarDefinition ==
                    null)
            {
                break;
            }

            currentDefinition =
                currentDefinition
                    .NextStarDefinition;

            appliedUpgradeSteps++;
        }

        return currentDefinition;
    }

    private static bool TryFindActiveAugment(
        out TemporaryBallUpgradeAugmentDefinition
            augmentDefinition,
        out int augmentLevel)
    {
        augmentDefinition = null;
        augmentLevel = 0;

        RunAugmentState runAugmentState =
            FindRunAugmentState();

        if (runAugmentState == null)
        {
            return false;
        }

        IReadOnlyList<AugmentRuntimeEntry>
            activeAugments =
                runAugmentState.ActiveAugments;

        if (activeAugments == null)
        {
            return false;
        }

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

            TemporaryBallUpgradeAugmentDefinition
                candidate =
                    entry.Definition as
                        TemporaryBallUpgradeAugmentDefinition;

            if (candidate == null)
            {
                continue;
            }

            augmentDefinition =
                candidate;

            augmentLevel =
                entry.Level;

            return true;
        }

        return false;
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