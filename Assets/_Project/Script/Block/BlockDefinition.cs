using UnityEngine;

[CreateAssetMenu(
    fileName = "BlockDefinition_New",
    menuName = "Ballchemy/Blocks/Block Definition"
)]
public sealed class BlockDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string blockId = "normal_01";

    [SerializeField]
    private string displayName = "Normal Block";

    [SerializeField]
    private BlockType blockType =
        BlockType.Normal;

    [Header("Visual")]
    [SerializeField]
    private Sprite sprite;

    [Tooltip(
        "스프라이트에 적용할 색상입니다. " +
        "기본값은 흰색입니다."
    )]
    [SerializeField]
    private Color color = Color.white;

    [Tooltip(
        "블록 이미지의 로컬 크기 배율입니다."
    )]
    [SerializeField]
    private Vector3 visualScale =
        Vector3.one;

    [Header("Random Selection")]
    [Tooltip(
        "같은 종류의 블록 중 랜덤으로 선택될 확률의 가중치입니다. " +
        "세 블록을 동일하게 뽑으려면 전부 1로 설정합니다."
    )]
    [SerializeField, Min(0)]
    private int selectionWeight = 1;

    public string BlockId =>
        blockId;

    public string DisplayName =>
        displayName;

    public BlockType BlockType =>
        blockType;

    public Sprite Sprite =>
        sprite;

    public Color Color =>
        color;

    public Vector3 VisualScale =>
        visualScale;

    public int SelectionWeight =>
        selectionWeight;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(blockId))
        {
            blockId = name;
        }

        selectionWeight =
            Mathf.Max(
                selectionWeight,
                0
            );

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
    }
}