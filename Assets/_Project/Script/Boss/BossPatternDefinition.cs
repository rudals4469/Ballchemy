using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BossPatternDefinition",
    menuName = "Ballchemy/Boss/Boss Pattern Definition"
)]
public sealed class BossPatternDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField]
    private string patternId =
        "boss_pattern_test";

    [SerializeField]
    private string displayName =
        "Boss Test Pattern";

    [Header("Definitions")]
    [SerializeField]
    private BlockDefinition bossDefinition;

    [SerializeField]
    private BlockDefinition breakablePatternDefinition;

    [SerializeField]
    private BlockDefinition indestructiblePatternDefinition;

    [Header("Stats")]
    [SerializeField, Min(1)]
    private int bossHealth = 100;

    [SerializeField, Min(0)]
    private int bossAttackPower = 0;

    [SerializeField, Min(1)]
    private int patternBlockHealth = 10;

    [Header("Pattern")]
    [Tooltip(
        ". = 빈칸, B = 보스, " +
        "X = 파괴 가능 블록, " +
        "# = 파괴 불가능 블록"
    )]
    [SerializeField]
    private List<string> rows =
        new List<string>
        {
            ".........",
            "...#.#...",
            "..#...#..",
            ".#..X..#.",
            "#..XBX..#",
            ".#..X..#.",
            "..#...#..",
            "...#.#...",
            "........."
        };

    public string PatternId =>
        patternId;

    public string DisplayName =>
        displayName;

    public BlockDefinition BossDefinition =>
        bossDefinition;

    public BlockDefinition BreakablePatternDefinition =>
        breakablePatternDefinition;

    public BlockDefinition IndestructiblePatternDefinition =>
        indestructiblePatternDefinition;

    public int BossHealth =>
        bossHealth;

    public int BossAttackPower =>
        bossAttackPower;

    public int PatternBlockHealth =>
        patternBlockHealth;

    public int Height =>
        rows != null
            ? rows.Count
            : 0;

    public int Width
    {
        get
        {
            if (rows == null)
            {
                return 0;
            }

            int maximumWidth = 0;

            for (int i = 0;
                 i < rows.Count;
                 i++)
            {
                string row =
                    rows[i];

                if (string.IsNullOrEmpty(
                        row))
                {
                    continue;
                }

                maximumWidth =
                    Mathf.Max(
                        maximumWidth,
                        row.Length
                    );
            }

            return maximumWidth;
        }
    }

    public char GetSymbol(
        int column,
        int row)
    {
        if (rows == null ||
            row < 0 ||
            row >= rows.Count)
        {
            return '.';
        }

        string rowData =
            rows[row];

        if (string.IsNullOrEmpty(
                rowData) ||
            column < 0 ||
            column >= rowData.Length)
        {
            return '.';
        }

        return char.ToUpperInvariant(
            rowData[column]
        );
    }

    private void OnValidate()
    {
        bossHealth =
            Mathf.Max(
                bossHealth,
                1
            );

        bossAttackPower =
            Mathf.Max(
                bossAttackPower,
                0
            );

        patternBlockHealth =
            Mathf.Max(
                patternBlockHealth,
                1
            );

        if (rows == null)
        {
            rows =
                new List<string>();
        }
    }
}