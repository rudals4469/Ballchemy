using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(GuardianBlockController))]
public sealed class GuardianLinkPresenter : MonoBehaviour
{
    [Header("Guardian Links")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private Color lineColor = new Color(0.2f, 0.9f, 1f, 0.9f);
    [SerializeField, Min(0.005f)] private float lineWidth = 0.035f;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 20;

    private readonly List<LineRenderer> lines = new List<LineRenderer>();
    private GuardianBlockController controller;

    private void Awake()
    {
        controller = GetComponent<GuardianBlockController>();
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

            lines[i].SetPosition(0, transform.position);
            lines[i].SetPosition(1, targets[i].transform.position);
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
