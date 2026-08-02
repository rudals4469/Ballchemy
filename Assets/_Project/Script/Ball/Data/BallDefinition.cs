using UnityEngine;

[CreateAssetMenu(
    fileName = "BallDefinition",
    menuName =
        "Ballchemy/Balls/Ball Definition"
)]
public sealed class BallDefinition :
    ScriptableObject
{
    [Header("Identity")]

    [SerializeField]
    private string ballId;

    [SerializeField]
    private string displayName;

    [Header("Grade")]

    [SerializeField]
    private BallStarGrade starGrade =
        BallStarGrade.OneStar;

    [Tooltip(
        "이 공을 1단계 승급했을 때 변경할 " +
        "다음 등급의 Ball Definition입니다.\n" +
        "1성은 같은 종류의 2성, " +
        "2성은 같은 종류의 3성을 연결합니다.\n" +
        "최종 등급이나 승급할 수 없는 공은 비워둡니다."
    )]
    [SerializeField]
    private BallDefinition nextStarDefinition;

    [Header("Combat")]

    [Tooltip(
        "공통 기본 피해에 더해지는 " +
        "이 공만의 직접 타격 피해 보너스입니다."
    )]
    [SerializeField]
    private int directDamageBonus;

    [Header("Trait")]

    [Tooltip(
        "이 공이 보유한 단 하나의 특성입니다."
    )]
    [SerializeField]
    private BallTraitDefinition traitDefinition;

    [Header("Visual")]

    [SerializeField]
    private Sprite sprite;

    [SerializeField]
    private Color color =
        Color.white;

    [SerializeField]
    private Vector3 visualScale =
        Vector3.one;

    [Header("Damage Text")]

    [Tooltip(
        "이 공이 피해를 입혔을 때 사용하는 " +
        "기본 데미지 텍스트 스타일입니다."
    )]
    [SerializeField]
    private BallDamageTextStyleDefinition
        damageTextStyle;

    [Header("Selection")]

    [Tooltip(
        "공 선택지에서 이 공이 등장할 " +
        "상대적인 가중치입니다."
    )]
    [SerializeField, Min(0)]
    private int selectionWeight = 1;

    public string BallId =>
        ballId;

    public string DisplayName =>
        displayName;

    public BallStarGrade StarGrade =>
        starGrade;

    public BallDefinition NextStarDefinition =>
        nextStarDefinition;

    public bool CanUpgrade =>
        nextStarDefinition != null;

    public int DirectDamageBonus =>
        directDamageBonus;

    public BallTraitDefinition TraitDefinition =>
        traitDefinition;

    public BallTraitType TraitType =>
        traitDefinition != null
            ? traitDefinition.TraitType
            : BallTraitType.Basic;

    public Sprite Sprite =>
        sprite;

    public Color Color =>
        color;

    public Vector3 VisualScale =>
        visualScale;

    public BallDamageTextStyleDefinition
        DamageTextStyle =>
            damageTextStyle;

    public int SelectionWeight =>
        selectionWeight;

    public bool HasTraitDefinition =>
        traitDefinition != null;

    private void OnValidate()
    {
        NormalizeSettings();
        ValidateTraitDefinition();
        ValidateGradeSettings();
    }

    private void NormalizeSettings()
    {
        visualScale.x =
            Mathf.Max(
                visualScale.x,
                0.01f
            );

        visualScale.y =
            Mathf.Max(
                visualScale.y,
                0.01f
            );

        visualScale.z =
            Mathf.Max(
                visualScale.z,
                0.01f
            );

        selectionWeight =
            Mathf.Max(
                selectionWeight,
                0
            );
    }

    private void ValidateTraitDefinition()
    {
        if (traitDefinition != null)
        {
            return;
        }

        Debug.LogWarning(
            $"BallDefinition: {name}에 " +
            "Trait Definition이 연결되지 않았습니다.",
            this
        );
    }

    private void ValidateGradeSettings()
    {
        if (traitDefinition != null &&
            traitDefinition.TraitType !=
            BallTraitType.Piercing &&
            starGrade ==
            BallStarGrade.None)
        {
            Debug.LogWarning(
                $"BallDefinition: {name}은 " +
                "관통 공이 아니지만 등급이 None입니다.",
                this
            );
        }

        if (nextStarDefinition == null)
        {
            return;
        }

        if (nextStarDefinition == this)
        {
            Debug.LogError(
                $"BallDefinition: {name}의 " +
                "Next Star Definition에 " +
                "자기 자신이 연결되어 있습니다.",
                this
            );

            return;
        }

        if (!IsExpectedNextGrade(
                starGrade,
                nextStarDefinition.StarGrade
            ))
        {
            Debug.LogWarning(
                $"BallDefinition: {name}의 다음 등급이 " +
                $"{nextStarDefinition.name}으로 연결되어 있지만, " +
                $"{starGrade} 다음 등급과 일치하지 않습니다.",
                this
            );
        }

        if (TraitType !=
            nextStarDefinition.TraitType)
        {
            Debug.LogWarning(
                $"BallDefinition: {name}과 " +
                $"{nextStarDefinition.name}의 " +
                "Trait Type이 서로 다릅니다.",
                this
            );
        }
    }

    private static bool IsExpectedNextGrade(
        BallStarGrade currentGrade,
        BallStarGrade nextGrade)
    {
        switch (currentGrade)
        {
            case BallStarGrade.OneStar:
                return nextGrade ==
                       BallStarGrade.TwoStar;

            case BallStarGrade.TwoStar:
                return nextGrade ==
                       BallStarGrade.ThreeStar;

            default:
                return false;
        }
    }
}