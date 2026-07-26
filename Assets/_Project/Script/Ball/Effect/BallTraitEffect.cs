using UnityEngine;

public abstract class BallTraitEffect :
    MonoBehaviour
{
    protected Ball Ball
    {
        get;
        private set;
    }

    protected BallCombatController CombatController
    {
        get;
        private set;
    }

    protected BallTraitDefinition TraitDefinition
    {
        get;
        private set;
    }

    public abstract BallTraitType TraitType
    {
        get;
    }

    public void Initialize(
        Ball ball,
        BallCombatController combatController,
        BallTraitDefinition traitDefinition)
    {
        Ball =
            ball;

        CombatController =
            combatController;

        TraitDefinition =
            traitDefinition;

        OnInitialized();
    }

    protected virtual void OnInitialized()
    {
    }

    public abstract BallHitResult ResolveHit(
        BallHitContext context);
}