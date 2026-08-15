using UnityEngine;

[CreateAssetMenu(fileName = "Augment_PoisonMaximumStack", menuName = "Ballchemy/Augments/Poison Maximum Stack")]
public sealed class PoisonMaximumStackAugmentDefinition : AugmentDefinition
{
    [SerializeField] private int[] totalBonusByLevel = { 2, 4, 6 };

    public int GetBonus(int level) => GetValue(totalBonusByLevel, level);
    public override bool ApplyLevel(RunAugmentState runState, int previousLevel, int newLevel) => runState != null && IsValidLevel(newLevel);
    public override string GetLevelDescription(int level) => $"독 최대 스택이 {GetBonus(level)} 증가합니다.";

    private static int GetValue(int[] values, int level)
    {
        if (values == null || values.Length == 0 || level <= 0) return 0;
        return Mathf.Max(values[Mathf.Clamp(level - 1, 0, values.Length - 1)], 0);
    }
}
