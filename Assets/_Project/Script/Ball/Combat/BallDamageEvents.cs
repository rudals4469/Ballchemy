using System;
using UnityEngine;

public static class BallDamageEvents
{
    public static event Action<BallDamageEvent>
        DamageApplied;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration
    )]
    private static void ResetStaticState()
    {
        DamageApplied = null;
    }

    public static void Publish(
        BallDamageEvent damageEvent)
    {
        if (damageEvent.Damage <= 0)
        {
            return;
        }

        DamageApplied?.Invoke(
            damageEvent
        );
    }
}