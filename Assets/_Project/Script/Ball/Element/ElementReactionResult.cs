public readonly struct ElementReactionResult
{
    public ElementType AppliedElement
    {
        get;
    }

    public int WetStackBefore
    {
        get;
    }

    public int ChargeStackBefore
    {
        get;
    }

    public int BurnStackBefore
    {
        get;
    }

    public int FrostStackBefore
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

    public int BurnStackAfter
    {
        get;
    }

    public int FrostStackAfter
    {
        get;
    }

    public int AppliedElementStack
    {
        get;
    }

    public ElementReactionKind ReactionKind
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
        ReactionKind !=
            ElementReactionKind.None &&
        ReactionCount > 0 &&
        TotalDamage > 0;

    public bool IsElectrocution =>
        ReactionKind ==
        ElementReactionKind.Electrocution;

    public bool IsThermalShock =>
        ReactionKind ==
        ElementReactionKind.ThermalShock;

    public ElementReactionResult(
        ElementType appliedElement,
        int wetStackBefore,
        int chargeStackBefore,
        int burnStackBefore,
        int frostStackBefore,
        int wetStackAfter,
        int chargeStackAfter,
        int burnStackAfter,
        int frostStackAfter,
        int appliedElementStack,
        ElementReactionKind reactionKind,
        int reactionCount,
        int damagePerReaction,
        int totalDamage)
    {
        AppliedElement =
            appliedElement;

        WetStackBefore =
            wetStackBefore;

        ChargeStackBefore =
            chargeStackBefore;

        BurnStackBefore =
            burnStackBefore;

        FrostStackBefore =
            frostStackBefore;

        WetStackAfter =
            wetStackAfter;

        ChargeStackAfter =
            chargeStackAfter;

        BurnStackAfter =
            burnStackAfter;

        FrostStackAfter =
            frostStackAfter;

        AppliedElementStack =
            appliedElementStack;

        ReactionKind =
            reactionKind;

        ReactionCount =
            reactionCount;

        DamagePerReaction =
            damagePerReaction;

        TotalDamage =
            totalDamage;
    }

    /*
     * 기존 물·전기 생성 코드와의 호환용 생성자입니다.
     */
    public ElementReactionResult(
        int wetStackBefore,
        int chargeStackBefore,
        int wetStackAfter,
        int chargeStackAfter,
        int appliedElementStack,
        int reactionCount,
        int damagePerReaction,
        int totalDamage)
        : this(
            ElementType.Water,
            wetStackBefore,
            chargeStackBefore,
            0,
            0,
            wetStackAfter,
            chargeStackAfter,
            0,
            0,
            appliedElementStack,
            reactionCount > 0
                ? ElementReactionKind.Electrocution
                : ElementReactionKind.None,
            reactionCount,
            damagePerReaction,
            totalDamage
        )
    {
    }
}