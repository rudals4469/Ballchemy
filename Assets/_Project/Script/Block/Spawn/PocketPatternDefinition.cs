using UnityEngine;

public enum PocketGrammarType
{
    Straight = 0,
    Elbow = 1,
    Zigzag = 2,
    DoubleBend = 3
}

public enum PocketLayoutCellType
{
    Tank = 0,
    Attacker = 1,
    Indestructible = 2,
    SpecialSlot = 3,
    NamedSlot = 4,
    TeleportSlot = 5
}

public enum MapTeleportMode
{
    None = 0,
    Optional = 1,
    Required = 2
}

[System.Serializable]
public sealed class PocketLayoutCell
{
    [SerializeField] private Vector2Int position;
    [SerializeField] private PocketLayoutCellType cellType;
    [SerializeField, Min(0)] private int pairGroupId;

    public Vector2Int Position => position;
    public PocketLayoutCellType CellType => cellType;
    public int PairGroupId => pairGroupId;

#if UNITY_EDITOR
    public void Configure(
        Vector2Int cellPosition,
        PocketLayoutCellType type,
        int teleportPairGroupId = 0)
    {
        position = cellPosition;
        cellType = type;
        pairGroupId = Mathf.Max(teleportPairGroupId, 0);
    }
#endif
}

[CreateAssetMenu(
    fileName = "PocketPattern_New",
    menuName = "Ballchemy/Block/Pocket Pattern Definition")]
public sealed class PocketPatternDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string patternId = "pocket_new";
    [SerializeField] private string displayName = "새 포켓";
    [SerializeField] private PocketGrammarType grammarType;

    [Header("Availability")]
    [SerializeField, Min(0)] private int minimumWaveIndex;
    [SerializeField] private int maximumWaveIndex = -1;
    [SerializeField, Min(5)] private int minimumRowCount = 5;
    [SerializeField, Min(0.01f)] private float selectionWeight = 1f;

    [Header("Shape")]
    [SerializeField, Range(2, 3)] private int minimumCorridorWidth = 2;
    [SerializeField, Range(2, 3)] private int maximumCorridorWidth = 3;
    [SerializeField, Range(0f, 1f)] private float pathNoiseChance = 0.35f;
    [SerializeField] private bool allowTranspose = true;

    [Header("Contents")]
    [SerializeField, Range(1, 4)] private int minimumAttackerCount = 1;
    [SerializeField, Range(1, 4)] private int maximumAttackerCount = 2;
    [SerializeField, Range(0f, 1f)] private float wallBreathingGapChance = 0.5f;
    [SerializeField] private bool preventFilledTwoByTwo = true;
    [SerializeField, Range(0, 3)] private int maximumIndestructibleAnchors = 2;
    [SerializeField, Range(0f, 1f)] private float indestructibleAnchorChance = 0.65f;

    [Header("Fixed Layout")]
    [SerializeField] private bool useFixedLayout;
    [SerializeField] private Vector2Int fixedLayoutSize = new Vector2Int(7, 5);
    [SerializeField] private bool allowHorizontalMirror = true;
    [SerializeField] private MapTeleportMode teleportMode;
    [SerializeField] private PocketLayoutCell[] fixedCells;

    public string PatternId => patternId;
    public string DisplayName => displayName;
    public PocketGrammarType GrammarType => grammarType;
    public int MinimumWaveIndex => minimumWaveIndex;
    public int MaximumWaveIndex => maximumWaveIndex;
    public int MinimumRowCount => minimumRowCount;
    public float SelectionWeight => selectionWeight;
    public int MinimumCorridorWidth => minimumCorridorWidth;
    public int MaximumCorridorWidth => maximumCorridorWidth;
    public float PathNoiseChance => pathNoiseChance;
    public bool AllowTranspose => allowTranspose;
    public int MinimumAttackerCount => minimumAttackerCount;
    public int MaximumAttackerCount => maximumAttackerCount;
    public float WallBreathingGapChance => wallBreathingGapChance;
    public bool PreventFilledTwoByTwo => preventFilledTwoByTwo;
    public int MaximumIndestructibleAnchors => maximumIndestructibleAnchors;
    public float IndestructibleAnchorChance => indestructibleAnchorChance;
    public bool UseFixedLayout => useFixedLayout;
    public Vector2Int FixedLayoutSize => fixedLayoutSize;
    public bool AllowHorizontalMirror => allowHorizontalMirror;
    public MapTeleportMode TeleportMode => teleportMode;
    public PocketLayoutCell[] FixedCells => fixedCells;

    public bool IsAvailable(int rowCount, int waveIndex) =>
        rowCount >= minimumRowCount &&
        waveIndex >= minimumWaveIndex &&
        (maximumWaveIndex < 0 || waveIndex <= maximumWaveIndex) &&
        (!useFixedLayout ||
         (fixedLayoutSize.x <= 0 || fixedLayoutSize.y <= 0 ||
          (fixedLayoutSize.x >= 5 && fixedLayoutSize.y <= rowCount)));

#if UNITY_EDITOR
    public void ConfigureFixedLayout(
        string id,
        string title,
        int minimumStageIndex,
        int maximumStageIndex,
        Vector2Int size,
        PocketLayoutCell[] cells,
        MapTeleportMode mapTeleportMode = MapTeleportMode.None)
    {
        patternId = id;
        displayName = title;
        minimumWaveIndex = minimumStageIndex;
        maximumWaveIndex = maximumStageIndex;
        minimumRowCount = size.y;
        selectionWeight = 1f;
        useFixedLayout = true;
        fixedLayoutSize = size;
        allowHorizontalMirror = true;
        allowTranspose = false;
        pathNoiseChance = 0f;
        wallBreathingGapChance = 0f;
        preventFilledTwoByTwo = true;
        maximumIndestructibleAnchors = 0;
        indestructibleAnchorChance = 0f;
        fixedCells = cells;
        teleportMode = mapTeleportMode;
    }
#endif

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(patternId)) patternId = name;
        if (string.IsNullOrWhiteSpace(displayName)) displayName = name;
        minimumWaveIndex = Mathf.Max(minimumWaveIndex, 0);
        maximumWaveIndex = maximumWaveIndex < 0
            ? -1
            : Mathf.Max(maximumWaveIndex, minimumWaveIndex);
        minimumRowCount = Mathf.Max(minimumRowCount, 5);
        selectionWeight = Mathf.Max(selectionWeight, 0.01f);
        minimumCorridorWidth = Mathf.Clamp(minimumCorridorWidth, 2, 3);
        maximumCorridorWidth = Mathf.Clamp(
            maximumCorridorWidth, minimumCorridorWidth, 3);
        minimumAttackerCount = Mathf.Clamp(minimumAttackerCount, 1, 4);
        maximumAttackerCount = Mathf.Clamp(
            maximumAttackerCount, minimumAttackerCount, 4);
        pathNoiseChance = Mathf.Clamp01(pathNoiseChance);
        wallBreathingGapChance = Mathf.Clamp01(wallBreathingGapChance);
        maximumIndestructibleAnchors = Mathf.Clamp(
            maximumIndestructibleAnchors, 0, 3);
        indestructibleAnchorChance = Mathf.Clamp01(
            indestructibleAnchorChance);
        fixedLayoutSize = new Vector2Int(
            Mathf.Max(fixedLayoutSize.x, 1),
            Mathf.Max(fixedLayoutSize.y, 1));
    }
}
