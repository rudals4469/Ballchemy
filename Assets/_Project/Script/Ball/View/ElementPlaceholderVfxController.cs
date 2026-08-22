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
        public Color Color;
    }
    private enum SpriteEffectKind { Symbol, Chain, Ring }

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
    private Sprite thermalRingSprite;
    private int nextIndex;
    private int nextSymbolIndex;
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
        {
            GameObject item = new GameObject($"ElementVfx_{i}");
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
    }

    private void Update()
    {
        float now = Time.time;
        for (int i = 0; i < pool.Count; i++)
        {
            State state = states[i];
            if (state == null || !pool[i].enabled) continue;
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
            Color color = state.Color;
            color.a *= fade;
            symbolPool[i].color = color;
        }
    }

    private void HandleImpact(ElementType element, Block target)
    {
        if (target == null) return;
        Vector3 center = target.transform.position;
        Vector2 randomOffset = Random.insideUnitCircle * 0.26f;
        center += new Vector3(randomOffset.x, randomOffset.y, 0f);
        Color color = ResolveColor(element);
        switch (element)
        {
            case ElementType.Electric:
                PlaySymbol(center, element, color);
                break;
            case ElementType.Water:
                PlaySymbol(center, element, color);
                break;
            case ElementType.Ice:
                PlaySymbol(center, element, color);
                break;
            case ElementType.Fire:
                PlaySymbol(center, element, color);
                break;
        }
    }

    private void LoadSymbolSprites()
    {
        LoadSymbol(ElementType.Electric, "VFX/ElementSymbols/Icon_Element_Lightning");
        LoadSymbol(ElementType.Water, "VFX/ElementSymbols/Icon_Element_Water");
        LoadSymbol(ElementType.Ice, "VFX/ElementSymbols/Icon_Element_Ice");
        LoadSymbol(ElementType.Fire, "VFX/ElementSymbols/Icon_Element_Fire");
        lightningChainSprite = LoadSprite(
            "VFX/ElementSymbols/Vfx_Chain_Lightning");
        thermalRingSprite = LoadSprite(
            "VFX/ElementSymbols/Vfx_Thermal_Ring");
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
        const int count = 40;
        for (int i = 0; i < count; i++)
        {
            GameObject item = new GameObject($"ElementSymbol_{i}");
            item.transform.SetParent(transform, false);
            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 27;
            renderer.enabled = false;
            symbolPool.Add(renderer);
            symbolStates.Add(null);
        }
    }

    private void PlaySymbol(Vector3 position, ElementType element, Color color)
    {
        if (!symbolSprites.TryGetValue(element, out Sprite sprite)) return;
        WarmSymbolPool();
        int index = nextSymbolIndex++ % symbolPool.Count;
        SpriteRenderer renderer = symbolPool[index];
        renderer.sprite = sprite;
        renderer.transform.position = position;
        renderer.transform.rotation = Quaternion.Euler(
            0f, 0f, Random.Range(-8f, 8f));
        renderer.enabled = true;
        symbolStates[index] = new SymbolState
        {
            Kind = SpriteEffectKind.Symbol,
            Began = Time.time,
            Duration = impactDuration,
            Scale = Random.Range(0.5f, 0.62f),
            Color = color
        };
    }

    private void PlayChainSprite(Vector3 start, Vector3 end, Color color)
    {
        if (lightningChainSprite == null) return;
        Vector3 delta = end - start;
        float length = delta.magnitude;
        if (length <= 0.001f) return;
        int index = nextSymbolIndex++ % symbolPool.Count;
        SpriteRenderer renderer = symbolPool[index];
        renderer.sprite = lightningChainSprite;
        renderer.transform.position = (start + end) * 0.5f;
        renderer.transform.rotation = Quaternion.Euler(
            0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        renderer.transform.localScale = new Vector3(length, 0.72f, 1f);
        renderer.enabled = true;
        symbolStates[index] = new SymbolState
        {
            Kind = SpriteEffectKind.Chain,
            Began = Time.time,
            Duration = travelDuration,
            Scale = 0.72f,
            Color = color
        };
    }

    private void PlayRingSprite(Vector3 position, Color color)
    {
        if (thermalRingSprite == null) return;
        int index = nextSymbolIndex++ % symbolPool.Count;
        SpriteRenderer renderer = symbolPool[index];
        renderer.sprite = thermalRingSprite;
        renderer.transform.position = position;
        renderer.transform.rotation = Quaternion.identity;
        renderer.transform.localScale = Vector3.zero;
        renderer.enabled = true;
        symbolStates[index] = new SymbolState
        {
            Kind = SpriteEffectKind.Ring,
            Began = Time.time,
            Duration = 0.62f,
            Scale = 3.8f,
            Color = color
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
            color = new Color(1f, 0.96f, 0.72f, 1f);
        Style style = ResolveStyle(element);
        if (element == ElementType.Electric)
        {
            Vector3 direction = end - start;
            if (direction.sqrMagnitude > 0.001f)
            {
                direction.Normalize();
                start -= direction * 0.14f;
                end += direction * 0.14f;
            }
            PlayChainSprite(start, end, color);
            return;
        }
        Play(start, end, color, travelDuration, 0.78f, style);

        Vector3 delta = end - start;
        Vector3 offset = delta.sqrMagnitude > 0.001f
            ? Vector3.Cross(delta.normalized, Vector3.forward) * 0.055f
            : Vector3.zero;
        Color echoColor = new Color(color.r, color.g, color.b, 0.55f);
        Play(start + offset, end + offset, echoColor,
            travelDuration * 0.78f, 0.32f, style);
    }

    private void HandleThermalShock(Block center)
    {
        if (center == null) return;
        Vector3 position = center.transform.position;
        Color orange = new Color(1f, 0.72f, 0.18f, 1f);
        PlayRingSprite(position, orange);

    }

    private void Play(Vector3 start, Vector3 end, Color color,
        float duration, float widthMultiplier, Style style)
    {
        WarmPool();
        int index = nextIndex++ % pool.Count;
        states[index] = new State
        {
            Start = start, End = end, Color = color,
            Began = Time.time, Duration = duration,
            Width = lineWidth * widthMultiplier,
            Seed = Random.Range(0f, 1000f), Style = style
        };
        pool[index].enabled = true;
        Animate(pool[index], states[index], 0f, Time.time);
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
            case ElementType.Water: return new Color(0.18f, 0.72f, 1f, 1f);
            case ElementType.Electric: return new Color(1f, 0.92f, 0.16f, 1f);
            case ElementType.Ice: return new Color(0.58f, 0.95f, 1f, 1f);
            case ElementType.Fire: return new Color(1f, 0.28f, 0.035f, 1f);
            default: return Color.white;
        }
    }
}
