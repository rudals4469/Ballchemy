using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
[RequireComponent(typeof(BlockElementStatus))]
public sealed class BlockElementStatusView :
    MonoBehaviour
{
    private enum StatusIntensity
    {
        Weak = 0,
        Medium = 1,
        Strong = 2
    }

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
        "속성 상태를 표시하는 SpriteRenderer입니다. " +
        "비워두면 자동으로 자식 오브젝트를 생성합니다."
    )]
    [SerializeField]
    private SpriteRenderer statusRenderer;

    [Header("Status Renderer")]
    [SerializeField]
    private string statusVisualObjectName =
        "ElementStatusVisual";

    [Tooltip(
        "기본 블록보다 상태 효과를 약간 크게 표시합니다."
    )]
    [SerializeField, Min(0.1f)]
    private float visualScaleMultiplier =
        1.03f;

    [Tooltip(
        "기본 블록보다 높은 렌더링 순서입니다."
    )]
    [SerializeField]
    private int sortingOrderOffset = 1;

    [Tooltip(
        "기본 블록과 같은 Material을 사용합니다."
    )]
    [SerializeField]
    private bool copyBaseMaterial = true;

    [Header("Water Sprites")]
    [Tooltip(
        "비어 있으면 기본 블록 스프라이트를 " +
        "파란색으로 복제하여 표시합니다."
    )]
    [SerializeField]
    private Sprite waterWeakSprite;

    [SerializeField]
    private Sprite waterMediumSprite;

    [SerializeField]
    private Sprite waterStrongSprite;

    [Header("Water Colors")]
    [SerializeField]
    private Color waterWeakColor =
        new Color(
            0.25f,
            0.65f,
            1f,
            0.18f
        );

    [SerializeField]
    private Color waterMediumColor =
        new Color(
            0.2f,
            0.6f,
            1f,
            0.28f
        );

    [SerializeField]
    private Color waterStrongColor =
        new Color(
            0.15f,
            0.55f,
            1f,
            0.42f
        );

    [Header("Electric Sprites")]
    [Tooltip(
        "비어 있으면 기본 블록 스프라이트를 " +
        "노란색으로 복제하여 표시합니다."
    )]
    [SerializeField]
    private Sprite electricWeakSprite;

    [SerializeField]
    private Sprite electricMediumSprite;

    [SerializeField]
    private Sprite electricStrongSprite;

    [Header("Electric Colors")]
    [SerializeField]
    private Color electricWeakColor =
        new Color(
            1f,
            0.9f,
            0.25f,
            0.2f
        );

    [SerializeField]
    private Color electricMediumColor =
        new Color(
            1f,
            0.82f,
            0.15f,
            0.32f
        );

    [SerializeField]
    private Color electricStrongColor =
        new Color(
            1f,
            0.72f,
            0.05f,
            0.48f
        );

    [Header("Pulse Animation")]
    [SerializeField]
    private bool usePulseAnimation = true;

    [SerializeField, Min(0f)]
    private float waterPulseSpeed = 2.5f;

    [SerializeField, Min(0f)]
    private float electricPulseSpeed = 8f;

    [SerializeField, Range(0f, 1f)]
    private float weakPulseAmount = 0.08f;

    [SerializeField, Range(0f, 1f)]
    private float mediumPulseAmount = 0.16f;

    [SerializeField, Range(0f, 1f)]
    private float strongPulseAmount = 0.25f;

    private Color currentBaseColor =
        Color.clear;

    private float currentPulseSpeed;
    private float currentPulseAmount;

    private bool isStatusVisible;
    private ElementType currentElement;
    private int currentStack;

    private void Awake()
    {
        NormalizeSettings();
        FindReferences();
        EnsureStatusRenderer();
        SynchronizeRendererLayout();
    }

    private void OnEnable()
    {
        FindReferences();
        EnsureStatusRenderer();

        SubscribeEvents();

        SynchronizeRendererLayout();
        RefreshStatusVisual();
    }

    private void Start()
    {
        /*
         * Block.Awake에서 BlockLayout이 적용된 이후
         * 최종 크기와 스프라이트를 한 번 더 복사합니다.
         */
        SynchronizeRendererLayout();
        RefreshStatusVisual();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
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

    private void OnValidate()
    {
        NormalizeSettings();

        if (!Application.isPlaying)
        {
            FindReferences();
            EnsureStatusRenderer();
            SynchronizeRendererLayout();
        }
    }

    private void NormalizeSettings()
    {
        visualScaleMultiplier =
            Mathf.Max(
                visualScaleMultiplier,
                0.1f
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

            if (candidate.gameObject.name ==
                statusVisualObjectName)
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
                existingTransform
                    .GetComponent<
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
        CopyBaseRendererSettings();

        statusRenderer.enabled = false;
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

    private void CopyBaseRendererSettings()
    {
        if (baseRenderer == null ||
            statusRenderer == null)
        {
            return;
        }

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
        HideStatusVisual();
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
        EnsureStatusRenderer();

        if (baseRenderer == null ||
            statusRenderer == null)
        {
            return;
        }

        CopyBaseRendererSettings();
        ResetStatusRendererTransform();

        statusRenderer.drawMode =
            baseRenderer.drawMode;

        statusRenderer.size =
            baseRenderer.size;
    }

    private void RefreshStatusVisual()
    {
        FindReferences();
        EnsureStatusRenderer();

        if (elementStatus == null ||
            statusRenderer == null ||
            !elementStatus.HasAnyStack)
        {
            HideStatusVisual();
            return;
        }

        int wetStack =
            elementStatus.WetStack;

        int chargeStack =
            elementStatus.ChargeStack;

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

        if (currentStack <= 0)
        {
            HideStatusVisual();
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

        /*
         * 전용 상태 스프라이트가 없을 때는
         * 기본 블록 스프라이트를 복제해서
         * 색상 오버레이로 사용합니다.
         */
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

        statusRenderer.sprite =
            resolvedSprite;

        statusRenderer.color =
            currentBaseColor;

        SynchronizeRendererLayout();

        statusRenderer.enabled = true;
        isStatusVisible = true;
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
            case ElementType.Water:
            {
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
            }

            case ElementType.Electric:
            {
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
        }

        return null;
    }

    private Color ResolveColor(
        ElementType element,
        StatusIntensity intensity)
    {
        switch (element)
        {
            case ElementType.Water:
            {
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
            }

            case ElementType.Electric:
            {
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
        if (!isStatusVisible ||
            statusRenderer == null ||
            !statusRenderer.enabled)
        {
            return;
        }

        if (!usePulseAnimation ||
            currentPulseSpeed <= 0f ||
            currentPulseAmount <= 0f)
        {
            statusRenderer.color =
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

        statusRenderer.color =
            animatedColor;
    }

    private void HideStatusVisual()
    {
        isStatusVisible = false;
        currentStack = 0;
        currentPulseSpeed = 0f;
        currentPulseAmount = 0f;
        currentBaseColor = Color.clear;

        if (statusRenderer == null)
        {
            return;
        }

        statusRenderer.enabled = false;
        statusRenderer.color = Color.clear;
    }
}