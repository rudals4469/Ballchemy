using UnityEngine;

[CreateAssetMenu(fileName = "Augment_Overconduction", menuName = "Ballchemy/Augments/Overconduction")]
public sealed class OverconductionAugmentDefinition : AugmentDefinition
{
    [SerializeField] private int[] extraChainTargetsByLevel = { 1, 2, 3 };
    public int GetExtraTargets(int level) => extraChainTargetsByLevel == null || extraChainTargetsByLevel.Length == 0 || level <= 0 ? 0 : Mathf.Max(extraChainTargetsByLevel[Mathf.Clamp(level - 1, 0, extraChainTargetsByLevel.Length - 1)], 0);
    public override bool ApplyLevel(RunAugmentState runState, int previousLevel, int newLevel) => runState != null && IsValidLevel(newLevel);
    public override string GetLevelDescription(int level) => $"젖은 블록의 감전 전도망이 최대 {GetExtraTargets(level)}개 대상까지 추가로 이어집니다.";
}
