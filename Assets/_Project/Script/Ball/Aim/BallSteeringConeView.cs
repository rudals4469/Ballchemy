using UnityEngine;

[DisallowMultipleComponent]
public sealed class BallSteeringConeView : MonoBehaviour
{
    private const int SegmentCount = 24;

    [SerializeField, Min(0.5f)] private float radius = 4.5f;
    [SerializeField] private Color coneColor =
        new Color(0.35f, 0.85f, 1f, 0.14f);
    [SerializeField] private Color boundaryColor =
        new Color(0.55f, 0.92f, 1f, 0.5f);
    [SerializeField] private Color currentDirectionColor =
        new Color(0.85f, 1f, 1f, 0.9f);
    [SerializeField, Min(0.01f)] private float boundaryWidth = 0.035f;
    [SerializeField, Min(0.01f)] private float currentDirectionWidth = 0.055f;

    private GameObject visualRoot;
    private Mesh coneMesh;
    private LineRenderer leftBoundary;
    private LineRenderer rightBoundary;
    private LineRenderer currentDirectionLine;
    private Material runtimeMaterial;

    public void Show(
        Vector2 origin,
        Vector2 centerDirection,
        Vector2 currentDirection,
        float halfAngle)
    {
        EnsureVisuals();

        if (visualRoot == null)
        {
            return;
        }

        visualRoot.SetActive(true);
        visualRoot.transform.position =
            new Vector3(origin.x, origin.y, -0.12f);

        Vector2 center = centerDirection.sqrMagnitude > 0.001f
            ? centerDirection.normalized
            : Vector2.up;
        Vector2 current = currentDirection.sqrMagnitude > 0.001f
            ? currentDirection.normalized
            : center;

        float centerAngle = Mathf.Atan2(center.y, center.x) * Mathf.Rad2Deg;
        float range = Mathf.Clamp(halfAngle, 0f, 80f);
        float startAngle = centerAngle - range;
        float endAngle = centerAngle + range;

        UpdateConeMesh(startAngle, endAngle);
        SetLine(leftBoundary, DirectionFromAngle(startAngle) * radius);
        SetLine(rightBoundary, DirectionFromAngle(endAngle) * radius);
        SetLine(currentDirectionLine, current * radius);
    }

    public void Hide()
    {
        if (visualRoot != null)
        {
            visualRoot.SetActive(false);
        }
    }

    private void EnsureVisuals()
    {
        if (visualRoot != null)
        {
            return;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            return;
        }

        runtimeMaterial = new Material(shader)
        {
            name = "Runtime Steering Cone Material",
            hideFlags = HideFlags.HideAndDontSave
        };

        visualRoot = new GameObject("Steering Cone View");
        visualRoot.transform.SetParent(transform, false);

        GameObject fillObject = new GameObject("Cone Fill");
        fillObject.transform.SetParent(visualRoot.transform, false);
        MeshFilter filter = fillObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = fillObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = runtimeMaterial;
        renderer.sortingOrder = 20;

        coneMesh = new Mesh
        {
            name = "Runtime Steering Cone Mesh",
            hideFlags = HideFlags.HideAndDontSave
        };
        filter.sharedMesh = coneMesh;

        leftBoundary = CreateLine("Left Boundary", boundaryColor, boundaryWidth, 21);
        rightBoundary = CreateLine("Right Boundary", boundaryColor, boundaryWidth, 21);
        currentDirectionLine = CreateLine(
            "Current Direction",
            currentDirectionColor,
            currentDirectionWidth,
            22);
        Hide();
    }

    private LineRenderer CreateLine(
        string objectName,
        Color color,
        float width,
        int sortingOrder)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(visualRoot.transform, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 2;
        line.sharedMaterial = runtimeMaterial;
        line.startColor = color;
        line.endColor = new Color(color.r, color.g, color.b, 0.08f);
        line.startWidth = width;
        line.endWidth = width * 0.35f;
        line.numCapVertices = 3;
        line.sortingOrder = sortingOrder;
        return line;
    }

    private void UpdateConeMesh(float startAngle, float endAngle)
    {
        Vector3[] vertices = new Vector3[SegmentCount + 2];
        Color[] colors = new Color[vertices.Length];
        int[] triangles = new int[SegmentCount * 3];
        vertices[0] = Vector3.zero;
        colors[0] = coneColor;

        for (int i = 0; i <= SegmentCount; i++)
        {
            float t = i / (float)SegmentCount;
            Vector2 direction = DirectionFromAngle(
                Mathf.Lerp(startAngle, endAngle, t));
            vertices[i + 1] = direction * radius;
            colors[i + 1] = new Color(
                coneColor.r, coneColor.g, coneColor.b, 0f);

            if (i < SegmentCount)
            {
                int index = i * 3;
                triangles[index] = 0;
                triangles[index + 1] = i + 1;
                triangles[index + 2] = i + 2;
            }
        }

        coneMesh.Clear();
        coneMesh.vertices = vertices;
        coneMesh.colors = colors;
        coneMesh.triangles = triangles;
        coneMesh.RecalculateBounds();
    }

    private static void SetLine(LineRenderer line, Vector2 end)
    {
        if (line == null)
        {
            return;
        }

        line.SetPosition(0, Vector3.zero);
        line.SetPosition(1, end);
    }

    private static Vector2 DirectionFromAngle(float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        if (coneMesh != null)
        {
            Destroy(coneMesh);
        }

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}
