using UnityEngine;

[CreateAssetMenu(
    fileName = "Trait_Piercing",
    menuName =
        "Ballchemy/Balls/Traits/Piercing"
)]
public sealed class PiercingBallTraitDefinition :
    BallTraitDefinition
{
    public override BallTraitType TraitType =>
        BallTraitType.Piercing;
}