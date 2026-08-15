using UnityEngine;

[CreateAssetMenu(fileName = "Augment_PoisonContagion", menuName = "Ballchemy/Augments/Poison Contagion")]
public sealed class PoisonContagionAugmentDefinition : AugmentDefinition
{
    [SerializeField] private int[] spreadStacksByLevel = { 1, 2, 3 };
    [SerializeField] private int[] maximumTargetsByLevel = { 1, 2, 3 };
    public int GetSpreadStacks(int level) => GetValue(spreadStacksByLevel, level);
    public int GetMaximumTargets(int level) => GetValue(maximumTargetsByLevel, level);
    public override bool ApplyLevel(RunAugmentState runState, int previousLevel, int newLevel) => runState != null && IsValidLevel(newLevel);
    public override string GetLevelDescription(int level) => $"최대 독 스택 블록을 다시 타격하면 주변 {GetMaximumTargets(level)}개 블록에 독 {GetSpreadStacks(level)}스택을 전파합니다.";
    private static int GetValue(int[] values, int level) => values == null || values.Length == 0 || level <= 0 ? 0 : Mathf.Max(values[Mathf.Clamp(level - 1, 0, values.Length - 1)], 0);
}
