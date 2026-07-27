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
        public Coroutine PlayingRoutine;
        public bool IsPlaying;
        public int PlaySequence;
    }

    [Header("Effect Root")]
    [Tooltip(
        "전도 VFX가 생성될 부모입니다. " +
        "비워두면 이 오브젝트 아래에 생성됩니다."
    )]
    [SerializeField]
    private Transform effectRoot;

    [Header("Line")]
    [Tooltip(
        "비워두면 실행 중 URP 2D용 Material을 생성합니다."
    )]
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
    private float lineWidth = 0.05f;

    [SerializeField, Range(3, 12)]
    private int linePointCount = 6;

    [SerializeField, Min(0f)]
    private float lineJitter = 0.12f;

    [Header("Target Flash")]
    [SerializeField]
    private Color targetFlashColor =
        new Color(
            1f,
            0.95f,
            0.4f,
            0.65f
        );

    [SerializeField, Min(1f)]
    private float targetFlashScale = 1.06f;

    [Header("Timing")]
    [SerializeField, Min(0.05f)]
    private float effectDuration = 0.2f;

    [SerializeField, Min(0.01f)]
    private float lineRefreshInterval = 0.035f;

    [Tooltip(
        "여러 대상에게 전도될 때 순서대로 번개가 뻗는 간격입니다."
    )]
    [SerializeField, Min(0f)]
    private float chainStepDelay = 0.035f;

    [Header("Rendering")]
    [SerializeField]
    private int sortingOrderOffset = 7;

    [SerializeField]
    private float zOffset;

    [Header("Pool")]
    [SerializeField, Min(0)]
    private int prewarmCount = 6;

    [SerializeField, Min(1)]
    private int maximumPoolCount = 24;

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

        effectDuration =
            Mathf.Max(
                effectDuration,
                0.05f
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
            linePointCount;

        lineRenderer.startWidth =
            lineWidth;

        lineRenderer.endWidth =
            lineWidth * 0.45f;

        lineRenderer.numCapVertices =
            2;

        lineRenderer.numCornerVertices =
            2;

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
        if (effect == null ||
            effect.Root == null)
        {
            return;
        }

        SetLayerRecursively(
            effect.Root,
            targetBlock.gameObject.layer
        );

        Transform rootTransform =
            effect.Root.transform;

        rootTransform.position =
            targetPosition;

        rootTransform.rotation =
            Quaternion.identity;

        rootTransform.localScale =
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
                    targetRenderer.bounds.size.x,
                    0.05f
                ),
                Mathf.Max(
                    targetRenderer.bounds.size.y,
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
            lineWidth * 0.45f;

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

        GenerateLine(
            lineRenderer,
            sourcePosition,
            targetPosition
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

        if (effect.FlashRenderer != null)
        {
            effect.FlashRenderer.enabled =
                true;
        }

        if (effect.LineRenderer != null)
        {
            effect.LineRenderer.gameObject
                .SetActive(
                    true
                );
        }

        float elapsed = 0f;
        float refreshTimer = 0f;

        GenerateLine(
            effect.LineRenderer,
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

            float flashPulse =
                Mathf.Sin(
                    normalizedTime *
                    Mathf.PI
                );

            Color animatedFlash =
                targetFlashColor;

            animatedFlash.a =
                Mathf.Clamp01(
                    targetFlashColor.a *
                    flashPulse
                );

            if (effect.FlashRenderer != null)
            {
                effect.FlashRenderer.color =
                    animatedFlash;
            }

            float flashScale =
                Mathf.Lerp(
                    1f,
                    targetFlashScale,
                    flashPulse
                );

            effect.Root.transform.localScale =
                Vector3.one *
                flashScale;

            float lineAlpha =
                Mathf.Clamp01(
                    1f -
                    normalizedTime
                );

            Color animatedLineColor =
                lineColor;

            animatedLineColor.a =
                lineColor.a *
                lineAlpha;

            if (effect.LineRenderer != null)
            {
                effect.LineRenderer.startColor =
                    animatedLineColor;

                effect.LineRenderer.endColor =
                    animatedLineColor;
            }

            refreshTimer +=
                Time.deltaTime;

            if (refreshTimer >=
                lineRefreshInterval)
            {
                refreshTimer = 0f;

                GenerateLine(
                    effect.LineRenderer,
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

    private void GenerateLine(
        LineRenderer lineRenderer,
        Vector3 sourcePosition,
        Vector3 targetPosition)
    {
        if (lineRenderer == null)
        {
            return;
        }

        Vector2 direction =
            (Vector2)(
                targetPosition -
                sourcePosition
            );

        Vector2 perpendicular =
            direction.sqrMagnitude >
            0.0001f
                ? new Vector2(
                    -direction.y,
                    direction.x
                ).normalized
                : Vector2.up;

        lineRenderer.positionCount =
            linePointCount;

        for (int i = 0;
             i < linePointCount;
             i++)
        {
            float t =
                linePointCount <= 1
                    ? 0f
                    : i /
                    (float)(
                        linePointCount - 1
                    );

            Vector3 point =
                Vector3.Lerp(
                    sourcePosition,
                    targetPosition,
                    t
                );

            if (i > 0 &&
                i < linePointCount - 1)
            {
                float centerStrength =
                    Mathf.Sin(
                        t *
                        Mathf.PI
                    );

                float randomOffset =
                    Random.Range(
                        -lineJitter,
                        lineJitter
                    ) *
                    centerStrength;

                point +=
                    (Vector3)(
                        perpendicular *
                        randomOffset
                    );
            }

            point.z =
                sourcePosition.z;

            lineRenderer.SetPosition(
                i,
                point
            );
        }
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
        if (effect == null)
        {
            return;
        }

        effect.PlayingRoutine =
            null;

        effect.IsPlaying =
            false;

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

        if (effect.Root != null)
        {
            effect.Root.transform.localScale =
                Vector3.one;

            effect.Root.SetActive(
                false
            );
        }
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
        if (targetObject == null)
        {
            return;
        }

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