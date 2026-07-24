using UnityEngine;

[CreateAssetMenu(
    fileName = "BlockDefinition",
    menuName = "Ballchemy/Blocks/Block Definition"
)]
public sealed class BlockDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string blockId;

    [SerializeField]
    private string displayName;

    [SerializeField]
    private BlockType blockType =
        BlockType.Normal;

    [Header("Behaviour")]
    [SerializeField]
    private BlockDestructionRule destructionRule =
        BlockDestructionRule.Breakable;

    [Header("Grid")]
    [SerializeField]
    private Vector2Int gridSize =
        Vector2Int.one;

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
    [SerializeField, Min(0)]
    private int selectionWeight = 1;

    public string BlockId =>
        blockId;

    public string DisplayName =>
        displayName;

    public BlockType BlockType =>
        blockType;

    public BlockDestructionRule DestructionRule =>
        destructionRule;

    public Vector2Int GridSize =>
        gridSize;

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