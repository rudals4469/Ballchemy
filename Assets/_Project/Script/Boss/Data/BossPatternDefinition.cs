using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BossPatternDefinition",
    menuName = "Ballchemy/Boss/Boss Pattern Definition"
)]
public sealed class BossPatternDefinition : ScriptableObject
{
    private const int ExpectedWidth = 9;
    private const int ExpectedHeight = 15;

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
    private int bossAttackPower;

    [SerializeField, Min(1)]
    private int patternBlockHealth = 10;

    [Header("Pattern")]
    [Tooltip(
        "패턴 전체를 한 번에 붙여넣습니다.\n" +
        ". = 빈칸\n" +
        "B = 보스\n" +
        "X = 파괴 가능 블록\n" +
        "# = 파괴 불가능 블록\n\n" +
        "현재 보드 규격은 가로 9칸, 세로 15칸입니다."
    )]
    [SerializeField, TextArea(15, 20)]
    private string patternText =
@".#X...X#.
X...#..X.
..X....X.
#..X.X..#
.X#...#X.
X......X.
.#X...X#.
X...B...X
.#X...X#.
.X......X
.X#...#X.
#..X.X..#
.X....X..
.X..#...X
.#X...X#.";

    [NonSerialized]
    private string cachedPatternText;

    [NonSerialized]
    private string[] parsedRows =
        Array.Empty<string>();

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

    public string PatternText =>
        patternText;

    public int Height
    {
        get
        {
            EnsureParsedPattern();

            return parsedRows.Length;
        }
    }

    public int Width
    {
        get
        {
            EnsureParsedPattern();

            int maximumWidth = 0;

            for (int i = 0;
                 i < parsedRows.Length;
                 i++)
            {
                string row =
                    parsedRows[i];

                if (string.IsNullOrEmpty(row))
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

    public IReadOnlyList<string> Rows
    {
        get
        {
            EnsureParsedPattern();

            return parsedRows;
        }
    }

    private void OnEnable()
    {
        EnsureParsedPattern(
            true
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

        patternText =
            NormalizePatternText(
                patternText
            );

        EnsureParsedPattern(
            true
        );

        if (!TryValidatePattern(
                out string validationMessage))
        {
            Debug.LogWarning(
                $"BossPatternDefinition ({name}):\n" +
                validationMessage,
                this
            );
        }
    }

    public char GetSymbol(
        int column,
        int row)
    {
        EnsureParsedPattern();

        if (row < 0 ||
            row >= parsedRows.Length)
        {
            return '.';
        }

        string rowData =
            parsedRows[row];

        if (string.IsNullOrEmpty(rowData) ||
            column < 0 ||
            column >= rowData.Length)
        {
            return '.';
        }

        return char.ToUpperInvariant(
            rowData[column]
        );
    }

    public bool TryValidatePattern(
        out string validationMessage)
    {
        EnsureParsedPattern();

        List<string> issues =
            new List<string>();

        if (parsedRows.Length !=
            ExpectedHeight)
        {
            issues.Add(
                $"패턴의 세로 길이가 " +
                $"{parsedRows.Length}줄입니다. " +
                $"{ExpectedHeight}줄이어야 합니다."
            );
        }

        int bossCount = 0;

        for (int row = 0;
             row < parsedRows.Length;
             row++)
        {
            string rowData =
                parsedRows[row] ??
                string.Empty;

            if (rowData.Length !=
                ExpectedWidth)
            {
                issues.Add(
                    $"Row {row:D2}의 길이가 " +
                    $"{rowData.Length}칸입니다. " +
                    $"{ExpectedWidth}칸이어야 합니다."
                );
            }

            for (int column = 0;
                 column < rowData.Length;
                 column++)
            {
                char symbol =
                    char.ToUpperInvariant(
                        rowData[column]
                    );

                if (symbol == 'B')
                {
                    bossCount++;
                }

                if (IsSupportedSymbol(symbol))
                {
                    continue;
                }

                issues.Add(
                    $"Row {row:D2}, " +
                    $"Column {column:D2}에 " +
                    $"지원하지 않는 문자 " +
                    $"'{rowData[column]}'가 있습니다."
                );
            }
        }

        if (bossCount != 1)
        {
            issues.Add(
                $"보스 문자 B가 {bossCount}개 있습니다. " +
                "정확히 1개여야 합니다."
            );
        }

        if (issues.Count == 0)
        {
            validationMessage =
                string.Empty;

            return true;
        }

        StringBuilder messageBuilder =
            new StringBuilder();

        for (int i = 0;
             i < issues.Count;
             i++)
        {
            messageBuilder.Append(
                "• "
            );

            messageBuilder.Append(
                issues[i]
            );

            if (i <
                issues.Count - 1)
            {
                messageBuilder.AppendLine();
            }
        }

        validationMessage =
            messageBuilder.ToString();

        return false;
    }

    private void EnsureParsedPattern(
        bool forceRefresh = false)
    {
        string normalizedText =
            NormalizePatternText(
                patternText
            );

        if (!forceRefresh &&
            cachedPatternText ==
            normalizedText &&
            parsedRows != null)
        {
            return;
        }

        cachedPatternText =
            normalizedText;

        if (string.IsNullOrEmpty(
                normalizedText))
        {
            parsedRows =
                Array.Empty<string>();

            return;
        }

        parsedRows =
            normalizedText.Split(
                '\n'
            );
    }

    private string NormalizePatternText(
        string rawPatternText)
    {
        if (string.IsNullOrWhiteSpace(
                rawPatternText))
        {
            return string.Empty;
        }

        string normalizedLineEndings =
            rawPatternText
                .Replace(
                    "\r\n",
                    "\n"
                )
                .Replace(
                    '\r',
                    '\n'
                );

        string[] sourceLines =
            normalizedLineEndings.Split(
                '\n'
            );

        int firstContentLine = 0;

        while (firstContentLine <
               sourceLines.Length &&
               string.IsNullOrWhiteSpace(
                   sourceLines[
                       firstContentLine
                   ]))
        {
            firstContentLine++;
        }

        int lastContentLine =
            sourceLines.Length - 1;

        while (lastContentLine >=
               firstContentLine &&
               string.IsNullOrWhiteSpace(
                   sourceLines[
                       lastContentLine
                   ]))
        {
            lastContentLine--;
        }

        if (firstContentLine >
            lastContentLine)
        {
            return string.Empty;
        }

        List<string> normalizedRows =
            new List<string>();

        for (int i = firstContentLine;
             i <= lastContentLine;
             i++)
        {
            string normalizedRow =
                sourceLines[i]
                    .Trim()
                    .ToUpperInvariant();

            normalizedRows.Add(
                normalizedRow
            );
        }

        return string.Join(
            "\n",
            normalizedRows
        );
    }

    private bool IsSupportedSymbol(
        char symbol)
    {
        return symbol == '.' ||
               symbol == 'B' ||
               symbol == 'X' ||
               symbol == '#';
    }
}