using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ElementPlaceholderVfxController : MonoBehaviour
{
    [SerializeField, Min(4)] private int poolSize = 32;
    [SerializeField, Min(0.01f)] private float travelDuration = 0.24f;
    [SerializeField, Min(0.01f)] private float impactDuration = 0.2f;
    [SerializeField, Min(0.001f)] private float lineWidth = 0.07f;
    [SerializeField, Min(0.05f)] private float impactRadius = 0.28f;

    private readonly List<LineRenderer> pool = new List<LineRenderer>();
    private readonly List<float> disableTimes = new List<float>();
    private int nextIndex;
    private Material sharedMaterial;

    private void Awake() => WarmPool();

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
            line.positionCount = 2;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.numCapVertices = 2;
            line.sortingOrder = 20;
            line.material = sharedMaterial;
            line.enabled = false;
            pool.Add(line);
            disableTimes.Add(0f);
        }
    }

    private void Update()
    {
        float now = Time.time;
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] != null && pool[i].enabled && now >= disableTimes[i])
                pool[i].enabled = false;
        }
    }

    private void HandleImpact(ElementType element, Block target)
    {
        if (target == null) return;
        Vector3 center = target.transform.position;
        Color color = ResolveColor(element);
        PlayLine(center + Vector3.left * impactRadius,
            center + Vector3.right * impactRadius,
            color, impactDuration, 1.8f);
        PlayLine(center + Vector3.down * impactRadius,
            center + Vector3.up * impactRadius,
            color, impactDuration, 1.8f);
    }

    private void HandleTravel(ElementType element, Block source, Block target)
    {
        if (source == null || target == null) return;
        Vector3 start = source.transform.position;
        Vector3 end = target.transform.position;
        Color color = ResolveColor(element);
        PlayLine(start, end, color, travelDuration, 1.65f);
        Vector3 direction = end - start;
        Vector3 offset = direction.sqrMagnitude > 0.001f
            ? Vector3.Cross(direction.normalized, Vector3.forward) * 0.055f
            : Vector3.zero;
        Color echo = new Color(color.r, color.g, color.b, 0.55f);
        PlayLine(start + offset, end + offset, echo, travelDuration * 0.8f, 0.75f);
    }

    private void HandleThermalShock(Block center)
    {
        if (center == null) return;
        Vector3 position = center.transform.position;
        Color color = new Color(1f, 0.75f, 0.3f, 1f);
        const float radius = 0.5f;
        PlayLine(position + new Vector3(-radius, -radius),
            position + new Vector3(radius, radius), color, 0.3f, 2.8f);
        PlayLine(position + new Vector3(-radius, radius),
            position + new Vector3(radius, -radius), color, 0.3f, 2.8f);
        PlayLine(position + Vector3.left * radius,
            position + Vector3.right * radius, color, 0.26f, 2.1f);
        PlayLine(position + Vector3.down * radius,
            position + Vector3.up * radius, color, 0.26f, 2.1f);
    }

    private void PlayLine(
        Vector3 start, Vector3 end, Color color,
        float duration, float widthMultiplier)
    {
        WarmPool();
        if (pool.Count == 0) return;
        int index = nextIndex++ % pool.Count;
        LineRenderer line = pool[index];
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startColor = color;
        line.endColor = new Color(color.r, color.g, color.b, 0.15f);
        line.startWidth = lineWidth * widthMultiplier;
        line.endWidth = lineWidth * widthMultiplier * 0.5f;
        line.enabled = true;
        disableTimes[index] = Time.time + duration;
    }

    private static Color ResolveColor(ElementType element)
    {
        switch (element)
        {
            case ElementType.Water: return new Color(0.25f, 0.7f, 1f, 1f);
            case ElementType.Electric: return new Color(1f, 0.9f, 0.2f, 1f);
            case ElementType.Ice: return new Color(0.65f, 0.95f, 1f, 1f);
            case ElementType.Fire: return new Color(1f, 0.32f, 0.08f, 1f);
            default: return Color.white;
        }
    }
}
