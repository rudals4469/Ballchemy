using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ElementPlaceholderVfxController : MonoBehaviour
{
    private enum Style
    {
        Lightning, Water, WaterDrop,
        LightningIcon, IceSnowflake, FireIcon,
        ThermalRing
    }
    private static readonly Vector2[] LightningShape =
    {
        new Vector2(0.16f, 0.5f), new Vector2(-0.2f, 0.08f),
        new Vector2(0.03f, 0.08f), new Vector2(-0.12f, -0.5f),
        new Vector2(0.26f, -0.02f)
    };
    private static readonly Vector2[] IceSnowflakeShape =
    {
        new Vector2(0f, 0.5f), new Vector2(0f, -0.5f),
        new Vector2(0f, 0f), new Vector2(0.43f, 0.25f),
        new Vector2(-0.43f, -0.25f), new Vector2(0f, 0f),
        new Vector2(-0.43f, 0.25f), new Vector2(0.43f, -0.25f)
    };
    private static readonly Vector2[] FireShape =
    {
        new Vector2(0f, -0.5f), new Vector2(-0.3f, -0.24f),
        new Vector2(-0.28f, 0.02f), new Vector2(-0.1f, 0.31f),
        new Vector2(-0.04f, 0.06f), new Vector2(0.08f, 0.52f),
        new Vector2(0.14f, 0.16f), new Vector2(0.3f, 0.31f),
        new Vector2(0.25f, -0.1f), new Vector2(0.12f, -0.38f),
        new Vector2(0f, -0.5f)
    };
    private sealed class State
    {
        public Vector3 Start, End;
        public Color Color;
        public float Began, Duration, Width, Seed;
        public Style Style;
    }
    private sealed class SymbolState
    {
        public SpriteEffectKind Kind;
        public float Began, Duration, Scale;
        public Color Color, EndColor;
    }
    private enum SpriteEffectKind { Symbol, Chain, Burst }

    [SerializeField, Min(8)] private int poolSize = 48;
    [SerializeField, Min(0.01f)] private float travelDuration = 0.51f;
    [SerializeField, Min(0.01f)] private float impactDuration = 0.45f;
    [SerializeField, Min(0.001f)] private float lineWidth = 0.18f;

    private readonly List<LineRenderer> pool = new List<LineRenderer>();
    private readonly List<State> states = new List<State>();
    private readonly List<SpriteRenderer> symbolPool = new List<SpriteRenderer>();
    private readonly List<SymbolState> symbolStates = new List<SymbolState>();
    private readonly Dictionary<ElementType, Sprite> symbolSprites =
        new Dictionary<ElementType, Sprite>();
    private Sprite lightningChainSprite;
    private Sprite thermalBurstSprite;
    private int nextIndex;
    private int nextSymbolIndex;
    private int electricSequenceFrame = -1;
    private int electricSequenceIndex;
    private Material sharedMaterial;

    private void Awake()
    {
        WarmPool();
        LoadSymbolSprites();
        WarmSymbolPool();
    }
    private void OnEnable()
    {
        ElementVisualEvents.Impact += HandleImpact;
        ElementVisualEvents.Travel += HandleTravel;
        ElementVisualEvents.ThermalShock += HandleThermalShock;
    }
    private void OnDisable()
    {
        ElementVisualEvents.Impact -= HandleImpact;
        ElementVisualEvents.Travel -= HandleTravel;
        ElementVisualEvents.ThermalShock -= HandleThermalShock;
    }

    private void WarmPool()
    {
        if (pool.Count > 0) return;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null) sharedMaterial = new Material(shader);
        for (int i = 0; i < poolSize; i++)
            AddLineRenderer();
    }

    private void AddLineRenderer()
    {
        GameObject item = new GameObject($"ElementVfx_{pool.Count}");
        item.transform.SetParent(transform, false);
        LineRenderer line = item.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.numCapVertices = 8;
        line.numCornerVertices = 8;
        line.sortingOrder = 25;
        line.material = sharedMaterial;
        line.enabled = false;
        pool.Add(line);
        states.Add(null);
    }

    private void Update()
    {
        float now = Time.time;
        for (int i = 0; i < pool.Count; i++)
        {
            State state = states[i];
            if (state == null || !pool[i].enabled) continue;
            if (now < state.Began)
            {
                pool[i].startColor = Color.clear;
                pool[i].endColor = Color.clear;
                continue;
            }
            float progress = (now - state.Began) / state.Duration;
            if (progress >= 1f)
            {
                pool[i].enabled = false;
                states[i] = null;
                continue;
            }
            Animate(pool[i], state, Mathf.Clamp01(progress), now);
        }
        for (int i = 0; i < symbolPool.Count; i++)
        {
            SymbolState state = symbolStates[i];
            if (state == null || !symbolPool[i].enabled) continue;
            if (now < state.Began)
            {
                Color waitingColor = state.Color;
                waitingColor.a = 0f;
                symbolPool[i].color = waitingColor;
                continue;
            }
            float progress = (now - state.Began) / state.Duration;
            if (progress >= 1f)
            {
                symbolPool[i].enabled = false;
                symbolStates[i] = null;
                continue;
            }
            float fade = 1f - Mathf.InverseLerp(0.55f, 1f, progress);
            if (state.Kind == SpriteEffectKind.Symbol)
            {
                float appear = Mathf.Clamp01(progress / 0.18f);
                float scale = state.Scale * Mathf.Lerp(0.72f, 1.08f, appear);
                symbolPool[i].transform.localScale = Vector3.one * scale;
            }
            else if (state.Kind == SpriteEffectKind.Chain)
            {
                float flicker = 0.82f + Mathf.PingPong(now * 17f, 0.18f);
                Vector3 scale = symbolPool[i].transform.localScale;
                scale.y = state.Scale * flicker;
                symbolPool[i].transform.localScale = scale;
            }
            else
            {
                float expansion = 1f - (1f - progress) * (1f - progress);
                symbolPool[i].transform.localScale =
                    Vector3.one * state.Scale * expansion;
            }
            Color color = state.Kind == SpriteEffectKind.Burst
                ? Color.Lerp(state.Color, state.EndColor,
                    Mathf.SmoothStep(0f, 1f, progress))
                : state.Color;
            color.a *= fade;
            symbolPool[i].color = color;
        }
    }

    private void HandleImpact(ElementType element, Block target)
    {
        // Direct-hit symbols are rendered by BallDamagePopupView beside the
        // damage number. Keeping a second world-space symbol here made every
        // hit look like two unrelated effects.
    }

    private void LoadSymbolSprites()
    {
        LoadSymbol(ElementType.Electric, "VFX/ElementSymbols/Icon_Element_Lightning");
        LoadSymbol(ElementType.Water, "VFX/ElementSymbols/Icon_Element_Water");
        LoadSymbol(ElementType.Ice, "VFX/ElementSymbols/Icon_Element_Ice");
        LoadSymbol(ElementType.Fire, "VFX/ElementSymbols/Icon_Element_Fire");
        lightningChainSprite = LoadSprite(
            "VFX/ElementSymbols/Vfx_Chain_Lightning");
        thermalBurstSprite = LoadSprite(
            "VFX/ElementSymbols/Vfx_Thermal_Burst");
    }

    private void LoadSymbol(ElementType element, string resourcePath)
    {
        Sprite sprite = LoadSprite(resourcePath);
        if (sprite != null) symbolSprites[element] = sprite;
    }

    private static Sprite LoadSprite(string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null) return null;
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            Mathf.Max(texture.width, texture.height));
    }

    private void WarmSymbolPool()
    {
        if (symbolPool.Count > 0) return;
        const int count = 64;
        for (int i = 0; i < count; i++)
            AddSymbolRenderer();
    }

    private void AddSymbolRenderer()
    {
        GameObject item = new GameObject($"ElementSymbol_{symbolPool.Count}");
        item.transform.SetParent(transform, false);
        SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 27;
        renderer.enabled = false;
        symbolPool.Add(renderer);
        symbolStates.Add(null);
    }

    private int AcquireSymbolIndex()
    {
        WarmSymbolPool();
        for (int offset = 0; offset < symbolPool.Count; offset++)
        {
            int index = (nextSymbolIndex + offset) % symbolPool.Count;
            if (symbolStates[index] != null && symbolPool[index].enabled)
                continue;

            nextSymbolIndex = (index + 1) % symbolPool.Count;
            return index;
        }

        int expandedIndex = symbolPool.Count;
        AddSymbolRenderer();
        nextSymbolIndex = 0;
        return expandedIndex;
    }

    private int AcquireLineIndex()
    {
        WarmPool();
        for (int offset = 0; offset < pool.Count; offset++)
        {
            int index = (nextIndex + offset) % pool.Count;
            if (states[index] != null && pool[index].enabled)
                continue;

            nextIndex = (index + 1) % pool.Count;
            return index;
        }

        int expandedIndex = pool.Count;
        AddLineRenderer();
        nextIndex = 0;
        return expandedIndex;
    }

    private void PlaySymbol(Vector3 position, ElementType element, Color color)
    {
        if (!symbolSprites.TryGetValue(element, out Sprite sprite)) return;
        int index = AcquireSymbolIndex();
        SpriteRenderer renderer = symbolPool[index];
        renderer.sprite = sprite;
        renderer.transform.position = position;
        renderer.transform.rotation = Quaternion.Euler(
            0f, 0f, Random.Range(-8f, 8f));
        renderer.flipX = false;
        renderer.flipY = false;
        renderer.enabled = true;
        symbolStates[index] = new SymbolState
        {
            Kind = SpriteEffectKind.Symbol,
            Began = Time.time,
            Duration = impactDuration,
            Scale = Random.Range(0.5f, 0.62f),
            Color = color,
            EndColor = color
        };
    }

    private void PlayChainSprite(
        Vector3 start,
        Vector3 end,
        Color color,
        float lateralOffset,
        float widthScale,
        bool flipY,
        float angleOffset,
        float delay = 0f,
        float durationScale = 1f)
    {
        if (lightningChainSprite == null) return;
        Vector3 delta = end - start;
        float length = delta.magnitude;
        if (length <= 0.001f) return;
        int index = AcquireSymbolIndex();
        SpriteRenderer renderer = symbolPool[index];
        renderer.sprite = lightningChainSprite;
        Vector3 perpendicular = Vector3.Cross(delta.normalized, Vector3.forward);
        renderer.transform.position =
            (start + end) * 0.5f + perpendicular * lateralOffset;
        renderer.transform.rotation = Quaternion.Euler(
            0f, 0f,
            Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + angleOffset);
        renderer.transform.localScale = new Vector3(length, widthScale, 1f);
        renderer.flipX = false;
        renderer.flipY = flipY;
        renderer.color = delay > 0f
            ? new Color(color.r, color.g, color.b, 0f)
            : color;
        renderer.enabled = true;
        symbolStates[index] = new SymbolState
        {
            Kind = SpriteEffectKind.Chain,
            Began = Time.time + delay,
            Duration = travelDuration * durationScale,
            Scale = widthScale,
            Color = color,
            EndColor = color
        };
    }

    private void PlayChainBundle(
        Vector3 start,
        Vector3 end,
        Color color,
        float sequenceDelay)
    {
        Vector3 delta = end - start;
        if (delta.sqrMagnitude <= 0.001f) return;

        Vector3 perpendicular =
            Vector3.Cross(delta.normalized, Vector3.forward);

        // 1. 얇은 예고 전류가 먼저 경로를 짚는다.
        Play(start, end, new Color(1f, 0.96f, 0f, 0.78f),
            0.055f, 0.24f, Style.Lightning, sequenceDelay);

        // 2. 거의 흰 중심 낙뢰와 노란 외곽 전류가 짧고 강하게 친다.
        float strikeDelay = sequenceDelay + 0.045f;
        Color hotCore = Color.Lerp(color, Color.white, 0.16f);
        PlayChainSprite(start, end, hotCore, 0f, 1.34f, false, -2.5f,
            strikeDelay, 0.3f);
        PlayChainSprite(start, end, WithAlpha(color, 0.86f),
            0.13f, 0.92f, true, 4.5f, strikeDelay, 0.34f);
        PlayChainSprite(start, end, WithAlpha(color, 0.72f),
            -0.12f, 0.8f, false, -5.5f, strikeDelay, 0.32f);

        // 기존 동적 번개도 양옆으로 꼬아 정지 이미지처럼 보이지 않게 한다.
        Play(start + perpendicular * 0.07f, end - perpendicular * 0.05f,
            WithAlpha(hotCore, 0.96f), 0.15f, 1.04f,
            Style.Lightning, strikeDelay);
        Play(start - perpendicular * 0.08f, end + perpendicular * 0.065f,
            WithAlpha(color, 0.76f), 0.17f, 0.76f,
            Style.Lightning, strikeDelay);

        // 주 전류 중간에서 짧은 가지가 갈라져 번개 실루엣을 만든다.
        Vector3 direction = delta.normalized;
        Vector3 middle = (start + end) * 0.5f;
        float branchSide = Random.value < 0.5f ? -1f : 1f;
        Vector3 firstBranchEnd = middle + direction * 0.12f +
            perpendicular * branchSide * Random.Range(0.32f, 0.48f);
        PlayChainSprite(middle - direction * 0.08f, firstBranchEnd,
            WithAlpha(hotCore, 0.88f), 0f, 0.72f, branchSide < 0f,
            branchSide * 8f, strikeDelay, 0.28f);

        Vector3 secondStart = Vector3.Lerp(start, end, 0.68f);
        Vector3 secondBranchEnd = secondStart - direction * 0.06f -
            perpendicular * branchSide * Random.Range(0.22f, 0.34f);
        PlayChainSprite(secondStart, secondBranchEnd,
            WithAlpha(color, 0.72f), 0f, 0.5f, branchSide > 0f,
            -branchSide * 10f, strikeDelay, 0.26f);

        // 3. 도착점 섬광과 방사형 파편이 선을 실제 타격으로 읽히게 한다.
        float impactDelay = strikeDelay + 0.055f;
        Play(end, end + Vector3.right * 0.21f,
            new Color(1f, 0.94f, 0f, 1f),
            0.16f, 0.72f, Style.ThermalRing, impactDelay);
        int sparkCount = Random.Range(4, 7);
        float angleOffset = Random.Range(0f, Mathf.PI * 2f);
        for (int i = 0; i < sparkCount; i++)
        {
            float angle = angleOffset + i * Mathf.PI * 2f / sparkCount;
            Vector3 sparkDirection = new Vector3(
                Mathf.Cos(angle), Mathf.Sin(angle));
            float sparkLength = Random.Range(0.2f, 0.38f);
            Play(end, end + sparkDirection * sparkLength,
                WithAlpha(color, Random.Range(0.72f, 1f)),
                Random.Range(0.1f, 0.16f), 0.58f,
                Style.Lightning, impactDelay + Random.Range(0f, 0.025f));
        }

        // 4. 본체가 꺼진 뒤에는 가는 주황빛 잔류 전류만 남는다.
        Play(start, end, new Color(1f, 0.82f, 0f, 0.58f),
            0.2f, 0.34f, Style.Lightning, strikeDelay + 0.105f);
    }

    private void PlayPrimaryLightningStrike(
        Vector3 target,
        Color color)
    {
        Vector3 sky = target + Vector3.up * 2.45f;
        Color whiteCore = Color.Lerp(color, Color.white, 0.24f);

        // 중심 블록 발동을 알리는 가장 크고 밝은 수직 낙뢰다.
        Play(sky, target, new Color(1f, 0.96f, 0f, 0.76f),
            0.045f, 0.28f, Style.Lightning);
        PlayChainSprite(sky, target, whiteCore,
            0f, 1.82f, false, -1.5f, 0.035f, 0.34f);
        PlayChainSprite(sky, target, WithAlpha(color, 0.94f),
            0.16f, 1.28f, true, 4f, 0.035f, 0.38f);
        PlayChainSprite(sky, target, WithAlpha(color, 0.76f),
            -0.14f, 1.02f, false, -5f, 0.035f, 0.35f);
        Play(sky + Vector3.left * 0.08f, target + Vector3.right * 0.04f,
            whiteCore, 0.18f, 1.34f, Style.Lightning, 0.035f);

        // 얼음-불 폭발과 겹쳐도 중심이 보이도록 이중 충격파를 남긴다.
        Play(target, target + Vector3.right * 0.29f,
            new Color(1f, 0.92f, 0f, 1f),
            0.2f, 0.92f, Style.ThermalRing, 0.075f);
        Play(target, target + Vector3.right * 0.18f,
            whiteCore, 0.14f, 0.68f, Style.ThermalRing, 0.09f);

        int sparkCount = 7;
        float angleOffset = Random.Range(0f, Mathf.PI * 2f);
        for (int i = 0; i < sparkCount; i++)
        {
            float angle = angleOffset + i * Mathf.PI * 2f / sparkCount;
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle));
            Play(target, target + direction * Random.Range(0.28f, 0.48f),
                WithAlpha(color, Random.Range(0.82f, 1f)),
                Random.Range(0.12f, 0.18f), 0.72f,
                Style.Lightning, 0.08f + Random.Range(0f, 0.025f));
        }
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a *= alpha;
        return color;
    }

    private void PlayBurstSprite(
        Vector3 position,
        Color startColor,
        Color endColor,
        float rotation,
        float scale,
        float delay,
        float duration)
    {
        if (thermalBurstSprite == null) return;
        int index = AcquireSymbolIndex();
        SpriteRenderer renderer = symbolPool[index];
        renderer.sprite = thermalBurstSprite;
        renderer.transform.position = position;
        renderer.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        renderer.transform.localScale = Vector3.zero;
        renderer.flipX = false;
        renderer.flipY = false;
        renderer.color = delay > 0f
            ? new Color(startColor.r, startColor.g, startColor.b, 0f)
            : startColor;
        renderer.enabled = true;
        symbolStates[index] = new SymbolState
        {
            Kind = SpriteEffectKind.Burst,
            Began = Time.time + delay,
            Duration = duration,
            Scale = scale,
            Color = startColor,
            EndColor = endColor
        };
    }

    private void HandleTravel(ElementType element, Block source, Block target)
    {
        if (source == null || target == null) return;
        if (element != ElementType.Electric && element != ElementType.Water)
            return;
        Vector3 start = source.transform.position;
        Vector3 end = target.transform.position;
        Color color = ResolveColor(element);
        if (element == ElementType.Electric)
            color = new Color(1f, 0.93f, 0f, 1f);
        if (element == ElementType.Electric)
        {
            if (electricSequenceFrame != Time.frameCount)
            {
                electricSequenceFrame = Time.frameCount;
                electricSequenceIndex = 0;
            }
            bool isFirstLink = electricSequenceIndex == 0;
            if (isFirstLink)
                PlayPrimaryLightningStrike(start, color);

            // 수직 낙뢰가 꽂힌 뒤 중심에서 바깥으로 전류가 퍼진다.
            float sequenceDelay = 0.115f + electricSequenceIndex * 0.055f;
            electricSequenceIndex++;

            Vector3 direction = end - start;
            if (direction.sqrMagnitude > 0.001f)
            {
                direction.Normalize();
                start -= direction * 0.22f;
                end += direction * 0.22f;
            }
            PlayChainBundle(start, end, color, sequenceDelay);
            return;
        }
        Vector3 delta = end - start;
        Vector3 perpendicular = delta.sqrMagnitude > 0.001f
            ? Vector3.Cross(delta.normalized, Vector3.forward)
            : Vector3.up;
        for (int i = 0; i < 3; i++)
        {
            float t = 0.25f + i * 0.25f;
            float side = i == 1 ? -0.08f : 0.07f;
            PlaySymbol(
                Vector3.Lerp(start, end, t) + perpendicular * side,
                ElementType.Water,
                WithAlpha(color, 0.82f - i * 0.12f));
        }
    }

    private void HandleThermalShock(Block center)
    {
        if (center == null) return;
        Vector3 position = center.transform.position;
        Color red = new Color(1f, 0f, 0f, 1f);
        Color orange = new Color(1f, 0.42f, 0f, 1f);
        int pattern = Random.Range(0, 3);
        if (pattern == 0)
        {
            PlayBurstSprite(position, red, orange,
                Random.Range(-12f, 12f), 4.8f, 0f, 0.66f);
            return;
        }

        if (pattern == 1)
        {
            PlayBurstSprite(position + new Vector3(-0.08f, 0.02f),
                red, orange, -18f, 4.25f, 0f, 0.62f);
            PlayBurstSprite(position + new Vector3(0.1f, -0.025f),
                WithAlpha(red, 0.82f), WithAlpha(orange, 0.82f),
                24f, 3.65f, 0.055f, 0.58f);
            return;
        }

        PlayBurstSprite(position, red, orange, 0f, 4.15f, 0f, 0.62f);
        PlayBurstSprite(position + new Vector3(-0.12f, 0.08f),
            WithAlpha(red, 0.76f), WithAlpha(orange, 0.76f),
            -28f, 3.15f, 0.07f, 0.54f);
        PlayBurstSprite(position + new Vector3(0.13f, -0.07f),
            WithAlpha(red, 0.68f), WithAlpha(orange, 0.68f),
            31f, 2.7f, 0.13f, 0.5f);

    }

    private void Play(Vector3 start, Vector3 end, Color color,
        float duration, float widthMultiplier, Style style,
        float delay = 0f)
    {
        int index = AcquireLineIndex();
        states[index] = new State
        {
            Start = start, End = end, Color = color,
            Began = Time.time + delay, Duration = duration,
            Width = lineWidth * widthMultiplier,
            Seed = Random.Range(0f, 1000f), Style = style
        };
        pool[index].enabled = true;
        if (delay <= 0f)
            Animate(pool[index], states[index], 0f, Time.time);
        else
        {
            pool[index].startColor = Color.clear;
            pool[index].endColor = Color.clear;
        }
    }

    private static void Animate(LineRenderer line, State state,
        float progress, float now)
    {
        int points = state.Style == Style.LightningIcon ? LightningShape.Length :
            state.Style == Style.IceSnowflake ? IceSnowflakeShape.Length :
            state.Style == Style.FireIcon ? FireShape.Length :
            state.Style == Style.WaterDrop || state.Style == Style.ThermalRing
                ? 18 : 10;
        line.positionCount = points;
        Vector3 delta = state.End - state.Start;
        Vector3 perpendicular = delta.sqrMagnitude > 0.0001f
            ? Vector3.Cross(delta.normalized, Vector3.forward) : Vector3.right;
        float fade = 1f - progress;
        for (int i = 0; i < points; i++)
        {
            float t = i / (float)(points - 1);
            if (state.Style == Style.LightningIcon)
            {
                line.SetPosition(i, state.Start +
                    (Vector3)(LightningShape[i] * Vector3.Distance(state.Start, state.End)));
                continue;
            }
            if (state.Style == Style.IceSnowflake)
            {
                line.SetPosition(i, state.Start +
                    (Vector3)(IceSnowflakeShape[i] * Vector3.Distance(state.Start, state.End)));
                continue;
            }
            if (state.Style == Style.FireIcon)
            {
                line.SetPosition(i, state.Start +
                    (Vector3)(FireShape[i] * Vector3.Distance(state.Start, state.End)));
                continue;
            }
            if (state.Style == Style.ThermalRing)
            {
                float angle = t * Mathf.PI * 2f;
                float maximumRadius = Vector3.Distance(state.Start, state.End);
                float radius = maximumRadius * (1f - (1f - progress) * (1f - progress));
                line.SetPosition(i, state.Start + new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius));
                continue;
            }
            if (state.Style == Style.WaterDrop)
            {
                float angle = t * Mathf.PI * 2f;
                float scale = Mathf.Sin(progress * Mathf.PI) *
                    Vector3.Distance(state.Start, state.End) * 0.34f;
                Vector3 center = state.Start + Vector3.up * 0.03f;
                float x = Mathf.Sin(angle) * scale * 0.78f;
                float y = -Mathf.Cos(angle) * scale;
                if (y > 0f)
                    x *= Mathf.Lerp(1f, 0.28f, y / Mathf.Max(scale, 0.001f));
                line.SetPosition(i, center + new Vector3(x, y));
                continue;
            }
            Vector3 point = Vector3.Lerp(state.Start, state.End, t);
            float envelope = Mathf.Sin(t * Mathf.PI);
            switch (state.Style)
            {
                case Style.Lightning:
                    float noise = Mathf.PerlinNoise(
                        state.Seed + i * 1.73f, now * 24f) * 2f - 1f;
                    point += perpendicular * noise * 0.48f * envelope;
                    break;
                case Style.Water:
                    point += perpendicular * Mathf.Sin(
                        t * Mathf.PI * 3f - now * 15f + state.Seed)
                        * 0.15f * envelope;
                    point += Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.14f;
                    break;
            }
            line.SetPosition(i, point);
        }
        Color startColor = state.Color;
        startColor.a *= fade;
        Color endColor = state.Color;
        bool isClosedShape = state.Style == Style.WaterDrop ||
            state.Style == Style.LightningIcon ||
            state.Style == Style.IceSnowflake ||
            state.Style == Style.FireIcon ||
            state.Style == Style.ThermalRing;
        endColor.a *= fade * (isClosedShape ? 1f : 0.12f);
        line.startColor = startColor;
        line.endColor = endColor;
        float pulse = state.Style == Style.Lightning
            ? 0.75f + Mathf.PingPong(now * 18f, 0.85f) : 1f;
        line.startWidth = state.Width * fade * pulse;
        line.endWidth = state.Width * fade * (isClosedShape ? 1f : 0.25f);
    }

    private static Vector3 Direction(int index, int count)
    {
        float angle = index * Mathf.PI * 2f / count;
        return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    private static Style ResolveStyle(ElementType element)
    {
        switch (element)
        {
            case ElementType.Water: return Style.Water;
            case ElementType.Electric: return Style.Lightning;
            default: return Style.Water;
        }
    }

    private static Color ResolveColor(ElementType element)
    {
        switch (element)
        {
            case ElementType.Water: return new Color(0f, 0.72f, 1f, 1f);
            case ElementType.Electric: return new Color(1f, 0.93f, 0f, 1f);
            case ElementType.Ice: return new Color(0f, 0.9f, 1f, 1f);
            case ElementType.Fire: return new Color(1f, 0f, 0f, 1f);
            default: return Color.white;
        }
    }
}
