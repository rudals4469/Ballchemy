using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
[RequireComponent(typeof(BlockElementStatus))]
public sealed class BlockElementSurfaceStatusView :
    MonoBehaviour
{
    private enum StatusIntensity
    {
        Weak = 0,
        Medium = 1,
        Strong = 2
    }

    /*
     * 이전 버전 기본값입니다.
     *
     * 기존 프리팹이 이 값을 그대로 사용 중인 경우에만
     * 새로운 진한 기본값으로 자동 교체합니다.
     *
     * 사용자가 직접 색을 수정한 경우에는
     * 해당 설정을 덮어쓰지 않습니다.
     */
    private static readonly Color
        LegacyFireWeakColor =
            new Color(
                1f,
                0.38f,
                0.08f,
                0.16f
            );

    private static readonly Color
        LegacyFireMediumColor =
            new Color(
                1f,
                0.3f,
                0.04f,
                0.27f
            );

    private static readonly Color
        LegacyFireStrongColor =
            new Color(
                1f,
                0.2f,
                0.02f,
                0.4f
            );

    private static readonly Color
        LegacyIceWeakColor =
            new Color(
                0.55f,
                0.9f,
                1f,
                0.16f
            );

    private static readonly Color
        LegacyIceMediumColor =
            new Color(
                0.4f,
                0.82f,
                1f,
                0.27f
            );

    private static readonly Color
        LegacyIceStrongColor =
            new Color(
                0.25f,
                0.72f,
                1f,
                0.4f
            );

    /*
     * 강화된 새로운 기본값입니다.
     */
    private static readonly Color
        DefaultFireWeakColor =
            new Color32(
                255,
                106,
                46,
                115
            );

    private static readonly Color
        DefaultFireMediumColor =
            new Color32(
                255,
                69,
                28,
                166
            );

    private static readonly Color
        DefaultFireStrongColor =
            new Color32(
                233,
                39,
                18,
                217
            );

    private static readonly Color
        DefaultIceWeakColor =
            new Color32(
                99,
                217,
                255,
                115
            );

    private static readonly Color
        DefaultIceMediumColor =
            new Color32(
                40,
                191,
                255,
                166
            );

    private static readonly Color
        DefaultIceStrongColor =
            new Color32(
                7,
                149,
                232,
                217
            );

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
        "불·얼음 표면 효과를 표시하는 SpriteRenderer입니다. " +
        "플레이 시작 시 없으면 자동으로 생성합니다."
    )]
    [SerializeField]
    private SpriteRenderer surfaceRenderer;

    [Header("Surface Renderer")]
    [SerializeField]
    private string surfaceVisualObjectName =
        "ElementSurfaceVisual";

    [Tooltip(
        "표면 효과가 외곽 테두리를 가리지 않도록 " +
        "기본 블록보다 조금 작게 표시합니다."
    )]
    [SerializeField, Min(0.1f)]
    private float visualScaleMultiplier =
        0.95f;

    [SerializeField]
    private int sortingOrderOffset = 2;

    [SerializeField]
    private bool copyBaseMaterial = true;

    [Header("Fire Sprites")]
    [Tooltip(
        "전용 이미지가 없으면 기본 블록 이미지를 " +
        "불 색상으로 복제하여 표시합니다."
    )]
    [SerializeField]
    private Sprite fireWeakSprite;

    [SerializeField]
    private Sprite fireMediumSprite;

    [SerializeField]
    private Sprite fireStrongSprite;

    [Header("Fire Colors")]
    [SerializeField]
    private Color fireWeakColor =
        new Color32(
            255,
            106,
            46,
            115
        );

    [SerializeField]
    private Color fireMediumColor =
        new Color32(
            255,
            69,
            28,
            166
        );

    [SerializeField]
    private Color fireStrongColor =
        new Color32(
            233,
            39,
            18,
            217
        );

    [Header("Ice Sprites")]
    [Tooltip(
        "전용 이미지가 없으면 기본 블록 이미지를 " +
        "얼음 색상으로 복제하여 표시합니다."
    )]
    [SerializeField]
    private Sprite iceWeakSprite;

    [SerializeField]
    private Sprite iceMediumSprite;

    [SerializeField]
    private Sprite iceStrongSprite;

    [Header("Ice Colors")]
    [SerializeField]
    private Color iceWeakColor =
        new Color32(
            99,
            217,
            255,
            115
        );

    [SerializeField]
    private Color iceMediumColor =
        new Color32(
            40,
            191,
            255,
            166
        );

    [SerializeField]
    private Color iceStrongColor =
        new Color32(
            7,
            149,
            232,
            217
        );

    [Header("Pulse Animation")]
    [SerializeField]
    private bool usePulseAnimation = true;

    [SerializeField, Min(0f)]
    private float firePulseSpeed = 5f;

    [SerializeField, Min(0f)]
    private float icePulseSpeed = 2f;

    [SerializeField, Range(0f, 1f)]
    private float weakPulseAmount = 0.06f;

    [SerializeField, Range(0f, 1f)]
    private float mediumPulseAmount = 0.12f;

    [SerializeField, Range(0f, 1f)]
    private float strongPulseAmount = 0.2f;

    private Color currentBaseColor =
        Color.clear;

    private float currentPulseSpeed;
    private float currentPulseAmount;

    private bool isSurfaceVisible;
    private int currentStack;

    private void Awake()
    {
        UpgradeLegacyVisualDefaults();
        NormalizeSettings();
        FindReferences();
        EnsureSurfaceRenderer();
        SynchronizeRendererLayout();
    }

    private void OnEnable()
    {
        /*
         * 편집 모드에서는 자식 생성이나
         * AddComponent를 실행하지 않습니다.
         *
         * 프리팹 검사 과정에서 발생하던
         * SendMessage 경고를 방지합니다.
         */
        if (!Application.isPlaying)
        {
            return;
        }

        FindReferences();
        EnsureSurfaceRenderer();

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
        if (!Application.isPlaying)
        {
            return;
        }

        UnsubscribeEvents();
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

    private void OnValidate()
    {
        /*
         * OnValidate에서는 값만 정리합니다.
         *
         * GameObject 생성, SetParent,
         * AddComponent 같은 계층 구조 변경은
         * 절대 실행하지 않습니다.
         */
        UpgradeLegacyVisualDefaults();
        NormalizeSettings();
    }

    private void UpgradeLegacyVisualDefaults()
    {
        if (Mathf.Approximately(
                visualScaleMultiplier,
                0.92f))
        {
            visualScaleMultiplier =
                0.95f;
        }

        if (AreColorsApproximatelyEqual(
                fireWeakColor,
                LegacyFireWeakColor))
        {
            fireWeakColor =
                DefaultFireWeakColor;
        }

        if (AreColorsApproximatelyEqual(
                fireMediumColor,
                LegacyFireMediumColor))
        {
            fireMediumColor =
                DefaultFireMediumColor;
        }

        if (AreColorsApproximatelyEqual(
                fireStrongColor,
                LegacyFireStrongColor))
        {
            fireStrongColor =
                DefaultFireStrongColor;
        }

        if (AreColorsApproximatelyEqual(
                iceWeakColor,
                LegacyIceWeakColor))
        {
            iceWeakColor =
                DefaultIceWeakColor;
        }

        if (AreColorsApproximatelyEqual(
                iceMediumColor,
                LegacyIceMediumColor))
        {
            iceMediumColor =
                DefaultIceMediumColor;
        }

        if (AreColorsApproximatelyEqual(
                iceStrongColor,
                LegacyIceStrongColor))
        {
            iceStrongColor =
                DefaultIceStrongColor;
        }
    }

    private static bool
        AreColorsApproximatelyEqual(
            Color left,
            Color right)
    {
        const float tolerance = 0.001f;

        return
            Mathf.Abs(
                left.r - right.r
            ) <= tolerance &&
            Mathf.Abs(
                left.g - right.g
            ) <= tolerance &&
            Mathf.Abs(
                left.b - right.b
            ) <= tolerance &&
            Mathf.Abs(
                left.a - right.a
            ) <= tolerance;
    }

    private void NormalizeSettings()
    {
        visualScaleMultiplier =
            Mathf.Max(
                visualScaleMultiplier,
                0.1f
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

            if (candidate.gameObject.name ==
                    surfaceVisualObjectName ||
                candidate.gameObject.name ==
                    "ElementStatusVisual")
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

        /*
         * 계층 구조 변경은 플레이 중에만 실행합니다.
         */
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
                existingTransform
                    .GetComponent<
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
        CopyBaseRendererSettings();

        surfaceRenderer.enabled = false;
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

    private void CopyBaseRendererSettings()
    {
        if (baseRenderer == null ||
            surfaceRenderer == null)
        {
            return;
        }

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
        HideSurfaceVisual();
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
        }

        if (baseRenderer == null ||
            surfaceRenderer == null)
        {
            return;
        }

        CopyBaseRendererSettings();
        ResetSurfaceRendererTransform();

        surfaceRenderer.drawMode =
            baseRenderer.drawMode;

        surfaceRenderer.size =
            baseRenderer.size;
    }

    private void RefreshSurfaceVisual()
    {
        FindReferences();
        EnsureSurfaceRenderer();

        if (elementStatus == null ||
            surfaceRenderer == null ||
            !elementStatus.HasSurfaceStack)
        {
            HideSurfaceVisual();
            return;
        }

        int burnStack =
            elementStatus.BurnStack;

        int frostStack =
            elementStatus.FrostStack;

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

        if (currentStack <= 0)
        {
            HideSurfaceVisual();
            return;
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

        currentBaseColor =
            ResolveColor(
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
            currentBaseColor;

        SynchronizeRendererLayout();

        surfaceRenderer.enabled = true;
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
            {
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
            }

            case ElementType.Ice:
            {
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
        }

        return null;
    }

    private Color ResolveColor(
        ElementType element,
        StatusIntensity intensity)
    {
        switch (element)
        {
            case ElementType.Fire:
            {
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
            }

            case ElementType.Ice:
            {
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
        if (!isSurfaceVisible ||
            surfaceRenderer == null ||
            !surfaceRenderer.enabled)
        {
            return;
        }

        if (!usePulseAnimation ||
            currentPulseSpeed <= 0f ||
            currentPulseAmount <= 0f)
        {
            surfaceRenderer.color =
                currentBaseColor;

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
            currentBaseColor;

        animatedColor.a =
            Mathf.Clamp01(
                currentBaseColor.a *
                alphaMultiplier
            );

        surfaceRenderer.color =
            animatedColor;
    }

    private void HideSurfaceVisual()
    {
        isSurfaceVisible = false;
        currentStack = 0;
        currentPulseSpeed = 0f;
        currentPulseAmount = 0f;
        currentBaseColor = Color.clear;

        if (surfaceRenderer == null)
        {
            return;
        }

        surfaceRenderer.enabled = false;
        surfaceRenderer.color = Color.clear;
    }
}