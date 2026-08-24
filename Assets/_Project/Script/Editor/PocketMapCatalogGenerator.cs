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
            if (AssetDatabase.IsValidFolder(OutputFolder) &&
                AssetDatabase.FindAssets(
                    "t:PocketPatternDefinition",
                    new[] { OutputFolder }).Length >= 50)
            {
                return;
            }

            Generate();
        };
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

        List<PocketLayoutCell> cells = new List<PocketLayoutCell>();
        for (int y = 0; y < spec.Rows.Length; y++)
        for (int x = 0; x < spec.Rows[y].Length; x++)
        {
            char symbol = spec.Rows[y][x];
                if (symbol != 'T' && symbol != 'A' && symbol != 'X' &&
                    symbol != 'S' && symbol != 'N' && symbol != '1')
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
            new Vector2Int(spec.Rows[0].Length, spec.Rows.Length),
            cells.ToArray(),
            spec.TeleportMode);
        EditorUtility.SetDirty(asset);
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
