using UnityEngine;

[CreateAssetMenu(
    fileName = "Trait_Piercing",
    menuName =
        "Ballchemy/Balls/Traits/Piercing"
)]
public sealed class PiercingBallTraitDefinition :
    BallTraitDefinition
{
    [Header("Multi Hit")]

    [Tooltip(
        "블록 안에 머무르는 동안 " +
        "추가 피해가 발생하는 시간 간격입니다."
    )]
    [SerializeField, Min(0.01f)]
    private float hitInterval = 0.04f;

    [Tooltip(
        "한 번 블록을 통과할 때 적용되는 " +
        "최대 타격 횟수입니다. 최초 진입 타격을 포함합니다."
    )]
    [SerializeField, Min(1)]
    private int maxHitsPerEntry = 3;

    [Tooltip(
        "관통 공의 타격 1회당 직접 피해 배율입니다."
    )]
    [SerializeField, Min(0.01f)]
    private float damageMultiplier = 1f;

    public override BallTraitType TraitType =>
        BallTraitType.Piercing;

    public float HitInterval =>
        hitInterval;

    public int MaxHitsPerEntry =>
        maxHitsPerEntry;

    public float DamageMultiplier =>
        damageMultiplier;

    public int CalculateDamage(
        int directDamage)
    {
        int safeDirectDamage =
            Mathf.Max(
                directDamage,
                1
            );

        int calculatedDamage =
            Mathf.FloorToInt(
                safeDirectDamage *
                damageMultiplier +
                0.5f
            );

        return Mathf.Max(
            calculatedDamage,
            1
        );
    }

    private void OnValidate()
    {
        hitInterval =
            Mathf.Max(
                hitInterval,
                0.01f
            );

        maxHitsPerEntry =
            Mathf.Max(
                maxHitsPerEntry,
                1
            );

        damageMultiplier =
            Mathf.Max(
                damageMultiplier,
                0.01f
            );
    }
}