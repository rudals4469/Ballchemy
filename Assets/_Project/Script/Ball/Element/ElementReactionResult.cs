public readonly struct ElementReactionResult
{
    public int WetStackBefore
    {
        get;
    }

    public int ChargeStackBefore
    {
        get;
    }

    public int WetStackAfter
    {
        get;
    }

    public int ChargeStackAfter
    {
        get;
    }

    public int AppliedElementStack
    {
        get;
    }

    public int ReactionCount
    {
        get;
    }

    public int DamagePerReaction
    {
        get;
    }

    public int TotalDamage
    {
        get;
    }

    public bool HasReaction =>
        ReactionCount > 0 &&
        TotalDamage > 0;

    public ElementReactionResult(
        int wetStackBefore,
        int chargeStackBefore,
        int wetStackAfter,
        int chargeStackAfter,
        int appliedElementStack,
        int reactionCount,
        int damagePerReaction,
        int totalDamage)
    {
        WetStackBefore =
            wetStackBefore;

        ChargeStackBefore =
            chargeStackBefore;

        WetStackAfter =
            wetStackAfter;

        ChargeStackAfter =
            chargeStackAfter;

        AppliedElementStack =
            appliedElementStack;

        ReactionCount =
            reactionCount;

        DamagePerReaction =
            damagePerReaction;

        TotalDamage =
            totalDamage;
    }
}