using UnityEngine;

public abstract class AugmentDefinition :
    ScriptableObject
{
    [Header("Identity")]

    [SerializeField]
    private string augmentId;

    [SerializeField]
    private string displayName;

    [TextArea(2, 5)]
    [SerializeField]
    private string description;

    [SerializeField]
    private Sprite icon;

    [Header("Level")]

    [SerializeField]
    private AugmentValueTier valueTier =
        AugmentValueTier.Value1;

    [Tooltip(
        "이번 런에서 이 증강이 도달할 수 있는 " +
        "최대 레벨입니다."
    )]
    [SerializeField, Min(1)]
    private int maxLevel = 3;

    public string AugmentId =>
        augmentId;

    public string DisplayName =>
        displayName;

    public string Description =>
        description;

    public Sprite Icon =>
        icon;

    public int MaxLevel =>
        maxLevel;

    public AugmentValueTier ValueTier =>
        valueTier;

    protected void ConfigureBase(
        string id,
        string title,
        string body,
        AugmentValueTier tier,
        int maximumLevel)
    {
        augmentId = id;
        displayName = title;
        description = body;
        valueTier = tier;
        maxLevel = Mathf.Max(maximumLevel, 1);
    }

    public bool IsValidLevel(
        int level)
    {
        return
            level >= 1 &&
            level <= maxLevel;
    }

    /*
     * previousLevel에서 newLevel로 증가할 때
     * 실제 런 효과를 적용합니다.
     *
     * 적용에 성공한 경우에만 true를 반환합니다.
     */
    public abstract bool ApplyLevel(
        RunAugmentState runState,
        int previousLevel,
        int newLevel);

    /*
     * 이후 보상 카드에서 현재 레벨과 다음 레벨 효과를
     * 비교해서 표시할 때 사용할 수 있습니다.
     */
    public virtual string GetLevelDescription(
        int level)
    {
        if (!IsValidLevel(level))
        {
            return string.Empty;
        }

        return description;
    }

    protected virtual void OnValidate()
    {
        maxLevel =
            Mathf.Max(
                maxLevel,
                1
            );

        if (string.IsNullOrWhiteSpace(
                augmentId
            ))
        {
            Debug.LogWarning(
                $"AugmentDefinition: {name}의 " +
                "Augment Id가 비어 있습니다.",
                this
            );
        }

        if (string.IsNullOrWhiteSpace(
                displayName
            ))
        {
            Debug.LogWarning(
                $"AugmentDefinition: {name}의 " +
                "Display Name이 비어 있습니다.",
                this
            );
        }
    }
}
