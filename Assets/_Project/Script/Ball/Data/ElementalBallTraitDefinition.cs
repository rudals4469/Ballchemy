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

    [Header("Wet Conduction")]
    [Tooltip(
        "번개 공으로 감전을 발생시켰을 때 " +
        "연결된 젖은 블록으로 전도할지 결정합니다."
    )]
    [SerializeField]
    private bool enableWetConduction = true;

    [Tooltip(
        "한 번의 감전으로 전도할 수 있는 최대 블록 수입니다."
    )]
    [SerializeField, Range(1, 3)]
    private int maximumConductionTargets = 3;

    [Tooltip(
        "전도 피해가 성공했을 때 대상에게서 소비할 젖음 스택입니다."
    )]
    [SerializeField, Min(1)]
    private int wetStackCostPerTarget = 1;

    [Header("Thermal Shock")]
    [Tooltip(
        "열충격 한 쌍당 직접 피해에 곱하는 배율입니다."
    )]
    [SerializeField, Min(0f)]
    private float thermalShockDamageMultiplier =
        0.75f;

    [Tooltip(
        "열충격 추가 피해 숫자에 사용할 스타일입니다."
    )]
    [SerializeField]
    private BallDamageTextStyleDefinition
        thermalShockDamageTextStyle;

    public override BallTraitType TraitType =>
        BallTraitType.Elemental;

    public ElementType ElementType =>
        elementType;

    public float ElectrocutionDamageMultiplier =>
        electrocutionDamageMultiplier;

    public BallDamageTextStyleDefinition
        ElectrocutionDamageTextStyle =>
            electrocutionDamageTextStyle;

    public bool EnableWetConduction =>
        enableWetConduction;

    public int MaximumConductionTargets =>
        maximumConductionTargets;

    public int WetStackCostPerTarget =>
        wetStackCostPerTarget;

    public float ThermalShockDamageMultiplier =>
        thermalShockDamageMultiplier;

    public BallDamageTextStyleDefinition
        ThermalShockDamageTextStyle =>
            thermalShockDamageTextStyle;

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

        maximumConductionTargets =
            Mathf.Clamp(
                maximumConductionTargets,
                1,
                3
            );

        wetStackCostPerTarget =
            Mathf.Max(
                wetStackCostPerTarget,
                1
            );

        thermalShockDamageMultiplier =
            Mathf.Max(
                thermalShockDamageMultiplier,
                0f
            );
    }
}