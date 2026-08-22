using System.Collections.Generic;
using UnityEngine;

public static class AugmentCombatModifiers
{
    private static RunAugmentState cachedState;
    private static readonly Dictionary<int, int> hitCounts = new Dictionary<int, int>();
    private static readonly HashSet<int> shockwaveTriggeredBalls = new HashSet<int>();
    private static readonly HashSet<int> dismantledBlocks = new HashSet<int>();
    private static readonly HashSet<int> splitTriggeredBalls = new HashSet<int>();
    private static readonly Dictionary<int, int> lastHitBlockByBall = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> differentBlockChains = new Dictionary<int, int>();
    private static int launchedElementMask;
    private static bool alchemyChainReady;
    private static int fireHitCount;
    private static int waterHitCount;
    private static int poisonCollapseCount;
    private static int electrocutionCount;
    private static bool iceAgeActive;
    private static bool floodTriggered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        cachedState = null;
        hitCounts.Clear();
        shockwaveTriggeredBalls.Clear();
        dismantledBlocks.Clear();
        splitTriggeredBalls.Clear();
        lastHitBlockByBall.Clear();
        differentBlockChains.Clear();
        launchedElementMask = 0;
        alchemyChainReady = false;
        fireHitCount = poisonCollapseCount = electrocutionCount = 0;
        waterHitCount = 0;
        iceAgeActive = false;
        floodTriggered = false;
    }

    public static void BeginTurn()
    {
        hitCounts.Clear();
        shockwaveTriggeredBalls.Clear();
        dismantledBlocks.Clear();
        splitTriggeredBalls.Clear();
        lastHitBlockByBall.Clear();
        differentBlockChains.Clear();
        launchedElementMask = 0;
        alchemyChainReady = false;
        fireHitCount = poisonCollapseCount = electrocutionCount = 0;
        waterHitCount = 0;
        iceAgeActive = false;
        floodTriggered = false;
    }

    public static void NotifyPoisonCollapse() => poisonCollapseCount++;
    public static int GetPoisonCycleStackBonus() =>
        GetRuleInteger(RuleAugmentEffectKind.PoisonCycle) > 0
            ? Mathf.Min(poisonCollapseCount, 2) : 0;
    public static void NotifyFrozenApplied() => iceAgeActive = true;
    public static void NotifyElectrocution() => electrocutionCount++;
    public static int GetElectrocutionCount() => electrocutionCount;
    public static int GetWetMaximumReduction() =>
        GetRuleInteger(RuleAugmentEffectKind.WetMaximumReduction);
    public static int GetHighHeatBurnDamagePercent() =>
        TryGetRule(RuleAugmentEffectKind.FireStackBonus, out _, out int level) &&
        level >= 2 ? 10 : 0;
    public static int GetPoisonCollapseBonus() =>
        TryGetRule(RuleAugmentEffectKind.HighGradeElementStack, out _, out int level) &&
        level >= 2 ? 5 : 0;

    public static int ApplyPercentage(int value, int percent) =>
        Mathf.Max(0, Mathf.RoundToInt(value * (1f + percent / 100f)));

    public static void NotifyBallLaunched(Ball ball)
    {
        ElementType? element = GetElement(ball);
        if (!element.HasValue || !TryGetRule(
                RuleAugmentEffectKind.AlchemyChain, out _, out _))
            return;

        launchedElementMask |= 1 << (int)element.Value;
        int distinctElements = 0;
        int mask = launchedElementMask;
        while (mask != 0)
        {
            distinctElements += mask & 1;
            mask >>= 1;
        }

        if (distinctElements >= 3)
            alchemyChainReady = true;
    }

    public static bool TryConsumeAlchemyChain(out int damageBonus)
    {
        damageBonus = 0;
        if (!alchemyChainReady) return false;
        damageBonus = GetRuleInteger(RuleAugmentEffectKind.AlchemyChain);
        if (damageBonus <= 0) return false;
        alchemyChainReady = false;
        launchedElementMask = 0;
        return true;
    }

    public static void MarkResidualCharge(Block block)
    {
        if (block == null || GetRuleInteger(RuleAugmentEffectKind.ResidualCharge) <= 0)
            return;
        ResidualChargeStatus status = block.GetComponent<ResidualChargeStatus>();
        if (status == null) status = block.gameObject.AddComponent<ResidualChargeStatus>();
        status.Mark();
    }

    public static void TryResolveShockTrajectory(
        Ball ball, Block center, BallCombatController combatController)
    {
        int percent = GetRuleInteger(RuleAugmentEffectKind.ShockTrajectory);
        if (percent <= 0 || ball == null || center == null || combatController == null ||
            ball.BounceCount < 6 || !shockwaveTriggeredBalls.Add(ball.GetInstanceID()))
            return;

        BlockGridManager grid = Object.FindFirstObjectByType<BlockGridManager>();
        if (grid == null) return;
        List<Block> targets = BlockNeighborhoodResolver.FindSurroundingBlocks(
            center, grid.ActiveBlocks, 1);
        if (center.IsAlive) targets.Insert(0, center);
        for (int i = 0; i < targets.Count; i++)
        {
            Block target = targets[i];
            if (target != null && target.IsAlive && target.IsBreakable)
                combatController.ApplyDamage(target,
                    Mathf.Max(1, Mathf.RoundToInt(ball.CurrentDamage * percent / 100f)),
                    target.transform.position);
        }
        ElementVisualEvents.RaiseThermalShock(center);
    }

    public static void TryResolveBallisticSplit(
        Ball ball, Vector2 position, Vector2 incomingVelocity)
    {
        int threshold = GetRuleInteger(RuleAugmentEffectKind.BallisticSplit);
        if (threshold <= 0 || ball == null || ball.BounceCount < threshold ||
            ball.GetComponent<TemporaryAugmentBall>() != null ||
            !splitTriggeredBalls.Add(ball.GetInstanceID())) return;
        BallLauncher launcher = Object.FindFirstObjectByType<BallLauncher>();
        if (launcher == null) return;
        Vector2 baseDirection = incomingVelocity.sqrMagnitude > 0.001f
            ? -incomingVelocity.normalized : Vector2.up;
        launcher.SpawnAugmentClone(ball, position,
            Quaternion.Euler(0f, 0f, -24f) * baseDirection, 50);
        launcher.SpawnAugmentClone(ball, position,
            Quaternion.Euler(0f, 0f, 24f) * baseDirection, 50);
    }

    public static void ResolveBasicHitEffects(
        Ball ball, Block center, BallCombatController combatController,
        bool wasDestroyed)
    {
        if (ball == null || center == null || combatController == null ||
            ball.TraitType != BallTraitType.Basic) return;

        BlockGridManager grid = Object.FindFirstObjectByType<BlockGridManager>();
        if (wasDestroyed)
        {
            int splashPercent = GetRuleInteger(
                RuleAugmentEffectKind.BasicDestroySplash);
            if (splashPercent > 0 && grid != null)
                DamageNeighbors(center, grid, combatController,
                    Mathf.Max(1, Mathf.RoundToInt(
                        ball.CurrentDamage * splashPercent / 100f)), 1);
            return;
        }

        if (GetRuleInteger(RuleAugmentEffectKind.AlchemyDismantle) > 0 &&
            TryConsumeMaximumState(center) &&
            dismantledBlocks.Add(center.GetInstanceID()))
        {
            int centerDamage = Mathf.Max(1, ball.CurrentDamage * 5);
            combatController.ApplyDamage(center, centerDamage, center.transform.position);
            if (grid != null)
                DamageNeighbors(center, grid, combatController,
                    Mathf.Max(1, ball.CurrentDamage * 3), 1);
            return;
        }

        ApplyBasicCatalyst(center);
    }

    private static void DamageNeighbors(Block center, BlockGridManager grid,
        BallCombatController combatController, int damage, int radius)
    {
        List<Block> targets = BlockNeighborhoodResolver.FindSurroundingBlocks(
            center, grid.ActiveBlocks, radius);
        for (int i = 0; i < targets.Count; i++)
        {
            Block target = targets[i];
            if (target != null && target.IsAlive && target.IsBreakable)
                combatController.ApplyDamage(target, damage, target.transform.position);
        }
    }

    private static void ApplyBasicCatalyst(Block block)
    {
        if (GetRuleInteger(RuleAugmentEffectKind.BasicCatalyst) < 0 || block == null)
            return;
        if (!TryGetRule(RuleAugmentEffectKind.BasicCatalyst, out _, out _)) return;

        PoisonBlockStatus poison = block.GetComponent<PoisonBlockStatus>();
        BlockElementStatus element = block.GetComponent<BlockElementStatus>();
        int poisonStack = poison != null ? poison.StackCount : 0;
        int wet = element != null ? element.WetStack : 0;
        int burn = element != null ? element.BurnStack : 0;
        int frost = element != null ? element.StoredFrostStack : 0;
        int maximum = Mathf.Max(poisonStack, Mathf.Max(wet, Mathf.Max(burn, frost)));
        if (maximum <= 0) return;
        if (poisonStack == maximum) poison.AddStacks(1, 5);
        else if (wet == maximum) element.AddWet(1);
        else if (burn == maximum) element.AddBurn(1);
        else element.AddFrost(1);
    }

    private static bool TryConsumeMaximumState(Block block)
    {
        if (block == null) return false;
        PoisonBlockStatus poison = block.GetComponent<PoisonBlockStatus>();
        if (poison != null && poison.StackCount >= 5)
        {
            poison.ClearStacks();
            return true;
        }
        BlockElementStatus status = block.GetComponent<BlockElementStatus>();
        if (status == null) return false;
        if (status.IsFrozen) return status.ConsumeFrozen();
        if (status.WetStack >= status.GetMaximumStack(ElementType.Water))
            return status.ConsumeWet(status.WetStack) > 0;
        if (status.BurnStack >= status.GetMaximumStack(ElementType.Fire))
            return status.ConsumeBurn(status.BurnStack) > 0;
        if (status.StoredFrostStack >= status.GetMaximumStack(ElementType.Ice))
            return status.ConsumeFrost(status.StoredFrostStack) > 0;
        return false;
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
            element == ElementType.Ice ? RuleAugmentEffectKind.FrostStackBonus : RuleAugmentEffectKind.None);
        if (element == ElementType.Ice && iceAgeActive) bonus += 1;
        if (element == ElementType.Fire)
            bonus += Mathf.Min(2, fireHitCount / 5);
        return bonus;
    }

    public static bool NotifyWaterHitAndShouldFlood()
    {
        waterHitCount++;
        int threshold = GetRuleInteger(RuleAugmentEffectKind.GlobalWetOnThreshold);
        if (floodTriggered || threshold <= 0 || waterHitCount < threshold) return false;
        floodTriggered = true;
        return true;
    }

    public static int GetPoisonAppliedStackBonus(Ball ball)
    {
        return GetRuleInteger(RuleAugmentEffectKind.HighGradeElementStack) +
            GetPoisonCycleStackBonus();
    }

    public static int ModifyDamage(
        Ball ball, Block target, int damage, bool isPrimaryDirectHit = true)
    {
        if (ball == null || target == null) return damage;

        int percent = 0;
        int fixedBonus = 0;
        BallTurnQueueController queue =
            Object.FindFirstObjectByType<BallTurnQueueController>();
        int launched = queue != null ? Mathf.Max(queue.NextLaunchIndex, 1) : 1;
        int remaining = queue != null ? queue.RemainingBallCount : 0;

        percent += Mathf.Min(45, launched / 10 *
            GetRuleInteger(RuleAugmentEffectKind.DamagePerLaunchedCount));
        percent += Mathf.Min(45, remaining / 10 *
            GetRuleInteger(RuleAugmentEffectKind.DamageByRemainingBalls));
        if (queue != null && queue.ActiveLaunchCount > 0 &&
            launched * 2 > queue.ActiveLaunchCount)
            percent += GetRuleInteger(RuleAugmentEffectKind.LateTurnOverdrive);

        BallStarGrade grade = GetEffectiveStarGrade(ball);
        if (grade == BallStarGrade.ThreeStar)
        {
            percent += GetRuleInteger(RuleAugmentEffectKind.HighGradeDamage);
            percent += GetRuleInteger(RuleAugmentEffectKind.GradeTranscendence);
        }
        if (grade >= BallStarGrade.TwoStar)
            percent += GetRuleInteger(RuleAugmentEffectKind.RefinedTraitDamage);

        BlockElementStatus elemental = target.GetComponent<BlockElementStatus>();
        PoisonBlockStatus poison = target.GetComponent<PoisonBlockStatus>();
        ElementType? element = GetElement(ball);
        bool isBasic = ball.TraitType == BallTraitType.Basic;

        if (isBasic) percent += GetRuleInteger(RuleAugmentEffectKind.BasicDamage);
        if (isBasic && (elemental == null || !elemental.HasAnyStack) &&
            (poison == null || poison.StackCount <= 0))
            percent += GetRuleInteger(RuleAugmentEffectKind.EmptyTargetDamage);
        if (elemental != null && elemental.HasBurn)
            percent += GetRuleInteger(RuleAugmentEffectKind.BurningTargetDamage);
        if (element == ElementType.Water && elemental != null && elemental.HasWet)
            percent += GetRuleInteger(RuleAugmentEffectKind.WaterCohesion);
        if (isBasic && elemental != null && elemental.HasAnyStack)
            percent += GetRuleInteger(RuleAugmentEffectKind.BasicCatalyst);
        if (elemental != null && elemental.IsFrozen)
        {
            percent += GetRuleInteger(RuleAugmentEffectKind.FrozenShatterDamage);
            if (TryGetRule(RuleAugmentEffectKind.FrostStackBonus,
                    out _, out int severeColdLevel) && severeColdLevel >= 2)
                percent += 20;
        }
        if (poison != null && poison.StackCount >= 3)
            fixedBonus += GetRuleInteger(RuleAugmentEffectKind.PoisonedTargetDamage);
        if (ball.TraitType == BallTraitType.Poison && poison != null &&
            poison.StackCount > 0 && ball.BounceCount > 0)
            percent += GetRuleInteger(RuleAugmentEffectKind.PoisonBounceSynergy);

        if (ball.BounceCount > 0)
            percent += GetRuleInteger(RuleAugmentEffectKind.BouncedTraitDamage);
        if (TryGetRule(RuleAugmentEffectKind.KineticBounce,
                out RuleAugmentDefinition kinetic, out int kineticLevel))
        {
            int threshold = Mathf.Max(kinetic.GetInteger(kineticLevel), 1);
            int maximum = kineticLevel == 1 ? 30 : kineticLevel == 2 ? 50 : 70;
            percent += Mathf.Min(maximum, ball.BounceCount / threshold * 10);
        }

        int ballId = ball.GetInstanceID();
        int targetId = target.GetInstanceID();
        if (isPrimaryDirectHit &&
            lastHitBlockByBall.TryGetValue(ballId, out int previousTarget))
        {
            if (previousTarget == targetId)
            {
                percent += GetRuleInteger(RuleAugmentEffectKind.SameBlockRehit);
                differentBlockChains[ballId] = 0;
            }
            else
            {
                differentBlockChains.TryGetValue(ballId, out int chain);
                chain++;
                differentBlockChains[ballId] = chain;
                int step = GetRuleInteger(RuleAugmentEffectKind.DifferentBlockChain);
                percent += Mathf.Min(step * 5, chain * step);
            }
        }
        if (isPrimaryDirectHit) lastHitBlockByBall[ballId] = targetId;

        int hitKey = (ballId * 397) ^ targetId;
        hitCounts.TryGetValue(hitKey, out int hits);
        if (isPrimaryDirectHit) hitCounts[hitKey] = hits + 1;
        if (element == ElementType.Fire)
        {
            int furnace = GetRuleInteger(RuleAugmentEffectKind.RepeatedFireHit);
            percent += Mathf.Min(furnace * 5, hits * furnace);
            if (isPrimaryDirectHit) fireHitCount++;
            percent += Mathf.Min(45, fireHitCount / 5 *
                GetRuleInteger(RuleAugmentEffectKind.FireTurnRamp));
        }

        if (queue != null && queue.PreparedQueue.Count > 0)
        {
            int index = Mathf.Clamp(queue.NextLaunchIndex - 1,
                0, queue.PreparedQueue.Count - 1);
            Ball previous = index > 0 ? queue.PreparedQueue[index - 1] : null;
            if (previous != null && previous.TraitType != ball.TraitType)
                percent += GetRuleInteger(RuleAugmentEffectKind.AlternatingElementBonus);
            if (index >= 2 && previous != null &&
                previous.TraitType == ball.TraitType &&
                queue.PreparedQueue[index - 2].TraitType == ball.TraitType)
                percent += GetRuleInteger(RuleAugmentEffectKind.ConsecutiveTraitBonus);
            if (previous != null && previous.StarGrade == ball.StarGrade)
                percent += GetRuleInteger(RuleAugmentEffectKind.ConsecutiveGradeBonus);

            if (isBasic)
            {
                int consecutive = 0;
                for (int i = index - 1; i >= 0 &&
                    queue.PreparedQueue[i] != null &&
                    queue.PreparedQueue[i].TraitType == BallTraitType.Basic; i--)
                    consecutive++;
                percent += Mathf.Min(50, consecutive *
                    GetRuleInteger(RuleAugmentEffectKind.ConsecutiveBasic));
            }
        }

        if (elemental != null && grade == BallStarGrade.ThreeStar &&
            IsAtMaximumState(elemental))
            percent += GetRuleInteger(RuleAugmentEffectKind.ThreeStarOverflow);
        if (element == ElementType.Water && elemental != null)
        {
            int step = GetRuleInteger(RuleAugmentEffectKind.WetNeighborBonus);
            percent += Mathf.Min(step * 4, CountWetNeighbors(target) * step);
        }
        if (element == ElementType.Electric)
            percent += GetRuleInteger(RuleAugmentEffectKind.ElectricDamageBonus) +
                (electrocutionCount >= 5 ? 50 : 0);
        if (element == ElementType.Ice && iceAgeActive)
            percent += GetRuleInteger(RuleAugmentEffectKind.IceAge);

        return Mathf.Max(1, ApplyPercentage(damage + fixedBonus, percent));
    }

    public static BallStarGrade GetEffectiveStarGrade(Ball ball)
    {
        BallStarGrade grade = ball != null ? ball.StarGrade : BallStarGrade.None;
        if (GetRuleInteger(RuleAugmentEffectKind.FirstLowGradeUpgrade) <= 0)
            return grade;
        if (grade == BallStarGrade.OneStar) return BallStarGrade.TwoStar;
        if (grade == BallStarGrade.TwoStar) return BallStarGrade.ThreeStar;
        return grade;
    }

    private static bool IsAtMaximumState(BlockElementStatus status)
    {
        return status != null && (status.IsFrozen ||
            status.WetStack >= status.GetMaximumStack(ElementType.Water) ||
            status.BurnStack >= status.GetMaximumStack(ElementType.Fire) ||
            status.StoredFrostStack >= status.GetMaximumStack(ElementType.Ice));
    }

    private static int ModifyLegacyDamage(Ball ball, Block target, int damage)
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
        ResidualChargeStatus residualCharge = target.GetComponent<ResidualChargeStatus>();
        if (ballElement == ElementType.Electric && residualCharge != null && residualCharge.IsMarked)
            bonus += GetRuleInteger(RuleAugmentEffectKind.ResidualCharge);
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

[DisallowMultipleComponent]
public sealed class ResidualChargeStatus : MonoBehaviour
{
    public bool IsMarked { get; private set; }

    public void Mark() => IsMarked = true;

    public bool TryConsume()
    {
        if (!IsMarked) return false;
        IsMarked = false;
        return true;
    }

    private void OnDisable() => IsMarked = false;
}

[DisallowMultipleComponent]
public sealed class TemporaryAugmentBall : MonoBehaviour
{
}
