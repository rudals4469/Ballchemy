using UnityEngine;

[CreateAssetMenu(
    fileName = "Trait_Explosion",
    menuName =
        "Ballchemy/Balls/Traits/Explosion Trait"
)]
public sealed class ExplosionBallTraitDefinition :
    BallTraitDefinition
{
    [Header("Pattern")]

    [Tooltip(
        "AllDirections는 십자와 대각선이 합쳐진 " +
        "총 8방향 폭발입니다."
    )]
    [SerializeField]
    private ExplosionPatternType patternType =
        ExplosionPatternType.AllDirections;

    [Header("Range By Star Grade")]

    [Tooltip(
        "1성 폭발 공이 각 방향으로 뻗는 거리입니다."
    )]
    [SerializeField, Min(1)]
    private int oneStarRange = 1;

    [Tooltip(
        "2성 폭발 공이 각 방향으로 뻗는 거리입니다."
    )]
    [SerializeField, Min(1)]
    private int twoStarRange = 2;

    [Tooltip(
        "3성 폭발 공이 각 방향으로 뻗는 거리입니다."
    )]
    [SerializeField, Min(1)]
    private int threeStarRange = 3;

    [Header("Explosion Damage")]

    [Tooltip(
        "공의 직접 피해에 곱해지는 " +
        "주변 폭발 피해 배율입니다."
    )]
    [SerializeField, Min(0f)]
    private float explosionDamageMultiplier =
        0.5f;

    [Tooltip(
        "배율 계산 후 추가되는 " +
        "고정 폭발 피해입니다."
    )]
    [SerializeField, Min(0)]
    private int flatExplosionDamageBonus;

    [Header("Damage Text")]

    [Tooltip(
        "주변 폭발 피해에 사용하는 " +
        "데미지 텍스트 스타일입니다. " +
        "비어 있으면 BallDefinition의 스타일을 사용합니다."
    )]
    [SerializeField]
    private BallDamageTextStyleDefinition
        explosionDamageTextStyle;

    public override BallTraitType TraitType =>
        BallTraitType.Explosion;

    public ExplosionPatternType PatternType =>
        patternType;

    public int OneStarRange =>
        oneStarRange;

    public int TwoStarRange =>
        twoStarRange;

    public int ThreeStarRange =>
        threeStarRange;

    public float ExplosionDamageMultiplier =>
        explosionDamageMultiplier;

    public int FlatExplosionDamageBonus =>
        flatExplosionDamageBonus;

    public BallDamageTextStyleDefinition
        ExplosionDamageTextStyle =>
            explosionDamageTextStyle;

    public int GetRange(
        BallStarGrade starGrade)
    {
        switch (starGrade)
        {
            case BallStarGrade.OneStar:
                return oneStarRange;

            case BallStarGrade.TwoStar:
                return twoStarRange;

            case BallStarGrade.ThreeStar:
                return threeStarRange;

            default:
                return oneStarRange;
        }
    }

    public int CalculateExplosionDamage(
        int directDamage)
    {
        directDamage =
            Mathf.Max(
                directDamage,
                1
            );

        int multipliedDamage =
            Mathf.RoundToInt(
                directDamage *
                explosionDamageMultiplier
            );

        int finalDamage =
            multipliedDamage +
            flatExplosionDamageBonus;

        return Mathf.Max(
            finalDamage,
            1
        );
    }

    private void OnValidate()
    {
        oneStarRange =
            Mathf.Max(
                oneStarRange,
                1
            );

        twoStarRange =
            Mathf.Max(
                twoStarRange,
                1
            );

        threeStarRange =
            Mathf.Max(
                threeStarRange,
                1
            );

        explosionDamageMultiplier =
            Mathf.Max(
                explosionDamageMultiplier,
                0f
            );

        flatExplosionDamageBonus =
            Mathf.Max(
                flatExplosionDamageBonus,
                0
            );
    }
}