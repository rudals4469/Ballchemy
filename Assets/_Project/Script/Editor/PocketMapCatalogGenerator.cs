#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PocketMapCatalogGenerator
{
    private const string OutputFolder =
        "Assets/_Project/Resources/PocketPatterns/Fixed";

    private sealed class MapSpec
    {
        public string Id;
        public string Name;
        public int MinimumStage;
        public int MaximumStage;
        public MapTeleportMode TeleportMode;
        public string[] Rows;
    }

    [InitializeOnLoadMethod]
    private static void GenerateCatalogWhenMissing()
    {
        EditorApplication.delayCall += () =>
        {
            if (!CatalogNeedsRegeneration())
            {
                return;
            }

            Generate();
        };
    }

    private static bool CatalogNeedsRegeneration()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
            return true;

        string[] guids = AssetDatabase.FindAssets(
            "t:PocketPatternDefinition", new[] { OutputFolder });
        if (guids.Length < 50)
            return true;

        for (int i = 0; i < guids.Length; i++)
        {
            PocketPatternDefinition map =
                AssetDatabase.LoadAssetAtPath<PocketPatternDefinition>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
            if (map == null || !HasSingleEntranceAndExit(map))
                return true;
        }

        return false;
    }

    private static bool HasSingleEntranceAndExit(PocketPatternDefinition map)
    {
        int entranceCount = 0;
        int exitCount = 0;
        PocketLayoutCell[] cells = map.FixedCells;
        if (cells == null) return false;
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == null) continue;
            if (cells[i].CellType == PocketLayoutCellType.Entrance)
                entranceCount++;
            else if (cells[i].CellType == PocketLayoutCellType.Exit)
                exitCount++;
        }
        return entranceCount == 1 && exitCount == 1;
    }

    [MenuItem("Ballchemy/Generate Fixed Pocket Maps")]
    public static void Generate()
    {
        EnsureFolder();
        string[] existing = AssetDatabase.FindAssets(
            "t:PocketPatternDefinition", new[] { OutputFolder });
        for (int i = 0; i < existing.Length; i++)
            AssetDatabase.DeleteAsset(
                AssetDatabase.GUIDToAssetPath(existing[i]));
        MapSpec[] maps = CreateSpecs();
        for (int i = 0; i < maps.Length; i++)
            CreateOrUpdate(maps[i]);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Fixed pocket maps generated: {maps.Length}");
    }

    private static void CreateOrUpdate(MapSpec spec)
    {
        string path = $"{OutputFolder}/{spec.Id}.asset";
        PocketPatternDefinition asset =
            AssetDatabase.LoadAssetAtPath<PocketPatternDefinition>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<PocketPatternDefinition>();
            AssetDatabase.CreateAsset(asset, path);
        }

        string[] normalizedRows = EnsureSingleEntranceAndExit(
            spec.Rows, spec.TeleportMode);
        List<PocketLayoutCell> cells = new List<PocketLayoutCell>();
        for (int y = 0; y < normalizedRows.Length; y++)
        for (int x = 0; x < normalizedRows[y].Length; x++)
        {
            char symbol = normalizedRows[y][x];
                if (symbol != 'T' && symbol != 'A' && symbol != 'X' &&
                    symbol != 'S' && symbol != 'N' && symbol != '1' &&
                    symbol != 'I' && symbol != 'O')
                continue;
            PocketLayoutCell cell = new PocketLayoutCell();
            PocketLayoutCellType type = symbol == 'A'
                ? PocketLayoutCellType.Attacker
                : symbol == 'X'
                    ? PocketLayoutCellType.Indestructible
                    : symbol == 'S'
                        ? PocketLayoutCellType.SpecialSlot
                        : symbol == 'N'
                            ? PocketLayoutCellType.NamedSlot
                            : symbol == '1'
                            ? PocketLayoutCellType.TeleportSlot
                            : symbol == 'I'
                                ? PocketLayoutCellType.Entrance
                                : symbol == 'O'
                                    ? PocketLayoutCellType.Exit
                                : PocketLayoutCellType.Tank;
            cell.Configure(
                new Vector2Int(x, y),
                type,
                symbol == '1' ? 1 : 0);
            cells.Add(cell);
        }

        asset.ConfigureFixedLayout(
            spec.Id,
            spec.Name,
            spec.MinimumStage,
            spec.MaximumStage,
            new Vector2Int(normalizedRows[0].Length, normalizedRows.Length),
            cells.ToArray(),
            spec.TeleportMode);
        EditorUtility.SetDirty(asset);
    }

    private static string[] EnsureSingleEntranceAndExit(
        string[] source, MapTeleportMode teleportMode)
    {
        int height = source.Length;
        int width = source[0].Length;
        char[][] grid = new char[height][];
        for (int y = 0; y < height; y++)
            grid[y] = source[y].ToCharArray();

        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            if (!IsBoundary(x, y, width, height) || grid[y][x] != '.')
                continue;
            if (HasOpenInnerNeighbour(grid, x, y, width, height))
                candidates.Add(new Vector2Int(x, y));
        }

        if (candidates.Count < 2)
        {
            AddBreakableBoundaryCandidates(
                grid, candidates, width, height);
        }

        Vector2Int entrance = candidates[0];
        Vector2Int exit = candidates[1];
        int bestDistance = -1;
        for (int i = 0; i < candidates.Count; i++)
        for (int j = i + 1; j < candidates.Count; j++)
        {
            int distance = Mathf.Abs(candidates[i].x - candidates[j].x) +
                           Mathf.Abs(candidates[i].y - candidates[j].y);
            if (distance <= bestDistance) continue;
            bestDistance = distance;
            entrance = candidates[i];
            exit = candidates[j];
        }

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            if (IsBoundary(x, y, width, height) && grid[y][x] == '.')
                grid[y][x] = 'T';
        grid[entrance.y][entrance.x] = 'I';
        grid[exit.y][exit.x] = 'O';
        Vector2Int entranceThroat = ResolveOpeningThroat(
            entrance, width, height);
        Vector2Int exitThroat = ResolveOpeningThroat(exit, width, height);
        grid[entranceThroat.y][entranceThroat.x] = '.';
        grid[exitThroat.y][exitThroat.x] = '.';
        if (teleportMode != MapTeleportMode.Required)
            EnsureOpenAreasConnected(grid, entrance, width, height);

        string[] result = new string[height];
        for (int y = 0; y < height; y++)
            result[y] = new string(grid[y]);
        return result;
    }

    private static void EnsureOpenAreasConnected(
        char[][] grid, Vector2Int entrance, int width, int height)
    {
        int safety = width * height;
        while (safety-- > 0)
        {
            HashSet<Vector2Int> reachable = FloodOpenArea(
                grid, entrance, width, height);
            Vector2Int target = new Vector2Int(-1, -1);
            for (int y = 0; y < height && target.x < 0; y++)
            for (int x = 0; x < width; x++)
                if (IsOpenCell(grid[y][x]) &&
                    !reachable.Contains(new Vector2Int(x, y)))
                {
                    target = new Vector2Int(x, y);
                    break;
                }
            if (target.x < 0) return;

            Dictionary<Vector2Int, Vector2Int> previous =
                new Dictionary<Vector2Int, Vector2Int>();
            Queue<Vector2Int> pending = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited =
                new HashSet<Vector2Int>(reachable);
            foreach (Vector2Int position in reachable)
                pending.Enqueue(position);

            while (pending.Count > 0 && !visited.Contains(target))
            {
                Vector2Int current = pending.Dequeue();
                foreach (Vector2Int next in EnumerateNeighbours(
                             current, width, height))
                {
                    if (visited.Contains(next) ||
                        !CanCarveCell(grid[next.y][next.x]))
                        continue;
                    visited.Add(next);
                    previous[next] = current;
                    pending.Enqueue(next);
                }
            }

            if (!visited.Contains(target)) return;
            Vector2Int cursor = target;
            while (!reachable.Contains(cursor))
            {
                if (grid[cursor.y][cursor.x] == 'T')
                    grid[cursor.y][cursor.x] = '.';
                if (!previous.TryGetValue(cursor, out cursor)) break;
            }
        }
    }

    private static HashSet<Vector2Int> FloodOpenArea(
        char[][] grid, Vector2Int start, int width, int height)
    {
        HashSet<Vector2Int> visited = new HashSet<Vector2Int> { start };
        Queue<Vector2Int> pending = new Queue<Vector2Int>();
        pending.Enqueue(start);
        while (pending.Count > 0)
        {
            Vector2Int current = pending.Dequeue();
            foreach (Vector2Int next in EnumerateNeighbours(
                         current, width, height))
            {
                if (visited.Contains(next) ||
                    !IsOpenCell(grid[next.y][next.x]))
                    continue;
                visited.Add(next);
                pending.Enqueue(next);
            }
        }
        return visited;
    }

    private static IEnumerable<Vector2Int> EnumerateNeighbours(
        Vector2Int position, int width, int height)
    {
        Vector2Int[] directions =
        {
            Vector2Int.right, Vector2Int.left,
            Vector2Int.up, Vector2Int.down
        };
        for (int i = 0; i < directions.Length; i++)
        {
            Vector2Int next = position + directions[i];
            if (next.x >= 0 && next.x < width &&
                next.y >= 0 && next.y < height)
                yield return next;
        }
    }

    private static bool IsOpenCell(char value) =>
        value == '.' || value == 'I' || value == 'O';

    private static bool CanCarveCell(char value) =>
        IsOpenCell(value) || value == 'T';

    private static Vector2Int ResolveOpeningThroat(
        Vector2Int opening, int width, int height)
    {
        if (opening.y == 0) return new Vector2Int(opening.x, 1);
        if (opening.y == height - 1)
            return new Vector2Int(opening.x, height - 2);
        if (opening.x == 0) return new Vector2Int(1, opening.y);
        return new Vector2Int(width - 2, opening.y);
    }

    private static void AddBreakableBoundaryCandidates(
        char[][] grid,
        List<Vector2Int> candidates,
        int width,
        int height)
    {
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            if (!IsBoundary(x, y, width, height) || grid[y][x] != 'T')
                continue;
            if (!HasOpenInnerNeighbour(grid, x, y, width, height))
                continue;
            Vector2Int position = new Vector2Int(x, y);
            if (!candidates.Contains(position))
                candidates.Add(position);
        }

        if (candidates.Count >= 2) return;

        // 극단적인 완전 밀폐형에서도 특수/텔레포트 셀은 보존하고
        // 일반 탱커 벽만 입구로 전환합니다.
        for (int y = 0; y < height && candidates.Count < 2; y++)
        for (int x = 0; x < width && candidates.Count < 2; x++)
        {
            if (!IsBoundary(x, y, width, height) || grid[y][x] != 'T')
                continue;
            Vector2Int position = new Vector2Int(x, y);
            if (!candidates.Contains(position))
                candidates.Add(position);
        }
    }

    private static bool IsBoundary(int x, int y, int width, int height) =>
        x == 0 || y == 0 || x == width - 1 || y == height - 1;

    private static bool HasOpenInnerNeighbour(
        char[][] grid, int x, int y, int width, int height)
    {
        if (y == 0 && height > 1 && grid[1][x] == '.') return true;
        if (y == height - 1 && height > 1 && grid[height - 2][x] == '.') return true;
        if (x == 0 && width > 1 && grid[y][1] == '.') return true;
        if (x == width - 1 && width > 1 && grid[y][width - 2] == '.') return true;
        return false;
    }

    private static void EnsureFolder()
    {
        const string parent = "Assets/_Project/Resources/PocketPatterns";
        if (!AssetDatabase.IsValidFolder(OutputFolder))
            AssetDatabase.CreateFolder(parent, "Fixed");
    }

    private static MapSpec[] CreateSpecs()
    {
        const string data = @"
S1_01|0|N|T......;T.A....;T......;T......;TTTTT..
S1_02|0|N|..TTTTT;....A.T;......T;......T;......T
S1_03|0|N|TTTTTTT;T.....T;T..A..T;T.....T;TT...TT
S1_04|0|N|TTTTTT.;.......;..A....;.......;.TTTTTT
S1_05|0|N|TTTT...;T..T...;T.AT...;T..T...;TT.T...
S1_06|0|N|...TTTT;...T..T;...TA.T;...T..T;...T.TT
S1_07|0|N|TTTTTT.;T......;T..A...;T......;TTTTTT.
S1_08|0|N|.TTTTT.;.T...T.;.T.A.T.;.T...T.;.TT.TT.
S1_09|0|O|TTT1TTT;T.....T;T..A..T;T.....T;TT.S1TT
S2_01|1|N|T......;T.A....;T......;T..S...;T......;TTTTTT.
S2_02|1|N|.TTTTTT;....A.T;......T;...S..T;......T;......T
S2_03|1|N|TTTTTTT;T.....T;T..A..T;T.....T;T..S..T;TT...TT
S2_04|1|N|T.....T;T..A..T;T.....T;T..S..T;T.....T;T.....T
S2_05|1|O|TT1..TT;T.A.A.T;T.....T;T..S..T;T.....T;TT..1TT
S2_06|1|N|TTT.TTT;...T...;A..T..A;...T...;...S...;TTT.TTT
S2_07|1|N|TTTT...;T.AT...;T..T...;T..T...;T.S....;TTTTT..
S2_08|1|R|TTTTTTT;T.....T;T.TAT.T;T.T1T.T;T.TST.T;1TTTTTT
S2_09|1|O|TTT.T1T;T.....T;T..A..T;T.....T;T.SN..T;TT1..TT
S3_01|2|O|TTT1TTT;T.A.A.T;T.TTT.T;T.....T;T..S..T;TT..1TT;.......
S3_02|2|N|TTT.TTT;..TAT..;T.T.T.T;..TXT..;T.T.T.T;..TST..;TTT.TTT
S3_03|2|N|TT...TT;TA...AT;T.TTT.T;T.....T;T..S..T;T..N..T;TT...TT
S3_04|2|R|TTTTTTT;T.....T;T.TAT.T;T.T1T.T;T.TST.T;T.....T;TT1TTTT
S3_05|2|N|TTT.TTT;T.A.A.T;TTT.TTT;T.....T;T..S..T;T.A.A.T;TTT.TTT
S3_06|2|N|TTTT...;T.AT...;T.TTT..;T...T..;T.A.T..;T.S.T..;TT..T..
S3_07|2|O|...TTTT;...TA.T;1.TTT.T;..T...T;..T.A.T;..T.S.T;..T1.TT
S3_08|2|N|TTT.TTT;T..T..T;..TAT..;T..X..T;..TAT..;T..N..T;TTTSTTT
S4_01|3|O|TTT1..TTT;T.A...A.T;T.TTTTT.T;T.......T;T...S...T;TT..1..TT;.........
S4_02|3|N|TTT.X.TTT;T...T...T;T.A.T.A.T;T...T...T;T...S...T;T...N...T;TTT...TTT
S4_03|3|R|XTT...TTX;T.A...A.T;T.TTTTT.T;T...1...T;T...S...T;T.......T;TT..1..TT
S4_04|3|N|TTTT.TTTT;T.A...A.T;TTTT.TTTT;T.......T;T...S...T;T...N...T;TTT...TTT
S4_05|3|N|XTTTTTTT.;T.......T;T...A...T;T.......T;T...S...T;T.......T;TTTTTTTT.
S4_06|3|O|TTTTT....;T.A.T....;T.TTTTT..;T....1T..;T.A.S.T..;T.....T..;TTT1..T..
S4_07|3|O|....TTTTT;....T.A.T;..TTTTT.T;..T1....T;..T.S.A.T;..T.....T;..T..1TTT
S4_08|3|N|TTT...TTT;T.A...A.T;T.TTTTT.T;T...N...T;T.T...T.T;T...S...T;TT.....TT
S5_01|4|O|TT1T.T.TT;TA.T.T.AT;T..T.T..T;T..S....T;T.TTTTT.T;TA...A..T;T...N...T;TT...1.TT
S5_02|4|N|T.T.T.T.T;TAT.T.ATA;T.T.T.T.T;T.T.S.T.T;T.T.T.T.T;TAT.N.ATA;T.T.T.T.T;TT.....TT
S5_03|4|R|XTT...TTX;T.A...A.T;T.TTTTT.T;T...1...T;T.T.N.T.T;T...A...T;T.......T;TT..1..TT
S5_04|4|N|TT.T.T.TT;T.A.T.A.T;T.T.T.T.T;T...S...T;T.TTTTT.T;T.A.N.A.T;T.......T;TT.....TT
S5_05|4|R|TTTTTTTTT;T.......T;T.A.N.A.T;T.TTTTT.T;T...1...T;T.T.S.T.T;T.A.T.A.T;TT1TTTTTT
S5_06|4|O|XTT...TTX;TA.....AT;T.TTTTT.T;T...S...T;T.T...T.T;TA..N..AT;T.1.....T;TT....1TT
S5_07|4|O|TTT.X.TTT;T.A.T.A.T;T...T...T;T.TTTTT.T;T...S...T;T...N...T;T.A1..A.T;TT....1TT
S5_08|4|N|XTTTT.TTX;T...T...T;T.A.T.A.T;T...T...T;T.TTTTT.T;T...S...T;T...N...T;TTT...TTT
S6_01|5|R|XTT...TTX;T.A...A.T;T.TTTTT.T;T...A...T;T.T.N.T.T;T...1...T;T.A...A.T;T.......T;TT..1..TT
S6_02|5|N|TTT...TTT;T.T.A.T.T;A.T.T.T.A;T.T.X.T.T;T.T.N.T.T;T.T.S.T.T;A.T.T.T.A;T.T.A.T.T;TTT...TTT
S6_03|5|R|XTX.T.XTX;TA..T..AT;T.T.T.T.T;T...1...T;T.TTTTT.T;T...N...T;TA..T..AT;T.T.S.T.T;TT..1..TT
S6_04|5|N|TTTT.TTTT;T.A...A.T;TTTT.TTTT;T.......T;T...N...T;T.......T;TTTTSTTTT;T.A...A.T;TT.....TT
S6_05|5|R|XTT...TTX;TA.....AT;T.TTTTT.T;T...A...T;T.T.N.T.T;T...1...T;T.TTTTT.T;TA.....AT;TT..1..TT
S6_06|5|O|TT1T.T.TT;TA.T.T.AT;T..T.T..T;T.TTTTT.T;T...N...T;T.TTTTT.T;T..T.T..T;TA.TST.AT;TT.T.1.TT
S6_07|5|O|XTTT.TTTX;T.A...A.T;T.TTTTT.T;T...A...T;T.T.N.T.T;T...S...T;T.TTTTT.T;T.A1..A.T;TT....1TT
S6_08|5|O|TT.T.T.TT;TA.X.X.AT;T.T.T.T.T;T...A...T;T.T.N.T.T;T...S...T;T.T.T.T.T;TA1....AT;TT.T.T1TT";
        string[] lines = data.Split(new[] { '\r', '\n' },
            System.StringSplitOptions.RemoveEmptyEntries);
        MapSpec[] result = new MapSpec[lines.Length];
        for (int i = 0; i < lines.Length; i++)
        {
            string[] parts = lines[i].Split('|');
            int stage = int.Parse(parts[1]);
            result[i] = new MapSpec
            {
                Id = "PocketMap_" + parts[0],
                Name = parts[0],
                MinimumStage = stage,
                MaximumStage = stage,
                TeleportMode = parts[2] == "R"
                    ? MapTeleportMode.Required
                    : parts[2] == "O"
                        ? MapTeleportMode.Optional
                        : MapTeleportMode.None,
                Rows = parts[3].Split(';')
            };
        }
        return result;
    }

    private static MapSpec M(
        string id,
        string name,
        int minimumStage,
        int maximumStage,
        params string[] rows) => new MapSpec
    {
        Id = id,
        Name = name,
        MinimumStage = minimumStage,
        MaximumStage = maximumStage,
        Rows = rows
    };
}
#endif
