using UnityEngine;

[CreateAssetMenu(
    fileName = "BasicBallTrait",
    menuName =
        "Ballchemy/Balls/Traits/Basic Trait"
)]
public sealed class BasicBallTraitDefinition :
    BallTraitDefinition
{
    public override BallTraitType TraitType =>
        BallTraitType.Basic;
}