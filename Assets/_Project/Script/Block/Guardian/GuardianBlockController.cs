using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
public sealed class GuardianBlockController : MonoBehaviour
{
    private readonly List<Block> targets = new List<Block>();
    private Block guardian;

    public IReadOnlyList<Block> Targets => targets;

    public event Action TargetsChanged;

    private void Awake()
    {
        guardian = GetComponent<Block>();
    }

    public void Configure(IReadOnlyList<Block> protectedTargets)
    {
        ReleaseAll();

        if (guardian == null)
        {
            guardian = GetComponent<Block>();
        }

        if (protectedTargets != null)
        {
            for (int i = 0; i < protectedTargets.Count; i++)
            {
                Block target = protectedTargets[i];

                if (target != null &&
                    target.RegisterGuardianProtection(guardian))
                {
                    targets.Add(target);
                    target.Destroyed += HandleTargetRemoved;
                    target.ExpiredWithoutReward += HandleTargetRemoved;
                }
            }
        }

        if (guardian != null)
        {
            guardian.Destroyed += HandleGuardianRemoved;
            guardian.ExpiredWithoutReward += HandleGuardianRemoved;
        }

        TargetsChanged?.Invoke();
    }

    private void OnDisable()
    {
        ReleaseAll();
    }

    private void OnDestroy()
    {
        ReleaseAll();
    }

    private void HandleGuardianRemoved(Block _)
    {
        ReleaseAll();
    }

    private void HandleTargetRemoved(Block target)
    {
        if (target == null || !targets.Remove(target))
        {
            return;
        }

        target.UnregisterGuardianProtection(guardian);
        target.Destroyed -= HandleTargetRemoved;
        target.ExpiredWithoutReward -= HandleTargetRemoved;
        TargetsChanged?.Invoke();
    }

    private void ReleaseAll()
    {
        if (guardian != null)
        {
            guardian.Destroyed -= HandleGuardianRemoved;
            guardian.ExpiredWithoutReward -= HandleGuardianRemoved;
        }

        for (int i = targets.Count - 1; i >= 0; i--)
        {
            Block target = targets[i];

            if (target == null)
            {
                continue;
            }

            target.UnregisterGuardianProtection(guardian);
            target.Destroyed -= HandleTargetRemoved;
            target.ExpiredWithoutReward -= HandleTargetRemoved;
        }

        bool hadTargets = targets.Count > 0;
        targets.Clear();

        if (hadTargets)
        {
            TargetsChanged?.Invoke();
        }
    }
}
