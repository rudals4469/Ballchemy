using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
public sealed class BlockOutlineView :
    MonoBehaviour
{
    private const string CategoryOutlineName =
        "Runtime_CategoryOutline";

    private const string ShieldOutlineName =
        "Runtime_ShieldOutline";

    private const string LegacyShieldOverlayName =
        "Runtime_ShieldOverlay";

    [Header("Category Outline")]
    [SerializeField]
    private bool showCategoryOutline = true;

    [SerializeField]
    private Color rewardColor =
        new Color(
            0.2f,
            1f,
            0.35f,
            0.9f
        );

    [SerializeField]
    private Color dangerColor =
        new Color(
            1f,
            0.2f,
            0.15f,
            0.9f
        );

    [SerializeField]
    private Color urgentColor =
        new Color(
            1f,
            0.6f,
            0.1f,
            0.9f
        );

    [SerializeField, Range(1.01f, 1.3f)]
    private float categoryOutlineScale = 1.1f;

    [SerializeField]
    private int categorySortingOrderOffset = -1;

    [Header("Shield Outline")]
    [SerializeField]
    private bool showShieldOutline = true;

    [SerializeField]
    private Color shieldColor =
        new Color(
            0.1f,
            0.75f,
            1f,
            0.9f
        );

    [SerializeField]
    private Color guardianProtectionColor =
        new Color(
            0.12f,
            0.42f,
            1f,
            0.95f
        );

    [Tooltip(
        "분류 테두리보다 조금 크게 표시해 " +
        "두 아웃라인이 동시에 보이도록 합니다."
    )]
    [SerializeField, Range(1.01f, 1.3f)]
    private float shieldOutlineScale = 1.16f;

    [SerializeField]
    private int shieldSortingOrderOffset = -2;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog;

    private Block block;

    private SpriteRenderer sourceRenderer;
    private SpriteRenderer categoryOutlineRenderer;
    private SpriteRenderer shieldOutlineRenderer;

    private bool isSubscribed;

    private void Awake()
    {
        FindReferences();
        CreateOutlinesIfNeeded();
        RefreshAll();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();

        CreateOutlinesIfNeeded();
        RefreshAll();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        NormalizeSettings();

        if (!Application.isPlaying)
        {
            return;
        }

        FindReferences();
        CreateOutlinesIfNeeded();
        RefreshAll();
    }

    private void NormalizeSettings()
    {
        categoryOutlineScale =
            Mathf.Clamp(
                categoryOutlineScale,
                1.01f,
                1.3f
            );

        shieldOutlineScale =
            Mathf.Clamp(
                shieldOutlineScale,
                1.01f,
                1.3f
            );

        if (categorySortingOrderOffset >= 0)
        {
            categorySortingOrderOffset = -1;
        }

        if (shieldSortingOrderOffset >=
            categorySortingOrderOffset)
        {
            shieldSortingOrderOffset =
                categorySortingOrderOffset - 1;
        }
    }

    private void FindReferences()
    {
        if (block == null)
        {
            block =
                GetComponent<Block>();
        }

        if (categoryOutlineRenderer == null)
        {
            categoryOutlineRenderer =
                FindRendererByName(
                    CategoryOutlineName
                );
        }

        if (shieldOutlineRenderer == null)
        {
            shieldOutlineRenderer =
                FindRendererByName(
                    ShieldOutlineName
                );

            if (shieldOutlineRenderer == null)
            {
                shieldOutlineRenderer =
                    FindRendererByName(
                        LegacyShieldOverlayName
                    );

                if (shieldOutlineRenderer != null)
                {
                    shieldOutlineRenderer
                        .gameObject.name =
                        ShieldOutlineName;
                }
            }
        }

        sourceRenderer =
            FindSourceRenderer();
    }

    private SpriteRenderer FindRendererByName(
        string objectName)
    {
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
            SpriteRenderer renderer =
                renderers[i];

            if (renderer == null)
            {
                continue;
            }

            if (renderer.gameObject.name ==
                objectName)
            {
                return renderer;
            }
        }

        return null;
    }

    private SpriteRenderer FindSourceRenderer()
    {
        SpriteRenderer rootRenderer =
            GetComponent<SpriteRenderer>();

        if (IsValidSourceRenderer(
                rootRenderer))
        {
            return rootRenderer;
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

            if (IsValidSourceRenderer(
                    candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private bool IsValidSourceRenderer(
        SpriteRenderer candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        string objectName =
            candidate.gameObject.name;

        if (objectName ==
            CategoryOutlineName)
        {
            return false;
        }

        if (objectName ==
            ShieldOutlineName)
        {
            return false;
        }

        if (objectName ==
            LegacyShieldOverlayName)
        {
            return false;
        }

        return true;
    }

    private void SubscribeEvents()
    {
        if (isSubscribed ||
            block == null)
        {
            return;
        }

        block.DefinitionChanged +=
            HandleDefinitionChanged;

        block.LayoutChanged +=
            HandleLayoutChanged;

        block.ShieldChanged +=
            HandleShieldChanged;

        block.GuardianProtectionChanged +=
            HandleGuardianProtectionChanged;

        isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed ||
            block == null)
        {
            return;
        }

        block.DefinitionChanged -=
            HandleDefinitionChanged;

        block.LayoutChanged -=
            HandleLayoutChanged;

        block.ShieldChanged -=
            HandleShieldChanged;

        block.GuardianProtectionChanged -=
            HandleGuardianProtectionChanged;

        isSubscribed = false;
    }

    private void CreateOutlinesIfNeeded()
    {
        if (sourceRenderer == null)
        {
            sourceRenderer =
                FindSourceRenderer();
        }

        if (sourceRenderer == null)
        {
            Debug.LogError(
                "BlockOutlineView: " +
                "기준 SpriteRenderer를 찾지 못했습니다.",
                this
            );

            return;
        }

        categoryOutlineRenderer =
            GetOrCreateOutlineRenderer(
                categoryOutlineRenderer,
                CategoryOutlineName
            );

        shieldOutlineRenderer =
            GetOrCreateOutlineRenderer(
                shieldOutlineRenderer,
                ShieldOutlineName
            );
    }

    private SpriteRenderer
        GetOrCreateOutlineRenderer(
        SpriteRenderer currentRenderer,
        string objectName)
    {
        if (currentRenderer != null)
        {
            currentRenderer.gameObject.name =
                objectName;

            return currentRenderer;
        }

        GameObject outlineObject =
            new GameObject(
                objectName
            );

        outlineObject.layer =
            sourceRenderer.gameObject.layer;

        outlineObject.transform.SetParent(
            sourceRenderer.transform,
            false
        );

        return outlineObject.AddComponent<
            SpriteRenderer
        >();
    }

    private void RefreshAll()
    {
        if (block == null ||
            sourceRenderer == null)
        {
            return;
        }

        CreateOutlinesIfNeeded();

        RefreshOutlineGeometry(
            categoryOutlineRenderer,
            categoryOutlineScale,
            categorySortingOrderOffset
        );

        RefreshOutlineGeometry(
            shieldOutlineRenderer,
            shieldOutlineScale,
            shieldSortingOrderOffset
        );

        RefreshCategoryOutline();
        RefreshShieldOutline();
    }

    private void RefreshCategoryOutline()
    {
        if (categoryOutlineRenderer == null)
        {
            return;
        }

        SpecialBlockCategory category =
            GetCurrentCategory();

        bool shouldShow =
            showCategoryOutline &&
            block != null &&
            block.IsAlive &&
            category !=
            SpecialBlockCategory.None;

        categoryOutlineRenderer.enabled =
            shouldShow;

        if (!shouldShow)
        {
            return;
        }

        categoryOutlineRenderer.color =
            GetCategoryColor(
                category
            );
    }

    private void RefreshShieldOutline()
    {
        if (shieldOutlineRenderer == null)
        {
            return;
        }

        bool shouldShow =
            showShieldOutline &&
            block != null &&
            block.IsAlive &&
            (block.HasShield ||
             block.IsGuardianProtected);

        shieldOutlineRenderer.enabled =
            shouldShow;

        if (!shouldShow)
        {
            return;
        }

        shieldOutlineRenderer.color =
            block.IsGuardianProtected
                ? guardianProtectionColor
                : shieldColor;
    }

    private void RefreshOutlineGeometry(
        SpriteRenderer outlineRenderer,
        float outlineScale,
        int sortingOrderOffset)
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

        outlineRenderer.sortingLayerID =
            sourceRenderer.sortingLayerID;

        outlineRenderer.sortingOrder =
            sourceRenderer.sortingOrder +
            sortingOrderOffset;

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

    private SpecialBlockCategory
        GetCurrentCategory()
    {
        if (block == null ||
            block.Definition == null)
        {
            return SpecialBlockCategory.None;
        }

        if (block.BlockType !=
            BlockType.Special)
        {
            return SpecialBlockCategory.None;
        }

        return block.Definition
            .SpecialCategory;
    }

    private Color GetCategoryColor(
        SpecialBlockCategory category)
    {
        switch (category)
        {
            case SpecialBlockCategory.Reward:
                return rewardColor;

            case SpecialBlockCategory
                .DangerOnDestroy:
                return dangerColor;

            case SpecialBlockCategory.Urgent:
                return urgentColor;

            default:
                return Color.clear;
        }
    }

    private void HandleDefinitionChanged(
        BlockDefinition definition)
    {
        FindReferences();
        RefreshAll();
    }

    private void HandleLayoutChanged(
        Vector2Int gridSize,
        float cellSize)
    {
        FindReferences();
        RefreshAll();
    }

    private void HandleShieldChanged(
        Block changedBlock,
        int currentShieldHitCount)
    {
        if (changedBlock != block)
        {
            return;
        }

        RefreshShieldOutline();

        if (showDebugLog)
        {
            Debug.Log(
                "BlockOutlineView: " +
                $"{block.name} 쉴드 갱신, " +
                $"현재 쉴드={currentShieldHitCount}",
                this
            );
        }
    }

    private void HandleGuardianProtectionChanged(
        Block changedBlock,
        bool isProtected)
    {
        if (changedBlock != block)
        {
            return;
        }

        RefreshShieldOutline();
    }
}
