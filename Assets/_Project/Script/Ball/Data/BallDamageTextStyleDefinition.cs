using DG.Tweening;
using TMPro;
using UnityEngine;

[CreateAssetMenu(
    fileName = "DamageTextStyle",
    menuName =
        "Ballchemy/Balls/Damage Text Style"
)]
public sealed class BallDamageTextStyleDefinition :
    ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string styleId;

    [Header("Optional Prefab Override")]

    [Tooltip(
        "비워두면 공용 데미지 텍스트 프리팹을 사용합니다. " +
        "완전히 다른 구조가 필요할 때만 전용 프리팹을 연결합니다."
    )]
    [SerializeField]
    private BallDamagePopupView
        popupPrefabOverride;

    [Header("Aggregation")]

    [Tooltip(
        "데미지 텍스트를 합산하는 방식입니다."
    )]
    [SerializeField]
    private BallDamageTextAggregationMode
        aggregationMode =
            BallDamageTextAggregationMode
                .SameTargetAndStyle;

    [Tooltip(
        "마지막 피해 이후 이 시간 동안 " +
        "같은 블록과 같은 스타일의 피해가 들어오면 합산합니다."
    )]
    [SerializeField, Min(0f)]
    private float aggregationWindow = 0.3f;

    [Header("Text")]
    [SerializeField]
    private TMP_FontAsset fontAsset;

    [Tooltip(
        "TMP Font Asset에서 만든 Material Preset을 " +
        "연결할 수 있습니다."
    )]
    [SerializeField]
    private Material fontMaterialPreset;

    [SerializeField]
    private Color textColor =
        Color.white;

    [SerializeField, Min(0.1f)]
    private float fontSize = 5f;

    [SerializeField]
    private FontStyles fontStyle =
        FontStyles.Bold;

    [SerializeField]
    private string prefix;

    [SerializeField]
    private string suffix;

    [Header("Spawn Position")]
    [SerializeField]
    private Vector2 spawnOffset =
        new Vector2(
            0f,
            0.15f
        );

    [SerializeField, Min(0f)]
    private float horizontalJitter = 0.12f;

    [SerializeField]
    private float worldZ = -1f;

    [Header("Movement")]

    [Tooltip(
        "누적이 끝난 뒤 상승하며 사라지는 시간입니다."
    )]
    [SerializeField, Min(0.05f)]
    private float duration = 0.55f;

    [SerializeField, Min(0f)]
    private float riseDistance = 0.7f;

    [Tooltip(
        "텍스트가 사라질 때 좌우로 움직이는 " +
        "거리의 무작위 범위입니다."
    )]
    [SerializeField]
    private Vector2 horizontalDriftRange =
        new Vector2(
            -0.08f,
            0.08f
        );

    [SerializeField]
    private Ease moveEase =
        Ease.OutCubic;

    [Header("Scale")]
    [SerializeField, Min(0.01f)]
    private float startScale = 0.9f;

    [SerializeField, Min(0.01f)]
    private float peakScale = 1.15f;

    [SerializeField, Min(0.01f)]
    private float endScale = 1f;

    [Tooltip(
        "팝 애니메이션에서 확대가 차지하는 비율입니다."
    )]
    [SerializeField, Range(0.01f, 1f)]
    private float popDurationRatio = 0.25f;

    [SerializeField]
    private Ease popEase =
        Ease.OutBack;

    [SerializeField]
    private Ease settleEase =
        Ease.OutQuad;

    [Header("Fade")]

    [Tooltip(
        "종료 애니메이션 중 어느 시점부터 " +
        "투명해지기 시작할지 나타냅니다."
    )]
    [SerializeField, Range(0f, 0.95f)]
    private float fadeStartRatio = 0.35f;

    [SerializeField]
    private Ease fadeEase =
        Ease.InQuad;

    [Header("Sorting")]
    [SerializeField]
    private string sortingLayerName =
        "Default";

    [SerializeField]
    private int sortingOrder = 200;

    public string StyleId =>
        styleId;

    public BallDamagePopupView PopupPrefabOverride =>
        popupPrefabOverride;

    public BallDamageTextAggregationMode
        AggregationMode =>
            aggregationMode;

    public float AggregationWindow =>
        aggregationWindow;

    public TMP_FontAsset FontAsset =>
        fontAsset;

    public Material FontMaterialPreset =>
        fontMaterialPreset;

    public Color TextColor =>
        textColor;

    public float FontSize =>
        fontSize;

    public FontStyles FontStyle =>
        fontStyle;

    public string Prefix =>
        prefix;

    public string Suffix =>
        suffix;

    public Vector2 SpawnOffset =>
        spawnOffset;

    public float HorizontalJitter =>
        horizontalJitter;

    public float WorldZ =>
        worldZ;

    public float Duration =>
        duration;

    public float RiseDistance =>
        riseDistance;

    public Vector2 HorizontalDriftRange =>
        horizontalDriftRange;

    public Ease MoveEase =>
        moveEase;

    public float StartScale =>
        startScale;

    public float PeakScale =>
        peakScale;

    public float EndScale =>
        endScale;

    public float PopDurationRatio =>
        popDurationRatio;

    public Ease PopEase =>
        popEase;

    public Ease SettleEase =>
        settleEase;

    public float FadeStartRatio =>
        fadeStartRatio;

    public Ease FadeEase =>
        fadeEase;

    public string SortingLayerName =>
        sortingLayerName;

    public int SortingOrder =>
        sortingOrder;

    private void OnValidate()
    {
        aggregationWindow =
            Mathf.Max(
                aggregationWindow,
                0f
            );

        fontSize =
            Mathf.Max(
                fontSize,
                0.1f
            );

        horizontalJitter =
            Mathf.Max(
                horizontalJitter,
                0f
            );

        duration =
            Mathf.Max(
                duration,
                0.05f
            );

        riseDistance =
            Mathf.Max(
                riseDistance,
                0f
            );

        startScale =
            Mathf.Max(
                startScale,
                0.01f
            );

        peakScale =
            Mathf.Max(
                peakScale,
                0.01f
            );

        endScale =
            Mathf.Max(
                endScale,
                0.01f
            );

        popDurationRatio =
            Mathf.Clamp(
                popDurationRatio,
                0.01f,
                1f
            );

        fadeStartRatio =
            Mathf.Clamp(
                fadeStartRatio,
                0f,
                0.95f
            );

        if (horizontalDriftRange.x >
            horizontalDriftRange.y)
        {
            float previousMinimum =
                horizontalDriftRange.x;

            horizontalDriftRange.x =
                horizontalDriftRange.y;

            horizontalDriftRange.y =
                previousMinimum;
        }
    }
}