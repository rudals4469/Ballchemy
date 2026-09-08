using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public sealed class RicochetPocketPatternBuilder
{
    public sealed class Cell
    {
        public Vector2Int Position;
        public BlockSpawnRequest.CombatRole Role;
        public bool Indestructible;
        public PocketLayoutCellType CellType;
        public int PairGroupId;
        public MapTeleportMode TeleportMode;
    }

    [SerializeField] private List<PocketPatternDefinition> patternDefinitions =
        new List<PocketPatternDefinition>();

    [NonSerialized] private PocketPatternDefinition[] resourceDefinitions;
    [NonSerialized] private string lastFixedPatternId;
    [NonSerialized] private PocketPatternDefinition preparedFixedLayout;
    [NonSerialized] private bool? preparedMirrorOverride;

    public bool HasPreparedFixedLayout => preparedFixedLayout != null;

    public int PrepareFixedLayout(
        int columns,
        int maximumRows,
        int stageIndex)
    {
        if (preparedFixedLayout == null)
        {
            preparedFixedLayout = SelectFixedDefinition(
                columns, maximumRows, stageIndex);
            preparedMirrorOverride = null;
        }
        return preparedFixedLayout != null
            ? preparedFixedLayout.FixedLayoutSize.y
            : 0;
    }

    public bool PrepareFixedLayout(
        PocketPatternDefinition definition,
        bool mirrorHorizontally,
        int columns,
        int maximumRows)
    {
        if (definition == null || !definition.UseFixedLayout ||
            definition.FixedLayoutSize.x > columns ||
            definition.FixedLayoutSize.y > maximumRows)
        {
            preparedFixedLayout = null;
            preparedMirrorOverride = null;
            return false;
        }

        preparedFixedLayout = definition;
        preparedMirrorOverride = mirrorHorizontally;
        return true;
    }

    public bool CanBuild(int columns, int rows) =>
        columns >= 5 && rows >= 5;

    public List<Cell> Build(int columns, int rows, int waveIndex = 0)
    {
        if (!CanBuild(columns, rows)) return new List<Cell>();

        PocketPatternDefinition definition = preparedFixedLayout;
        bool? mirrorOverride = preparedMirrorOverride;
        preparedFixedLayout = null;
        preparedMirrorOverride = null;
        if (definition == null)
            definition = SelectFixedDefinition(columns, rows, waveIndex);

        if (definition != null)
        {
            List<Cell> fixedLayout = BuildFixedLayout(
                definition, columns, rows, mirrorOverride);
            if (fixedLayout.Count > 0) return fixedLayout;
        }

        return BuildSafeFallback(columns, rows);
    }

    private PocketPatternDefinition SelectFixedDefinition(
        int columns,
        int rows,
        int stageIndex)
    {
        List<PocketPatternDefinition> available =
            new List<PocketPatternDefinition>();
        IReadOnlyList<PocketPatternDefinition> source = GetDefinitions();
        for (int i = 0; i < source.Count; i++)
        {
            PocketPatternDefinition definition = source[i];
            if (definition == null || !definition.UseFixedLayout ||
                !definition.IsAvailable(rows, stageIndex) ||
                definition.FixedLayoutSize.x > columns ||
                definition.FixedLayoutSize.y > rows)
                continue;
            available.Add(definition);
        }
        if (available.Count == 0) return null;

        if (available.Count > 1 && !string.IsNullOrEmpty(lastFixedPatternId))
            available.RemoveAll(definition =>
                definition.PatternId == lastFixedPatternId);

        float totalWeight = 0f;
        for (int i = 0; i < available.Count; i++)
            totalWeight += available[i].SelectionWeight;
        float roll = Random.value * Mathf.Max(totalWeight, 0.01f);
        for (int i = 0; i < available.Count; i++)
        {
            roll -= available[i].SelectionWeight;
            if (roll > 0f) continue;
            lastFixedPatternId = available[i].PatternId;
            return available[i];
        }

        PocketPatternDefinition fallback = available[available.Count - 1];
        lastFixedPatternId = fallback.PatternId;
        return fallback;
    }

    private static List<Cell> BuildSafeFallback(int columns, int rows)
    {
        List<Cell> result = new List<Cell>();
        int width = Mathf.Min(columns, 5);
        int height = Mathf.Min(rows, 5);
        int offsetX = Mathf.Max((columns - width) / 2, 0);
        int offsetY = Mathf.Max((rows - height) / 2, 0);

        for (int y = 0; y < height; y++)
            result.Add(new Cell
            {
                Position = new Vector2Int(offsetX, offsetY + y),
                Role = BlockSpawnRequest.CombatRole.Tank
            });
        for (int x = 1; x < width; x++)
            result.Add(new Cell
            {
                Position = new Vector2Int(offsetX + x, offsetY + height - 1),
                Role = BlockSpawnRequest.CombatRole.Tank
            });
        if (width >= 3 && height >= 3)
            result.Add(new Cell
            {
                Position = new Vector2Int(offsetX + 2, offsetY + 2),
                Role = BlockSpawnRequest.CombatRole.Attacker
            });
        return result;
    }

    private static List<Cell> BuildFixedLayout(
        PocketPatternDefinition definition,
        int columns,
        int rows,
        bool? mirrorOverride = null)
    {
        List<Cell> result = new List<Cell>();
        Vector2Int size = definition.FixedLayoutSize;
        PocketLayoutCell[] source = definition.FixedCells;
        if (source == null || source.Length == 0 ||
            size.x > columns || size.y > rows)
            return result;

        int offsetX = Mathf.Max((columns - size.x) / 2, 0);
        int offsetY = Mathf.Max((rows - size.y) / 2, 0);
        bool mirror = mirrorOverride ??
            (definition.AllowHorizontalMirror && Random.value < 0.5f);
        for (int i = 0; i < source.Length; i++)
        {
            PocketLayoutCell layoutCell = source[i];
            if (layoutCell == null) continue;
            Vector2Int local = layoutCell.Position;
            if (mirror) local.x = size.x - 1 - local.x;
            if (local.x < 0 || local.x >= size.x ||
                local.y < 0 || local.y >= size.y)
                continue;

            PocketLayoutCellType type = layoutCell.CellType;
            Vector2Int finalPosition = local + new Vector2Int(offsetX, offsetY);
            if (type == PocketLayoutCellType.Entrance ||
                type == PocketLayoutCellType.Exit)
            {
                result.RemoveAll(cell => cell.Position == finalPosition);
                Vector2Int throat = ResolveOpeningThroat(local, size);
                Vector2Int finalThroat = throat +
                    new Vector2Int(offsetX, offsetY);
                result.RemoveAll(cell => cell.Position == finalThroat);
            }
            result.Add(new Cell
            {
                Position = finalPosition,
                Role = type == PocketLayoutCellType.Attacker
                    ? BlockSpawnRequest.CombatRole.Attacker
                    : BlockSpawnRequest.CombatRole.Tank,
                Indestructible = type == PocketLayoutCellType.Indestructible,
                CellType = type,
                PairGroupId = layoutCell.PairGroupId,
                TeleportMode = definition.TeleportMode
            });
        }
        return result;
    }

    private static Vector2Int ResolveOpeningThroat(
        Vector2Int opening,
        Vector2Int size)
    {
        if (opening.y == 0)
            return new Vector2Int(opening.x, Mathf.Min(1, size.y - 1));
        if (opening.y == size.y - 1)
            return new Vector2Int(opening.x, Mathf.Max(size.y - 2, 0));
        if (opening.x == 0)
            return new Vector2Int(Mathf.Min(1, size.x - 1), opening.y);
        return new Vector2Int(Mathf.Max(size.x - 2, 0), opening.y);
    }

    private IReadOnlyList<PocketPatternDefinition> GetDefinitions()
    {
        if (patternDefinitions != null && patternDefinitions.Count > 0)
            return patternDefinitions;
        if (resourceDefinitions == null || resourceDefinitions.Length == 0)
            resourceDefinitions = Resources.LoadAll<PocketPatternDefinition>(
                "PocketPatterns");
        return resourceDefinitions;
    }
}
