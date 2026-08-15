using UnityEngine;

public abstract class RewardDefinition :
    ScriptableObject
{
    [Header("Identity")]

    [SerializeField]
    private string rewardId;

    [SerializeField]
    private string displayName;

    [TextArea(2, 5)]
    [SerializeField]
    private string description;

    [SerializeField]
    private Sprite icon;

    [Header("Classification")]

    [SerializeField]
    private RewardTier rewardTier =
        RewardTier.Tier1;

    [Header("Selection")]

    [Tooltip(
        "보상 선택지에 등장할 상대적인 가중치입니다. " +
        "0이면 일반 선택지 생성 대상에서 제외됩니다."
    )]
    [SerializeField, Min(0)]
    private int selectionWeight = 1;

    public string RewardId =>
        rewardId;

    public string DisplayName =>
        displayName;

    public string Description =>
        description;

    public Sprite Icon =>
        icon;

    public RewardTier RewardTier =>
        rewardTier;

    public int SelectionWeight =>
        selectionWeight;

    public bool CanBeSelected =>
        rewardTier != RewardTier.None &&
        selectionWeight > 0;

    public abstract RewardType RewardType
    {
        get;
    }

    public abstract bool CanApply(
        RewardApplyContext context);

    public abstract bool Apply(
        RewardApplyContext context);

    protected void ConfigureBase(
        string id,
        string title,
        string body,
        RewardTier tier,
        int weight)
    {
        rewardId = id;
        displayName = title;
        description = body;
        rewardTier = tier;
        selectionWeight = Mathf.Max(weight, 0);
    }

    protected virtual void OnValidate()
    {
        selectionWeight =
            Mathf.Max(
                selectionWeight,
                0
            );

        if (string.IsNullOrWhiteSpace(
                rewardId
            ))
        {
            Debug.LogWarning(
                $"RewardDefinition: {name}의 " +
                "Reward Id가 비어 있습니다.",
                this
            );
        }

        if (string.IsNullOrWhiteSpace(
                displayName
            ))
        {
            Debug.LogWarning(
                $"RewardDefinition: {name}의 " +
                "Display Name이 비어 있습니다.",
                this
            );
        }

        if (rewardTier ==
            RewardTier.None)
        {
            Debug.LogWarning(
                $"RewardDefinition: {name}의 " +
                "Reward Tier가 None입니다.",
                this
            );
        }
    }
}
