using System;

public static class ElementVisualEvents
{
    public static event Action<ElementType, Block> Impact;
    public static event Action<ElementType, Block, Block> Travel;
    public static event Action<Block> ThermalShock;

    [UnityEngine.RuntimeInitializeOnLoadMethod(
        UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        Impact = null;
        Travel = null;
        ThermalShock = null;
    }

    public static void RaiseImpact(ElementType element, Block target) =>
        Impact?.Invoke(element, target);
    public static void RaiseTravel(ElementType element, Block source, Block target) =>
        Travel?.Invoke(element, source, target);
    public static void RaiseThermalShock(Block center) =>
        ThermalShock?.Invoke(center);
}
