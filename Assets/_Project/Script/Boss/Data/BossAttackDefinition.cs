using UnityEngine;

public enum BossAttackType
{
    DirectBossAttack = 0,
    SelectedBlockAttack = 1
}

[CreateAssetMenu(
    fileName = "BossAttackDefinition",
    menuName = "Ballchemy/Boss/Boss Attack Definition"
)]
public sealed class BossAttackDefinition : ScriptableObject
{
    [SerializeField] private string attackId = "boss_attack";
    [SerializeField] private string displayName = "Boss Attack";
    [SerializeField] private BossAttackType attackType;
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField, Min(1)] private int selectedBlockCount = 1;

    public string AttackId => attackId;
    public string DisplayName => displayName;
    public BossAttackType AttackType => attackType;
    public int Damage => Mathf.Max(damage, 1);
    public int SelectedBlockCount => Mathf.Max(selectedBlockCount, 1);

    private void OnValidate()
    {
        damage = Mathf.Max(damage, 1);
        selectedBlockCount = Mathf.Max(selectedBlockCount, 1);
    }
}
