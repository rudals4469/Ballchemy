using System;
using UnityEngine;

[Serializable]
public sealed class BlockLayout
{
    [Header("References")]
    [SerializeField]
    private SpriteRenderer visualRenderer;

    [SerializeField]
    private BoxCollider2D boxCollider;

    [Header("Spacing")]
    [Tooltip(
        "블록 외형과 셀 경계 사이의 간격입니다."
    )]
    [SerializeField, Min(0f)]
    private float visualPadding = 0.04f;

    [Tooltip(
        "콜라이더와 셀 경계 사이의 간격입니다."
    )]
    [SerializeField, Min(0f)]
    private float colliderInset = 0.06f;

    public void Validate(
        GameObject owner)
    {
        visualPadding =
            Mathf.Max(
                visualPadding,
                0f
            );

        colliderInset =
            Mathf.Max(
                colliderInset,
                0f
            );

        FindReferences(
            owner
        );
    }

    public void Apply(
        GameObject owner,
        BlockDefinition definition,
        Vector2Int gridSize,
        float cellSize)
    {
        if (owner == null)
        {
            return;
        }

        FindReferences(
            owner
        );

        cellSize =
            Mathf.Max(
                cellSize,
                0.1f
            );

        Vector2 worldSize =
            new Vector2(
                Mathf.Max(
                    gridSize.x,
                    1
                ) *
                cellSize,

                Mathf.Max(
                    gridSize.y,
                    1
                ) *
                cellSize
            );

        ApplyDefinitionVisual(
            definition
        );

        ApplyVisualLayout(
            owner.transform,
            definition,
            worldSize
        );

        ApplyColliderLayout(
            worldSize
        );
    }

    public void ApplyRuntimeSprite(
        GameObject owner,
        Sprite sprite)
    {
        if (owner == null || sprite == null)
        {
            return;
        }

        FindReferences(owner);
        if (visualRenderer == null)
        {
            return;
        }

        visualRenderer.sprite = sprite;
        visualRenderer.color = Color.white;
    }

    private void FindReferences(
        GameObject owner)
    {
        if (owner == null)
        {
            return;
        }

        if (visualRenderer == null)
        {
            visualRenderer =
                owner.GetComponent<SpriteRenderer>();
        }

        if (visualRenderer == null)
        {
            visualRenderer =
                owner.GetComponentInChildren<SpriteRenderer>(
                    true
                );
        }

        if (boxCollider == null)
        {
            boxCollider =
                owner.GetComponent<BoxCollider2D>();
        }
    }

    private void ApplyDefinitionVisual(
        BlockDefinition definition)
    {
        if (visualRenderer == null ||
            definition == null)
        {
            return;
        }

        if (definition.Sprite != null)
        {
            visualRenderer.sprite =
                definition.Sprite;
        }

        visualRenderer.color =
            definition.Color;
    }

    private void ApplyVisualLayout(
        Transform rootTransform,
        BlockDefinition definition,
        Vector2 desiredWorldSize)
    {
        if (visualRenderer == null)
        {
            return;
        }

        Vector3 definitionScale =
            definition != null
                ? definition.VisualScale
                : Vector3.one;

        Vector2 desiredVisualWorldSize =
            new Vector2(
                Mathf.Max(
                    desiredWorldSize.x -
                    visualPadding,
                    0.05f
                ) *
                definitionScale.x,

                Mathf.Max(
                    desiredWorldSize.y -
                    visualPadding,
                    0.05f
                ) *
                definitionScale.y
            );

        Vector2 localRendererSize =
            ConvertWorldSizeToLocalSize(
                visualRenderer.transform,
                desiredVisualWorldSize
            );

        visualRenderer.drawMode =
            SpriteDrawMode.Sliced;

        visualRenderer.size =
            localRendererSize;

        if (visualRenderer.transform ==
            rootTransform)
        {
            return;
        }

        Vector3 localPosition =
            visualRenderer
                .transform
                .localPosition;

        localPosition.x = 0f;
        localPosition.y = 0f;

        visualRenderer.transform.localPosition =
            localPosition;

        visualRenderer.transform.localRotation =
            Quaternion.identity;
    }

    private void ApplyColliderLayout(
        Vector2 desiredWorldSize)
    {
        if (boxCollider == null)
        {
            return;
        }

        Vector2 desiredColliderWorldSize =
            new Vector2(
                Mathf.Max(
                    desiredWorldSize.x -
                    colliderInset * 2f,
                    0.05f
                ),

                Mathf.Max(
                    desiredWorldSize.y -
                    colliderInset * 2f,
                    0.05f
                )
            );

        Vector2 localColliderSize =
            ConvertWorldSizeToLocalSize(
                boxCollider.transform,
                desiredColliderWorldSize
            );

        boxCollider.size =
            localColliderSize;

        boxCollider.offset =
            Vector2.zero;
    }

    private Vector2 ConvertWorldSizeToLocalSize(
        Transform targetTransform,
        Vector2 desiredWorldSize)
    {
        Vector3 lossyScale =
            targetTransform.lossyScale;

        float scaleX =
            Mathf.Max(
                Mathf.Abs(
                    lossyScale.x
                ),
                0.0001f
            );

        float scaleY =
            Mathf.Max(
                Mathf.Abs(
                    lossyScale.y
                ),
                0.0001f
            );

        return new Vector2(
            desiredWorldSize.x /
            scaleX,

            desiredWorldSize.y /
            scaleY
        );
    }
}
