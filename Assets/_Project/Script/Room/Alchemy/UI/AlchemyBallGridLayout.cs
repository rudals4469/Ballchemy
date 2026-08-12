using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AlchemyBallGridLayout : LayoutGroup
{
    [SerializeField, Min(1)] private int columns = 10;
    [SerializeField] private Vector2 cellSize = new Vector2(54f, 68f);
    [SerializeField] private Vector2 spacing = new Vector2(8f, 8f);

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        int columnCount = Mathf.Max(columns, 1);
        float width = padding.horizontal +
            columnCount * cellSize.x +
            Mathf.Max(columnCount - 1, 0) * spacing.x;
        SetLayoutInputForAxis(width, width, -1f, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
        int rows = Mathf.CeilToInt(rectChildren.Count /
            (float)Mathf.Max(columns, 1));
        float height = padding.vertical +
            rows * cellSize.y +
            Mathf.Max(rows - 1, 0) * spacing.y;
        SetLayoutInputForAxis(height, height, -1f, 1);
    }

    public override void SetLayoutHorizontal()
    {
        ArrangeChildren();
    }

    public override void SetLayoutVertical()
    {
        ArrangeChildren();
    }

    private void ArrangeChildren()
    {
        int columnCount = Mathf.Max(columns, 1);
        for (int i = 0; i < rectChildren.Count; i++)
        {
            int column = i % columnCount;
            int row = i / columnCount;
            SetChildAlongAxis(
                rectChildren[i], 0,
                padding.left + column * (cellSize.x + spacing.x),
                cellSize.x);
            SetChildAlongAxis(
                rectChildren[i], 1,
                padding.top + row * (cellSize.y + spacing.y),
                cellSize.y);
        }
    }
}
