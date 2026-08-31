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

    [SerializeField, Range(2, 3)] private int minimumCorridorWidth = 2;
    [SerializeField, Range(2, 3)] private int maximumCorridorWidth = 3;
    [SerializeField, Range(1, 3)] private int minimumAttackerCount = 1;
    [SerializeField, Range(1, 4)] private int maximumAttackerCount = 2;
    [SerializeField, Range(0, 3)] private int maximumIndestructibleAnchors = 2;
    [SerializeField, Range(0f, 1f)] private float indestructibleAnchorChance = 0.65f;
    [SerializeField, Range(0f, 1f)] private float pathNoiseChance = 0.45f;
    [SerializeField, Range(0f, 1f)] private float wallBreathingGapChance = 0.5f;
    [SerializeField] private List<PocketPatternDefinition> patternDefinitions =
        new List<PocketPatternDefinition>();

    [NonSerialized] private PocketPatternDefinition[] resourceDefinitions;
    [NonSerialized] private bool runtimeAllowTranspose = true;
    [NonSerialized] private bool runtimePreventFilledTwoByTwo = true;
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

    public void Normalize(int columns, int rows)
    {
        minimumCorridorWidth = Mathf.Clamp(minimumCorridorWidth, 2, 3);
        maximumCorridorWidth = Mathf.Clamp(
            maximumCorridorWidth, minimumCorridorWidth, 3);
        minimumAttackerCount = Mathf.Max(minimumAttackerCount, 1);
        maximumAttackerCount = Mathf.Max(
            maximumAttackerCount, minimumAttackerCount);
        maximumIndestructibleAnchors = Mathf.Max(
            maximumIndestructibleAnchors, 0);
        indestructibleAnchorChance = Mathf.Clamp01(
            indestructibleAnchorChance);
        pathNoiseChance = Mathf.Clamp01(pathNoiseChance);
        wallBreathingGapChance = Mathf.Clamp01(wallBreathingGapChance);
    }

    public bool CanBuild(int columns, int rows) =>
        columns >= 5 && rows >= 5;

    public List<Cell> Build(int columns, int rows, int waveIndex = 0)
    {
        Normalize(columns, rows);
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

    private List<Cell> BuildCandidate(
        int columns,
        int rows,
        int waveIndex,
        PocketGrammarType grammar,
        out bool isValid)
    {
        List<Cell> result = new List<Cell>();

        bool transpose = runtimeAllowTranspose && Random.value < 0.5f;
        int width = transpose ? rows : columns;
        int height = transpose ? columns : rows;
        List<Vector2Int> path = BuildPath(grammar, width, height);
        int corridorWidth = Random.Range(
            minimumCorridorWidth, maximumCorridorWidth + 1);
        HashSet<Vector2Int> corridor = BuildCorridor(
            path, width, height, corridorWidth);
        HashSet<Vector2Int> wall = BuildWall(corridor, width, height);

        OpenEntrance(wall, path, corridorWidth);
        int attackerBudget = Mathf.Clamp(
            minimumAttackerCount +
            Mathf.Max(rows - 5, 0) / 3 +
            Mathf.Max(waveIndex, 0) / 5,
            minimumAttackerCount,
            maximumAttackerCount);
        List<Vector2Int> attackerPositions = SelectAttackers(
            path, corridor, wall, attackerBudget);
        ReinforceAttackerPockets(
            wall, path, attackerPositions, width, height);
        if (runtimePreventFilledTwoByTwo)
        {
            PreventFilledTwoByTwoClusters(
                wall, attackerPositions, width, height);
        }
        CarveBreathingGaps(
            wall, attackerPositions, width, height);
        EnsurePocketEntrances(
            wall, path, attackerPositions, width, height);
        isValid = ValidatePocket(
            wall, path, attackerPositions, width, height);
        HashSet<Vector2Int> anchors = SelectAnchors(
            wall, corridor, path, maximumIndestructibleAnchors, transpose);

        foreach (Vector2Int position in wall)
        {
            result.Add(new Cell
            {
                Position = Transform(position, transpose),
                Role = BlockSpawnRequest.CombatRole.Tank,
                Indestructible = anchors.Contains(position)
            });
        }

        for (int i = 0; i < attackerPositions.Count; i++)
        {
            result.Add(new Cell
            {
                Position = Transform(attackerPositions[i], transpose),
                Role = BlockSpawnRequest.CombatRole.Attacker,
                Indestructible = false
            });
        }

        return result;
    }

    private static PocketGrammarType SelectGrammar(
        int rows,
        int waveIndex,
        int attempt)
    {
        int complexity = Mathf.Clamp(
            (Mathf.Max(rows - 5, 0) / 2) +
            (Mathf.Max(waveIndex, 0) / 4),
            0,
            (int)PocketGrammarType.DoubleBend);
        int maximumGrammar = Mathf.Clamp(
            1 + complexity,
            (int)PocketGrammarType.Elbow,
            (int)PocketGrammarType.DoubleBend);

        if (attempt > 0)
            return (PocketGrammarType)(attempt % (maximumGrammar + 1));

        return (PocketGrammarType)Random.Range(0, maximumGrammar + 1);
    }

    private PocketPatternDefinition SelectDefinition(
        int rows,
        int waveIndex,
        int attempt)
    {
        List<PocketPatternDefinition> available =
            new List<PocketPatternDefinition>();
        IReadOnlyList<PocketPatternDefinition> source = GetDefinitions();
        for (int i = 0; i < source.Count; i++)
        {
            PocketPatternDefinition definition = source[i];
            if (definition != null && definition.IsAvailable(rows, waveIndex))
                available.Add(definition);
        }
        if (available.Count == 0) return null;

        bool hasFixedLayout = available.Exists(
            definition => definition.UseFixedLayout);
        if (hasFixedLayout)
        {
            available.RemoveAll(definition => !definition.UseFixedLayout);
            if (available.Count > 1 &&
                !string.IsNullOrEmpty(lastFixedPatternId))
            {
                available.RemoveAll(definition =>
                    definition.PatternId == lastFixedPatternId);
            }
        }

        if (attempt > 0)
            return RememberSelection(
                available[(attempt - 1) % available.Count]);

        float totalWeight = 0f;
        for (int i = 0; i < available.Count; i++)
            totalWeight += available[i].SelectionWeight;
        float roll = Random.value * totalWeight;
        for (int i = 0; i < available.Count; i++)
        {
            roll -= available[i].SelectionWeight;
            if (roll <= 0f) return RememberSelection(available[i]);
        }
        return RememberSelection(available[available.Count - 1]);
    }

    private PocketPatternDefinition RememberSelection(
        PocketPatternDefinition definition)
    {
        if (definition != null && definition.UseFixedLayout)
            lastFixedPatternId = definition.PatternId;
        return definition;
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

    private void ApplyDefinition(PocketPatternDefinition definition)
    {
        minimumCorridorWidth = definition.MinimumCorridorWidth;
        maximumCorridorWidth = definition.MaximumCorridorWidth;
        minimumAttackerCount = definition.MinimumAttackerCount;
        maximumAttackerCount = definition.MaximumAttackerCount;
        maximumIndestructibleAnchors = definition.MaximumIndestructibleAnchors;
        indestructibleAnchorChance = definition.IndestructibleAnchorChance;
        pathNoiseChance = definition.PathNoiseChance;
        wallBreathingGapChance = definition.WallBreathingGapChance;
        runtimeAllowTranspose = definition.AllowTranspose;
        runtimePreventFilledTwoByTwo = definition.PreventFilledTwoByTwo;
    }

    private List<Vector2Int> BuildPath(
        PocketGrammarType grammar,
        int width,
        int height)
    {
        switch (grammar)
        {
            case PocketGrammarType.Straight:
                return BuildStraightPath(width, height);
            case PocketGrammarType.Zigzag:
                return BuildZigzagPath(width, height, false);
            case PocketGrammarType.DoubleBend:
                return BuildZigzagPath(width, height, true);
            default:
                return BuildElbowPath(width, height);
        }
    }

    private static void ReinforceAttackerPockets(
        HashSet<Vector2Int> wall,
        IReadOnlyList<Vector2Int> path,
        IReadOnlyList<Vector2Int> attackers,
        int width,
        int height)
    {
        if (wall == null || path == null || attackers == null) return;

        HashSet<Vector2Int> attackerSet =
            new HashSet<Vector2Int>(attackers);
        Vector2Int[] directions =
        {
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.down
        };

        for (int i = 0; i < attackers.Count; i++)
        {
            Vector2Int attacker = attackers[i];
            int pathIndex = FindLastPathIndex(path, attacker);
            Vector2Int entranceDirection = pathIndex > 0
                ? path[pathIndex - 1] - attacker
                : Vector2Int.down;

            if (Mathf.Abs(entranceDirection.x) > Mathf.Abs(entranceDirection.y))
                entranceDirection = new Vector2Int(Math.Sign(entranceDirection.x), 0);
            else
                entranceDirection = new Vector2Int(0, Math.Sign(entranceDirection.y));

            // 공격형 블럭을 탱커 벽 세 면으로 감싸고,
            // 이전 경로를 향한 한 면만 공이 들어오는 입구로 남긴다.
            for (int directionIndex = 0;
                 directionIndex < directions.Length;
                 directionIndex++)
            {
                Vector2Int direction = directions[directionIndex];
                if (direction == entranceDirection) continue;

                Vector2Int wallPosition = attacker + direction;
                if (!IsInside(wallPosition, width, height) ||
                    attackerSet.Contains(wallPosition))
                    continue;

                wall.Add(wallPosition);
            }

            wall.Remove(attacker);
            wall.Remove(attacker + entranceDirection);
        }
    }

    private static int FindLastPathIndex(
        IReadOnlyList<Vector2Int> path,
        Vector2Int target)
    {
        for (int i = path.Count - 1; i >= 0; i--)
        {
            if (path[i] == target) return i;
        }

        return -1;
    }

    private static void PreventFilledTwoByTwoClusters(
        HashSet<Vector2Int> wall,
        IReadOnlyList<Vector2Int> attackers,
        int width,
        int height)
    {
        if (wall == null || wall.Count == 0) return;

        HashSet<Vector2Int> protectedCells = new HashSet<Vector2Int>();
        HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>(wall);
        for (int i = 0; i < attackers.Count; i++)
        {
            Vector2Int attacker = attackers[i];
            occupiedCells.Add(attacker);
            protectedCells.Add(attacker + Vector2Int.left);
            protectedCells.Add(attacker + Vector2Int.right);
            protectedCells.Add(attacker + Vector2Int.up);
            protectedCells.Add(attacker + Vector2Int.down);
        }

        int safety = Mathf.Max(width * height, 1);
        while (safety-- > 0)
        {
            Vector2Int removable;
            if (!TryFindDenseRectangleCell(
                    occupiedCells, wall, protectedCells,
                    width, height, 2, 2,
                    out removable))
                break;

            wall.Remove(removable);
            occupiedCells.Remove(removable);
        }
    }

    private static bool TryFindDenseRectangleCell(
        HashSet<Vector2Int> occupiedCells,
        HashSet<Vector2Int> removableWalls,
        HashSet<Vector2Int> protectedCells,
        int boardWidth,
        int boardHeight,
        int rectangleWidth,
        int rectangleHeight,
        out Vector2Int removable)
    {
        for (int y = 0; y <= boardHeight - rectangleHeight; y++)
        for (int x = 0; x <= boardWidth - rectangleWidth; x++)
        {
            bool isFull = true;
            for (int offsetY = 0; offsetY < rectangleHeight && isFull; offsetY++)
            for (int offsetX = 0; offsetX < rectangleWidth; offsetX++)
            {
                if (!occupiedCells.Contains(new Vector2Int(x + offsetX, y + offsetY)))
                {
                    isFull = false;
                    break;
                }
            }
            if (!isFull) continue;

            // 중앙에 가까운 비필수 벽을 우선 제거해 긴 외곽선은 남긴다.
            for (int offsetY = rectangleHeight - 1; offsetY >= 0; offsetY--)
            for (int offsetX = rectangleWidth - 1; offsetX >= 0; offsetX--)
            {
                Vector2Int candidate =
                    new Vector2Int(x + offsetX, y + offsetY);
                if (protectedCells.Contains(candidate) ||
                    !removableWalls.Contains(candidate))
                    continue;
                removable = candidate;
                return true;
            }

            // 네 칸이 모두 포켓 핵심 벽이어도 완성된 2x2는 허용하지 않는다.
            // 이 경우 한 칸을 제거하고 이후 포켓 검증에서 진입성과 벽 수를
            // 다시 확인한다.
            for (int offsetY = rectangleHeight - 1; offsetY >= 0; offsetY--)
            for (int offsetX = rectangleWidth - 1; offsetX >= 0; offsetX--)
            {
                Vector2Int candidate =
                    new Vector2Int(x + offsetX, y + offsetY);
                if (!removableWalls.Contains(candidate)) continue;
                removable = candidate;
                return true;
            }
        }

        removable = default;
        return false;
    }

    private void CarveBreathingGaps(
        HashSet<Vector2Int> wall,
        IReadOnlyList<Vector2Int> attackers,
        int width,
        int height)
    {
        if (wall == null || wall.Count == 0) return;

        HashSet<Vector2Int> protectedCells = new HashSet<Vector2Int>();
        for (int i = 0; i < attackers.Count; i++)
        {
            Vector2Int attacker = attackers[i];
            protectedCells.Add(attacker + Vector2Int.left);
            protectedCells.Add(attacker + Vector2Int.right);
            protectedCells.Add(attacker + Vector2Int.up);
            protectedCells.Add(attacker + Vector2Int.down);
        }

        Vector2Int[] directions =
        {
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.down
        };
        List<Vector2Int> candidates = new List<Vector2Int>();
        foreach (Vector2Int cell in wall)
        {
            if (protectedCells.Contains(cell)) continue;

            int touchingWalls = 0;
            for (int i = 0; i < directions.Length; i++)
            {
                if (wall.Contains(cell + directions[i])) touchingWalls++;
            }

            if (touchingWalls >= 3) candidates.Add(cell);
        }

        Shuffle(candidates);
        HashSet<Vector2Int> removed = new HashSet<Vector2Int>();
        for (int i = 0; i < candidates.Count; i++)
        {
            Vector2Int candidate = candidates[i];
            if (!IsInside(candidate, width, height) ||
                Random.value > wallBreathingGapChance)
                continue;

            bool touchesAnotherGap = false;
            for (int directionIndex = 0;
                 directionIndex < directions.Length;
                 directionIndex++)
            {
                if (removed.Contains(candidate + directions[directionIndex]))
                {
                    touchesAnotherGap = true;
                    break;
                }
            }

            if (touchesAnotherGap) continue;
            wall.Remove(candidate);
            removed.Add(candidate);
        }
    }

    private static void EnsurePocketEntrances(
        HashSet<Vector2Int> wall,
        IReadOnlyList<Vector2Int> path,
        IReadOnlyList<Vector2Int> attackers,
        int width,
        int height)
    {
        if (wall == null || path == null || attackers == null) return;

        for (int i = 0; i < attackers.Count; i++)
        {
            Vector2Int attacker = attackers[i];
            int pathIndex = FindLastPathIndex(path, attacker);
            Vector2Int entranceDirection = pathIndex > 0
                ? path[pathIndex - 1] - attacker
                : Vector2Int.down;

            if (Mathf.Abs(entranceDirection.x) > Mathf.Abs(entranceDirection.y))
                entranceDirection = new Vector2Int(Math.Sign(entranceDirection.x), 0);
            else
                entranceDirection = new Vector2Int(0, Math.Sign(entranceDirection.y));

            // 포켓 입구 바로 뒤가 다른 벽으로 막히지 않도록
            // 공격형 블럭 앞쪽으로 최소 두 칸의 진입 통로를 보장한다.
            for (int distance = 1; distance <= 2; distance++)
            {
                Vector2Int entranceCell =
                    attacker + entranceDirection * distance;
                if (IsInside(entranceCell, width, height))
                    wall.Remove(entranceCell);
            }
        }
    }

    private bool ValidatePocket(
        HashSet<Vector2Int> wall,
        IReadOnlyList<Vector2Int> path,
        IReadOnlyList<Vector2Int> attackers,
        int width,
        int height)
    {
        if (wall == null || attackers == null || attackers.Count == 0)
            return false;
        if (wall.Count < attackers.Count * 2 + 4)
            return false;
        if (wall.Count > Mathf.CeilToInt(width * height * 0.78f))
            return false;
        if (runtimePreventFilledTwoByTwo)
        {
            HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>(wall);
            for (int i = 0; i < attackers.Count; i++)
                occupiedCells.Add(attackers[i]);
            if (ContainsFilledRectangle(
                    occupiedCells, width, height, 2, 2))
                return false;
        }

        Vector2Int[] directions =
        {
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.down
        };
        for (int attackerIndex = 0;
             attackerIndex < attackers.Count;
             attackerIndex++)
        {
            Vector2Int attacker = attackers[attackerIndex];
            int pathIndex = FindLastPathIndex(path, attacker);
            if (pathIndex <= 0) return false;

            Vector2Int entranceDirection = path[pathIndex - 1] - attacker;
            if (Mathf.Abs(entranceDirection.x) > Mathf.Abs(entranceDirection.y))
                entranceDirection = new Vector2Int(Math.Sign(entranceDirection.x), 0);
            else
                entranceDirection = new Vector2Int(0, Math.Sign(entranceDirection.y));

            int surroundingWalls = 0;
            for (int i = 0; i < directions.Length; i++)
            {
                if (directions[i] != entranceDirection &&
                    wall.Contains(attacker + directions[i]))
                    surroundingWalls++;
            }
            if (surroundingWalls < 2) return false;

            for (int distance = 1; distance <= 2; distance++)
            {
                Vector2Int entranceCell = attacker + entranceDirection * distance;
                if (IsInside(entranceCell, width, height) &&
                    wall.Contains(entranceCell))
                    return false;
            }
        }

        return true;
    }

    private static bool ContainsFilledRectangle(
        HashSet<Vector2Int> wall,
        int boardWidth,
        int boardHeight,
        int rectangleWidth,
        int rectangleHeight)
    {
        for (int y = 0; y <= boardHeight - rectangleHeight; y++)
        for (int x = 0; x <= boardWidth - rectangleWidth; x++)
        {
            bool isFull = true;
            for (int offsetY = 0; offsetY < rectangleHeight && isFull; offsetY++)
            for (int offsetX = 0; offsetX < rectangleWidth; offsetX++)
            {
                if (!wall.Contains(new Vector2Int(x + offsetX, y + offsetY)))
                {
                    isFull = false;
                    break;
                }
            }
            if (isFull) return true;
        }

        return false;
    }

    private List<Vector2Int> BuildStraightPath(int width, int height)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        int x = Random.Range(1, width - 1);
        AddConnected(path, new Vector2Int(x, 0));
        for (int y = 1; y <= height - 2; y++)
        {
            if (y > 1 && y < height - 2 &&
                Random.value < pathNoiseChance * 0.35f)
                x = Mathf.Clamp(x + (Random.value < 0.5f ? -1 : 1), 1, width - 2);
            AddConnected(path, new Vector2Int(x, y));
        }
        return path;
    }

    private List<Vector2Int> BuildZigzagPath(
        int width,
        int height,
        bool doubleBend)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        int left = 1;
        int right = width - 2;
        bool startLeft = Random.value < 0.5f;
        int firstX = startLeft ? left : right;
        int secondX = startLeft ? right : left;
        int firstBend = Mathf.Clamp(height / 3, 2, height - 2);
        int secondBend = Mathf.Clamp((height * 2) / 3, firstBend, height - 2);

        AddConnected(path, new Vector2Int(
            Mathf.Clamp((firstX + secondX) / 2, 1, width - 2), 0));
        AddConnected(path, new Vector2Int(firstX, firstBend));
        AddConnected(path, new Vector2Int(secondX, secondBend));
        if (doubleBend)
            AddConnected(path, new Vector2Int(firstX, height - 2));

        return path;
    }

    private List<Vector2Int> BuildElbowPath(int width, int height)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        int startX = Random.Range(1, width - 1);
        int bendY = Mathf.Clamp(
            Random.Range(2, Mathf.Max(3, height - 1)), 2, height - 2);
        int horizontalDirection = startX < width / 2 ? 1 : -1;
        if (Random.value < 0.35f) horizontalDirection *= -1;

        int x = startX;
        for (int y = 0; y <= bendY; y++)
        {
            if (y > 1 && y < bendY &&
                Random.value < pathNoiseChance)
            {
                int shift = Random.value < 0.5f ? -1 : 1;
                x = Mathf.Clamp(x + shift, 1, width - 2);
            }
            AddConnected(path, new Vector2Int(x, y));
        }

        int targetX = horizontalDirection > 0 ? width - 2 : 1;
        int step = targetX >= x ? 1 : -1;
        for (int currentX = x + step;
             currentX != targetX + step;
             currentX += step)
        {
            int noisyY = bendY;
            if (currentX != targetX && Random.value < pathNoiseChance * 0.5f)
                noisyY = Mathf.Clamp(bendY + (Random.value < 0.5f ? -1 : 1),
                    1, height - 2);
            AddConnected(path, new Vector2Int(currentX, noisyY));
        }
        return path;
    }

    private static void AddConnected(
        List<Vector2Int> path, Vector2Int destination)
    {
        if (path.Count == 0)
        {
            path.Add(destination);
            return;
        }
        Vector2Int current = path[path.Count - 1];
        while (current.x != destination.x)
        {
            current.x += Math.Sign(destination.x - current.x);
            if (path[path.Count - 1] != current) path.Add(current);
        }
        while (current.y != destination.y)
        {
            current.y += Math.Sign(destination.y - current.y);
            if (path[path.Count - 1] != current) path.Add(current);
        }
    }

    private static HashSet<Vector2Int> BuildCorridor(
        IReadOnlyList<Vector2Int> path, int width, int height,
        int corridorWidth)
    {
        HashSet<Vector2Int> result = new HashSet<Vector2Int>();
        int radius = corridorWidth >= 3 ? 1 : 0;
        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int center = path[i];
            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (Mathf.Abs(dx) + Mathf.Abs(dy) > radius) continue;
                AddInside(result, center + new Vector2Int(dx, dy), width, height);
            }
            if (corridorWidth == 2)
            {
                Vector2Int direction = i + 1 < path.Count
                    ? path[i + 1] - center
                    : center - path[Mathf.Max(i - 1, 0)];
                Vector2Int side = new Vector2Int(-direction.y, direction.x);
                AddInside(result, center + side, width, height);
            }
        }
        return result;
    }

    private static HashSet<Vector2Int> BuildWall(
        HashSet<Vector2Int> corridor, int width, int height)
    {
        HashSet<Vector2Int> wall = new HashSet<Vector2Int>();
        Vector2Int[] directions =
        {
            Vector2Int.left, Vector2Int.right,
            Vector2Int.up, Vector2Int.down
        };
        foreach (Vector2Int cell in corridor)
        {
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int candidate = cell + directions[i];
                if (IsInside(candidate, width, height) &&
                    !corridor.Contains(candidate))
                    wall.Add(candidate);
            }
        }
        return wall;
    }

    private static void OpenEntrance(
        HashSet<Vector2Int> wall, IReadOnlyList<Vector2Int> path, int width)
    {
        int count = Mathf.Min(width + 1, path.Count);
        for (int i = 0; i < count; i++)
        {
            Vector2Int center = path[i];
            wall.Remove(center + Vector2Int.left);
            wall.Remove(center + Vector2Int.right);
            if (center.y <= 1) wall.Remove(center + Vector2Int.down);
        }
    }

    private HashSet<Vector2Int> SelectAnchors(
        HashSet<Vector2Int> wall, HashSet<Vector2Int> corridor,
        IReadOnlyList<Vector2Int> path, int maximumCount, bool transpose)
    {
        HashSet<Vector2Int> result = new HashSet<Vector2Int>();
        if (maximumCount <= 0 || Random.value > indestructibleAnchorChance)
            return result;
        List<Vector2Int> candidates = new List<Vector2Int>();
        foreach (Vector2Int cell in wall)
        {
            // 전치 여부와 관계없이 실제 배치 행이 0인 최상단 셀은
            // 파괴 불가 앵커 후보에서 제외한다.
            if (Transform(cell, transpose).y == 0) continue;
            if (Vector2Int.Distance(cell, path[0]) < 3f) continue;
            int touching = 0;
            if (corridor.Contains(cell + Vector2Int.left)) touching++;
            if (corridor.Contains(cell + Vector2Int.right)) touching++;
            if (corridor.Contains(cell + Vector2Int.up)) touching++;
            if (corridor.Contains(cell + Vector2Int.down)) touching++;
            if (touching >= 2) candidates.Add(cell);
        }
        Shuffle(candidates);
        int count = Mathf.Min(Random.Range(1, maximumCount + 1), candidates.Count);
        for (int i = 0; i < count; i++) result.Add(candidates[i]);
        return result;
    }

    private List<Vector2Int> SelectAttackers(
        IReadOnlyList<Vector2Int> path, HashSet<Vector2Int> corridor,
        HashSet<Vector2Int> wall, int requestedMaximum)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        int desired = Random.Range(
            minimumAttackerCount,
            Mathf.Max(minimumAttackerCount, requestedMaximum) + 1);
        int start = Mathf.Max(path.Count / 2, 2);
        for (int i = path.Count - 2; i >= start && result.Count < desired; i -= 2)
        {
            Vector2Int candidate = path[i];
            if (!corridor.Contains(candidate) || wall.Contains(candidate) ||
                result.Contains(candidate)) continue;
            result.Add(candidate);
        }
        if (result.Count == 0 && path.Count > 2)
            result.Add(path[path.Count - 2]);
        return result;
    }

    private static Vector2Int Transform(Vector2Int value, bool transpose) =>
        transpose ? new Vector2Int(value.y, value.x) : value;

    private static void AddInside(
        HashSet<Vector2Int> set, Vector2Int value, int width, int height)
    {
        if (IsInside(value, width, height)) set.Add(value);
    }

    private static bool IsInside(Vector2Int value, int width, int height) =>
        value.x >= 0 && value.x < width && value.y >= 0 && value.y < height;

    private static void Shuffle(List<Vector2Int> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int index = Random.Range(0, i + 1);
            Vector2Int temporary = values[i];
            values[i] = values[index];
            values[index] = temporary;
        }
    }
}
