using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FrozenShatterVfxSpawner :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BlockElementSystem blockElementSystem;

    [SerializeField]
    private Transform effectRoot;

    [Header("Line Material")]
    [Tooltip(
        "비워두면 런타임에 사용할 머티리얼을 " +
        "자동으로 생성합니다."
    )]
    [SerializeField]
    private Material lineMaterial;

    [Header("Normal Frozen Shatter")]
    [SerializeField]
    private Color normalShardStartColor =
        new Color32(
            225,
            250,
            255,
            255
        );

    [SerializeField]
    private Color normalShardEndColor =
        new Color32(
            50,
            190,
            255,
            220
        );

    [SerializeField]
    private Color normalFlashColor =
        new Color32(
            180,
            235,
            255,
            210
        );

    [SerializeField, Min(1)]
    private int normalShardCount = 7;

    [Header("Fire Frozen Shatter")]
    [SerializeField]
    private Color fireShardStartColor =
        new Color32(
            255,
            247,
            180,
            255
        );

    [SerializeField]
    private Color fireShardEndColor =
        new Color32(
            255,
            75,
            20,
            235
        );

    [SerializeField]
    private Color fireFlashColor =
        new Color32(
            255,
            120,
            35,
            225
        );

    [SerializeField, Min(1)]
    private int fireShardCount = 9;

    [Header("Frozen Attack Cancel")]
    [Tooltip(
        "동결로 공격이 취소될 때도 " +
        "작은 해동 효과를 표시합니다."
    )]
    [SerializeField]
    private bool playAttackCancelVfx = true;

    [SerializeField]
    private Color attackCancelShardColor =
        new Color32(
            200,
            245,
            255,
            205
        );

    [SerializeField, Min(1)]
    private int attackCancelShardCount = 4;

    [Header("Shard Shape")]
    [SerializeField, Min(0.001f)]
    private float shardWidth = 0.055f;

    [Tooltip(
        "블록 크기에 곱해지는 파편 길이입니다."
    )]
    [SerializeField, Min(0.1f)]
    private float shardRadiusMultiplier = 0.72f;

    [SerializeField, Min(0f)]
    private float shardStartRadiusMultiplier = 0.12f;

    [SerializeField, Min(0f)]
    private float shardJitter = 0.12f;

    [SerializeField, Min(0f)]
    private float shardDuration = 0.28f;

    [Header("Flash")]
    [SerializeField, Min(0.1f)]
    private float flashStartScale = 0.88f;

    [SerializeField, Min(0.1f)]
    private float flashEndScale = 1.16f;

    [SerializeField, Min(0f)]
    private float flashDuration = 0.2f;

    [Header("Sorting")]
    [SerializeField]
    private int sortingOrderOffset = 12;

    [Header("Pool")]
    [SerializeField, Min(0)]
    private int prewarmLineCount = 24;

    [SerializeField, Min(1)]
    private int maximumLineCount = 80;

    [SerializeField, Min(0)]
    private int prewarmFlashCount = 4;

    [SerializeField, Min(1)]
    private int maximumFlashCount = 16;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog;

    private readonly Queue<LineRenderer>
        availableLines =
            new Queue<LineRenderer>();

    private readonly Queue<SpriteRenderer>
        availableFlashes =
            new Queue<SpriteRenderer>();

    private Material runtimeLineMaterial;

    private int createdLineCount;
    private int createdFlashCount;

    private void Awake()
    {
        NormalizeSettings();
        FindReferences();
        PrepareLineMaterial();
        PrewarmPools();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();

        if (runtimeLineMaterial != null)
        {
            Destroy(
                runtimeLineMaterial
            );

            runtimeLineMaterial = null;
        }
    }

    private void OnValidate()
    {
        NormalizeSettings();
    }

    private void NormalizeSettings()
    {
        normalShardCount =
            Mathf.Max(
                normalShardCount,
                1
            );

        fireShardCount =
            Mathf.Max(
                fireShardCount,
                1
            );

        attackCancelShardCount =
            Mathf.Max(
                attackCancelShardCount,
                1
            );

        shardWidth =
            Mathf.Max(
                shardWidth,
                0.001f
            );

        shardRadiusMultiplier =
            Mathf.Max(
                shardRadiusMultiplier,
                0.1f
            );

        shardStartRadiusMultiplier =
            Mathf.Max(
                shardStartRadiusMultiplier,
                0f
            );

        shardJitter =
            Mathf.Max(
                shardJitter,
                0f
            );

        shardDuration =
            Mathf.Max(
                shardDuration,
                0f
            );

        flashStartScale =
            Mathf.Max(
                flashStartScale,
                0.1f
            );

        flashEndScale =
            Mathf.Max(
                flashEndScale,
                0.1f
            );

        flashDuration =
            Mathf.Max(
                flashDuration,
                0f
            );

        prewarmLineCount =
            Mathf.Max(
                prewarmLineCount,
                0
            );

        maximumLineCount =
            Mathf.Max(
                maximumLineCount,
                1
            );

        prewarmLineCount =
            Mathf.Min(
                prewarmLineCount,
                maximumLineCount
            );

        prewarmFlashCount =
            Mathf.Max(
                prewarmFlashCount,
                0
            );

        maximumFlashCount =
            Mathf.Max(
                maximumFlashCount,
                1
            );

        prewarmFlashCount =
            Mathf.Min(
                prewarmFlashCount,
                maximumFlashCount
            );
    }

    private void FindReferences()
    {
        if (blockElementSystem == null)
        {
            blockElementSystem =
                GetComponent<
                    BlockElementSystem
                >();
        }

        if (blockElementSystem == null)
        {
            blockElementSystem =
                GetComponentInParent<
                    BlockElementSystem
                >();
        }

        if (blockElementSystem == null &&
            Application.isPlaying)
        {
            blockElementSystem =
                FindFirstObjectByType<
                    BlockElementSystem
                >();
        }

        if (effectRoot == null)
        {
            effectRoot =
                transform;
        }
    }

    private void SubscribeEvents()
    {
        UnsubscribeEvents();

        if (blockElementSystem == null)
        {
            return;
        }

        blockElementSystem
            .FrozenShatterDamageApplied +=
            HandleFrozenShatterDamageApplied;

        blockElementSystem
            .FrozenAttackCancelled +=
            HandleFrozenAttackCancelled;
    }

    private void UnsubscribeEvents()
    {
        if (blockElementSystem == null)
        {
            return;
        }

        blockElementSystem
            .FrozenShatterDamageApplied -=
            HandleFrozenShatterDamageApplied;

        blockElementSystem
            .FrozenAttackCancelled -=
            HandleFrozenAttackCancelled;
    }

    private void HandleFrozenShatterDamageApplied(
        Block targetBlock,
        int appliedDamage,
        bool isFireShatter)
    {
        PlayShatter(
            targetBlock,
            isFireShatter
        );
    }

    private void HandleFrozenAttackCancelled(
        Block targetBlock)
    {
        if (!playAttackCancelVfx)
        {
            return;
        }

        PlayAttackCancel(
            targetBlock
        );
    }

    public void PlayShatter(
        Block targetBlock,
        bool isFireShatter)
    {
        if (targetBlock == null)
        {
            return;
        }

        SpriteRenderer baseRenderer =
            FindBaseRenderer(
                targetBlock
            );

        Vector3 center =
            baseRenderer != null
                ? baseRenderer.bounds.center
                : targetBlock.transform.position;

        float radius =
            CalculateEffectRadius(
                targetBlock
            );

        int shardCount =
            isFireShatter
                ? fireShardCount
                : normalShardCount;

        Color shardStartColor =
            isFireShatter
                ? fireShardStartColor
                : normalShardStartColor;

        Color shardEndColor =
            isFireShatter
                ? fireShardEndColor
                : normalShardEndColor;

        Color flashColor =
            isFireShatter
                ? fireFlashColor
                : normalFlashColor;

        SpawnShardBurst(
            targetBlock,
            center,
            radius,
            shardCount,
            shardStartColor,
            shardEndColor
        );

        SpawnFlash(
            targetBlock,
            baseRenderer,
            flashColor
        );

        if (showDebugLog)
        {
            Debug.Log(
                "FrozenShatterVfxSpawner: " +
                $"{targetBlock.name} 파쇄 VFX, " +
                $"불 강화={isFireShatter}",
                targetBlock
            );
        }
    }

    private void PlayAttackCancel(
        Block targetBlock)
    {
        if (targetBlock == null)
        {
            return;
        }

        SpriteRenderer baseRenderer =
            FindBaseRenderer(
                targetBlock
            );

        Vector3 center =
            baseRenderer != null
                ? baseRenderer.bounds.center
                : targetBlock.transform.position;

        float radius =
            CalculateEffectRadius(
                targetBlock
            ) *
            0.55f;

        SpawnShardBurst(
            targetBlock,
            center,
            radius,
            attackCancelShardCount,
            attackCancelShardColor,
            attackCancelShardColor
        );

        SpawnFlash(
            targetBlock,
            baseRenderer,
            attackCancelShardColor
        );
    }

    private void SpawnShardBurst(
        Block sourceBlock,
        Vector3 center,
        float radius,
        int shardCount,
        Color startColor,
        Color endColor)
    {
        if (shardCount <= 0 ||
            radius <= 0f)
        {
            return;
        }

        float startingAngle =
            Random.Range(
                0f,
                360f
            );

        float angleStep =
            360f /
            shardCount;

        int sortingLayerId = 0;
        int sortingOrder =
            sortingOrderOffset;

        SpriteRenderer baseRenderer =
            FindBaseRenderer(
                sourceBlock
            );

        if (baseRenderer != null)
        {
            sortingLayerId =
                baseRenderer.sortingLayerID;

            sortingOrder =
                baseRenderer.sortingOrder +
                sortingOrderOffset;
        }

        for (int i = 0;
             i < shardCount;
             i++)
        {
            LineRenderer line =
                GetLineRenderer();

            if (line == null)
            {
                break;
            }

            float angle =
                startingAngle +
                angleStep *
                i +
                Random.Range(
                    -10f,
                    10f
                );

            float radians =
                angle *
                Mathf.Deg2Rad;

            Vector2 direction =
                new Vector2(
                    Mathf.Cos(
                        radians
                    ),
                    Mathf.Sin(
                        radians
                    )
                );

            Vector2 perpendicular =
                new Vector2(
                    -direction.y,
                    direction.x
                );

            float randomizedRadius =
                radius *
                Random.Range(
                    0.78f,
                    1.16f
                );

            Vector3 startPoint =
                center +
                (Vector3)(
                    direction *
                    radius *
                    shardStartRadiusMultiplier
                );

            Vector3 middlePoint =
                center +
                (Vector3)(
                    direction *
                    randomizedRadius *
                    0.55f
                ) +
                (Vector3)(
                    perpendicular *
                    Random.Range(
                        -shardJitter,
                        shardJitter
                    )
                );

            Vector3 endPoint =
                center +
                (Vector3)(
                    direction *
                    randomizedRadius
                ) +
                (Vector3)(
                    perpendicular *
                    Random.Range(
                        -shardJitter,
                        shardJitter
                    )
                );

            line.sortingLayerID =
                sortingLayerId;

            line.sortingOrder =
                sortingOrder;

            line.positionCount = 3;

            line.SetPosition(
                0,
                startPoint
            );

            line.SetPosition(
                1,
                middlePoint
            );

            line.SetPosition(
                2,
                endPoint
            );

            line.startWidth =
                shardWidth;

            line.endWidth =
                shardWidth *
                0.35f;

            line.startColor =
                startColor;

            line.endColor =
                endColor;

            line.gameObject.SetActive(
                true
            );

            StartCoroutine(
                AnimateShardRoutine(
                    line,
                    startColor,
                    endColor
                )
            );
        }
    }

    private IEnumerator AnimateShardRoutine(
        LineRenderer line,
        Color startColor,
        Color endColor)
    {
        if (line == null)
        {
            yield break;
        }

        if (shardDuration <= 0f)
        {
            ReturnLineRenderer(
                line
            );

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < shardDuration)
        {
            if (line == null)
            {
                yield break;
            }

            elapsed +=
                Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    shardDuration
                );

            float remaining =
                1f -
                progress;

            Color animatedStartColor =
                startColor;

            Color animatedEndColor =
                endColor;

            animatedStartColor.a *=
                remaining;

            animatedEndColor.a *=
                remaining;

            line.startColor =
                animatedStartColor;

            line.endColor =
                animatedEndColor;

            line.startWidth =
                shardWidth *
                Mathf.Lerp(
                    1f,
                    0.25f,
                    progress
                );

            line.endWidth =
                line.startWidth *
                0.35f;

            yield return null;
        }

        ReturnLineRenderer(
            line
        );
    }

    private void SpawnFlash(
        Block targetBlock,
        SpriteRenderer baseRenderer,
        Color flashColor)
    {
        if (targetBlock == null ||
            baseRenderer == null)
        {
            return;
        }

        SpriteRenderer flashRenderer =
            GetFlashRenderer();

        if (flashRenderer == null)
        {
            return;
        }

        Transform flashTransform =
            flashRenderer.transform;

        flashTransform.SetParent(
            baseRenderer.transform,
            false
        );

        flashTransform.localPosition =
            Vector3.zero;

        flashTransform.localRotation =
            Quaternion.identity;

        flashTransform.localScale =
            Vector3.one *
            flashStartScale;

        flashRenderer.sprite =
            baseRenderer.sprite;

        flashRenderer.drawMode =
            baseRenderer.drawMode;

        flashRenderer.size =
            baseRenderer.size;

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
            sortingOrderOffset -
            1;

        flashRenderer.sharedMaterial =
            baseRenderer.sharedMaterial;

        flashRenderer.color =
            flashColor;

        flashRenderer.gameObject.SetActive(
            true
        );

        StartCoroutine(
            AnimateFlashRoutine(
                flashRenderer,
                flashColor
            )
        );
    }

    private IEnumerator AnimateFlashRoutine(
        SpriteRenderer flashRenderer,
        Color flashColor)
    {
        if (flashRenderer == null)
        {
            yield break;
        }

        if (flashDuration <= 0f)
        {
            ReturnFlashRenderer(
                flashRenderer
            );

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            if (flashRenderer == null)
            {
                yield break;
            }

            elapsed +=
                Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed /
                    flashDuration
                );

            float easedProgress =
                1f -
                Mathf.Pow(
                    1f - progress,
                    3f
                );

            flashRenderer.transform.localScale =
                Vector3.one *
                Mathf.Lerp(
                    flashStartScale,
                    flashEndScale,
                    easedProgress
                );

            Color animatedColor =
                flashColor;

            animatedColor.a *=
                1f -
                progress;

            flashRenderer.color =
                animatedColor;

            yield return null;
        }

        ReturnFlashRenderer(
            flashRenderer
        );
    }

    private float CalculateEffectRadius(
        Block targetBlock)
    {
        if (targetBlock == null)
        {
            return 0.5f;
        }

        Vector2 worldSize =
            targetBlock.WorldSize;

        float largestSize =
            Mathf.Max(
                worldSize.x,
                worldSize.y
            );

        return Mathf.Max(
            largestSize *
            shardRadiusMultiplier,
            0.25f
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

        if (IsValidBaseRenderer(
                rootRenderer))
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

            if (IsValidBaseRenderer(
                    candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private bool IsValidBaseRenderer(
        SpriteRenderer candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        string objectName =
            candidate.gameObject.name;

        if (objectName ==
                "ElementSurfaceVisual" ||
            objectName ==
                "ElementStatusVisual" ||
            objectName ==
                "FrozenShatterFlash" ||
            objectName ==
                "ConductionFlash")
        {
            return false;
        }

        return candidate.sprite != null;
    }

    private void PrepareLineMaterial()
    {
        if (lineMaterial != null)
        {
            return;
        }

        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/" +
                "2D/Sprite-Unlit-Default"
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
                "FrozenShatterVfxSpawner: " +
                "라인 렌더러용 Shader를 찾지 못했습니다.",
                this
            );

            return;
        }

        runtimeLineMaterial =
            new Material(
                shader
            );

        runtimeLineMaterial.name =
            "Runtime_FrozenShatterLine";

        lineMaterial =
            runtimeLineMaterial;
    }

    private void PrewarmPools()
    {
        for (int i = 0;
             i < prewarmLineCount;
             i++)
        {
            LineRenderer line =
                CreateLineRenderer();

            if (line == null)
            {
                break;
            }

            line.gameObject.SetActive(
                false
            );

            availableLines.Enqueue(
                line
            );
        }

        for (int i = 0;
             i < prewarmFlashCount;
             i++)
        {
            SpriteRenderer flash =
                CreateFlashRenderer();

            if (flash == null)
            {
                break;
            }

            flash.gameObject.SetActive(
                false
            );

            availableFlashes.Enqueue(
                flash
            );
        }
    }

    private LineRenderer GetLineRenderer()
    {
        while (availableLines.Count > 0)
        {
            LineRenderer line =
                availableLines.Dequeue();

            if (line != null)
            {
                return line;
            }
        }

        return CreateLineRenderer();
    }

    private LineRenderer CreateLineRenderer()
    {
        if (createdLineCount >=
            maximumLineCount)
        {
            return null;
        }

        GameObject lineObject =
            new GameObject(
                $"FrozenShardLine_" +
                $"{createdLineCount + 1}"
            );

        lineObject.transform.SetParent(
            effectRoot,
            false
        );

        LineRenderer line =
            lineObject.AddComponent<
                LineRenderer
            >();

        line.useWorldSpace = true;
        line.loop = false;
        line.alignment =
            LineAlignment.View;

        line.textureMode =
            LineTextureMode.Stretch;

        line.numCapVertices = 2;
        line.numCornerVertices = 2;

        line.sharedMaterial =
            lineMaterial;

        line.positionCount = 3;

        createdLineCount++;

        return line;
    }

    private void ReturnLineRenderer(
        LineRenderer line)
    {
        if (line == null)
        {
            return;
        }

        line.gameObject.SetActive(
            false
        );

        line.positionCount = 0;

        availableLines.Enqueue(
            line
        );
    }

    private SpriteRenderer GetFlashRenderer()
    {
        while (availableFlashes.Count > 0)
        {
            SpriteRenderer flash =
                availableFlashes.Dequeue();

            if (flash != null)
            {
                return flash;
            }
        }

        return CreateFlashRenderer();
    }

    private SpriteRenderer CreateFlashRenderer()
    {
        if (createdFlashCount >=
            maximumFlashCount)
        {
            return null;
        }

        GameObject flashObject =
            new GameObject(
                $"FrozenShatterFlash_" +
                $"{createdFlashCount + 1}"
            );

        flashObject.transform.SetParent(
            effectRoot,
            false
        );

        SpriteRenderer flashRenderer =
            flashObject.AddComponent<
                SpriteRenderer
            >();

        createdFlashCount++;

        return flashRenderer;
    }

    private void ReturnFlashRenderer(
        SpriteRenderer flashRenderer)
    {
        if (flashRenderer == null)
        {
            return;
        }

        flashRenderer.gameObject.SetActive(
            false
        );

        flashRenderer.sprite = null;
        flashRenderer.color = Color.clear;

        flashRenderer.transform.SetParent(
            effectRoot,
            false
        );

        flashRenderer.transform.localPosition =
            Vector3.zero;

        flashRenderer.transform.localRotation =
            Quaternion.identity;

        flashRenderer.transform.localScale =
            Vector3.one;

        availableFlashes.Enqueue(
            flashRenderer
        );
    }
}