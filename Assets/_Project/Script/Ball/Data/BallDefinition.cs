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

    [Header("Combat")]

    [Tooltip(
        "공통 기본 피해에 더해지는 " +
        "이 공만의 직접 타격 피해 보너스입니다. " +
        "기본 공처럼 직접 타격이 강한 공에 사용합니다."
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

    public int SelectionWeight =>
        selectionWeight;

    public bool HasTraitDefinition =>
        traitDefinition != null;

    private void OnValidate()
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

        if (traitDefinition == null)
        {
            Debug.LogWarning(
                $"BallDefinition: {name}에 " +
                "Trait Definition이 연결되지 않았습니다.",
                this
            );
        }

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
    }
}