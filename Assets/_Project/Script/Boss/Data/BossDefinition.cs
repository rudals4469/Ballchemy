using UnityEngine;

[CreateAssetMenu(fileName = "BossDefinition", menuName = "Ballchemy/Boss/Boss Definition")]
public sealed class BossDefinition : ScriptableObject
{
    [SerializeField] private string bossId = "boss";
    [SerializeField] private string displayName = "Boss";
    [SerializeField] private BossPatternDefinition patternDefinition;
    [Tooltip("RequiredEnemy 보스 블록에 적용할 체력 배율입니다.")]
    [SerializeField, Min(0.01f)] private float requiredEnemyHealthMultiplier = 1f;

    public string BossId => bossId;
    public string DisplayName => displayName;
    public BossPatternDefinition PatternDefinition => patternDefinition;
    public float RequiredEnemyHealthMultiplier => Mathf.Max(requiredEnemyHealthMultiplier, 0.01f);

    private void OnValidate()
    {
        requiredEnemyHealthMultiplier = Mathf.Max(requiredEnemyHealthMultiplier, 0.01f);
    }
}
