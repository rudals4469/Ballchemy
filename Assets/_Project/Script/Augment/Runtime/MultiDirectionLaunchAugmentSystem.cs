using System.Collections.Generic;
using UnityEngine;

public readonly struct
    MultiDirectionLaunchSettings
{
    public static MultiDirectionLaunchSettings
        Default =>
            new MultiDirectionLaunchSettings(
                false,
                1,
                0f
            );

    public bool IsActive
    {
        get;
    }

    public int BranchCount
    {
        get;
    }

    public float SpreadAngle
    {
        get;
    }

    public MultiDirectionLaunchSettings(
        bool isActive,
        int branchCount,
        float spreadAngle)
    {
        IsActive =
            isActive;

        BranchCount =
            Mathf.Max(
                branchCount,
                1
            );

        SpreadAngle =
            Mathf.Clamp(
                spreadAngle,
                0f,
                45f
            );
    }
}

public static class
    MultiDirectionLaunchAugmentSystem
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

    public static MultiDirectionLaunchSettings
        GetCurrentSettings()
    {
        if (!TryFindActiveAugment(
                out MultiDirectionLaunchAugmentDefinition
                    definition,
                out int level
            ))
        {
            return
                MultiDirectionLaunchSettings
                    .Default;
        }

        int branchCount =
            definition.GetBranchCount(
                level
            );

        float spreadAngle =
            definition.GetSpreadAngle(
                level
            );

        return new MultiDirectionLaunchSettings(
            branchCount > 1,
            branchCount,
            spreadAngle
        );
    }

    public static Vector2 GetBranchDirection(
        Vector2 baseDirection,
        int branchIndex,
        int branchCount,
        float spreadAngle)
    {
        if (baseDirection.sqrMagnitude <=
            0.001f)
        {
            baseDirection =
                Vector2.up;
        }

        baseDirection =
            baseDirection.normalized;

        branchCount =
            Mathf.Max(
                branchCount,
                1
            );

        branchIndex =
            Mathf.Clamp(
                branchIndex,
                0,
                branchCount - 1
            );

        if (branchCount <= 1 ||
            spreadAngle <= 0f)
        {
            return baseDirection;
        }

        float rotationAngle =
            ResolveBranchAngle(
                branchIndex,
                branchCount,
                spreadAngle
            );

        return RotateDirection(
            baseDirection,
            rotationAngle
        );
    }

    public static Vector2 GetSteeredBaseDirection(
        Vector2 requestedDirection,
        MultiDirectionLaunchSettings settings,
        float minimumUpwardComponent)
    {
        if (requestedDirection.sqrMagnitude <= 0.001f)
        {
            requestedDirection = Vector2.up;
        }

        requestedDirection.Normalize();

        int branchCount = settings.IsActive
            ? Mathf.Max(settings.BranchCount, 1)
            : 1;

        float outerSpread = branchCount > 1
            ? Mathf.Clamp(settings.SpreadAngle, 0f, 45f)
            : 0f;

        float minimumY = Mathf.Clamp(
            minimumUpwardComponent,
            0.01f,
            1f);

        float maximumAngleFromUp =
            Mathf.Acos(minimumY) * Mathf.Rad2Deg;

        float maximumCenterAngle = Mathf.Max(
            maximumAngleFromUp - outerSpread,
            0f);

        float requestedAngleFromUp = Mathf.Atan2(
            requestedDirection.x,
            requestedDirection.y) * Mathf.Rad2Deg;

        float steeredAngle = Mathf.Clamp(
            requestedAngleFromUp,
            -maximumCenterAngle,
            maximumCenterAngle);

        float radians = steeredAngle * Mathf.Deg2Rad;

        return new Vector2(
            Mathf.Sin(radians),
            Mathf.Cos(radians));
    }

    private static float ResolveBranchAngle(
        int branchIndex,
        int branchCount,
        float spreadAngle)
    {
        if (branchCount == 2)
        {
            return branchIndex == 0
                ? -spreadAngle
                : spreadAngle;
        }

        if (branchCount == 3)
        {
            switch (branchIndex)
            {
                case 0:
                    return -spreadAngle;

                case 1:
                    return 0f;

                case 2:
                default:
                    return spreadAngle;
            }
        }

        /*
         * 현재 기획은 최대 3갈래지만,
         * 데이터가 바뀌어도 균등한 부채꼴이 되도록
         * 일반 계산식을 남겨둡니다.
         */
        float interpolation =
            branchCount > 1
                ? branchIndex /
                  (float)(branchCount - 1)
                : 0.5f;

        return Mathf.Lerp(
            -spreadAngle,
            spreadAngle,
            interpolation
        );
    }

    private static Vector2 RotateDirection(
        Vector2 direction,
        float angleDegrees)
    {
        float radians =
            angleDegrees *
            Mathf.Deg2Rad;

        float cosine =
            Mathf.Cos(
                radians
            );

        float sine =
            Mathf.Sin(
                radians
            );

        Vector2 rotatedDirection =
            new Vector2(
                direction.x * cosine -
                direction.y * sine,
                direction.x * sine +
                direction.y * cosine
            );

        if (rotatedDirection.sqrMagnitude <=
            0.001f)
        {
            return direction.normalized;
        }

        return rotatedDirection.normalized;
    }

    private static bool TryFindActiveAugment(
        out MultiDirectionLaunchAugmentDefinition
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
                entry.Definition == null ||
                entry.Level <= 0)
            {
                continue;
            }

            MultiDirectionLaunchAugmentDefinition
                candidate =
                    entry.Definition as
                        MultiDirectionLaunchAugmentDefinition;

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
