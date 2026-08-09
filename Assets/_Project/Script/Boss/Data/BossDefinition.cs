using System;
using UnityEngine;

[CreateAssetMenu(fileName = "BossDefinition", menuName = "Ballchemy/Boss/Boss Definition")]
public sealed class BossDefinition : ScriptableObject
{
    [SerializeField] private string bossId = "boss";
    [SerializeField] private string displayName = "Boss";
    [SerializeField] private BossPatternDefinition patternDefinition;
    [Tooltip("RequiredEnemy 보스 블록에 적용할 체력 배율입니다.")]
    [SerializeField, Min(0.01f)] private float requiredEnemyHealthMultiplier = 1f;
    [SerializeField, Min(1)] private int attackIntervalTurns = 2;
    [SerializeField] private BossAttackDefinition[] attacks = Array.Empty<BossAttackDefinition>();
    [SerializeField, Min(0)] private int spawnedBlockCountPerAttack = 2;
    [SerializeField, Min(1)] private int spawnedBlockHealth = 10;

    public string BossId => bossId;
    public string DisplayName => displayName;
    public BossPatternDefinition PatternDefinition => patternDefinition;
    public float RequiredEnemyHealthMultiplier => Mathf.Max(requiredEnemyHealthMultiplier, 0.01f);
    public int AttackIntervalTurns => Mathf.Max(attackIntervalTurns, 1);
    public int AttackCount => attacks != null ? attacks.Length : 0;
    public int SpawnedBlockCountPerAttack => Mathf.Max(spawnedBlockCountPerAttack, 0);
    public int SpawnedBlockHealth => Mathf.Max(spawnedBlockHealth, 1);

    public BossAttackDefinition GetAttack(int index)
    {
        if (attacks == null || attacks.Length == 0)
        {
            return null;
        }

        index = Mathf.Abs(index) % attacks.Length;
        return attacks[index];
    }

    private void OnValidate()
    {
        requiredEnemyHealthMultiplier = Mathf.Max(requiredEnemyHealthMultiplier, 0.01f);
        attackIntervalTurns = Mathf.Max(attackIntervalTurns, 1);
        spawnedBlockCountPerAttack = Mathf.Max(spawnedBlockCountPerAttack, 0);
        spawnedBlockHealth = Mathf.Max(spawnedBlockHealth, 1);
    }
}
