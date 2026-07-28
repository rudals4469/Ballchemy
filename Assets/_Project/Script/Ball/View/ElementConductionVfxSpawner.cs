using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class ElementConductionVfxSpawner :
    MonoBehaviour
{
    private sealed class EffectInstance
    {
        public GameObject Root;
        public SpriteRenderer FlashRenderer;
        public LineRenderer LineRenderer;
        public Vector3[] LinePoints;
        public Coroutine PlayingRoutine;
        public bool IsPlaying;
        public int PlaySequence;
    }

    [Header("Effect Root")]
    [SerializeField]
    private Transform effectRoot;

    [Header("Line")]
    [SerializeField]
    private Material lineMaterial;

    [SerializeField]
    private Color lineColor =
        new Color(
            1f,
            0.88f,
            0.15f,
            1f
        );

    [SerializeField, Min(0.001f)]
    private float lineWidth = 0.095f;

    [SerializeField, Range(0.1f, 1f)]
    private float lineEndWidthRatio = 0.68f;

    [SerializeField, Range(3, 12)]
    private int linePointCount = 7;

    [SerializeField, Min(0f)]
    private float lineJitter = 0.18f;

    [Header("Target Flash")]
    [SerializeField]
    private Color targetFlashColor =
        new Color(
            1f,
            0.95f,
            0.4f,
            0.9f
        );

    [SerializeField, Min(1f)]
    private float targetFlashScale = 1.2f;

    [SerializeField, Min(0f)]
    private float targetFlashPadding = 0.12f;

    [Header("Timing")]
    [SerializeField, Min(0.05f)]
    private float effectDuration = 0.34f;

    [Tooltip(
        "전체 시간 중 번개가 대상까지 뻗는 데 사용하는 비율입니다."
    )]
    [SerializeField, Range(0.05f, 0.9f)]
    private float extendDurationRatio = 0.36f;

    [Tooltip(
        "전체 시간 중 전도선 페이드아웃이 시작되는 시점입니다."
    )]
    [SerializeField, Range(0f, 0.95f)]
    private float fadeStartRatio = 0.62f;

    [SerializeField, Min(0.01f)]
    private float lineRefreshInterval = 0.045f;

    [SerializeField, Min(0f)]
    private float chainStepDelay = 0.055f;

    [Header("Rendering")]
    [SerializeField]
    private int sortingOrderOffset = 9;

    [SerializeField]
    private float zOffset = -0.04f;

    [Header("Pool")]
    [SerializeField, Min(0)]
    private int prewarmCount = 8;

    [SerializeField, Min(1)]
    private int maximumPoolCount = 32;

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
        lineWidth = 0.095f;
        lineEndWidthRatio = 0.68f;
        linePointCount = 7;
        lineJitter = 0.18f;

        targetFlashScale = 1.2f;
        targetFlashPadding = 0.12f;

        effectDuration = 0.34f;
        extendDurationRatio = 0.36f;
        fadeStartRatio = 0.62f;
        lineRefreshInterval = 0.045f;
        chainStepDelay = 0.055f;

        sortingOrderOffset = 9;

        NormalizeSettings();
    }

    public void Play(
        Block sourceBlock,
        Block targetBlock,
        int chainIndex)
    {
        if (sourceBlock == null ||
            targetBlock == null)
        {
            return;
        }

        SpriteRenderer sourceRenderer =
            FindBaseRenderer(
                sourceBlock
            );

        SpriteRenderer targetRenderer =
            FindBaseRenderer(
                targetBlock
            );

        if (sourceRenderer == null ||
            targetRenderer == null)
        {
            return;
        }

        EffectInstance effect =
            AcquireEffectInstance();

        if (effect == null)
        {
            return;
        }

        Vector3 sourcePosition =
            sourceRenderer.bounds.center;

        Vector3 targetPosition =
            targetRenderer.bounds.center;

        sourcePosition.z +=
            zOffset;

        targetPosition.z +=
            zOffset;

        ConfigureEffectInstance(
            effect,
            targetBlock,
            targetRenderer,
            sourcePosition,
            targetPosition
        );

        effect.IsPlaying = true;
        effect.PlaySequence =
            ++nextPlaySequence;

        effect.PlayingRoutine =
            StartCoroutine(
                PlayEffectRoutine(
                    effect,
                    sourcePosition,
                    targetPosition,
                    Mathf.Max(
                        chainIndex,
                        0
                    )
                )
            );

        if (showDebugLog)
        {
            Debug.Log(
                "ElementConductionVfxSpawner: " +
                $"{sourceBlock.name} → " +
                $"{targetBlock.name} 전도 VFX",
                targetBlock
            );
        }
    }

    private void NormalizeSettings()
    {
        lineWidth =
            Mathf.Max(
                lineWidth,
                0.001f
            );

        lineEndWidthRatio =
            Mathf.Clamp(
                lineEndWidthRatio,
                0.1f,
                1f
            );

        linePointCount =
            Mathf.Clamp(
                linePointCount,
                3,
                12
            );

        lineJitter =
            Mathf.Max(
                lineJitter,
                0f
            );

        targetFlashScale =
            Mathf.Max(
                targetFlashScale,
                1f
            );

        targetFlashPadding =
            Mathf.Max(
                targetFlashPadding,
                0f
            );

        effectDuration =
            Mathf.Max(
                effectDuration,
                0.05f
            );

        extendDurationRatio =
            Mathf.Clamp(
                extendDurationRatio,
                0.05f,
                0.9f
            );

        fadeStartRatio =
            Mathf.Clamp(
                fadeStartRatio,
                0f,
                0.95f
            );

        lineRefreshInterval =
            Mathf.Max(
                lineRefreshInterval,
                0.01f
            );

        chainStepDelay =
            Mathf.Max(
                chainStepDelay,
                0f
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
                "ElementConductionVfxSpawner: " +
                "전도선 Shader를 찾지 못했습니다.",
                this
            );

            return;
        }

        runtimeLineMaterial =
            new Material(
                resolvedShader
            );

        runtimeLineMaterial.name =
            "Runtime_Element_Conduction_Line";

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
                "ElementConductionVfx"
            );

        rootObject.transform.SetParent(
            ResolvedEffectRoot,
            false
        );

        SpriteRenderer flashRenderer =
            rootObject.AddComponent<
                SpriteRenderer
            >();

        flashRenderer.enabled =
            false;

        GameObject lineObject =
            new GameObject(
                "ConductionLine"
            );

        lineObject.transform.SetParent(
            rootObject.transform,
            false
        );

        LineRenderer lineRenderer =
            lineObject.AddComponent<
                LineRenderer
            >();

        ConfigureNewLineRenderer(
            lineRenderer
        );

        lineObject.SetActive(
            false
        );

        rootObject.SetActive(
            false
        );

        return new EffectInstance
        {
            Root = rootObject,
            FlashRenderer = flashRenderer,
            LineRenderer = lineRenderer,
            LinePoints =
                new Vector3[
                    linePointCount
                ],
            PlayingRoutine = null,
            IsPlaying = false,
            PlaySequence = 0
        };
    }

    private void ConfigureNewLineRenderer(
        LineRenderer lineRenderer)
    {
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;

        lineRenderer.alignment =
            LineAlignment.View;

        lineRenderer.textureMode =
            LineTextureMode.Stretch;

        lineRenderer.positionCount =
            linePointCount;

        lineRenderer.startWidth =
            lineWidth;

        lineRenderer.endWidth =
            lineWidth *
            lineEndWidthRatio;

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
        SpriteRenderer targetRenderer,
        Vector3 sourcePosition,
        Vector3 targetPosition)
    {
        SetLayerRecursively(
            effect.Root,
            targetBlock.gameObject.layer
        );

        effect.Root.transform.position =
            targetPosition;

        effect.Root.transform.rotation =
            Quaternion.identity;

        effect.Root.transform.localScale =
            Vector3.one;

        SpriteRenderer flashRenderer =
            effect.FlashRenderer;

        flashRenderer.sprite =
            targetRenderer.sprite;

        flashRenderer.drawMode =
            SpriteDrawMode.Sliced;

        flashRenderer.size =
            new Vector2(
                Mathf.Max(
                    targetRenderer.bounds.size.x +
                    targetFlashPadding * 2f,
                    0.05f
                ),
                Mathf.Max(
                    targetRenderer.bounds.size.y +
                    targetFlashPadding * 2f,
                    0.05f
                )
            );

        flashRenderer.flipX =
            targetRenderer.flipX;

        flashRenderer.flipY =
            targetRenderer.flipY;

        flashRenderer.maskInteraction =
            targetRenderer.maskInteraction;

        flashRenderer.spriteSortPoint =
            targetRenderer.spriteSortPoint;

        flashRenderer.sortingLayerID =
            targetRenderer.sortingLayerID;

        flashRenderer.sortingOrder =
            targetRenderer.sortingOrder +
            sortingOrderOffset;

        flashRenderer.sharedMaterial =
            targetRenderer.sharedMaterial;

        flashRenderer.color =
            Color.clear;

        flashRenderer.enabled =
            false;

        LineRenderer lineRenderer =
            effect.LineRenderer;

        lineRenderer.positionCount =
            linePointCount;

        lineRenderer.startWidth =
            lineWidth;

        lineRenderer.endWidth =
            lineWidth *
            lineEndWidthRatio;

        lineRenderer.sortingLayerID =
            targetRenderer.sortingLayerID;

        lineRenderer.sortingOrder =
            targetRenderer.sortingOrder +
            sortingOrderOffset +
            1;

        if (runtimeLineMaterial != null)
        {
            lineRenderer.sharedMaterial =
                runtimeLineMaterial;
        }

        lineRenderer.startColor =
            Color.clear;

        lineRenderer.endColor =
            Color.clear;

        if (effect.LinePoints == null ||
            effect.LinePoints.Length !=
            linePointCount)
        {
            effect.LinePoints =
                new Vector3[
                    linePointCount
                ];
        }

        GenerateLinePoints(
            effect.LinePoints,
            sourcePosition,
            targetPosition
        );

        ApplyLineProgress(
            lineRenderer,
            effect.LinePoints,
            0f
        );
    }

    private IEnumerator PlayEffectRoutine(
        EffectInstance effect,
        Vector3 sourcePosition,
        Vector3 targetPosition,
        int chainIndex)
    {
        float startDelay =
            chainStepDelay *
            chainIndex;

        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(
                startDelay
            );
        }

        if (effect == null ||
            effect.Root == null)
        {
            yield break;
        }

        effect.Root.SetActive(
            true
        );

        effect.FlashRenderer.enabled =
            true;

        effect.LineRenderer.gameObject
            .SetActive(
                true
            );

        float elapsed = 0f;
        float refreshTimer = 0f;

        GenerateLinePoints(
            effect.LinePoints,
            sourcePosition,
            targetPosition
        );

        while (elapsed <
               effectDuration)
        {
            float normalizedTime =
                Mathf.Clamp01(
                    elapsed /
                    effectDuration
                );

            float extensionProgress =
                Mathf.Clamp01(
                    normalizedTime /
                    extendDurationRatio
                );

            extensionProgress =
                SmoothStep01(
                    extensionProgress
                );

            ApplyLineProgress(
                effect.LineRenderer,
                effect.LinePoints,
                extensionProgress
            );

            float arrivalStart =
                extendDurationRatio *
                0.72f;

            float targetFlashTime =
                Mathf.InverseLerp(
                    arrivalStart,
                    1f,
                    normalizedTime
                );

            float flashPulse =
                Mathf.Sin(
                    targetFlashTime *
                    Mathf.PI
                );

            Color animatedFlash =
                targetFlashColor;

            animatedFlash.a =
                Mathf.Clamp01(
                    targetFlashColor.a *
                    flashPulse
                );

            effect.FlashRenderer.color =
                animatedFlash;

            float flashScale =
                Mathf.Lerp(
                    1f,
                    targetFlashScale,
                    flashPulse
                );

            effect.Root.transform.localScale =
                Vector3.one *
                flashScale;

            float appearanceAlpha =
                Mathf.Clamp01(
                    normalizedTime /
                    0.05f
                );

            float fadeProgress =
                Mathf.InverseLerp(
                    fadeStartRatio,
                    1f,
                    normalizedTime
                );

            float lineAlpha =
                appearanceAlpha *
                (
                    1f -
                    fadeProgress
                );

            Color animatedLineColor =
                lineColor;

            animatedLineColor.a =
                Mathf.Clamp01(
                    lineColor.a *
                    lineAlpha
                );

            effect.LineRenderer.startColor =
                animatedLineColor;

            effect.LineRenderer.endColor =
                animatedLineColor;

            refreshTimer +=
                Time.deltaTime;

            if (refreshTimer >=
                lineRefreshInterval)
            {
                refreshTimer = 0f;

                GenerateLinePoints(
                    effect.LinePoints,
                    sourcePosition,
                    targetPosition
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

    private void GenerateLinePoints(
        Vector3[] points,
        Vector3 sourcePosition,
        Vector3 targetPosition)
    {
        if (points == null ||
            points.Length <= 0)
        {
            return;
        }

        Vector2 direction =
            targetPosition -
            sourcePosition;

        Vector2 perpendicular =
            direction.sqrMagnitude >
            0.0001f
                ? new Vector2(
                    -direction.y,
                    direction.x
                ).normalized
                : Vector2.up;

        int pointCount =
            points.Length;

        for (int i = 0;
             i < pointCount;
             i++)
        {
            float t =
                pointCount <= 1
                    ? 0f
                    : i /
                    (float)(
                        pointCount - 1
                    );

            Vector3 point =
                Vector3.Lerp(
                    sourcePosition,
                    targetPosition,
                    t
                );

            if (i > 0 &&
                i < pointCount - 1)
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
                            -lineJitter,
                            lineJitter
                        ) *
                        centerStrength
                    );
            }

            point.z =
                sourcePosition.z;

            points[i] =
                point;
        }
    }

    private void ApplyLineProgress(
        LineRenderer lineRenderer,
        Vector3[] points,
        float progress)
    {
        if (lineRenderer == null ||
            points == null ||
            points.Length <= 0)
        {
            return;
        }

        progress =
            Mathf.Clamp01(
                progress
            );

        int pointCount =
            points.Length;

        lineRenderer.positionCount =
            pointCount;

        if (progress >= 1f)
        {
            for (int i = 0;
                 i < pointCount;
                 i++)
            {
                lineRenderer.SetPosition(
                    i,
                    points[i]
                );
            }

            return;
        }

        float scaledProgress =
            progress *
            (
                pointCount -
                1
            );

        int segmentIndex =
            Mathf.Clamp(
                Mathf.FloorToInt(
                    scaledProgress
                ),
                0,
                pointCount - 1
            );

        int nextIndex =
            Mathf.Min(
                segmentIndex + 1,
                pointCount - 1
            );

        float segmentProgress =
            scaledProgress -
            segmentIndex;

        Vector3 currentTip =
            Vector3.Lerp(
                points[segmentIndex],
                points[nextIndex],
                segmentProgress
            );

        for (int i = 0;
             i < pointCount;
             i++)
        {
            Vector3 position =
                i <= segmentIndex
                    ? points[i]
                    : currentTip;

            lineRenderer.SetPosition(
                i,
                position
            );
        }
    }

    private static float SmoothStep01(
        float value)
    {
        value =
            Mathf.Clamp01(
                value
            );

        return value *
               value *
               (
                   3f -
                   2f *
                   value
               );
    }

    private SpriteRenderer FindBaseRenderer(
        Block targetBlock)
    {
        if (targetBlock == null)
        {
            return null;
        }

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
                    "ConductionFlash")
            {
                continue;
            }

            return candidate;
        }

        return null;
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

        if (effect.LineRenderer != null)
        {
            effect.LineRenderer.gameObject
                .SetActive(
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