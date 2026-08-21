using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
[RequireComponent(typeof(BlockElementStatus))]
public sealed class BlockWetChargeStatusView :
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

    [Tooltip(
        "블록의 기본 외형을 표시하는 SpriteRenderer입니다. " +
        "비워두면 자동으로 찾습니다."
    )]
    [SerializeField]
    private SpriteRenderer baseRenderer;

    [Tooltip(
        "블록 내부 속성 색상을 표시하는 SpriteRenderer입니다. " +
        "플레이 시작 시 없으면 자동 생성합니다."
    )]
    [SerializeField]
    private SpriteRenderer statusRenderer;

    [Tooltip(
        "속성 테두리를 표시하는 LineRenderer입니다. " +
        "플레이 시작 시 없으면 자동 생성합니다."
    )]
    [SerializeField]
    private LineRenderer outlineRenderer;

    [Header("Status Renderer")]
    [SerializeField]
    private string statusVisualObjectName =
        "ElementStatusVisual";

    [Tooltip(
        "기본 블록보다 내부 틴트를 조금 작게 표시합니다."
    )]
    [SerializeField, Min(0.1f)]
    private float visualScaleMultiplier = 0.98f;

    [SerializeField]
    private int sortingOrderOffset = 1;

    [SerializeField]
    private bool copyBaseMaterial = true;

    [Header("Outline Renderer")]
    [SerializeField]
    private string outlineVisualObjectName =
        "ElementStatusOutline";

    [Tooltip(
        "블록 외곽에서 테두리를 얼마나 바깥쪽으로 " +
        "확장할지 결정합니다."
    )]
    [SerializeField, Min(0f)]
    private float outlinePadding = 0.025f;

    [SerializeField, Min(0.001f)]
    private float outlineWidth = 0.06f;

    [SerializeField]
    private int outlineSortingOrderOffset = 2;

    [SerializeField, Range(0, 8)]
    private int outlineCornerVertices = 4;

    [Tooltip(
        "비워두면 URP 2D용 머티리얼을 " +
        "실행 중 자동 생성합니다."
    )]
    [SerializeField]
    private Material outlineMaterial;

    [Header("Water Sprites")]
    [SerializeField]
    private Sprite waterWeakSprite;

    [SerializeField]
    private Sprite waterMediumSprite;

    [SerializeField]
    private Sprite waterStrongSprite;

    [Header("Water Fill Colors")]
    [SerializeField]
    private Color waterWeakColor =
        new Color32(
            18,
            126,
            176,
            72
        );

    [SerializeField]
    private Color waterMediumColor =
        new Color32(
            10,
            107,
            175,
            102
        );

    [SerializeField]
    private Color waterStrongColor =
        new Color32(
            0,
            82,
            160,
            132
        );

    [Header("Water Outline Colors")]
    [SerializeField]
    private Color waterWeakOutlineColor =
        new Color32(
            95,
            220,
            255,
            190
        );

    [SerializeField]
    private Color waterMediumOutlineColor =
        new Color32(
            45,
            195,
            255,
            220
        );

    [SerializeField]
    private Color waterStrongOutlineColor =
        new Color32(
            10,
            170,
            255,
            245
        );

    [Header("Electric Sprites")]
    [SerializeField]
    private Sprite electricWeakSprite;

    [SerializeField]
    private Sprite electricMediumSprite;

    [SerializeField]
    private Sprite electricStrongSprite;

    [Header("Electric Fill Colors")]
    [Tooltip(
        "전기 상태 내부는 노란 네모처럼 보이지 않도록 " +
        "어두운 보라색으로 표시합니다."
    )]
    [SerializeField]
    private Color electricWeakColor =
        new Color32(
            58,
            40,
            125,
            72
        );

    [SerializeField]
    private Color electricMediumColor =
        new Color32(
            75,
            38,
            145,
            102
        );

    [SerializeField]
    private Color electricStrongColor =
        new Color32(
            92,
            35,
            165,
            132
        );

    [Header("Electric Outline Colors")]
    [SerializeField]
    private Color electricWeakOutlineColor =
        new Color32(
            255,
            235,
            110,
            190
        );

    [SerializeField]
    private Color electricMediumOutlineColor =
        new Color32(
            255,
            210,
            45,
            220
        );

    [SerializeField]
    private Color electricStrongOutlineColor =
        new Color32(
            255,
            178,
            10,
            245
        );

    [Header("Outline Pulse")]
    [Tooltip(
        "블록 전체가 아니라 테두리만 약하게 맥동합니다."
    )]
    [SerializeField]
    private bool usePulseAnimation = true;

    [SerializeField, Min(0f)]
    private float waterPulseSpeed = 2.2f;

    [SerializeField, Min(0f)]
    private float electricPulseSpeed = 6.5f;

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

    private bool isStatusVisible;
    private ElementType currentElement;
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
        EnsureStatusRenderer();
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
        EnsureStatusRenderer();
        EnsureOutlineRenderer();

        SubscribeEvents();

        SynchronizeRendererLayout();
        RefreshStatusVisual();
    }

    private void Start()
    {
        SynchronizeRendererLayout();
        RefreshStatusVisual();
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
        {
            UnsubscribeEvents();
        }

        HideStatusVisual();
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
        if (isStatusVisible)
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
        visualScaleMultiplier = 0.98f;

        outlinePadding = 0.025f;
        outlineWidth = 0.06f;
        outlineCornerVertices = 4;

        waterWeakColor =
            new Color32(
                18,
                126,
                176,
                72
            );

        waterMediumColor =
            new Color32(
                10,
                107,
                175,
                102
            );

        waterStrongColor =
            new Color32(
                0,
                82,
                160,
                132
            );

        waterWeakOutlineColor =
            new Color32(
                95,
                220,
                255,
                190
            );

        waterMediumOutlineColor =
            new Color32(
                45,
                195,
                255,
                220
            );

        waterStrongOutlineColor =
            new Color32(
                10,
                170,
                255,
                245
            );

        electricWeakColor =
            new Color32(
                58,
                40,
                125,
                72
            );

        electricMediumColor =
            new Color32(
                75,
                38,
                145,
                102
            );

        electricStrongColor =
            new Color32(
                92,
                35,
                165,
                132
            );

        electricWeakOutlineColor =
            new Color32(
                255,
                235,
                110,
                190
            );

        electricMediumOutlineColor =
            new Color32(
                255,
                210,
                45,
                220
            );

        electricStrongOutlineColor =
            new Color32(
                255,
                178,
                10,
                245
            );

        waterPulseSpeed = 2.2f;
        electricPulseSpeed = 6.5f;

        weakPulseAmount = 0.05f;
        mediumPulseAmount = 0.09f;
        strongPulseAmount = 0.14f;

        visualPresetVersion =
            CurrentVisualPresetVersion;

        NormalizeSettings();

        if (Application.isPlaying)
        {
            SynchronizeRendererLayout();
            RefreshStatusVisual();
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

        waterPulseSpeed =
            Mathf.Max(
                waterPulseSpeed,
                0f
            );

        electricPulseSpeed =
            Mathf.Max(
                electricPulseSpeed,
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
                statusVisualObjectName))
        {
            statusVisualObjectName =
                "ElementStatusVisual";
        }

        if (string.IsNullOrWhiteSpace(
                outlineVisualObjectName))
        {
            outlineVisualObjectName =
                "ElementStatusOutline";
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
            baseRenderer != statusRenderer)
        {
            return;
        }

        SpriteRenderer rootRenderer =
            GetComponent<SpriteRenderer>();

        if (rootRenderer != null &&
            rootRenderer != statusRenderer)
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
                candidate == statusRenderer)
            {
                continue;
            }

            string objectName =
                candidate.gameObject.name;

            if (objectName ==
                    statusVisualObjectName ||
                objectName ==
                    "ElementSurfaceVisual" ||
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

    private void EnsureStatusRenderer()
    {
        if (statusRenderer != null)
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
                "BlockElementStatusView: " +
                "기본 SpriteRenderer를 찾지 못했습니다.",
                this
            );

            return;
        }

        Transform existingTransform =
            baseRenderer.transform.Find(
                statusVisualObjectName
            );

        if (existingTransform != null)
        {
            statusRenderer =
                existingTransform.GetComponent<
                    SpriteRenderer
                >();
        }

        if (statusRenderer == null)
        {
            GameObject statusObject =
                new GameObject(
                    statusVisualObjectName
                );

            statusObject.transform.SetParent(
                baseRenderer.transform,
                false
            );

            statusRenderer =
                statusObject.AddComponent<
                    SpriteRenderer
                >();
        }

        ResetStatusRendererTransform();
        CopyRendererSettings();

        statusRenderer.enabled = false;
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
                "BlockElementStatusView: " +
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
            "Runtime_BlockElementStatusOutline";

        sharedRuntimeOutlineMaterial.hideFlags =
            HideFlags.HideAndDontSave;

        return sharedRuntimeOutlineMaterial;
    }

    private void ResetStatusRendererTransform()
    {
        if (statusRenderer == null)
        {
            return;
        }

        Transform statusTransform =
            statusRenderer.transform;

        statusTransform.localPosition =
            Vector3.zero;

        statusTransform.localRotation =
            Quaternion.identity;

        statusTransform.localScale =
            Vector3.one *
            visualScaleMultiplier;
    }

    private void CopyRendererSettings()
    {
        if (baseRenderer == null)
        {
            return;
        }

        if (statusRenderer != null)
        {
            statusRenderer.sortingLayerID =
                baseRenderer.sortingLayerID;

            statusRenderer.sortingOrder =
                baseRenderer.sortingOrder +
                sortingOrderOffset;

            statusRenderer.maskInteraction =
                baseRenderer.maskInteraction;

            statusRenderer.spriteSortPoint =
                baseRenderer.spriteSortPoint;

            statusRenderer.flipX =
                baseRenderer.flipX;

            statusRenderer.flipY =
                baseRenderer.flipY;

            if (copyBaseMaterial)
            {
                statusRenderer.sharedMaterial =
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
        RefreshStatusVisual();
    }

    private void HandleStatusCleared(
        BlockElementStatus clearedStatus)
    {
        RefreshStatusVisual();
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
        RefreshStatusVisual();
    }

    private void SynchronizeRendererLayout()
    {
        FindBaseRenderer();

        if (Application.isPlaying)
        {
            EnsureStatusRenderer();
            EnsureOutlineRenderer();
        }

        if (baseRenderer == null ||
            statusRenderer == null)
        {
            return;
        }

        CopyRendererSettings();
        ResetStatusRendererTransform();

        statusRenderer.drawMode =
            baseRenderer.drawMode;

        statusRenderer.size =
            baseRenderer.size;

        UpdateOutlineGeometry();
    }

    private void RefreshStatusVisual()
    {
        FindReferences();
        EnsureStatusRenderer();
        EnsureOutlineRenderer();

        if (elementStatus == null ||
            statusRenderer == null)
        {
            HideStatusVisual();
            return;
        }

        int wetStack =
            elementStatus.WetStack;

        int chargeStack =
            elementStatus.ChargeStack;

        if (wetStack <= 0 &&
            chargeStack <= 0)
        {
            HideStatusVisual();
            return;
        }

        if (wetStack >= chargeStack)
        {
            currentElement =
                ElementType.Water;

            currentStack =
                wetStack;
        }
        else
        {
            currentElement =
                ElementType.Electric;

            currentStack =
                chargeStack;
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
            HideStatusVisual();
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

        statusRenderer.sprite =
            resolvedSprite;

        statusRenderer.color =
            currentFillColor;

        SynchronizeRendererLayout();

        statusRenderer.enabled = true;

        if (outlineRenderer != null)
        {
            SetOutlineColor(
                currentOutlineColor
            );

            outlineRenderer.enabled = true;
        }

        isStatusVisible = true;
    }

    private StatusIntensity ResolveIntensity(
        int stack)
    {
        int required = elementStatus != null
            ? Mathf.Max(elementStatus.GetMaximumStack(currentElement), 1)
            : 5;
        float normalized = Mathf.Clamp01((float)stack / required);
        if (normalized < 0.4f) return StatusIntensity.Weak;
        if (normalized < 1f) return StatusIntensity.Medium;
        return StatusIntensity.Strong;
    }

    private Sprite ResolveSprite(
        ElementType element,
        StatusIntensity intensity)
    {
        switch (element)
        {
            case ElementType.Water:
                switch (intensity)
                {
                    case StatusIntensity.Weak:
                        return waterWeakSprite;

                    case StatusIntensity.Medium:
                        return waterMediumSprite;

                    case StatusIntensity.Strong:
                        return waterStrongSprite;
                }

                break;

            case ElementType.Electric:
                switch (intensity)
                {
                    case StatusIntensity.Weak:
                        return electricWeakSprite;

                    case StatusIntensity.Medium:
                        return electricMediumSprite;

                    case StatusIntensity.Strong:
                        return electricStrongSprite;
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
            case ElementType.Water:
                switch (intensity)
                {
                    case StatusIntensity.Weak:
                        return waterWeakColor;

                    case StatusIntensity.Medium:
                        return waterMediumColor;

                    case StatusIntensity.Strong:
                        return waterStrongColor;
                }

                break;

            case ElementType.Electric:
                switch (intensity)
                {
                    case StatusIntensity.Weak:
                        return electricWeakColor;

                    case StatusIntensity.Medium:
                        return electricMediumColor;

                    case StatusIntensity.Strong:
                        return electricStrongColor;
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
            case ElementType.Water:
                switch (intensity)
                {
                    case StatusIntensity.Weak:
                        return waterWeakOutlineColor;

                    case StatusIntensity.Medium:
                        return waterMediumOutlineColor;

                    case StatusIntensity.Strong:
                        return waterStrongOutlineColor;
                }

                break;

            case ElementType.Electric:
                switch (intensity)
                {
                    case StatusIntensity.Weak:
                        return electricWeakOutlineColor;

                    case StatusIntensity.Medium:
                        return electricMediumOutlineColor;

                    case StatusIntensity.Strong:
                        return electricStrongOutlineColor;
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
            case ElementType.Water:
                return waterPulseSpeed;

            case ElementType.Electric:
                return electricPulseSpeed;

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
        if (!isStatusVisible)
        {
            return;
        }

        if (statusRenderer != null)
        {
            statusRenderer.color =
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

    private void HideStatusVisual()
    {
        isStatusVisible = false;

        currentStack = 0;
        currentPulseSpeed = 0f;
        currentPulseAmount = 0f;

        currentFillColor =
            Color.clear;

        currentOutlineColor =
            Color.clear;

        if (statusRenderer != null)
        {
            statusRenderer.enabled = false;
            statusRenderer.color = Color.clear;
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
