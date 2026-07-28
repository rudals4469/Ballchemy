using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class ElementReactionVfxSpawner :
    MonoBehaviour
{
    private sealed class EffectInstance
    {
        public GameObject Root;
        public SpriteRenderer FlashRenderer;
        public LineRenderer[] BoltRenderers;
        public Coroutine PlayingRoutine;
        public bool IsPlaying;
        public int PlaySequence;
    }

    [Header("Effect Root")]
    [SerializeField]
    private Transform effectRoot;

    [Header("Flash")]
    [SerializeField]
    private Color flashColor =
        new Color(
            1f,
            0.96f,
            0.5f,
            0.95f
        );

    [SerializeField, Min(1f)]
    private float flashScaleMultiplier = 1.22f;

    [Tooltip(
        "블록 크기보다 플래시가 더 크게 표시되는 여백입니다."
    )]
    [SerializeField, Min(0f)]
    private float flashBoundsPadding = 0.12f;

    [Header("Electric Bolts")]
    [SerializeField]
    private Material lineMaterial;

    [SerializeField]
    private Color boltColor =
        new Color(
            1f,
            0.88f,
            0.15f,
            1f
        );

    [SerializeField, Min(0.001f)]
    private float boltWidth = 0.09f;

    [SerializeField, Range(0.1f, 1f)]
    private float boltEndWidthRatio = 0.68f;

    [SerializeField, Min(0f)]
    private float boltJitter = 0.16f;

    [SerializeField, Min(0f)]
    private float boundsPadding = 0.15f;

    [SerializeField, Range(3, 10)]
    private int boltPointCount = 6;

    [Header("Bolt Count")]
    [SerializeField, Range(1, 8)]
    private int weakBoltCount = 3;

    [SerializeField, Range(1, 8)]
    private int mediumBoltCount = 5;

    [SerializeField, Range(1, 8)]
    private int strongBoltCount = 7;

    [Header("Timing")]
    [SerializeField, Min(0.05f)]
    private float effectDuration = 0.32f;

    [SerializeField, Min(0.01f)]
    private float boltRefreshInterval = 0.04f;

    [Header("Rendering")]
    [SerializeField]
    private int sortingOrderOffset = 8;

    [SerializeField]
    private float zOffset = -0.03f;

    [Header("Pool")]
    [SerializeField, Min(0)]
    private int prewarmCount = 8;

    [SerializeField, Min(1)]
    private int maximumPoolCount = 32;

    [SerializeField, Range(1, 8)]
    private int maximumBoltCount = 8;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog;

    private readonly List<EffectInstance>
        effectPool =
            new List<EffectInstance>();

    private Material runtimeLineMaterial;
    private bool ownsRuntimeLineMaterial;
    private int nextPlaySequence;

    private Transform ResolvedEffectRoot =>
        effectRoot != null
            ? effectRoot
            : transform;

    private void Reset()
    {
        ApplyReadableVfxPreset();
    }

    private void Awake()
    {
        NormalizeSettings();
        EnsureLineMaterial();
        PrewarmPool();
    }

    private void OnValidate()
    {
        NormalizeSettings();
    }

    private void OnDisable()
    {
        StopAndReturnAllEffects();
    }

    private void OnDestroy()
    {
        StopAndReturnAllEffects();

        if (ownsRuntimeLineMaterial &&
            runtimeLineMaterial != null)
        {
            Destroy(
                runtimeLineMaterial
            );
        }
    }

    [ContextMenu(
        "Apply Readable VFX Preset"
    )]
    private void ApplyReadableVfxPreset()
    {
        flashScaleMultiplier = 1.22f;
        flashBoundsPadding = 0.12f;

        boltWidth = 0.09f;
        boltEndWidthRatio = 0.68f;
        boltJitter = 0.16f;
        boundsPadding = 0.15f;
        boltPointCount = 6;

        weakBoltCount = 3;
        mediumBoltCount = 5;
        strongBoltCount = 7;

        effectDuration = 0.32f;
        boltRefreshInterval = 0.04f;
        sortingOrderOffset = 8;

        NormalizeSettings();
    }

    private void NormalizeSettings()
    {
        flashScaleMultiplier =
            Mathf.Max(
                flashScaleMultiplier,
                1f
            );

        flashBoundsPadding =
            Mathf.Max(
                flashBoundsPadding,
                0f
            );

        boltWidth =
            Mathf.Max(
                boltWidth,
                0.001f
            );

        boltEndWidthRatio =
            Mathf.Clamp(
                boltEndWidthRatio,
                0.1f,
                1f
            );

        boltJitter =
            Mathf.Max(
                boltJitter,
                0f
            );

        boundsPadding =
            Mathf.Max(
                boundsPadding,
                0f
            );

        boltPointCount =
            Mathf.Clamp(
                boltPointCount,
                3,
                10
            );

        maximumBoltCount =
            Mathf.Clamp(
                maximumBoltCount,
                1,
                8
            );

        weakBoltCount =
            Mathf.Clamp(
                weakBoltCount,
                1,
                maximumBoltCount
            );

        mediumBoltCount =
            Mathf.Clamp(
                mediumBoltCount,
                1,
                maximumBoltCount
            );

        strongBoltCount =
            Mathf.Clamp(
                strongBoltCount,
                1,
                maximumBoltCount
            );

        effectDuration =
            Mathf.Max(
                effectDuration,
                0.05f
            );

        boltRefreshInterval =
            Mathf.Max(
                boltRefreshInterval,
                0.01f
            );

        prewarmCount =
            Mathf.Max(
                prewarmCount,
                0
            );

        maximumPoolCount =
            Mathf.Max(
                maximumPoolCount,
                1
            );

        prewarmCount =
            Mathf.Min(
                prewarmCount,
                maximumPoolCount
            );
    }

    private void EnsureLineMaterial()
    {
        if (lineMaterial != null)
        {
            runtimeLineMaterial =
                lineMaterial;

            ownsRuntimeLineMaterial =
                false;

            return;
        }

        Shader resolvedShader =
            Shader.Find(
                "Universal Render Pipeline/2D/" +
                "Sprite-Unlit-Default"
            );

        if (resolvedShader == null)
        {
            resolvedShader =
                Shader.Find(
                    "Sprites/Default"
                );
        }

        if (resolvedShader == null)
        {
            Debug.LogWarning(
                "ElementReactionVfxSpawner: " +
                "번개 선에 사용할 Shader를 찾지 못했습니다.",
                this
            );

            return;
        }

        runtimeLineMaterial =
            new Material(
                resolvedShader
            );

        runtimeLineMaterial.name =
            "Runtime_Electrocution_Line";

        ownsRuntimeLineMaterial =
            true;
    }

    private void PrewarmPool()
    {
        for (int i = effectPool.Count;
             i < prewarmCount;
             i++)
        {
            effectPool.Add(
                CreateEffectInstance()
            );
        }
    }

    public void PlayElectrocution(
        Block targetBlock,
        int reactionCount)
    {
        if (targetBlock == null ||
            reactionCount <= 0)
        {
            return;
        }

        SpriteRenderer baseRenderer =
            FindBaseRenderer(
                targetBlock
            );

        if (baseRenderer == null ||
            baseRenderer.sprite == null)
        {
            return;
        }

        EffectInstance effect =
            AcquireEffectInstance();

        if (effect == null)
        {
            return;
        }

        Bounds targetBounds =
            baseRenderer.bounds;

        Vector3 center =
            targetBounds.center;

        center.z +=
            zOffset;

        Vector2 halfSize =
            new Vector2(
                Mathf.Max(
                    targetBounds.extents.x +
                    boundsPadding,
                    0.05f
                ),
                Mathf.Max(
                    targetBounds.extents.y +
                    boundsPadding,
                    0.05f
                )
            );

        Vector3 expandedFlashSize =
            targetBounds.size +
            new Vector3(
                flashBoundsPadding * 2f,
                flashBoundsPadding * 2f,
                0f
            );

        int boltCount =
            ResolveBoltCount(
                reactionCount
            );

        ConfigureEffectInstance(
            effect,
            targetBlock,
            baseRenderer,
            center,
            expandedFlashSize,
            boltCount
        );

        effect.IsPlaying = true;
        effect.PlaySequence =
            ++nextPlaySequence;

        effect.Root.SetActive(
            true
        );

        effect.PlayingRoutine =
            StartCoroutine(
                PlayEffectRoutine(
                    effect,
                    center,
                    halfSize,
                    boltCount,
                    reactionCount
                )
            );

        if (showDebugLog)
        {
            Debug.Log(
                "ElementReactionVfxSpawner: " +
                $"{targetBlock.name} 감전 VFX, " +
                $"반응={reactionCount}, 번개={boltCount}",
                targetBlock
            );
        }
    }

    private SpriteRenderer FindBaseRenderer(
        Block targetBlock)
    {
        SpriteRenderer rootRenderer =
            targetBlock.GetComponent<
                SpriteRenderer
            >();

        if (rootRenderer != null)
        {
            return rootRenderer;
        }

        SpriteRenderer[] renderers =
            targetBlock.GetComponentsInChildren<
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

            string objectName =
                candidate.gameObject.name;

            if (objectName ==
                    "ElementStatusVisual" ||
                objectName ==
                    "ElementSurfaceVisual" ||
                objectName ==
                    "ElectrocutionFlash")
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private int ResolveBoltCount(
        int reactionCount)
    {
        if (reactionCount <= 1)
        {
            return weakBoltCount;
        }

        if (reactionCount == 2)
        {
            return mediumBoltCount;
        }

        return strongBoltCount;
    }

    private EffectInstance AcquireEffectInstance()
    {
        for (int i = 0;
             i < effectPool.Count;
             i++)
        {
            EffectInstance candidate =
                effectPool[i];

            if (candidate != null &&
                !candidate.IsPlaying)
            {
                return candidate;
            }
        }

        if (effectPool.Count <
            maximumPoolCount)
        {
            EffectInstance createdEffect =
                CreateEffectInstance();

            effectPool.Add(
                createdEffect
            );

            return createdEffect;
        }

        EffectInstance oldestEffect =
            null;

        for (int i = 0;
             i < effectPool.Count;
             i++)
        {
            EffectInstance candidate =
                effectPool[i];

            if (candidate == null)
            {
                continue;
            }

            if (oldestEffect == null ||
                candidate.PlaySequence <
                oldestEffect.PlaySequence)
            {
                oldestEffect =
                    candidate;
            }
        }

        if (oldestEffect != null)
        {
            StopEffectInstance(
                oldestEffect
            );
        }

        return oldestEffect;
    }

    private EffectInstance CreateEffectInstance()
    {
        GameObject rootObject =
            new GameObject(
                "ElectrocutionVfx"
            );

        rootObject.transform.SetParent(
            ResolvedEffectRoot,
            false
        );

        rootObject.transform.localPosition =
            Vector3.zero;

        rootObject.transform.localRotation =
            Quaternion.identity;

        rootObject.transform.localScale =
            Vector3.one;

        SpriteRenderer flashRenderer =
            rootObject.AddComponent<
                SpriteRenderer
            >();

        flashRenderer.enabled = false;

        LineRenderer[] boltRenderers =
            new LineRenderer[
                maximumBoltCount
            ];

        for (int i = 0;
             i < boltRenderers.Length;
             i++)
        {
            GameObject boltObject =
                new GameObject(
                    $"Bolt_{i + 1:00}"
                );

            boltObject.transform.SetParent(
                rootObject.transform,
                false
            );

            LineRenderer lineRenderer =
                boltObject.AddComponent<
                    LineRenderer
                >();

            ConfigureNewLineRenderer(
                lineRenderer
            );

            boltObject.SetActive(
                false
            );

            boltRenderers[i] =
                lineRenderer;
        }

        rootObject.SetActive(
            false
        );

        return new EffectInstance
        {
            Root = rootObject,
            FlashRenderer = flashRenderer,
            BoltRenderers = boltRenderers,
            PlayingRoutine = null,
            IsPlaying = false,
            PlaySequence = 0
        };
    }

    private void ConfigureNewLineRenderer(
        LineRenderer lineRenderer)
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.useWorldSpace =
            true;

        lineRenderer.loop =
            false;

        lineRenderer.alignment =
            LineAlignment.View;

        lineRenderer.textureMode =
            LineTextureMode.Stretch;

        lineRenderer.positionCount =
            boltPointCount;

        lineRenderer.startWidth =
            boltWidth;

        lineRenderer.endWidth =
            boltWidth *
            boltEndWidthRatio;

        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 4;

        lineRenderer.shadowCastingMode =
            ShadowCastingMode.Off;

        lineRenderer.receiveShadows =
            false;

        if (runtimeLineMaterial != null)
        {
            lineRenderer.sharedMaterial =
                runtimeLineMaterial;
        }
    }

    private void ConfigureEffectInstance(
        EffectInstance effect,
        Block targetBlock,
        SpriteRenderer baseRenderer,
        Vector3 center,
        Vector3 worldSize,
        int activeBoltCount)
    {
        SetLayerRecursively(
            effect.Root,
            targetBlock.gameObject.layer
        );

        effect.Root.transform.position =
            center;

        effect.Root.transform.rotation =
            Quaternion.identity;

        effect.Root.transform.localScale =
            Vector3.one;

        SpriteRenderer flashRenderer =
            effect.FlashRenderer;

        flashRenderer.sprite =
            baseRenderer.sprite;

        flashRenderer.drawMode =
            SpriteDrawMode.Sliced;

        flashRenderer.size =
            new Vector2(
                Mathf.Max(
                    worldSize.x,
                    0.05f
                ),
                Mathf.Max(
                    worldSize.y,
                    0.05f
                )
            );

        flashRenderer.flipX =
            baseRenderer.flipX;

        flashRenderer.flipY =
            baseRenderer.flipY;

        flashRenderer.maskInteraction =
            baseRenderer.maskInteraction;

        flashRenderer.spriteSortPoint =
            baseRenderer.spriteSortPoint;

        flashRenderer.sortingLayerID =
            baseRenderer.sortingLayerID;

        flashRenderer.sortingOrder =
            baseRenderer.sortingOrder +
            sortingOrderOffset;

        flashRenderer.sharedMaterial =
            baseRenderer.sharedMaterial;

        flashRenderer.color =
            flashColor;

        flashRenderer.enabled =
            true;

        for (int i = 0;
             i < effect.BoltRenderers.Length;
             i++)
        {
            LineRenderer boltRenderer =
                effect.BoltRenderers[i];

            bool shouldEnable =
                i < activeBoltCount;

            boltRenderer.gameObject.SetActive(
                shouldEnable
            );

            if (!shouldEnable)
            {
                continue;
            }

            boltRenderer.positionCount =
                boltPointCount;

            boltRenderer.startWidth =
                boltWidth;

            boltRenderer.endWidth =
                boltWidth *
                boltEndWidthRatio;

            boltRenderer.sortingLayerID =
                baseRenderer.sortingLayerID;

            boltRenderer.sortingOrder =
                baseRenderer.sortingOrder +
                sortingOrderOffset +
                1;

            if (runtimeLineMaterial != null)
            {
                boltRenderer.sharedMaterial =
                    runtimeLineMaterial;
            }

            boltRenderer.startColor =
                boltColor;

            boltRenderer.endColor =
                boltColor;
        }
    }

    private IEnumerator PlayEffectRoutine(
        EffectInstance effect,
        Vector3 center,
        Vector2 halfSize,
        int activeBoltCount,
        int reactionCount)
    {
        float strengthMultiplier =
            Mathf.Clamp(
                0.9f +
                reactionCount *
                0.14f,
                1f,
                1.55f
            );

        float resolvedDuration =
            effectDuration *
            Mathf.Clamp(
                0.95f +
                reactionCount *
                0.06f,
                1f,
                1.3f
            );

        GenerateAllBolts(
            effect,
            center,
            halfSize,
            activeBoltCount
        );

        float elapsed = 0f;
        float refreshTimer = 0f;

        while (elapsed <
               resolvedDuration)
        {
            float normalizedTime =
                Mathf.Clamp01(
                    elapsed /
                    resolvedDuration
                );

            float flashPulse =
                Mathf.Sin(
                    normalizedTime *
                    Mathf.PI
                );

            Color animatedFlashColor =
                flashColor;

            animatedFlashColor.a =
                Mathf.Clamp01(
                    flashColor.a *
                    flashPulse *
                    strengthMultiplier
                );

            effect.FlashRenderer.color =
                animatedFlashColor;

            float flashScale =
                Mathf.Lerp(
                    1f,
                    flashScaleMultiplier,
                    flashPulse
                );

            effect.Root.transform.localScale =
                Vector3.one *
                flashScale;

            float boltFlicker =
                0.78f +
                Mathf.Abs(
                    Mathf.Sin(
                        elapsed *
                        58f
                    )
                ) *
                0.22f;

            float fadeProgress =
                Mathf.InverseLerp(
                    0.62f,
                    1f,
                    normalizedTime
                );

            float boltAlpha =
                Mathf.Clamp01(
                    (
                        1f -
                        fadeProgress
                    ) *
                    boltFlicker
                );

            ApplyBoltAlpha(
                effect,
                activeBoltCount,
                boltAlpha
            );

            refreshTimer +=
                Time.deltaTime;

            if (refreshTimer >=
                boltRefreshInterval)
            {
                refreshTimer = 0f;

                GenerateAllBolts(
                    effect,
                    center,
                    halfSize,
                    activeBoltCount
                );
            }

            elapsed +=
                Time.deltaTime;

            yield return null;
        }

        ReturnEffectInstance(
            effect
        );
    }

    private void GenerateAllBolts(
        EffectInstance effect,
        Vector3 center,
        Vector2 halfSize,
        int activeBoltCount)
    {
        for (int i = 0;
             i < activeBoltCount;
             i++)
        {
            if (i >=
                effect.BoltRenderers.Length)
            {
                break;
            }

            GenerateBolt(
                effect.BoltRenderers[i],
                center,
                halfSize
            );
        }
    }

    private void GenerateBolt(
        LineRenderer lineRenderer,
        Vector3 center,
        Vector2 halfSize)
    {
        int startSide =
            Random.Range(
                0,
                4
            );

        int endSide =
            (
                startSide +
                Random.Range(
                    1,
                    4
                )
            ) %
            4;

        Vector3 startPosition =
            GetRandomEdgePoint(
                center,
                halfSize,
                startSide
            );

        Vector3 endPosition =
            GetRandomEdgePoint(
                center,
                halfSize,
                endSide
            );

        Vector2 direction =
            endPosition -
            startPosition;

        Vector2 perpendicular =
            direction.sqrMagnitude >
            0.0001f
                ? new Vector2(
                    -direction.y,
                    direction.x
                ).normalized
                : Vector2.up;

        lineRenderer.positionCount =
            boltPointCount;

        for (int i = 0;
             i < boltPointCount;
             i++)
        {
            float t =
                i /
                (float)(
                    boltPointCount -
                    1
                );

            Vector3 point =
                Vector3.Lerp(
                    startPosition,
                    endPosition,
                    t
                );

            if (i > 0 &&
                i < boltPointCount - 1)
            {
                float centerStrength =
                    Mathf.Sin(
                        t *
                        Mathf.PI
                    );

                point +=
                    (Vector3)(
                        perpendicular *
                        Random.Range(
                            -boltJitter,
                            boltJitter
                        ) *
                        centerStrength
                    );
            }

            point.z =
                center.z;

            lineRenderer.SetPosition(
                i,
                point
            );
        }
    }

    private Vector3 GetRandomEdgePoint(
        Vector3 center,
        Vector2 halfSize,
        int side)
    {
        switch (side)
        {
            case 0:
                return new Vector3(
                    center.x -
                    halfSize.x,
                    center.y +
                    Random.Range(
                        -halfSize.y,
                        halfSize.y
                    ),
                    center.z
                );

            case 1:
                return new Vector3(
                    center.x +
                    Random.Range(
                        -halfSize.x,
                        halfSize.x
                    ),
                    center.y +
                    halfSize.y,
                    center.z
                );

            case 2:
                return new Vector3(
                    center.x +
                    halfSize.x,
                    center.y +
                    Random.Range(
                        -halfSize.y,
                        halfSize.y
                    ),
                    center.z
                );

            default:
                return new Vector3(
                    center.x +
                    Random.Range(
                        -halfSize.x,
                        halfSize.x
                    ),
                    center.y -
                    halfSize.y,
                    center.z
                );
        }
    }

    private void ApplyBoltAlpha(
        EffectInstance effect,
        int activeBoltCount,
        float alpha)
    {
        Color animatedBoltColor =
            boltColor;

        animatedBoltColor.a =
            Mathf.Clamp01(
                boltColor.a *
                alpha
            );

        for (int i = 0;
             i < activeBoltCount;
             i++)
        {
            if (i >=
                effect.BoltRenderers.Length)
            {
                break;
            }

            LineRenderer lineRenderer =
                effect.BoltRenderers[i];

            lineRenderer.startColor =
                animatedBoltColor;

            lineRenderer.endColor =
                animatedBoltColor;
        }
    }

    private void ReturnEffectInstance(
        EffectInstance effect)
    {
        effect.PlayingRoutine = null;
        effect.IsPlaying = false;

        if (effect.FlashRenderer != null)
        {
            effect.FlashRenderer.enabled =
                false;

            effect.FlashRenderer.color =
                Color.clear;
        }

        for (int i = 0;
             i < effect.BoltRenderers.Length;
             i++)
        {
            effect.BoltRenderers[i]
                .gameObject.SetActive(
                    false
                );
        }

        effect.Root.transform.localScale =
            Vector3.one;

        effect.Root.SetActive(
            false
        );
    }

    private void StopEffectInstance(
        EffectInstance effect)
    {
        if (effect == null)
        {
            return;
        }

        if (effect.PlayingRoutine != null)
        {
            StopCoroutine(
                effect.PlayingRoutine
            );
        }

        ReturnEffectInstance(
            effect
        );
    }

    private void StopAndReturnAllEffects()
    {
        for (int i = 0;
             i < effectPool.Count;
             i++)
        {
            StopEffectInstance(
                effectPool[i]
            );
        }
    }

    private void SetLayerRecursively(
        GameObject targetObject,
        int targetLayer)
    {
        targetObject.layer =
            targetLayer;

        Transform targetTransform =
            targetObject.transform;

        for (int i = 0;
             i < targetTransform.childCount;
             i++)
        {
            SetLayerRecursively(
                targetTransform
                    .GetChild(i)
                    .gameObject,
                targetLayer
            );
        }
    }
}