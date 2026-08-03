using UnityEngine;

[CreateAssetMenu(
    fileName = "UnknownEvent_CatalystExtraction",
    menuName =
        "Ballchemy/Events/Unknown/Alchemy/Catalyst Extraction"
)]
public sealed class
    CatalystExtractionUnknownEventDefinition :
        UnknownEventDefinition
{
    [Header("Catalyst Extraction")]

    [Tooltip(
        "무작위로 제거할 공의 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int ballsToRemove = 8;

    [Tooltip(
        "이벤트 적용 후 반드시 남겨둘 최소 공 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int minimumRemainingBalls = 1;

    [Tooltip(
        "모든 공에 추가할 런 고정 직접 피해입니다."
    )]
    [SerializeField, Min(1)]
    private int directDamageBonus = 2;

    public int BallsToRemove =>
        ballsToRemove;

    public int MinimumRemainingBalls =>
        minimumRemainingBalls;

    public int DirectDamageBonus =>
        directDamageBonus;

    public override bool CanApply(
        UnknownEventApplyContext context)
    {
        if (context == null ||
            !context.HasBallCollection ||
            !context.HasBallRuntimeStats)
        {
            return false;
        }

        BallCollection collection =
            context.BallCollection;

        if (!collection.CanModifyBallComposition)
        {
            return false;
        }

        int removableCount =
            collection.Count -
            minimumRemainingBalls;

        return removableCount >=
               ballsToRemove;
    }

    public override UnknownEventResult Apply(
        UnknownEventApplyContext context)
    {
        if (!CanApply(
                context
            ))
        {
            return new UnknownEventResult(
                this,
                false,
                "촉매 추출을 적용할 수 없습니다."
            );
        }

        BallCollection collection =
            context.BallCollection;

        int removedCount =
            collection.RemoveRandomBalls(
                ballsToRemove,
                minimumRemainingBalls
            );

        if (removedCount !=
            ballsToRemove)
        {
            Debug.LogError(
                "CatalystExtractionUnknownEventDefinition: " +
                $"공 제거 결과가 예상과 다릅니다. " +
                $"예상={ballsToRemove}, " +
                $"실제={removedCount}",
                this
            );

            return new UnknownEventResult(
                this,
                false,
                $"공 {removedCount}개만 제거되어 " +
                "촉매 추출을 완료하지 못했습니다."
            );
        }

        context
            .BallRuntimeStats
            .AddRunDirectDamageBonus(
                directDamageBonus
            );

        string resultText =
            $"공 {removedCount}개를 촉매로 추출했습니다. " +
            $"모든 공의 직접 피해가 " +
            $"+{directDamageBonus} 증가했습니다.";

        Debug.Log(
            "CatalystExtractionUnknownEventDefinition: " +
            resultText,
            this
        );

        return new UnknownEventResult(
            this,
            true,
            resultText
        );
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        ballsToRemove =
            Mathf.Max(
                ballsToRemove,
                1
            );

        minimumRemainingBalls =
            Mathf.Max(
                minimumRemainingBalls,
                1
            );

        directDamageBonus =
            Mathf.Max(
                directDamageBonus,
                1
            );
    }
}