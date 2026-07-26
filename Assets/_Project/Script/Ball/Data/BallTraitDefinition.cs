using UnityEngine;

public abstract class BallTraitDefinition :
    ScriptableObject
{
    public abstract BallTraitType TraitType
    {
        get;
    }
}