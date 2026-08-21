using UnityEngine;

[DisallowMultipleComponent]
public sealed class ElementRuntimeParameters : MonoBehaviour
{
    private static ElementRuntimeParameters current;

    [Header("Stack Thresholds")]
    [SerializeField, Min(1)] private int wetMaximumStack = 5;
    [SerializeField, Min(1)] private int freezeThreshold = 5;

    [Header("Stack Amount By Grade")]
    [SerializeField, Min(1)] private int waterOneStarStack = 1;
    [SerializeField, Min(1)] private int waterTwoStarStack = 2;
    [SerializeField, Min(1)] private int waterThreeStarStack = 3;
    [SerializeField, Min(1)] private int iceOneStarStack = 1;
    [SerializeField, Min(1)] private int iceTwoStarStack = 2;
    [SerializeField, Min(1)] private int iceThreeStarStack = 3;

    [Header("Lightning")]
    [SerializeField, Min(0)] private int lightningAdditionalDamage = 1;
    [SerializeField, Min(0)] private int chainLightningDamage = 1;
    [SerializeField, Min(0)] private int chainLightningMaximumTargets = 4;

    [Header("Fire")]
    [SerializeField, Min(0)] private int fireDamagePerStack = 1;
    [SerializeField, Min(1)] private int fireSpreadStackAmount = 1;

    [Header("Thermal Shock")]
    [SerializeField, Min(0)] private int thermalShockCenterDamage = 8;
    [SerializeField, Min(0)] private int thermalShockNeighborDamage = 3;

    private int wetMaximumStackModifier;
    private int freezeThresholdModifier;
    private int waterStackModifier;
    private int iceStackModifier;
    private int lightningAdditionalDamageModifier;
    private int chainLightningDamageModifier;
    private int chainLightningTargetModifier;
    private int fireDamageModifier;
    private int fireSpreadModifier;
    private int thermalShockCenterModifier;
    private int thermalShockNeighborModifier;

    public static ElementRuntimeParameters Current
    {
        get
        {
            if (current == null)
                current = FindFirstObjectByType<ElementRuntimeParameters>();
            return current;
        }
    }

    public int CurrentWetMaxStack => Mathf.Max(1,
        wetMaximumStack + wetMaximumStackModifier);
    public int CurrentFreezeThreshold => Mathf.Max(1,
        freezeThreshold + freezeThresholdModifier);
    public int CurrentLightningAdditionalDamage => Mathf.Max(0,
        lightningAdditionalDamage + lightningAdditionalDamageModifier);
    public int CurrentChainLightningDamage => Mathf.Max(0,
        chainLightningDamage + chainLightningDamageModifier);
    public int CurrentChainLightningMaximumTargets => Mathf.Max(0,
        chainLightningMaximumTargets + chainLightningTargetModifier);
    public int CurrentFireDamagePerStack => Mathf.Max(0,
        fireDamagePerStack + fireDamageModifier);
    public int CurrentFireSpreadStackAmount => Mathf.Max(1,
        fireSpreadStackAmount + fireSpreadModifier);
    public int CurrentThermalShockCenterDamage => Mathf.Max(0,
        thermalShockCenterDamage + thermalShockCenterModifier);
    public int CurrentThermalShockNeighborDamage => Mathf.Max(0,
        thermalShockNeighborDamage + thermalShockNeighborModifier);

    public int GetWaterStackAmount(BallStarGrade grade) => Mathf.Max(1,
        ResolveGradeAmount(grade, waterOneStarStack,
            waterTwoStarStack, waterThreeStarStack) + waterStackModifier);
    public int GetIceStackAmount(BallStarGrade grade) => Mathf.Max(1,
        ResolveGradeAmount(grade, iceOneStarStack,
            iceTwoStarStack, iceThreeStarStack) + iceStackModifier);

    private void Awake() => current = this;

    private void OnDestroy()
    {
        if (current == this) current = null;
    }

    private void OnValidate()
    {
        wetMaximumStack = Mathf.Max(1, wetMaximumStack);
        freezeThreshold = Mathf.Max(1, freezeThreshold);
        waterOneStarStack = Mathf.Max(1, waterOneStarStack);
        waterTwoStarStack = Mathf.Max(1, waterTwoStarStack);
        waterThreeStarStack = Mathf.Max(1, waterThreeStarStack);
        iceOneStarStack = Mathf.Max(1, iceOneStarStack);
        iceTwoStarStack = Mathf.Max(1, iceTwoStarStack);
        iceThreeStarStack = Mathf.Max(1, iceThreeStarStack);
        lightningAdditionalDamage = Mathf.Max(0, lightningAdditionalDamage);
        chainLightningDamage = Mathf.Max(0, chainLightningDamage);
        chainLightningMaximumTargets = Mathf.Max(0, chainLightningMaximumTargets);
        fireDamagePerStack = Mathf.Max(0, fireDamagePerStack);
        fireSpreadStackAmount = Mathf.Max(1, fireSpreadStackAmount);
        thermalShockCenterDamage = Mathf.Max(0, thermalShockCenterDamage);
        thermalShockNeighborDamage = Mathf.Max(0, thermalShockNeighborDamage);
    }

    public void AddWetMaximumStackModifier(int amount) =>
        wetMaximumStackModifier += amount;
    public void AddFreezeThresholdModifier(int amount) =>
        freezeThresholdModifier += amount;
    public void AddWaterStackModifier(int amount) => waterStackModifier += amount;
    public void AddIceStackModifier(int amount) => iceStackModifier += amount;
    public void AddLightningAdditionalDamageModifier(int amount) =>
        lightningAdditionalDamageModifier += amount;
    public void AddChainLightningDamageModifier(int amount) =>
        chainLightningDamageModifier += amount;
    public void AddChainLightningTargetModifier(int amount) =>
        chainLightningTargetModifier += amount;
    public void AddFireDamageModifier(int amount) => fireDamageModifier += amount;
    public void AddFireSpreadModifier(int amount) => fireSpreadModifier += amount;
    public void AddThermalShockCenterModifier(int amount) =>
        thermalShockCenterModifier += amount;
    public void AddThermalShockNeighborModifier(int amount) =>
        thermalShockNeighborModifier += amount;

    private static int ResolveGradeAmount(
        BallStarGrade grade, int oneStar, int twoStar, int threeStar)
    {
        switch (grade)
        {
            case BallStarGrade.TwoStar: return twoStar;
            case BallStarGrade.ThreeStar: return threeStar;
            default: return oneStar;
        }
    }
}
