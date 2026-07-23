using UnityEngine;

[CreateAssetMenu(
    fileName = "BoardGridSettings",
    menuName = "Ballchemy/Grid/Board Grid Settings"
)]
public sealed class BoardGridSettings : ScriptableObject
{
    [Header("Grid Size")]
    [SerializeField, Min(1)]
    private int columnCount = 9;

    [SerializeField, Min(1)]
    private int rowCount = 13;

    [Header("Cell")]
    [SerializeField, Min(0.01f)]
    private float cellSize = 1f;

    public int ColumnCount =>
        columnCount;

    public int RowCount =>
        rowCount;

    public float CellSize =>
        cellSize;

    public int CenterColumn =>
        (columnCount - 1) / 2;

    public int CenterRow =>
        (rowCount - 1) / 2;

    public bool HasExactCenterCell =>
        columnCount % 2 == 1 &&
        rowCount % 2 == 1;

    public float BoardWidth =>
        columnCount * cellSize;

    public float BoardHeight =>
        rowCount * cellSize;

    public float HalfBoardWidth =>
        BoardWidth * 0.5f;

    public float HalfBoardHeight =>
        BoardHeight * 0.5f;

    public bool IsInside(
        int column,
        int row)
    {
        return column >= 0 &&
               column < columnCount &&
               row >= 0 &&
               row < rowCount;
    }

    private void OnValidate()
    {
        columnCount =
            Mathf.Max(
                columnCount,
                1
            );

        rowCount =
            Mathf.Max(
                rowCount,
                1
            );

        cellSize =
            Mathf.Max(
                cellSize,
                0.01f
            );
    }
}