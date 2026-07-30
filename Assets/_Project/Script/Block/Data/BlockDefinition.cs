using UnityEngine;

[CreateAssetMenu(
    fileName = "BlockDefinition",
    menuName = "Ballchemy/Blocks/Block Definition"
)]
public sealed class BlockDefinition :
    ScriptableObject
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

    [Tooltip(
        "방 클리어 판정에서 이 블록이 맡는 역할입니다. " +
        "Auto는 BlockType에 따라 자동 결정합니다."
    )]
    [SerializeField]
    private BlockClearRole clearRole =
        BlockClearRole.Auto;

    [Header("Special Feedback")]
    [Tooltip(
        "Special 블록의 시각적 분류입니다. " +
        "일반 블록은 None을 사용합니다."
    )]
    [SerializeField]
    private SpecialBlockCategory specialCategory =
        SpecialBlockCategory.None;

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

    public BlockClearRole ClearRole =>
        ResolveClearRole();

    public SpecialBlockCategory SpecialCategory =>
        specialCategory;

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

    private BlockClearRole ResolveClearRole()
    {
        if (clearRole != BlockClearRole.Auto)
        {
            return clearRole;
        }

        switch (blockType)
        {
            case BlockType.Normal:
            case BlockType.Named:
            case BlockType.Boss:
                return BlockClearRole.RequiredEnemy;

            case BlockType.Special:
                return BlockClearRole.Optional;

            case BlockType.Pattern:
            default:
                return BlockClearRole.Ignore;
        }
    }

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

        if (blockType !=
            BlockType.Special)
        {
            specialCategory =
                SpecialBlockCategory.None;
        }
    }
}