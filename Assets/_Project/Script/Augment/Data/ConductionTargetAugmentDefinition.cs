using UnityEngine;

[CreateAssetMenu(fileName = "Augment_ConductionTarget", menuName = "Ballchemy/Augments/Conduction Target")]
public sealed class ConductionTargetAugmentDefinition : AugmentDefinition
{
    [SerializeField] private int[] totalBonusByLevel = { 1, 2, 3 };
    public int GetBonus(int level) => GetValue(totalBonusByLevel, level);
    public override bool ApplyLevel(RunAugmentState runState, int previousLevel, int newLevel) => runState != null && IsValidLevel(newLevel);
    public override string GetLevelDescription(int level) => $"번개 전이 최대 대상이 {GetBonus(level)} 증가합니다.";
    private static int GetValue(int[] values, int level) => values == null || values.Length == 0 || level <= 0 ? 0 : Mathf.Max(values[Mathf.Clamp(level - 1, 0, values.Length - 1)], 0);
}
