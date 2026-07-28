using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
[RequireComponent(typeof(BlockElementStatus))]
public sealed class BlockBurnFrostStatusView :
    MonoBehaviour
{
    private enum StatusIntensity
    {
        Weak = 0,
        Medium = 1,
        Strong = 2
    }

    private const int CurrentVisualPresetVersion = 1;

    private static Material sharedRuntimeOutlineMaterial;

    [Header("References")]
    [SerializeField]
    private Block block;

    [SerializeField]
    private BlockElementStatus elementStatus;

    [SerializeField]
    private SpriteRenderer baseRenderer;

    [Tooltip(
        "불·얼음 블록 내부 색상을 표시합니다. " +
        "플레이 시작 시 없으면 자동 생성합니다."
    )]
    [SerializeField]
    private SpriteRenderer surfaceRenderer;

    [Tooltip(
        "불·얼음 외곽 테두리를 표시합니다. " +
        "플레이 시작 시 없으면 자동 생성합니다."
    )]
    [SerializeField]
    private LineRenderer outlineRenderer;

    [Header("Surface Renderer")]
    [SerializeField]
    private string surfaceVisualObjectName =
        "ElementSurfaceVisual";

    [SerializeField, Min(0.1f)]
    private float visualScaleMultiplier = 0.96f;

    [Tooltip(
        "물·전기 상태 표시보다 위에 표시합니다."
    )]
    [SerializeField]
    private int sortingOrderOffset = 3;

    [SerializeField]
    private bool copyBaseMaterial = true;

    [Header("Outline Renderer")]
    [SerializeField]
    private string outlineVisualObjectName =
        "ElementSurfaceOutline";

    [SerializeField, Min(0f)]
    private float outlinePadding = 0.055f;

    [SerializeField, Min(0.001f)]
    private float outlineWidth = 0.065f;

    [SerializeField]
    private int outlineSortingOrderOffset = 4;

    [SerializeField, Range(0, 8)]
    private int outlineCornerVertices = 4;

    [SerializeField]
    private Material outlineMaterial;

    [Header("Fire Sprites")]
    [SerializeField]
    private Sprite fireWeakSprite;

    [SerializeField]
    private Sprite fireMediumSprite;

    [SerializeField]
    private Sprite fireStrongSprite;

    [Header("Fire Fill Colors")]
    [SerializeField]
    private Color fireWeakColor =
        new Color32(
            190,
            45,
            12,
            72
        );

    [SerializeField]
    private Color fireMediumColor =
        new Color32(
            210,
            30,
            6,
            102
        );

    [SerializeField]
    private Color fireStrongColor =
        new Color32(
            230,
            20,
            2,
            132
        );

    [Header("Fire Outline Colors")]
    [SerializeField]
    private Color fireWeakOutlineColor =
        new Color32(
            255,
            170,
            70,
            190
        );

    [SerializeField]
    private Color fireMediumOutlineColor =
        new Color32(
            255,
            115,
            25,
            220
        );

    [SerializeField]
    private Color fireStrongOutlineColor =
        new Color32(
            255,
            65,
            8,
            245
        );

    [Header("Ice Sprites")]
    [SerializeField]
    private Sprite iceWeakSprite;

    [SerializeField]
    private Sprite iceMediumSprite;

    [SerializeField]
    private Sprite iceStrongSprite;

    [Header("Ice Fill Colors")]
    [SerializeField]
    private Color iceWeakColor =
        new Color32(
            15,
            100,
            160,
            68
        );

    [SerializeField]
    private Color iceMediumColor =
        new Color32(
            5,
            120,
            190,
            96
        );

    [SerializeField]
    private Color iceStrongColor =
        new Color32(
            0,
            145,
            215,
            122
        );

    [Header("Ice Outline Colors")]
    [SerializeField]
    private Color iceWeakOutlineColor =
        new Color32(
            190,
            245,
            255,
            195
        );

    [SerializeField]
    private Color iceMediumOutlineColor =
        new Color32(
            125,
            225,
            255,
            225
        );

    [SerializeField]
    private Color iceStrongOutlineColor =
        new Color32(
            225,
            255,
            255,
            245
        );

    [Header("Outline Pulse")]
    [SerializeField]
    private bool usePulseAnimation = true;

    [SerializeField, Min(0f)]
    private float firePulseSpeed = 4f;

    [SerializeField, Min(0f)]
    private float icePulseSpeed = 1.8f;

    [SerializeField, Range(0f, 1f)]
    private float weakPulseAmount = 0.05f;

    [SerializeField, Range(0f, 1f)]
    private float mediumPulseAmount = 0.09f;

    [SerializeField, Range(0f, 1f)]
    private float strongPulseAmount = 0.14f;

    [SerializeField, HideInInspector]
    private int visualPresetVersion;

    private Color currentFillColor =
        Color.clear;

    private Color currentOutlineColor =
        Color.clear;

    private float currentPulseSpeed;
    private float currentPulseAmount;

    private bool isSurfaceVisible;
    private int currentStack;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration
    )]
    private static void ResetStaticState()
    {
        sharedRuntimeOutlineMaterial = null;
    }

    private void Awake()
    {
        UpgradeVisualPresetIfNeeded();
        NormalizeSettings();

        FindReferences();
        EnsureSurfaceRenderer();
        EnsureOutlineRenderer();

        SynchronizeRendererLayout();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        FindReferences();
        EnsureSurfaceRenderer();
        EnsureOutlineRenderer();

        SubscribeEvents();

        SynchronizeRendererLayout();
        RefreshSurfaceVisual();
    }

    private void Start()
    {
        SynchronizeRendererLayout();
        RefreshSurfaceVisual();
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            UnsubscribeEvents();
        }

        HideSurfaceVisual();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void Update()
    {
        UpdatePulseAnimation();
    }

    private void LateUpdate()
    {
        if (isSurfaceVisible)
        {
            UpdateOutlineGeometry();
        }
    }

    private void OnValidate()
    {
        UpgradeVisualPresetIfNeeded();
        NormalizeSettings();
    }

    [ContextMenu(
        "Apply Distinct Element Visual Preset"
    )]
    private void ApplyDistinctElementVisualPreset()
    {
        visualScaleMultiplier = 0.96f;

        sortingOrderOffset = 3;

        outlinePadding = 0.055f;
        outlineWidth = 0.065f;
        outlineSortingOrderOffset = 4;
        outlineCornerVertices = 4;

        fireWeakColor =
            new Color32(
                190,
                45,
                12,
                72
            );

        fireMediumColor =
            new Color32(
                210,
                30,
                6,
                102
            );

        fireStrongColor =
            new Color32(
                230,
                20,
                2,
                132
            );

        fireWeakOutlineColor =
            new Color32(
                255,
                170,
                70,
                190
            );

        fireMediumOutlineColor =
            new Color32(
                255,
                115,
                25,
                220
            );

        fireStrongOutlineColor =
            new Color32(
                255,
                65,
                8,
                245
            );

        iceWeakColor =
            new Color32(
                15,
                100,
                160,
                68
            );

        iceMediumColor =
            new Color32(
                5,
                120,
                190,
                96
            );

        iceStrongColor =
            new Color32(
                0,
                145,
                215,
                122
            );

        iceWeakOutlineColor =
            new Color32(
                190,
                245,
                255,
                195
            );

        iceMediumOutlineColor =
            new Color32(
                125,
                225,
                255,
                225
            );

        iceStrongOutlineColor =
            new Color32(
                225,
                255,
                255,
                245
            );

        firePulseSpeed = 4f;
        icePulseSpeed = 1.8f;

        weakPulseAmount = 0.05f;
        mediumPulseAmount = 0.09f;
        strongPulseAmount = 0.14f;

        visualPresetVersion =
            CurrentVisualPresetVersion;

        NormalizeSettings();

        if (Application.isPlaying)
        {
            SynchronizeRendererLayout();
            RefreshSurfaceVisual();
        }
    }

    private void UpgradeVisualPresetIfNeeded()
    {
        if (visualPresetVersion >=
            CurrentVisualPresetVersion)
        {
            return;
        }

        ApplyDistinctElementVisualPreset();
    }

    private void NormalizeSettings()
    {
        visualScaleMultiplier =
            Mathf.Max(
                visualScaleMultiplier,
                0.1f
            );

        outlinePadding =
            Mathf.Max(
                outlinePadding,
                0f
            );

        outlineWidth =
            Mathf.Max(
                outlineWidth,
                0.001f
            );

        outlineCornerVertices =
            Mathf.Clamp(
                outlineCornerVertices,
                0,
                8
            );

        firePulseSpeed =
            Mathf.Max(
                firePulseSpeed,
                0f
            );

        icePulseSpeed =
            Mathf.Max(
                icePulseSpeed,
                0f
            );

        weakPulseAmount =
            Mathf.Clamp01(
                weakPulseAmount
            );

        mediumPulseAmount =
            Mathf.Clamp01(
                mediumPulseAmount
            );

        strongPulseAmount =
            Mathf.Clamp01(
                strongPulseAmount
            );

        if (string.IsNullOrWhiteSpace(
                surfaceVisualObjectName))
        {
            surfaceVisualObjectName =
                "ElementSurfaceVisual";
        }

        if (string.IsNullOrWhiteSpace(
                outlineVisualObjectName))
        {
            outlineVisualObjectName =
                "ElementSurfaceOutline";
        }
    }

    private void FindReferences()
    {
        if (block == null)
        {
            block =
                GetComponent<Block>();
        }

        if (elementStatus == null)
        {
            elementStatus =
                GetComponent<
                    BlockElementStatus
                >();
        }

        FindBaseRenderer();
    }

    private void FindBaseRenderer()
    {
        if (baseRenderer != null &&
            baseRenderer != surfaceRenderer)
        {
            return;
        }

        SpriteRenderer rootRenderer =
            GetComponent<SpriteRenderer>();

        if (rootRenderer != null &&
            rootRenderer != surfaceRenderer)
        {
            baseRenderer =
                rootRenderer;

            return;
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

            if (candidate == null ||
                candidate == surfaceRenderer)
            {
                continue;
            }

            string objectName =
                candidate.gameObject.name;

            if (objectName ==
                    surfaceVisualObjectName ||
                objectName ==
                    "ElementStatusVisual" ||
                objectName ==
                    "ElectrocutionVfx" ||
                objectName ==
                    "ElementConductionVfx")
            {
                continue;
            }

            baseRenderer =
                candidate;

            return;
        }
    }

    private void EnsureSurfaceRenderer()
    {
        if (surfaceRenderer != null)
        {
            return;
        }

        if (!Application.isPlaying)
        {
            return;
        }

        FindBaseRenderer();

        if (baseRenderer == null)
        {
            Debug.LogWarning(
                "BlockElementSurfaceStatusView: " +
                "기본 SpriteRenderer를 찾지 못했습니다.",
                this
            );

            return;
        }

        Transform existingTransform =
            baseRenderer.transform.Find(
                surfaceVisualObjectName
            );

        if (existingTransform != null)
        {
            surfaceRenderer =
                existingTransform.GetComponent<
                    SpriteRenderer
                >();
        }

        if (surfaceRenderer == null)
        {
            GameObject surfaceObject =
                new GameObject(
                    surfaceVisualObjectName
                );

            surfaceObject.transform.SetParent(
                baseRenderer.transform,
                false
            );

            surfaceRenderer =
                surfaceObject.AddComponent<
                    SpriteRenderer
                >();
        }

        ResetSurfaceRendererTransform();
        CopyRendererSettings();

        surfaceRenderer.enabled = false;
    }

    private void EnsureOutlineRenderer()
    {
        if (outlineRenderer != null)
        {
            return;
        }

        if (!Application.isPlaying ||
            baseRenderer == null)
        {
            return;
        }

        Transform existingTransform =
            baseRenderer.transform.Find(
                outlineVisualObjectName
            );

        if (existingTransform != null)
        {
            outlineRenderer =
                existingTransform.GetComponent<
                    LineRenderer
                >();
        }

        if (outlineRenderer == null)
        {
            GameObject outlineObject =
                new GameObject(
                    outlineVisualObjectName
                );

            outlineObject.transform.SetParent(
                baseRenderer.transform,
                false
            );

            outlineRenderer =
                outlineObject.AddComponent<
                    LineRenderer
                >();
        }

        ConfigureOutlineRenderer();

        outlineRenderer.enabled = false;
    }

    private void ConfigureOutlineRenderer()
    {
        if (outlineRenderer == null)
        {
            return;
        }

        outlineRenderer.useWorldSpace = true;
        outlineRenderer.loop = true;
        outlineRenderer.positionCount = 4;

        outlineRenderer.alignment =
            LineAlignment.View;

        outlineRenderer.textureMode =
            LineTextureMode.Stretch;

        outlineRenderer.startWidth =
            outlineWidth;

        outlineRenderer.endWidth =
            outlineWidth;

        outlineRenderer.numCapVertices =
            outlineCornerVertices;

        outlineRenderer.numCornerVertices =
            outlineCornerVertices;

        Material resolvedMaterial =
            ResolveOutlineMaterial();

        if (resolvedMaterial != null)
        {
            outlineRenderer.sharedMaterial =
                resolvedMaterial;
        }
    }

    private Material ResolveOutlineMaterial()
    {
        if (outlineMaterial != null)
        {
            return outlineMaterial;
        }

        if (sharedRuntimeOutlineMaterial != null)
        {
            return sharedRuntimeOutlineMaterial;
        }

        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/2D/" +
                "Sprite-Unlit-Default"
            );

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Sprites/Default"
                );
        }

        if (shader == null)
        {
            Debug.LogWarning(
                "BlockElementSurfaceStatusView: " +
                "테두리용 Shader를 찾지 못했습니다.",
                this
            );

            return null;
        }

        sharedRuntimeOutlineMaterial =
            new Material(
                shader
            );

        sharedRuntimeOutlineMaterial.name =
            "Runtime_BlockElementSurfaceOutline";

        sharedRuntimeOutlineMaterial.hideFlags =
            HideFlags.HideAndDontSave;

        return sharedRuntimeOutlineMaterial;
    }

    private void ResetSurfaceRendererTransform()
    {
        if (surfaceRenderer == null)
        {
            return;
        }

        Transform surfaceTransform =
            surfaceRenderer.transform;

        surfaceTransform.localPosition =
            Vector3.zero;

        surfaceTransform.localRotation =
            Quaternion.identity;

        surfaceTransform.localScale =
            Vector3.one *
            visualScaleMultiplier;
    }

    private void CopyRendererSettings()
    {
        if (baseRenderer == null)
        {
            return;
        }

        if (surfaceRenderer != null)
        {
            surfaceRenderer.sortingLayerID =
                baseRenderer.sortingLayerID;

            surfaceRenderer.sortingOrder =
                baseRenderer.sortingOrder +
                sortingOrderOffset;

            surfaceRenderer.maskInteraction =
                baseRenderer.maskInteraction;

            surfaceRenderer.spriteSortPoint =
                baseRenderer.spriteSortPoint;

            surfaceRenderer.flipX =
                baseRenderer.flipX;

            surfaceRenderer.flipY =
                baseRenderer.flipY;

            if (copyBaseMaterial)
            {
                surfaceRenderer.sharedMaterial =
                    baseRenderer.sharedMaterial;
            }
        }

        if (outlineRenderer != null)
        {
            outlineRenderer.sortingLayerID =
                baseRenderer.sortingLayerID;

            outlineRenderer.sortingOrder =
                baseRenderer.sortingOrder +
                outlineSortingOrderOffset;

            outlineRenderer.startWidth =
                outlineWidth;

            outlineRenderer.endWidth =
                outlineWidth;
        }
    }

    private void SubscribeEvents()
    {
        UnsubscribeEvents();

        if (elementStatus != null)
        {
            elementStatus.StatusChanged +=
                HandleStatusChanged;

            elementStatus.StatusCleared +=
                HandleStatusCleared;
        }

        if (block != null)
        {
            block.LayoutChanged +=
                HandleLayoutChanged;

            block.DefinitionChanged +=
                HandleDefinitionChanged;
        }
    }

    private void UnsubscribeEvents()
    {
        if (elementStatus != null)
        {
            elementStatus.StatusChanged -=
                HandleStatusChanged;

            elementStatus.StatusCleared -=
                HandleStatusCleared;
        }

        if (block != null)
        {
            block.LayoutChanged -=
                HandleLayoutChanged;

            block.DefinitionChanged -=
                HandleDefinitionChanged;
        }
    }

    private void HandleStatusChanged(
        BlockElementStatus changedStatus)
    {
        RefreshSurfaceVisual();
    }

    private void HandleStatusCleared(
        BlockElementStatus clearedStatus)
    {
        RefreshSurfaceVisual();
    }

    private void HandleLayoutChanged(
        Vector2Int gridSize,
        float cellSize)
    {
        SynchronizeRendererLayout();
    }

    private void HandleDefinitionChanged(
        BlockDefinition definition)
    {
        SynchronizeRendererLayout();
        RefreshSurfaceVisual();
    }

    private void SynchronizeRendererLayout()
    {
        FindBaseRenderer();

        if (Application.isPlaying)
        {
            EnsureSurfaceRenderer();
            EnsureOutlineRenderer();
        }

        if (baseRenderer == null ||
            surfaceRenderer == null)
        {
            return;
        }

        CopyRendererSettings();
        ResetSurfaceRendererTransform();

        surfaceRenderer.drawMode =
            baseRenderer.drawMode;

        surfaceRenderer.size =
            baseRenderer.size;

        UpdateOutlineGeometry();
    }

    private void RefreshSurfaceVisual()
    {
        FindReferences();
        EnsureSurfaceRenderer();
        EnsureOutlineRenderer();

        if (elementStatus == null ||
            surfaceRenderer == null)
        {
            HideSurfaceVisual();
            return;
        }

        int burnStack =
            elementStatus.BurnStack;

        int frostStack =
            elementStatus.FrostStack;

        if (burnStack <= 0 &&
            frostStack <= 0)
        {
            HideSurfaceVisual();
            return;
        }

        ElementType currentElement;

        if (burnStack >= frostStack)
        {
            currentElement =
                ElementType.Fire;

            currentStack =
                burnStack;
        }
        else
        {
            currentElement =
                ElementType.Ice;

            currentStack =
                frostStack;
        }

        StatusIntensity intensity =
            ResolveIntensity(
                currentStack
            );

        Sprite resolvedSprite =
            ResolveSprite(
                currentElement,
                intensity
            );

        if (resolvedSprite == null &&
            baseRenderer != null)
        {
            resolvedSprite =
                baseRenderer.sprite;
        }

        if (resolvedSprite == null)
        {
            HideSurfaceVisual();
            return;
        }

        currentFillColor =
            ResolveFillColor(
                currentElement,
                intensity
            );

        currentOutlineColor =
            ResolveOutlineColor(
                currentElement,
                intensity
            );

        currentPulseSpeed =
            ResolvePulseSpeed(
                currentElement
            );

        currentPulseAmount =
            ResolvePulseAmount(
                intensity
            );

        surfaceRenderer.sprite =
            resolvedSprite;

        surfaceRenderer.color =
            currentFillColor;

        SynchronizeRendererLayout();

        surfaceRenderer.enabled = true;

        if (outlineRenderer != null)
        {
            SetOutlineColor(
                currentOutlineColor
            );

            outlineRenderer.enabled = true;
        }

        isSurfaceVisible = true;
    }

    private StatusIntensity ResolveIntensity(
        int stack)
    {
        if (stack <= 2)
        {
            return StatusIntensity.Weak;
        }

        if (stack <= 4)
        {
            return StatusIntensity.Medium;
        }

        return StatusIntensity.Strong;
    }

    private Sprite ResolveSprite(
        ElementType element,
        StatusIntensity intensity)
    {
        switch (element)
        {
            case ElementType.Fire:
                switch (intensity)
                {
                    case StatusIntensity.Weak:
                        return fireWeakSprite;

                    case StatusIntensity.Medium:
                        return fireMediumSprite;

                    case StatusIntensity.Strong:
                        return fireStrongSprite;
                }

                break;

            case ElementType.Ice:
                switch (intensity)
                {
                    case StatusIntensity.Weak:
                        return iceWeakSprite;

                    case StatusIntensity.Medium:
                        return iceMediumSprite;

                    case StatusIntensity.Strong:
                        return iceStrongSprite;
                }

                break;
        }

        return null;
    }

    private Color ResolveFillColor(
        ElementType element,
        StatusIntensity intensity)
    {
        switch (element)
        {
            case ElementType.Fire:
                switch (intensity)
                {
                    case StatusIntensity.Weak:
                        return fireWeakColor;

                    case StatusIntensity.Medium:
                        return fireMediumColor;

                    case StatusIntensity.Strong:
                        return fireStrongColor;
                }

                break;

            case ElementType.Ice:
                switch (intensity)
                {
                    case StatusIntensity.Weak:
                        return iceWeakColor;

                    case StatusIntensity.Medium:
                        return iceMediumColor;

                    case StatusIntensity.Strong:
                        return iceStrongColor;
                }

                break;
        }

        return Color.clear;
    }

    private Color ResolveOutlineColor(
        ElementType element,
        StatusIntensity intensity)
    {
        switch (element)
        {
            case ElementType.Fire:
                switch (intensity)
                {
                    case StatusIntensity.Weak:
                        return fireWeakOutlineColor;

                    case StatusIntensity.Medium:
                        return fireMediumOutlineColor;

                    case StatusIntensity.Strong:
                        return fireStrongOutlineColor;
                }

                break;

            case ElementType.Ice:
                switch (intensity)
                {
                    case StatusIntensity.Weak:
                        return iceWeakOutlineColor;

                    case StatusIntensity.Medium:
                        return iceMediumOutlineColor;

                    case StatusIntensity.Strong:
                        return iceStrongOutlineColor;
                }

                break;
        }

        return Color.clear;
    }

    private float ResolvePulseSpeed(
        ElementType element)
    {
        switch (element)
        {
            case ElementType.Fire:
                return firePulseSpeed;

            case ElementType.Ice:
                return icePulseSpeed;

            default:
                return 0f;
        }
    }

    private float ResolvePulseAmount(
        StatusIntensity intensity)
    {
        switch (intensity)
        {
            case StatusIntensity.Weak:
                return weakPulseAmount;

            case StatusIntensity.Medium:
                return mediumPulseAmount;

            case StatusIntensity.Strong:
                return strongPulseAmount;

            default:
                return 0f;
        }
    }

    private void UpdatePulseAnimation()
    {
        if (!isSurfaceVisible)
        {
            return;
        }

        if (surfaceRenderer != null)
        {
            surfaceRenderer.color =
                currentFillColor;
        }

        if (outlineRenderer == null ||
            !outlineRenderer.enabled)
        {
            return;
        }

        if (!usePulseAnimation ||
            currentPulseSpeed <= 0f ||
            currentPulseAmount <= 0f)
        {
            SetOutlineColor(
                currentOutlineColor
            );

            return;
        }

        float pulse =
            Mathf.Sin(
                Time.time *
                currentPulseSpeed
            );

        float alphaMultiplier =
            1f +
            pulse *
            currentPulseAmount;

        Color animatedColor =
            currentOutlineColor;

        animatedColor.a =
            Mathf.Clamp01(
                currentOutlineColor.a *
                alphaMultiplier
            );

        SetOutlineColor(
            animatedColor
        );
    }

    private void SetOutlineColor(
        Color color)
    {
        if (outlineRenderer == null)
        {
            return;
        }

        outlineRenderer.startColor =
            color;

        outlineRenderer.endColor =
            color;
    }

    private void UpdateOutlineGeometry()
    {
        if (outlineRenderer == null ||
            baseRenderer == null)
        {
            return;
        }

        Bounds bounds =
            baseRenderer.bounds;

        float minX =
            bounds.min.x -
            outlinePadding;

        float maxX =
            bounds.max.x +
            outlinePadding;

        float minY =
            bounds.min.y -
            outlinePadding;

        float maxY =
            bounds.max.y +
            outlinePadding;

        float z =
            bounds.center.z;

        outlineRenderer.positionCount = 4;

        outlineRenderer.SetPosition(
            0,
            new Vector3(
                minX,
                minY,
                z
            )
        );

        outlineRenderer.SetPosition(
            1,
            new Vector3(
                minX,
                maxY,
                z
            )
        );

        outlineRenderer.SetPosition(
            2,
            new Vector3(
                maxX,
                maxY,
                z
            )
        );

        outlineRenderer.SetPosition(
            3,
            new Vector3(
                maxX,
                minY,
                z
            )
        );
    }

    private void HideSurfaceVisual()
    {
        isSurfaceVisible = false;

        currentStack = 0;
        currentPulseSpeed = 0f;
        currentPulseAmount = 0f;

        currentFillColor =
            Color.clear;

        currentOutlineColor =
            Color.clear;

        if (surfaceRenderer != null)
        {
            surfaceRenderer.enabled = false;
            surfaceRenderer.color = Color.clear;
        }

        if (outlineRenderer != null)
        {
            outlineRenderer.enabled = false;

            SetOutlineColor(
                Color.clear
            );
        }
    }
}