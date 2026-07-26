using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "Trait_Critical",
    menuName =
        "Ballchemy/Balls/Traits/Critical Trait"
)]
public sealed class CriticalBallTraitDefinition :
    BallTraitDefinition
{
    [Header("Critical Damage Multiplier")]

    [Tooltip(
        "치명타 공 1성의 피해 배율입니다."
    )]
    [FormerlySerializedAs(
        "criticalDamageMultiplier"
    )]
    [SerializeField, Min(1f)]
    private float oneStarMultiplier = 1.5f;

    [Tooltip(
        "치명타 공 2성의 피해 배율입니다."
    )]
    [SerializeField, Min(1f)]
    private float twoStarMultiplier = 2f;

    [Tooltip(
        "치명타 공 3성의 피해 배율입니다."
    )]
    [SerializeField, Min(1f)]
    private float threeStarMultiplier = 3f;

    public override BallTraitType TraitType =>
        BallTraitType.Critical;

    public float OneStarMultiplier =>
        oneStarMultiplier;

    public float TwoStarMultiplier =>
        twoStarMultiplier;

    public float ThreeStarMultiplier =>
        threeStarMultiplier;

    public float GetMultiplier(
        BallStarGrade starGrade)
    {
        switch (starGrade)
        {
            case BallStarGrade.OneStar:
                return oneStarMultiplier;

            case BallStarGrade.TwoStar:
                return twoStarMultiplier;

            case BallStarGrade.ThreeStar:
                return threeStarMultiplier;

            case BallStarGrade.None:
            default:
                return 1f;
        }
    }

    private void OnValidate()
    {
        oneStarMultiplier =
            Mathf.Max(
                oneStarMultiplier,
                1f
            );

        twoStarMultiplier =
            Mathf.Max(
                twoStarMultiplier,
                1f
            );

        threeStarMultiplier =
            Mathf.Max(
                threeStarMultiplier,
                1f
            );
    }
}