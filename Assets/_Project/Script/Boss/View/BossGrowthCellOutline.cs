using UnityEngine;

[DisallowMultipleComponent]
public sealed class BossGrowthCellOutline : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private Material runtimeMaterial;
    private Block parent;
    private Color baseColor;
    private float pulseOffset;

    public void Configure(
        BoardGrid boardGrid,
        Vector2Int cell,
        Color color,
        Block sourceParent)
    {
        if (boardGrid == null)
        {
            gameObject.SetActive(false);
            return;
        }

        parent = sourceParent;
        baseColor = color;
        pulseOffset = Random.value * Mathf.PI * 2f;

        lineRenderer =
            gameObject.AddComponent<LineRenderer>();

        Shader shader =
            Shader.Find("Sprites/Default");

        if (shader != null)
        {
            runtimeMaterial =
                new Material(shader);

            lineRenderer.sharedMaterial =
                runtimeMaterial;
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
        lineRenderer.positionCount = 5;
        lineRenderer.numCornerVertices = 2;
        lineRenderer.numCapVertices = 2;
        lineRenderer.startWidth = boardGrid.CellSize * 0.065f;
        lineRenderer.endWidth = boardGrid.CellSize * 0.065f;
        lineRenderer.sortingOrder = 100;

        Vector3 center =
            boardGrid.GetCellWorldPosition(cell);

        Vector3 horizontal =
            boardGrid.transform.right.normalized *
            boardGrid.CellSize * 0.43f;

        Vector3 vertical =
            boardGrid.transform.up.normalized *
            boardGrid.CellSize * 0.43f;

        lineRenderer.SetPosition(0, center - horizontal - vertical);
        lineRenderer.SetPosition(1, center + horizontal - vertical);
        lineRenderer.SetPosition(2, center + horizontal + vertical);
        lineRenderer.SetPosition(3, center - horizontal + vertical);
        lineRenderer.SetPosition(4, center - horizontal - vertical);

        ApplyColor(1f);

        if (parent != null)
        {
            parent.Destroyed +=
                HandleParentDestroyed;
        }
    }

    private void Update()
    {
        if (lineRenderer == null ||
            !lineRenderer.enabled)
        {
            return;
        }

        float pulse =
            0.68f +
            Mathf.Sin(
                Time.time * 5f +
                pulseOffset
            ) * 0.22f;

        ApplyColor(pulse);
    }

    private void ApplyColor(
        float alphaMultiplier)
    {
        if (lineRenderer == null)
        {
            return;
        }

        Color color = baseColor;
        color.a *= Mathf.Clamp01(alphaMultiplier);
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }

    private void HandleParentDestroyed(
        Block destroyedParent)
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (parent != null)
        {
            parent.Destroyed -=
                HandleParentDestroyed;
        }

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}
