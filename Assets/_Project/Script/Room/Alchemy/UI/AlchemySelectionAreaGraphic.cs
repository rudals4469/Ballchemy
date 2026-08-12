using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AlchemySelectionAreaGraphic : MaskableGraphic
{
    [SerializeField] private Color backgroundColor =
        new Color(0.82f, 0.82f, 0.82f, 0.06f);
    [SerializeField] private Color borderColor =
        new Color(0.72f, 0.58f, 0.92f, 0.95f);
    [SerializeField, Min(0.5f)] private float borderThickness = 1.5f;

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect targetRect = rectTransform.rect;
        if (targetRect.width <= 0f || targetRect.height <= 0f)
        {
            return;
        }

        AddQuad(vertexHelper, targetRect, backgroundColor);

        float thickness = Mathf.Min(
            borderThickness,
            Mathf.Min(targetRect.width, targetRect.height) * 0.5f);
        AddQuad(vertexHelper, new Rect(
            targetRect.xMin, targetRect.yMax - thickness,
            targetRect.width, thickness), borderColor);
        AddQuad(vertexHelper, new Rect(
            targetRect.xMin, targetRect.yMin,
            targetRect.width, thickness), borderColor);
        AddQuad(vertexHelper, new Rect(
            targetRect.xMin, targetRect.yMin,
            thickness, targetRect.height), borderColor);
        AddQuad(vertexHelper, new Rect(
            targetRect.xMax - thickness, targetRect.yMin,
            thickness, targetRect.height), borderColor);
    }

    private static void AddQuad(
        VertexHelper vertexHelper,
        Rect rect,
        Color color)
    {
        int startIndex = vertexHelper.currentVertCount;
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = new Vector3(rect.xMin, rect.yMin);
        vertexHelper.AddVert(vertex);
        vertex.position = new Vector3(rect.xMin, rect.yMax);
        vertexHelper.AddVert(vertex);
        vertex.position = new Vector3(rect.xMax, rect.yMax);
        vertexHelper.AddVert(vertex);
        vertex.position = new Vector3(rect.xMax, rect.yMin);
        vertexHelper.AddVert(vertex);

        vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
    }
}
