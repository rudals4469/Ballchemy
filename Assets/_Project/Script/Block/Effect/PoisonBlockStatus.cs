using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PoisonBlockStatus : MonoBehaviour
{
    private static readonly HashSet<PoisonBlockStatus> ActiveStatuses =
        new HashSet<PoisonBlockStatus>();

    private int stackCount;

    public int StackCount => stackCount;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        ActiveStatuses.Clear();
    }

    public void AddStacks(int amount, int maximumStacks)
    {
        if (amount <= 0)
        {
            return;
        }

        stackCount = Mathf.Clamp(
            stackCount + amount,
            0,
            Mathf.Max(maximumStacks, 1));

        ActiveStatuses.Add(this);
    }

    public static int GetBonusDamage(Block block)
    {
        if (block == null)
        {
            return 0;
        }

        PoisonBlockStatus status =
            block.GetComponent<PoisonBlockStatus>();

        return status != null
            ? Mathf.Max(status.stackCount, 0)
            : 0;
    }

    public static void ClearAllStacks()
    {
        PoisonBlockStatus[] statuses =
            new PoisonBlockStatus[ActiveStatuses.Count];

        ActiveStatuses.CopyTo(statuses);

        foreach (PoisonBlockStatus status in statuses)
        {
            if (status != null)
            {
                status.stackCount = 0;
            }
        }

        ActiveStatuses.Clear();
    }

    private void OnDisable()
    {
        stackCount = 0;
        ActiveStatuses.Remove(this);
    }
}
