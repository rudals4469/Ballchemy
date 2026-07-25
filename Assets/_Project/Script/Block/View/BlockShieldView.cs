using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
public sealed class BlockShieldView :
    MonoBehaviour
{
    private const string OutlineObjectName =
        "Runtime_ShieldOutline";

    private const string LegacyOverlayObjectName =
        "Runtime_ShieldOverlay";

    [Header("Outline")]
    [SerializeField]
    private Color outlineColor =
        new Color(
            0.1f,
            0.75f,
            1f,
            0.9f
        );

    [Tooltip(
        "원본 블록보다 외곽선 스프라이트를 " +
        "얼마나 크게 표시할지 결정합니다."
    )]
    [SerializeField, Range(1.01f, 1.3f)]
    private float outlineScale = 1.1f;

    [Tooltip(
        "원본 스프라이트보다 뒤에서 " +
        "그려지도록 음수 값을 사용합니다."
    )]
    [SerializeField]
    private int sortingOrderOffset = -1;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog;

    private Block block;

    private SpriteRenderer sourceRenderer;
    private SpriteRenderer outlineRenderer;

    private void Awake()
    {
        FindReferences();
        CreateOutlineIfNeeded();
        RefreshOutlineGeometry();
        RefreshVisibility();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();

        CreateOutlineIfNeeded();
        RefreshOutlineGeometry();
        RefreshVisibility();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        outlineScale =
            Mathf.Clamp(
                outlineScale,
                1.01f,
                1.3f
            );

        if (sortingOrderOffset >= 0)
        {
            sortingOrderOffset = -1;
        }

        FindReferences();

        if (outlineRenderer != null)
        {
            outlineRenderer.color =
                outlineColor;

            ApplyOutlineTransform();
            ApplySorting();
        }
    }

    private void FindReferences()
    {
        if (block == null)
        {
            block =
                GetComponent<Block>();
        }

        SpriteRenderer[] renderers =
            GetComponentsInChildren<
                SpriteRenderer
            >(
                true
            );

        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            SpriteRenderer candidate =
                renderers[i];

            if (candidate == null)
            {
                continue;
            }

            bool isOutlineRenderer =
                candidate.gameObject.name ==
                OutlineObjectName ||
                candidate.gameObject.name ==
                LegacyOverlayObjectName;

            if (isOutlineRenderer)
            {
                if (outlineRenderer == null)
                {
                    outlineRenderer =
                        candidate;

                    outlineRenderer.gameObject.name =
                        OutlineObjectName;
                }

                continue;
            }

            if (sourceRenderer == null)
            {
                sourceRenderer =
                    candidate;
            }
        }
    }

    private void SubscribeEvents()
    {
        if (block == null)
        {
            return;
        }

        block.ShieldChanged -=
            HandleShieldChanged;

        block.ShieldChanged +=
            HandleShieldChanged;

        block.DefinitionChanged -=
            HandleDefinitionChanged;

        block.DefinitionChanged +=
            HandleDefinitionChanged;

        block.LayoutChanged -=
            HandleLayoutChanged;

        block.LayoutChanged +=
            HandleLayoutChanged;
    }

    private void UnsubscribeEvents()
    {
        if (block == null)
        {
            return;
        }

        block.ShieldChanged -=
            HandleShieldChanged;

        block.DefinitionChanged -=
            HandleDefinitionChanged;

        block.LayoutChanged -=
            HandleLayoutChanged;
    }

    private void CreateOutlineIfNeeded()
    {
        if (outlineRenderer != null)
        {
            ApplyOutlineTransform();
            return;
        }

        if (sourceRenderer == null)
        {
            FindReferences();
        }

        if (sourceRenderer == null)
        {
            Debug.LogError(
                "BlockShieldView: " +
                "기준 SpriteRenderer를 찾지 못했습니다.",
                this
            );

            return;
        }

        GameObject outlineObject =
            new GameObject(
                OutlineObjectName
            );

        outlineObject.layer =
            sourceRenderer.gameObject.layer;

        outlineObject.transform.SetParent(
            sourceRenderer.transform,
            false
        );

        outlineRenderer =
            outlineObject.AddComponent<
                SpriteRenderer
            >();

        ApplyOutlineTransform();
    }

    private void ApplyOutlineTransform()
    {
        if (outlineRenderer == null ||
            sourceRenderer == null)
        {
            return;
        }

        Transform outlineTransform =
            outlineRenderer.transform;

        if (outlineTransform.parent !=
            sourceRenderer.transform)
        {
            outlineTransform.SetParent(
                sourceRenderer.transform,
                false
            );
        }

        outlineTransform.localPosition =
            Vector3.zero;

        outlineTransform.localRotation =
            Quaternion.identity;

        outlineTransform.localScale =
            new Vector3(
                outlineScale,
                outlineScale,
                1f
            );
    }

    private void RefreshOutlineGeometry()
    {
        if (sourceRenderer == null ||
            outlineRenderer == null)
        {
            return;
        }

        outlineRenderer.sprite =
            sourceRenderer.sprite;

        outlineRenderer.drawMode =
            sourceRenderer.drawMode;

        outlineRenderer.size =
            sourceRenderer.size;

        outlineRenderer.flipX =
            sourceRenderer.flipX;

        outlineRenderer.flipY =
            sourceRenderer.flipY;

        outlineRenderer.maskInteraction =
            sourceRenderer.maskInteraction;

        outlineRenderer.sharedMaterial =
            sourceRenderer.sharedMaterial;

        outlineRenderer.color =
            outlineColor;

        ApplyOutlineTransform();
        ApplySorting();
    }

    private void ApplySorting()
    {
        if (sourceRenderer == null ||
            outlineRenderer == null)
        {
            return;
        }

        outlineRenderer.sortingLayerID =
            sourceRenderer.sortingLayerID;

        outlineRenderer.sortingOrder =
            sourceRenderer.sortingOrder +
            sortingOrderOffset;
    }

    private void RefreshVisibility()
    {
        if (outlineRenderer == null)
        {
            return;
        }

        bool shouldShow =
            block != null &&
            block.HasShield &&
            block.IsAlive;

        outlineRenderer.enabled =
            shouldShow;
    }

    private void HandleShieldChanged(
        Block changedBlock,
        int currentShieldHitCount)
    {
        if (changedBlock != block)
        {
            return;
        }

        RefreshVisibility();

        if (showDebugLog)
        {
            Debug.Log(
                "BlockShieldView: " +
                $"{block.name} 쉴드 외곽선 갱신, " +
                $"현재 쉴드={currentShieldHitCount}",
                this
            );
        }
    }

    private void HandleDefinitionChanged(
        BlockDefinition blockDefinition)
    {
        FindReferences();
        RefreshOutlineGeometry();
        RefreshVisibility();
    }

    private void HandleLayoutChanged(
        Vector2Int gridSize,
        float cellSize)
    {
        FindReferences();
        RefreshOutlineGeometry();
        RefreshVisibility();
    }
}