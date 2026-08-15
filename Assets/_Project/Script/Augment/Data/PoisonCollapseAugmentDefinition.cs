using UnityEngine;

[CreateAssetMenu(fileName = "Augment_PoisonCollapse", menuName = "Ballchemy/Augments/Poison Collapse")]
public sealed class PoisonCollapseAugmentDefinition : AugmentDefinition
{
    [SerializeField] private int[] damageMultiplierByLevel = { 1, 1, 2 };
    [SerializeField] private int explosionRadius = 1;
    public int ExplosionRadius => Mathf.Max(explosionRadius, 1);
    public int GetDamageMultiplier(int level) => damageMultiplierByLevel == null || damageMultiplierByLevel.Length == 0 || level <= 0 ? 0 : Mathf.Max(damageMultiplierByLevel[Mathf.Clamp(level - 1, 0, damageMultiplierByLevel.Length - 1)], 0);
    public override bool ApplyLevel(RunAugmentState runState, int previousLevel, int newLevel) => runState != null && IsValidLevel(newLevel);
    public override string GetLevelDescription(int level) => $"최대 독 스택 블록을 다시 중독시키면 독을 모두 소비하고 스택의 {GetDamageMultiplier(level)}배 피해로 폭발합니다.";
}
