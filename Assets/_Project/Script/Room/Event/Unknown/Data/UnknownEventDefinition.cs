using UnityEngine;

public sealed class UnknownEventApplyContext
{
    public BallCollection BallCollection
    {
        get;
    }

    public PlayerHealth PlayerHealth
    {
        get;
    }

    public RunRewardState RunRewardState
    {
        get;
    }

    public BallRuntimeStats BallRuntimeStats =>
        BallCollection != null
            ? BallCollection.RuntimeStats
            : null;

    public bool HasBallCollection =>
        BallCollection != null &&
        BallCollection.IsInitialized;

    public bool HasPlayerHealth =>
        PlayerHealth != null;

    public bool HasBallRuntimeStats =>
        BallRuntimeStats != null;

    public bool HasRunRewardState =>
        RunRewardState != null;

    /*
     * 기존 참조처의 컴파일 호환성을 유지하기 위한
     * 기존 생성자입니다.
     */
    public UnknownEventApplyContext(
        BallCollection ballCollection,
        PlayerHealth playerHealth)
        : this(
            ballCollection,
            playerHealth,
            null
        )
    {
    }

    /*
     * 다음 전투방 보상 상태까지 전달하는
     * 확장 생성자입니다.
     */
    public UnknownEventApplyContext(
        BallCollection ballCollection,
        PlayerHealth playerHealth,
        RunRewardState runRewardState)
    {
        BallCollection =
            ballCollection;

        PlayerHealth =
            playerHealth;

        RunRewardState =
            runRewardState;
    }
}

public sealed class UnknownEventResult
{
    public UnknownEventDefinition Definition
    {
        get;
    }

    public bool WasApplied
    {
        get;
    }

    public string ResultText
    {
        get;
    }

    public UnknownEventResult(
        UnknownEventDefinition definition,
        bool wasApplied,
        string resultText)
    {
        Definition =
            definition;

        WasApplied =
            wasApplied;

        ResultText =
            resultText ?? string.Empty;
    }
}

public abstract class UnknownEventDefinition :
    ScriptableObject
{
    [Header("Identity")]

    [SerializeField]
    private string eventId;

    [SerializeField]
    private string displayName;

    [TextArea(2, 5)]
    [SerializeField]
    private string description;

    [SerializeField]
    private Sprite icon;

    [Header("Selection")]

    [Tooltip(
        "비밀 이벤트 풀에서 등장할 상대적인 가중치입니다. " +
        "0이면 추첨 대상에서 제외됩니다."
    )]
    [SerializeField, Min(0)]
    private int selectionWeight = 1;

    public string EventId =>
        eventId;

    public string DisplayName =>
        displayName;

    public string Description =>
        description;

    public Sprite Icon =>
        icon;

    public int SelectionWeight =>
        selectionWeight;

    public bool CanBeSelected =>
        selectionWeight > 0;

    public abstract bool CanApply(
        UnknownEventApplyContext context);

    public abstract UnknownEventResult Apply(
        UnknownEventApplyContext context);

    protected virtual void OnValidate()
    {
        selectionWeight =
            Mathf.Max(
                selectionWeight,
                0
            );

        if (string.IsNullOrWhiteSpace(
                eventId
            ))
        {
            Debug.LogWarning(
                $"UnknownEventDefinition: {name}의 " +
                "Event Id가 비어 있습니다.",
                this
            );
        }

        if (string.IsNullOrWhiteSpace(
                displayName
            ))
        {
            Debug.LogWarning(
                $"UnknownEventDefinition: {name}의 " +
                "Display Name이 비어 있습니다.",
                this
            );
        }
    }
}