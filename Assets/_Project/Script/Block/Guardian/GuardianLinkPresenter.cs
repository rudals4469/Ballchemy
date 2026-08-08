using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(GuardianBlockController))]
public sealed class GuardianLinkPresenter : MonoBehaviour
{
    [Header("Guardian Links")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Color lineColor = new Color(0.2f, 0.9f, 1f, 0.9f);
    [SerializeField, Min(0.005f)] private float lineWidth = 0.06f;
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

            GetClosestBoundaryPoints(
                guardian,
                targets[i],
                out Vector3 guardianPoint,
                out Vector3 targetPoint
            );

            lines[i].SetPosition(0, guardianPoint);
            lines[i].SetPosition(1, targetPoint);
        }
    }

    private void GetClosestBoundaryPoints(
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

        ResolveClosestAxis(
            sourceBounds.min.x,
            sourceBounds.max.x,
            targetBounds.min.x,
            targetBounds.max.x,
            out float sourceX,
            out float targetX
        );

        ResolveClosestAxis(
            sourceBounds.min.y,
            sourceBounds.max.y,
            targetBounds.min.y,
            targetBounds.max.y,
            out float sourceY,
            out float targetY
        );

        sourcePoint = new Vector3(
            sourceX,
            sourceY,
            sourceBounds.center.z
        );

        targetPoint = new Vector3(
            targetX,
            targetY,
            targetBounds.center.z
        );
    }

    private void ResolveClosestAxis(
        float sourceMinimum,
        float sourceMaximum,
        float targetMinimum,
        float targetMaximum,
        out float sourceCoordinate,
        out float targetCoordinate)
    {
        if (sourceMaximum < targetMinimum)
        {
            sourceCoordinate = sourceMaximum;
            targetCoordinate = targetMinimum;
            return;
        }

        if (targetMaximum < sourceMinimum)
        {
            sourceCoordinate = sourceMinimum;
            targetCoordinate = targetMaximum;
            return;
        }

        float overlapMinimum =
            Mathf.Max(sourceMinimum, targetMinimum);

        float overlapMaximum =
            Mathf.Min(sourceMaximum, targetMaximum);

        sourceCoordinate = targetCoordinate =
            (overlapMinimum + overlapMaximum) * 0.5f;
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

            if (lineMaterial != null)
            {
                line.sharedMaterial = lineMaterial;
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

    private void OnValidate()
    {
        lineWidth = Mathf.Max(lineWidth, 0.005f);
    }
}
