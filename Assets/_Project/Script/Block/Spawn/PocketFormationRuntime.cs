using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PocketFormationRuntime : MonoBehaviour
{
    private sealed class Group
    {
        public readonly List<Block> Tanks = new List<Block>();
        public readonly List<Block> Attackers = new List<Block>();
        public int InitialAttackerCount;
        public int RemainingAttackerCount;
    }

    private readonly Dictionary<int, Group> groups =
        new Dictionary<int, Group>();
    private readonly Dictionary<Block, Group> attackerGroups =
        new Dictionary<Block, Group>();
    private float weakeningTotal;
    private float finalCollapseDamage;

    public void Configure(
        IReadOnlyList<BlockSpawnRequest> requests,
        IReadOnlyList<Block> blocks,
        float requestedWeakeningTotal,
        float requestedFinalCollapseDamage)
    {
        ClearSubscriptions();
        weakeningTotal = Mathf.Clamp01(requestedWeakeningTotal);
        finalCollapseDamage = Mathf.Clamp01(requestedFinalCollapseDamage);
        if (requests == null || blocks == null) return;

        int count = Mathf.Min(requests.Count, blocks.Count);
        for (int i = 0; i < count; i++)
        {
            BlockSpawnRequest request = requests[i];
            Block block = blocks[i];
            if (request == null || block == null || !request.HasPocketGroup)
                continue;
            if (!groups.TryGetValue(request.PocketGroupId, out Group group))
            {
                group = new Group();
                groups.Add(request.PocketGroupId, group);
            }
            if (request.AssignedCombatRole ==
                BlockSpawnRequest.CombatRole.Tank)
                group.Tanks.Add(block);
            else if (request.AssignedCombatRole ==
                     BlockSpawnRequest.CombatRole.Attacker)
                group.Attackers.Add(block);
        }

        foreach (Group group in groups.Values)
        {
            group.InitialAttackerCount = group.Attackers.Count;
            group.RemainingAttackerCount = group.Attackers.Count;
            for (int i = 0; i < group.Attackers.Count; i++)
            {
                Block attacker = group.Attackers[i];
                attackerGroups[attacker] = group;
                attacker.Destroyed += HandleAttackerDestroyed;
            }
        }
    }

    private void HandleAttackerDestroyed(Block attacker)
    {
        if (attacker == null ||
            !attackerGroups.TryGetValue(attacker, out Group group))
            return;
        attacker.Destroyed -= HandleAttackerDestroyed;
        attackerGroups.Remove(attacker);
        group.RemainingAttackerCount = Mathf.Max(
            group.RemainingAttackerCount - 1, 0);

        float perAttacker = group.InitialAttackerCount > 0
            ? weakeningTotal / group.InitialAttackerCount
            : 0f;
        DamageTanks(group, perAttacker);
        if (group.RemainingAttackerCount == 0)
            DamageTanks(group, finalCollapseDamage);
    }

    private static void DamageTanks(Group group, float healthRatio)
    {
        if (group == null || healthRatio <= 0f) return;
        for (int i = 0; i < group.Tanks.Count; i++)
        {
            Block tank = group.Tanks[i];
            if (tank == null || !tank.IsAlive || !tank.IsBreakable) continue;
            int damage = Mathf.Max(1, Mathf.CeilToInt(
                tank.InitialMaxHealth * healthRatio));
            tank.TakeScriptedDamage(damage);
        }
    }

    private void OnDestroy() => ClearSubscriptions();

    private void ClearSubscriptions()
    {
        foreach (KeyValuePair<Block, Group> pair in attackerGroups)
        {
            if (pair.Key != null)
                pair.Key.Destroyed -= HandleAttackerDestroyed;
        }
        attackerGroups.Clear();
        groups.Clear();
    }
}
