using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ExplosionPatternVfxSpawner :
    MonoBehaviour
{
    [Header("References")]

    [Tooltip(
        "생성된 폭발 범위 선이 들어갈 부모입니다. " +
        "비어 있으면 현재 오브젝트를 사용합니다."
    )]
    [SerializeField]
    private Transform effectRoot;

    [Tooltip(
        "폭발 범위 선에 사용할 머티리얼입니다. " +
        "비워두면 URP 2D용 머티리얼을 런타임에 생성합니다."
    )]
    [SerializeField]
    private Material lineMaterial;

    [Header("Presentation")]

    [ColorUsage(
        true,
        true
    )]
    [SerializeField]
    private Color lineColor =
        new Color(
            1f,
            0.42f,
            0.06f,
            0.95f
        );

    [Tooltip(
        "폭발 범위 선이 유지되는 시간입니다."
    )]
    [SerializeField, Min(0.01f)]
    private float duration = 0.22f;

    [Tooltip(
        "폭발 선의 최대 두께입니다."
    )]
    [SerializeField, Min(0.001f)]
    private float beamWidth = 0.13f;

    [Tooltip(
        "선 끝부분의 두께 배율입니다."
    )]
    [SerializeField, Range(0f, 1f)]
    private float tipWidthRatio = 0.25f;

    [Tooltip(
        "충돌 지점 중앙에서 선이 시작되는 간격입니다."
    )]
    [SerializeField, Min(0f)]
    private float innerGap = 0.06f;

    [Tooltip(
        "전체 시간 중 선이 끝까지 뻗는 데 사용하는 비율입니다."
    )]
    [SerializeField, Range(0.05f, 1f)]
    private float extendDurationRatio = 0.28f;

    [Tooltip(
        "전체 시간 중 페이드아웃이 시작되는 시점입니다."
    )]
    [SerializeField, Range(0f, 0.95f)]
    private float fadeStartRatio = 0.3f;

    [Tooltip(
        "블록보다 카메라 쪽으로 얼마나 이동시킬지 결정합니다. " +
        "일반적인 2D 구성에서는 음수가 앞쪽입니다."
    )]
    [SerializeField]
    private float zOffset = -0.15f;

    [Header("Renderer")]

    [SerializeField]
    private string sortingLayerName =
        "Default";

    [SerializeField]
    private int sortingOrder = 150;

    [SerializeField, Range(0, 8)]
    private int capVertices = 2;

    [Header("Pool")]

    [SerializeField, Min(1)]
    private int prewarmCount = 16;

    [SerializeField, Min(1)]
    private int maximumPoolSize = 64;

    private readonly Queue<LineRenderer>
        availableLines =
            new Queue<LineRenderer>();

    private readonly List<LineRenderer>
        activeLines =
            new List<LineRenderer>();

    private readonly Dictionary<
        LineRenderer,
        Coroutine
    > runningRoutines =
        new Dictionary<
            LineRenderer,
            Coroutine
        >();

    private Material runtimeMaterial;
    private int createdLineCount;

    private void Awake()
    {
        NormalizeSettings();

        if (effectRoot == null)
        {
            effectRoot =
                transform;
        }

        Prewarm();
    }

    private void OnValidate()
    {
        NormalizeSettings();
    }

    private void OnDisable()
    {
        StopAndReturnAllLines();
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(
                runtimeMaterial
            );

            runtimeMaterial =
                null;
        }
    }

    private void NormalizeSettings()
    {
        duration =
            Mathf.Max(
                duration,
                0.01f
            );

        beamWidth =
            Mathf.Max(
                beamWidth,
                0.001f
            );

        tipWidthRatio =
            Mathf.Clamp01(
                tipWidthRatio
            );

        innerGap =
            Mathf.Max(
                innerGap,
                0f
            );

        extendDurationRatio =
            Mathf.Clamp(
                extendDurationRatio,
                0.05f,
                1f
            );

        fadeStartRatio =
            Mathf.Clamp(
                fadeStartRatio,
                0f,
                0.95f
            );

        prewarmCount =
            Mathf.Max(
                prewarmCount,
                1
            );

        maximumPoolSize =
            Mathf.Max(
                maximumPoolSize,
                prewarmCount
            );

        capVertices =
            Mathf.Clamp(
                capVertices,
                0,
                8
            );
    }

    private void Prewarm()
    {
        int amount =
            Mathf.Min(
                prewarmCount,
                maximumPoolSize
            );

        for (int i = 0;
             i < amount;
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
    }

    public void Play(
        Block sourceBlock,
        ExplosionPatternType patternType,
        int range)
    {
        if (sourceBlock == null)
        {
            return;
        }

        range =
            Mathf.Max(
                range,
                1
            );

        BoardGrid boardGrid =
            sourceBlock.BoardGrid;

        Vector3 boardRight =
            boardGrid != null
                ? boardGrid.transform.right.normalized
                : Vector3.right;

        Vector3 boardUp =
            boardGrid != null
                ? boardGrid.transform.up.normalized
                : Vector3.up;

        float cellSize =
            boardGrid != null
                ? boardGrid.CellSize
                : sourceBlock.CellSize;

        cellSize =
            Mathf.Max(
                cellSize,
                0.01f
            );

        Vector2Int gridSize =
            sourceBlock.GridSize;

        /*
         * 1×1 블록:
         * range 1이면 중심에서 한 셀 떨어진 위치까지 표시
         *
         * 2×2 블록:
         * 블록 중심에서 바깥쪽 첫 번째 셀 중심까지의 거리를
         * 추가로 반영합니다.
         */
        float horizontalDistance =
            (
                (
                    Mathf.Max(
                        gridSize.x,
                        1
                    ) -
                    1
                ) *
                0.5f +
                range
            ) *
            cellSize;

        float verticalDistance =
            (
                (
                    Mathf.Max(
                        gridSize.y,
                        1
                    ) -
                    1
                ) *
                0.5f +
                range
            ) *
            cellSize;

        Vector3 center =
            sourceBlock.transform.position;

        center.z +=
            zOffset;

        bool showCross =
            patternType ==
                ExplosionPatternType.Cross ||
            patternType ==
                ExplosionPatternType
                    .AllDirections;

        bool showDiagonal =
            patternType ==
                ExplosionPatternType.Diagonal ||
            patternType ==
                ExplosionPatternType
                    .AllDirections;

        if (showCross)
        {
            SpawnBeam(
                center,
                boardRight *
                horizontalDistance
            );

            SpawnBeam(
                center,
                -boardRight *
                horizontalDistance
            );

            SpawnBeam(
                center,
                boardUp *
                verticalDistance
            );

            SpawnBeam(
                center,
                -boardUp *
                verticalDistance
            );
        }

        if (showDiagonal)
        {
            SpawnBeam(
                center,
                boardRight *
                horizontalDistance +
                boardUp *
                verticalDistance
            );

            SpawnBeam(
                center,
                -boardRight *
                horizontalDistance +
                boardUp *
                verticalDistance
            );

            SpawnBeam(
                center,
                boardRight *
                horizontalDistance -
                boardUp *
                verticalDistance
            );

            SpawnBeam(
                center,
                -boardRight *
                horizontalDistance -
                boardUp *
                verticalDistance
            );
        }
    }

    private void SpawnBeam(
        Vector3 center,
        Vector3 targetOffset)
    {
        if (targetOffset.sqrMagnitude <=
            0.0001f)
        {
            return;
        }

        LineRenderer line =
            AcquireLine();

        if (line == null)
        {
            return;
        }

        Vector3 direction =
            targetOffset.normalized;

        Vector3 startPosition =
            center +
            direction *
            innerGap;

        Vector3 endPosition =
            center +
            targetOffset;

        line.gameObject.SetActive(
            true
        );

        line.positionCount = 2;

        line.SetPosition(
            0,
            startPosition
        );

        line.SetPosition(
            1,
            startPosition
        );

        line.startWidth = 0f;
        line.endWidth = 0f;

        SetLineAlpha(
            line,
            0f
        );

        activeLines.Add(
            line
        );

        Coroutine routine =
            StartCoroutine(
                PlayBeamRoutine(
                    line,
                    startPosition,
                    endPosition
                )
            );

        runningRoutines[
            line
        ] =
            routine;
    }

    private IEnumerator PlayBeamRoutine(
        LineRenderer line,
        Vector3 startPosition,
        Vector3 endPosition)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (line == null)
            {
                yield break;
            }

            float normalizedTime =
                Mathf.Clamp01(
                    elapsed /
                    duration
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

            Vector3 currentEndPosition =
                Vector3.Lerp(
                    startPosition,
                    endPosition,
                    extensionProgress
                );

            line.SetPosition(
                0,
                startPosition
            );

            line.SetPosition(
                1,
                currentEndPosition
            );

            float appearanceAlpha =
                Mathf.Clamp01(
                    normalizedTime /
                    0.08f
                );

            float fadeProgress =
                Mathf.InverseLerp(
                    fadeStartRatio,
                    1f,
                    normalizedTime
                );

            float alpha =
                appearanceAlpha *
                (
                    1f -
                    fadeProgress
                );

            float widthFactor =
                CalculateWidthFactor(
                    normalizedTime
                );

            line.startWidth =
                beamWidth *
                widthFactor;

            line.endWidth =
                beamWidth *
                tipWidthRatio *
                widthFactor;

            SetLineAlpha(
                line,
                alpha
            );

            elapsed +=
                Time.deltaTime;

            yield return null;
        }

        if (line != null)
        {
            line.SetPosition(
                1,
                endPosition
            );

            SetLineAlpha(
                line,
                0f
            );
        }

        ReleaseLine(
            line
        );
    }

    private float CalculateWidthFactor(
        float normalizedTime)
    {
        const float peakTime = 0.18f;

        if (normalizedTime <= peakTime)
        {
            return Mathf.Clamp01(
                normalizedTime /
                peakTime
            );
        }

        return 1f -
               Mathf.InverseLerp(
                   peakTime,
                   1f,
                   normalizedTime
               );
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

    private void SetLineAlpha(
        LineRenderer line,
        float alpha)
    {
        if (line == null)
        {
            return;
        }

        alpha =
            Mathf.Clamp01(
                alpha
            );

        Color startColor =
            lineColor;

        startColor.a *=
            alpha;

        Color endColor =
            lineColor;

        endColor.a *=
            alpha *
            0.55f;

        line.startColor =
            startColor;

        line.endColor =
            endColor;
    }

    private LineRenderer AcquireLine()
    {
        while (availableLines.Count > 0)
        {
            LineRenderer line =
                availableLines.Dequeue();

            if (line != null)
            {
                RefreshRendererSettings(
                    line
                );

                return line;
            }
        }

        if (createdLineCount >=
            maximumPoolSize)
        {
            return null;
        }

        return CreateLineRenderer();
    }

    private LineRenderer CreateLineRenderer()
    {
        if (createdLineCount >=
            maximumPoolSize)
        {
            return null;
        }

        GameObject lineObject =
            new GameObject(
                $"ExplosionPatternLine_" +
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
        line.positionCount = 2;
        line.loop = false;

        line.alignment =
            LineAlignment.View;

        line.textureMode =
            LineTextureMode.Stretch;

        line.numCapVertices =
            capVertices;

        line.numCornerVertices =
            0;

        Material resolvedMaterial =
            GetResolvedMaterial();

        if (resolvedMaterial != null)
        {
            line.sharedMaterial =
                resolvedMaterial;
        }

        RefreshRendererSettings(
            line
        );

        lineObject.SetActive(
            false
        );

        createdLineCount++;

        return line;
    }

    private void RefreshRendererSettings(
        LineRenderer line)
    {
        if (line == null)
        {
            return;
        }

        line.sortingLayerName =
            sortingLayerName;

        line.sortingOrder =
            sortingOrder;

        line.numCapVertices =
            capVertices;

        Material resolvedMaterial =
            GetResolvedMaterial();

        if (resolvedMaterial != null &&
            line.sharedMaterial !=
            resolvedMaterial)
        {
            line.sharedMaterial =
                resolvedMaterial;
        }
    }

    private Material GetResolvedMaterial()
    {
        if (lineMaterial != null)
        {
            return lineMaterial;
        }

        if (runtimeMaterial != null)
        {
            return runtimeMaterial;
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
            Debug.LogError(
                "ExplosionPatternVfxSpawner: " +
                "LineRenderer용 셰이더를 찾지 못했습니다. " +
                "Line Material을 직접 연결해 주세요.",
                this
            );

            return null;
        }

        runtimeMaterial =
            new Material(
                shader
            );

        runtimeMaterial.name =
            "Runtime_ExplosionPatternLine";

        runtimeMaterial.hideFlags =
            HideFlags.HideAndDontSave;

        return runtimeMaterial;
    }

    private void ReleaseLine(
        LineRenderer line)
    {
        if (line == null)
        {
            return;
        }

        runningRoutines.Remove(
            line
        );

        activeLines.Remove(
            line
        );

        line.gameObject.SetActive(
            false
        );

        availableLines.Enqueue(
            line
        );
    }

    private void StopAndReturnAllLines()
    {
        foreach (KeyValuePair<
                     LineRenderer,
                     Coroutine
                 > pair in runningRoutines)
        {
            if (pair.Value != null)
            {
                StopCoroutine(
                    pair.Value
                );
            }
        }

        runningRoutines.Clear();

        for (int i = activeLines.Count - 1;
             i >= 0;
             i--)
        {
            LineRenderer line =
                activeLines[i];

            if (line == null)
            {
                continue;
            }

            line.gameObject.SetActive(
                false
            );

            availableLines.Enqueue(
                line
            );
        }

        activeLines.Clear();
    }
}