using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(GuardianBlockController))]
public sealed class GuardianLinkPresenter : MonoBehaviour
{
    private static Material runtimeLineMaterial;

    [Header("Guardian Links")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Color lineColor = new Color(0.35f, 0.95f, 1f, 1f);
    [SerializeField] private Color backingColor = new Color(0.02f, 0.16f, 0.38f, 0.92f);
    [SerializeField, Min(0.005f)] private float lineWidth = 0.085f;
    [SerializeField, Min(0.005f)] private float backingWidth = 0.15f;
    [SerializeField, Range(0f, 0.45f)] private float sourceInsetRatio = 0.14f;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 20;

    private readonly List<LineRenderer> lines = new List<LineRenderer>();
    private readonly List<LineRenderer> backingLines = new List<LineRenderer>();
    private GuardianBlockController controller;
    private Block guardian;

    private void Awake()
    {
        controller = GetComponent<GuardianBlockController>();
        guardian = GetComponent<Block>();
    }

    private void OnEnable()
    {
        controller.TargetsChanged += RebuildLines;
        RebuildLines();
    }

    private void OnDisable()
    {
        if (controller != null)
        {
            controller.TargetsChanged -= RebuildLines;
        }

        ClearLines();
    }

    private void LateUpdate()
    {
        IReadOnlyList<Block> targets = controller.Targets;
        int count = Mathf.Min(
            Mathf.Min(lines.Count, backingLines.Count), targets.Count);
        float pulse = 1f +
                      (Mathf.Sin(Time.unscaledTime * 6f) + 1f) * 0.08f;

        for (int i = 0; i < count; i++)
        {
            if (lines[i] == null || targets[i] == null)
            {
                continue;
            }

            GetClosestSideCenterPoints(
                guardian,
                targets[i],
                out Vector3 guardianPoint,
                out Vector3 targetPoint
            );

            lines[i].SetPosition(0, guardianPoint);
            lines[i].SetPosition(1, targetPoint);
            backingLines[i].SetPosition(0, guardianPoint);
            backingLines[i].SetPosition(1, targetPoint);
            lines[i].startWidth = lineWidth * pulse;
            lines[i].endWidth = lineWidth * pulse;
            backingLines[i].startWidth = backingWidth * pulse;
            backingLines[i].endWidth = backingWidth * pulse;
        }
    }

    private void GetClosestSideCenterPoints(
        Block source,
        Block target,
        out Vector3 sourcePoint,
        out Vector3 targetPoint)
    {
        sourcePoint = source != null
            ? source.transform.position
            : transform.position;

        targetPoint = target != null
            ? target.transform.position
            : sourcePoint;

        if (source == null || target == null)
        {
            return;
        }

        Collider2D sourceCollider =
            source.GetComponent<Collider2D>();

        Collider2D targetCollider =
            target.GetComponent<Collider2D>();

        if (sourceCollider == null ||
            targetCollider == null)
        {
            return;
        }

        Bounds sourceBounds = sourceCollider.bounds;
        Bounds targetBounds = targetCollider.bounds;
        float shortestSqrDistance = float.PositiveInfinity;

        for (int sourceSide = 0; sourceSide < 4; sourceSide++)
        {
            Vector3 sourceCandidate =
                GetSideCenter(sourceBounds, sourceSide);

            for (int targetSide = 0; targetSide < 4; targetSide++)
            {
                Vector3 targetCandidate =
                    GetSideCenter(targetBounds, targetSide);

                float sqrDistance =
                    (targetCandidate - sourceCandidate)
                    .sqrMagnitude;

                if (sqrDistance >= shortestSqrDistance)
                {
                    continue;
                }

                shortestSqrDistance = sqrDistance;
                sourcePoint = Vector3.Lerp(
                    sourceCandidate,
                    sourceBounds.center,
                    sourceInsetRatio
                );
                targetPoint = Vector3.Lerp(
                    targetCandidate,
                    targetBounds.center,
                    sourceInsetRatio
                );
            }
        }
    }

    private Vector3 GetSideCenter(
        Bounds bounds,
        int sideIndex)
    {
        switch (sideIndex)
        {
            case 0:
                return new Vector3(
                    bounds.center.x,
                    bounds.max.y,
                    bounds.center.z
                );

            case 1:
                return new Vector3(
                    bounds.center.x,
                    bounds.min.y,
                    bounds.center.z
                );

            case 2:
                return new Vector3(
                    bounds.min.x,
                    bounds.center.y,
                    bounds.center.z
                );

            default:
                return new Vector3(
                    bounds.max.x,
                    bounds.center.y,
                    bounds.center.z
                );
        }
    }

    private void RebuildLines()
    {
        ClearLines();

        if (controller == null)
        {
            return;
        }

        for (int i = 0; i < controller.Targets.Count; i++)
        {
            backingLines.Add(CreateLine(
                $"GuardianLinkBacking_{i + 1}",
                backingColor, backingWidth, sortingOrder));
            lines.Add(CreateLine(
                $"GuardianLinkCore_{i + 1}",
                lineColor, lineWidth, sortingOrder + 1));
        }
    }

    private LineRenderer CreateLine(
        string objectName,
        Color color,
        float width,
        int order)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(transform, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = width;
        line.endWidth = width;
        line.startColor = color;
        line.endColor = color;
        line.sortingLayerName = sortingLayerName;
        line.sortingOrder = order;
        line.numCapVertices = 6;

        Material resolvedMaterial = ResolveLineMaterial();
        if (resolvedMaterial != null)
            line.sharedMaterial = resolvedMaterial;
        return line;
    }

    private void ClearLines()
    {
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            if (lines[i] != null)
            {
                Destroy(lines[i].gameObject);
            }
        }

        lines.Clear();

        for (int i = backingLines.Count - 1; i >= 0; i--)
        {
            if (backingLines[i] != null)
            {
                Destroy(backingLines[i].gameObject);
            }
        }

        backingLines.Clear();
    }

    private Material ResolveLineMaterial()
    {
        if (lineMaterial != null)
        {
            return lineMaterial;
        }

        if (runtimeLineMaterial != null)
        {
            return runtimeLineMaterial;
        }

        Shader lineShader = Shader.Find(
            "Universal Render Pipeline/2D/Sprite-Unlit-Default"
        );

        if (lineShader == null)
        {
            lineShader = Shader.Find("Sprites/Default");
        }

        if (lineShader == null)
        {
            return null;
        }

        runtimeLineMaterial = new Material(lineShader)
        {
            name = "Runtime_GuardianLink_Blue",
            hideFlags = HideFlags.HideAndDontSave
        };

        return runtimeLineMaterial;
    }

    private void OnValidate()
    {
        lineWidth = Mathf.Max(lineWidth, 0.005f);
        backingWidth = Mathf.Max(backingWidth, lineWidth + 0.01f);
        sourceInsetRatio = Mathf.Clamp(sourceInsetRatio, 0f, 0.45f);
    }
}
