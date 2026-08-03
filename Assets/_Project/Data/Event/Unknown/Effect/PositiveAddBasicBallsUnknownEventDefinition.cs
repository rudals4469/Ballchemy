using UnityEngine;

[CreateAssetMenu(
    fileName = "UnknownEvent_AddBasicBalls",
    menuName =
        "Ballchemy/Events/Unknown/Positive/Add Basic Balls"
)]
public sealed class
    AddBasicBallsUnknownEventDefinition :
        UnknownEventDefinition
{
    [Header("Add Basic Balls")]

    [Tooltip(
        "이 결과로 지급할 기본 공 Definition입니다."
    )]
    [SerializeField]
    private BallDefinition basicBallDefinition;

    [Tooltip(
        "지급할 기본 공 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int amount = 8;

    public BallDefinition BasicBallDefinition =>
        basicBallDefinition;

    public int Amount =>
        amount;

    public override bool CanApply(
        UnknownEventApplyContext context)
    {
        if (context == null ||
            !context.HasBallCollection)
        {
            return false;
        }

        if (basicBallDefinition == null ||
            amount <= 0)
        {
            return false;
        }

        return context
            .BallCollection
            .IsInitialized;
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
                "기본 공을 지급할 수 없습니다."
            );
        }

        int addedCount =
            context
                .BallCollection
                .AddBalls(
                    amount,
                    basicBallDefinition
                );

        if (addedCount != amount)
        {
            Debug.LogError(
                "AddBasicBallsUnknownEventDefinition: " +
                "기본 공 지급 결과가 예상과 다릅니다. " +
                $"예상={amount}, 실제={addedCount}",
                this
            );

            return new UnknownEventResult(
                this,
                false,
                $"기본 공 {addedCount}개만 지급되었습니다."
            );
        }

        string resultText =
            $"기본 공을 {addedCount}개 획득했습니다.";

        Debug.Log(
            "AddBasicBallsUnknownEventDefinition: " +
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

        amount =
            Mathf.Max(
                amount,
                1
            );

        if (basicBallDefinition == null)
        {
            Debug.LogWarning(
                "AddBasicBallsUnknownEventDefinition: " +
                $"{name}에 기본 공 Definition이 " +
                "연결되지 않았습니다.",
                this
            );

            return;
        }

        if (basicBallDefinition.TraitType !=
            BallTraitType.Basic)
        {
            Debug.LogWarning(
                "AddBasicBallsUnknownEventDefinition: " +
                $"{basicBallDefinition.name}은 " +
                "기본 공이 아닙니다.",
                this
            );
        }

        if (basicBallDefinition.StarGrade !=
            BallStarGrade.OneStar)
        {
            Debug.LogWarning(
                "AddBasicBallsUnknownEventDefinition: " +
                $"{basicBallDefinition.name}은 " +
                "1성 공이 아닙니다.",
                this
            );
        }
    }
}