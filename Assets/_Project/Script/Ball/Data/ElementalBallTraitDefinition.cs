using UnityEngine;

[CreateAssetMenu(
    fileName = "Trait_Element",
    menuName =
        "Ballchemy/Balls/Traits/Elemental Trait"
)]
public sealed class ElementalBallTraitDefinition :
    BallTraitDefinition
{
    [Header("Element")]
    [SerializeField]
    private ElementType elementType =
        ElementType.Water;

    [Header("Stack By Star Grade")]
    [SerializeField, Min(1)]
    private int oneStarStackAmount = 1;

    [SerializeField, Min(1)]
    private int twoStarStackAmount = 2;

    [SerializeField, Min(1)]
    private int threeStarStackAmount = 3;

    [Header("Electrocution")]
    [Tooltip(
        "감전 한 쌍당 직접 피해에 곱하는 배율입니다."
    )]
    [SerializeField, Min(0f)]
    private float electrocutionDamageMultiplier =
        0.5f;

    [Tooltip(
        "감전 추가 피해 숫자에 사용할 스타일입니다."
    )]
    [SerializeField]
    private BallDamageTextStyleDefinition
        electrocutionDamageTextStyle;

    public override BallTraitType TraitType =>
        BallTraitType.Elemental;

    public ElementType ElementType =>
        elementType;

    public float ElectrocutionDamageMultiplier =>
        electrocutionDamageMultiplier;

    public BallDamageTextStyleDefinition
        ElectrocutionDamageTextStyle =>
            electrocutionDamageTextStyle;

    public int GetStackAmount(
        BallStarGrade starGrade)
    {
        switch (starGrade)
        {
            case BallStarGrade.OneStar:
                return oneStarStackAmount;

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
        oneStarStackAmount =
            Mathf.Max(
                oneStarStackAmount,
                1
            );

        twoStarStackAmount =
            Mathf.Max(
                twoStarStackAmount,
                1
            );

        threeStarStackAmount =
            Mathf.Max(
                threeStarStackAmount,
                1
            );

        electrocutionDamageMultiplier =
            Mathf.Max(
                electrocutionDamageMultiplier,
                0f
            );
    }
}