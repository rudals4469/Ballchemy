using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(GuardianBlockController))]
public sealed class GuardianLinkPresenter : MonoBehaviour
{
    private static Material runtimeLineMaterial;

    [Header("Guardian Links")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Color lineColor = new Color(0.12f, 0.42f, 1f, 0.95f);
    [SerializeField, Min(0.005f)] private float lineWidth = 0.06f;
    [SerializeField, Range(0f, 0.45f)] private float sourceInsetRatio = 0.14f;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 20;

    private readonly List<LineRenderer> lines = new List<LineRenderer>();
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
        int count = Mathf.Min(lines.Count, targets.Count);

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
                targetPoint = targetCandidate;
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
            GameObject lineObject = new GameObject($"GuardianLink_{i + 1}");
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.startColor = lineColor;
            line.endColor = lineColor;
            line.sortingLayerName = sortingLayerName;
            line.sortingOrder = sortingOrder;

            Material resolvedMaterial =
                ResolveLineMaterial();

            if (resolvedMaterial != null)
            {
                line.sharedMaterial = resolvedMaterial;
            }

            lines.Add(line);
        }
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
        sourceInsetRatio = Mathf.Clamp(sourceInsetRatio, 0f, 0.45f);
    }
}
