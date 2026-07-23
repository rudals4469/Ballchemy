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

    [Header("Grid Size")]
    [Tooltip(
        "블록이 그리드에서 차지하는 크기입니다. " +
        "X는 가로 칸 수, Y는 세로 칸 수입니다."
    )]
    [SerializeField]
    private Vector2Int gridSize =
        Vector2Int.one;

    [Header("Visual")]
    [SerializeField]
    private Sprite sprite;

    [SerializeField]
    private Color color =
        Color.white;

    [Tooltip(
        "계산된 블록 크기에 추가로 적용할 " +
        "외형 배율입니다."
    )]
    [SerializeField]
    private Vector3 visualScale =
        Vector3.one;

    [Header("Random Selection")]
    [SerializeField, Min(0)]
    private int selectionWeight = 1;

    public string BlockId =>
        blockId;

    public string DisplayName =>
        displayName;

    public BlockType BlockType =>
        blockType;

    public Vector2Int GridSize =>
        new Vector2Int(
            Mathf.Max(1, gridSize.x),
            Mathf.Max(1, gridSize.y)
        );

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
        if (string.IsNullOrWhiteSpace(
                blockId))
        {
            blockId = name;
        }

        gridSize.x =
            Mathf.Max(
                gridSize.x,
                1
            );

        gridSize.y =
            Mathf.Max(
                gridSize.y,
                1
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

        selectionWeight =
            Mathf.Max(
                selectionWeight,
                0
            );
    }
}