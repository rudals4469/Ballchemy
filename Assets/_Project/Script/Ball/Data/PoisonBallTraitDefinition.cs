using UnityEngine;

[CreateAssetMenu(
    fileName = "Trait_Poison",
    menuName = "Ballchemy/Balls/Traits/Poison")]
public sealed class PoisonBallTraitDefinition :
    BallTraitDefinition
{
    [SerializeField, Min(1)]
    private int oneStarStackAmount = 1;

    [SerializeField, Min(1)]
    private int twoStarStackAmount = 2;

    [SerializeField, Min(1)]
    private int threeStarStackAmount = 3;

    [SerializeField, Min(1)]
    private int maximumStacks = 5;

    public override BallTraitType TraitType =>
        BallTraitType.Poison;

    public int MaximumStacks =>
        maximumStacks;

    public int GetStackAmount(BallStarGrade starGrade)
    {
        switch (starGrade)
        {
            case BallStarGrade.TwoStar:
                return twoStarStackAmount;

            case BallStarGrade.ThreeStar:
                return threeStarStackAmount;

            default:
                return oneStarStackAmount;
        }
    }

    private void OnValidate()
    {
        oneStarStackAmount = Mathf.Max(oneStarStackAmount, 1);
        twoStarStackAmount = Mathf.Max(twoStarStackAmount, 1);
        threeStarStackAmount = Mathf.Max(threeStarStackAmount, 1);
        maximumStacks = Mathf.Max(maximumStacks, 1);
    }
}
