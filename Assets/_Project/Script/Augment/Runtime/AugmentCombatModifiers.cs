using System.Collections.Generic;
using UnityEngine;

public static class AugmentCombatModifiers
{
    private static RunAugmentState cachedState;
    private static readonly Dictionary<int, int> hitCounts = new Dictionary<int, int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        cachedState = null;
        hitCounts.Clear();
    }

    public static int GetPoisonMaximumStackBonus() => GetValue<PoisonMaximumStackAugmentDefinition>((d, l) => d.GetBonus(l));
    public static int GetConductionTargetBonus() => GetValue<ConductionTargetAugmentDefinition>((d, l) => d.GetBonus(l));
    public static int GetOverconductionTargetBonus() => GetValue<OverconductionAugmentDefinition>((d, l) => d.GetExtraTargets(l));
    public static bool TryGetPoisonContagion(out PoisonContagionAugmentDefinition definition, out int level) => TryGet(out definition, out level);
    public static bool TryGetPoisonCollapse(out PoisonCollapseAugmentDefinition definition, out int level) => TryGet(out definition, out level);

    public static bool TryGetRule(RuleAugmentEffectKind kind, out RuleAugmentDefinition definition, out int level)
    {
        definition = null;
        level = 0;
        IReadOnlyList<AugmentRuntimeEntry> entries = GetEntries();
        if (entries == null) return false;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i]?.Definition is RuleAugmentDefinition rule &&
                rule.EffectKind == kind && entries[i].Level > 0)
            {
                definition = rule;
                level = entries[i].Level;
                return true;
            }
        }
        return false;
    }

    public static int GetRuleInteger(RuleAugmentEffectKind kind)
    {
        return TryGetRule(kind, out RuleAugmentDefinition rule, out int level)
            ? rule.GetInteger(level) : 0;
    }

    public static int GetElementMaximumStackBonus(ElementType element)
    {
        RuleAugmentEffectKind kind = element == ElementType.Water
            ? RuleAugmentEffectKind.WetMaximumBonus
            : element == ElementType.Electric ? RuleAugmentEffectKind.ChargeMaximumBonus : RuleAugmentEffectKind.None;
        return kind == RuleAugmentEffectKind.None ? 0 : GetRuleInteger(kind);
    }

    public static int GetAppliedStackBonus(Ball ball, ElementType element)
    {
        int bonus = GetRuleInteger(element == ElementType.Fire ? RuleAugmentEffectKind.FireStackBonus :
            element == ElementType.Water ? RuleAugmentEffectKind.WaterStackBonus :
            element == ElementType.Ice ? RuleAugmentEffectKind.FrostStackBonus : RuleAugmentEffectKind.None);
        if (ball != null && ball.StarGrade >= BallStarGrade.TwoStar)
            bonus += GetRuleInteger(RuleAugmentEffectKind.HighGradeElementStack);
        if (ball != null && ball.BounceCount > 0)
        {
            if (element == ElementType.Fire) bonus += GetRuleInteger(RuleAugmentEffectKind.BounceFireSynergy);
            if (element == ElementType.Ice) bonus += GetRuleInteger(RuleAugmentEffectKind.BounceIceSynergy);
        }
        return bonus;
    }

    public static int GetPoisonAppliedStackBonus(Ball ball)
    {
        int bonus = ball != null && ball.StarGrade >= BallStarGrade.TwoStar
            ? GetRuleInteger(RuleAugmentEffectKind.HighGradeElementStack) : 0;
        return bonus;
    }

    public static int ModifyDamage(Ball ball, Block target, int damage)
    {
        if (ball == null || target == null) return damage;
        int bonus = 0;
        BallTurnQueueController queue = Object.FindFirstObjectByType<BallTurnQueueController>();
        int launched = queue != null ? Mathf.Max(queue.NextLaunchIndex, 1) : 1;
        int remaining = queue != null ? queue.RemainingBallCount : 0;
        bonus += (launched / 10) * GetRuleInteger(RuleAugmentEffectKind.DamagePerLaunchedCount);
        bonus += (remaining / 10) * GetRuleInteger(RuleAugmentEffectKind.DamageByRemainingBalls);
        if (queue != null && queue.ActiveLaunchCount > 0 && launched * 2 > queue.ActiveLaunchCount)
            bonus += GetRuleInteger(RuleAugmentEffectKind.LateTurnOverdrive);
        if (ball.StarGrade == BallStarGrade.ThreeStar)
            bonus += GetRuleInteger(RuleAugmentEffectKind.HighGradeDamage);
        BallCollection collection = Object.FindFirstObjectByType<BallCollection>();
        if (collection != null && collection.Count <= 15)
            bonus += GetRuleInteger(RuleAugmentEffectKind.FewBallElite);
        BlockElementStatus elemental = target.GetComponent<BlockElementStatus>();
        PoisonBlockStatus poison = target.GetComponent<PoisonBlockStatus>();
        if (elemental != null && elemental.HasBurn)
            bonus += GetRuleInteger(RuleAugmentEffectKind.BurningTargetDamage);
        if (poison != null && poison.StackCount >= 5)
            bonus += GetRuleInteger(RuleAugmentEffectKind.PoisonedTargetDamage);
        if (poison != null && poison.StackCount >= 5 && ball.BounceCount > 0)
            bonus += GetRuleInteger(RuleAugmentEffectKind.PoisonBounceSynergy);
        if (ball.BounceCount > 0 && GetElement(ball) == ElementType.Electric)
            bonus += ball.BounceCount * GetRuleInteger(RuleAugmentEffectKind.BounceLightningSynergy);
        if (elemental != null && elemental.IsFrozen)
            bonus += GetRuleInteger(RuleAugmentEffectKind.FrozenShatterDamage);
        int id = target.GetInstanceID();
        hitCounts.TryGetValue(id, out int hits);
        hitCounts[id] = hits + 1;
        if (GetElement(ball) == ElementType.Fire)
            bonus += hits * GetRuleInteger(RuleAugmentEffectKind.RepeatedFireHit);
        int diversity = CountOwnedElements(collection);
        bonus += diversity * GetRuleInteger(RuleAugmentEffectKind.ElementDiversityDamage);
        if (queue != null && queue.PreparedQueue.Count > 0)
        {
            int index = Mathf.Clamp(queue.NextLaunchIndex - 1, 0, queue.PreparedQueue.Count - 1);
            Ball previous = index > 0 ? queue.PreparedQueue[index - 1] : null;
            if (previous != null && GetElement(previous) != GetElement(ball))
            {
                bonus += GetRuleInteger(RuleAugmentEffectKind.AlternatingElementBonus);
                bonus += GetRuleInteger(RuleAugmentEffectKind.PreviousElementInheritance);
            }
            if (index >= 2 && previous != null &&
                previous.TraitType == ball.TraitType &&
                queue.PreparedQueue[index - 2].TraitType == ball.TraitType)
                bonus += GetRuleInteger(RuleAugmentEffectKind.ConsecutiveTraitBonus);
            if (previous != null && previous.StarGrade == ball.StarGrade)
                bonus += GetRuleInteger(RuleAugmentEffectKind.ConsecutiveGradeBonus);
        }
        if (elemental != null && ball.StarGrade == BallStarGrade.ThreeStar &&
            (elemental.WetStack >= elemental.MaximumStack || elemental.ChargeStack >= elemental.MaximumStack ||
             elemental.BurnStack >= elemental.MaximumStack || elemental.FrostStack >= elemental.MaximumStack))
            bonus += GetRuleInteger(RuleAugmentEffectKind.ThreeStarOverflow);
        ElementType? ballElement = GetElement(ball);
        if (ballElement == ElementType.Fire)
            bonus += (launched / 5) * GetRuleInteger(RuleAugmentEffectKind.FireTurnRamp);
        if (ballElement == ElementType.Water && elemental != null)
            bonus += CountWetNeighbors(target) * GetRuleInteger(RuleAugmentEffectKind.WetNeighborBonus);
        if (elemental != null && elemental.HasAnyStack)
        {
            bonus += GetRuleInteger(RuleAugmentEffectKind.ReactionEcho);
            bonus += GetRuleInteger(RuleAugmentEffectKind.CriticalReaction);
        }
        if (ballElement.HasValue && (int)ballElement.Value == (launched - 1) % 4)
            bonus += GetRuleInteger(RuleAugmentEffectKind.ElementCycle);
        if (ball.StarGrade == BallStarGrade.OneStar && launched == 1)
            bonus += 3 * GetRuleInteger(RuleAugmentEffectKind.FirstLowGradeUpgrade);
        if (elemental != null && elemental.BurnStack >= elemental.MaximumStack)
            bonus += elemental.BurnStack * GetRuleInteger(RuleAugmentEffectKind.FireOverheatExplosion);
        if (ballElement == ElementType.Water && elemental != null && elemental.HasWet)
            bonus += GetRuleInteger(RuleAugmentEffectKind.RefundConsumedWet);
        if (ballElement == ElementType.Electric)
        {
            bonus += Mathf.Max(0, launched - 1) * GetRuleInteger(RuleAugmentEffectKind.ConductionDamageRamp);
            if (elemental != null && elemental.WetStack >= 3)
                bonus += GetRuleInteger(RuleAugmentEffectKind.ClosedCircuitStrike);
            bonus += GetRuleInteger(RuleAugmentEffectKind.ChainLightning);
            if (launched >= 5) bonus += GetRuleInteger(RuleAugmentEffectKind.LightningStorm);
        }
        if (elemental != null && elemental.IsFrozen)
        {
            if (ball.TraitType == BallTraitType.Basic)
                bonus += GetRuleInteger(RuleAugmentEffectKind.BasicShatterSplash);
            bonus += GetRuleInteger(RuleAugmentEffectKind.MassFreeze);
            bonus += GetRuleInteger(RuleAugmentEffectKind.IceAge);
        }
        if (poison != null && poison.StackCount > 0)
        {
            bonus += GetRuleInteger(RuleAugmentEffectKind.PlagueCollapse);
            bonus += GetRuleInteger(RuleAugmentEffectKind.PoisonCycle);
        }
        int returnThreshold = GetRuleInteger(RuleAugmentEffectKind.ReturnTrajectory);
        if (returnThreshold > 0 && ball.BounceCount >= returnThreshold) bonus += 4;
        if (ballElement.HasValue)
            bonus += GetRuleInteger(RuleAugmentEffectKind.TemporaryElementMutation);
        return Mathf.Max(1, damage + bonus);
    }

    private static int CountWetNeighbors(Block source)
    {
        BlockGridManager grid = Object.FindFirstObjectByType<BlockGridManager>();
        if (grid == null || source == null) return 0;
        List<Block> neighbors = BlockNeighborhoodResolver.FindSurroundingBlocks(source, grid.ActiveBlocks, 1);
        int count = 0;
        for (int i = 0; i < neighbors.Count; i++)
        {
            BlockElementStatus status = neighbors[i] != null ? neighbors[i].GetComponent<BlockElementStatus>() : null;
            if (status != null && status.HasWet) count++;
        }
        return count;
    }

    private static ElementType? GetElement(Ball ball)
    {
        return ball?.Definition?.TraitDefinition is ElementalBallTraitDefinition elemental
            ? elemental.ElementType : (ElementType?)null;
    }

    private static int CountOwnedElements(BallCollection collection)
    {
        if (collection == null) return 0;
        bool[] found = new bool[4];
        IReadOnlyList<Ball> balls = collection.Balls;
        for (int i = 0; i < balls.Count; i++)
        {
            ElementType? element = GetElement(balls[i]);
            if (element.HasValue) found[(int)element.Value] = true;
        }
        int count = 0;
        for (int i = 0; i < found.Length; i++) if (found[i]) count++;
        return count;
    }

    private static IReadOnlyList<AugmentRuntimeEntry> GetEntries()
    {
        if (cachedState == null) cachedState = Object.FindFirstObjectByType<RunAugmentState>(FindObjectsInactive.Include);
        return cachedState != null ? cachedState.ActiveAugments : null;
    }

    private static int GetValue<T>(System.Func<T, int, int> resolver) where T : AugmentDefinition
    {
        return TryGet(out T definition, out int level) ? resolver(definition, level) : 0;
    }

    private static bool TryGet<T>(out T definition, out int level) where T : AugmentDefinition
    {
        definition = null;
        level = 0;
        IReadOnlyList<AugmentRuntimeEntry> entries = GetEntries();
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
