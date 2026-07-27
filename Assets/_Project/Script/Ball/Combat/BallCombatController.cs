using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Ball))]
[RequireComponent(typeof(BallVisualView))]
public sealed class BallCombatController : MonoBehaviour
{
    [Header("Ball Data")]
    [SerializeField]
    private BallDefinition definition;

    [Header("Runtime Stats")]
    [SerializeField]
    private BallRuntimeStats runtimeStats;

    [Header("Individual Ball Bonuses")]
    [SerializeField]
    private int individualDirectDamageBonus;

    [SerializeField, Min(0f)]
    private float
        individualCriticalDamageMultiplierBonus;

    [Header("Fallback")]
    [SerializeField, Min(1)]
    private int fallbackDirectDamage = 1;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog;

    private Ball ball;

    private BallTraitEffect
        activeTraitEffect;

    public BallDefinition Definition =>
        definition;

    public BallRuntimeStats RuntimeStats =>
        runtimeStats;

    public BallTraitDefinition TraitDefinition =>
        definition != null
            ? definition.TraitDefinition
            : null;

    public BallTraitType TraitType =>
        definition != null
            ? definition.TraitType
            : BallTraitType.Basic;

    public BallStarGrade StarGrade =>
        definition != null
            ? definition.StarGrade
            : BallStarGrade.None;

    public int DefinitionDirectDamageBonus =>
        definition != null
            ? definition.DirectDamageBonus
            : 0;

    public int IndividualDirectDamageBonus =>
        individualDirectDamageBonus;

    public float
        IndividualCriticalDamageMultiplierBonus =>
            individualCriticalDamageMultiplierBonus;

    public int DirectDamage
    {
        get
        {
            if (runtimeStats != null)
            {
                return runtimeStats
                    .CalculateDirectDamage(
                        DefinitionDirectDamageBonus,
                        individualDirectDamageBonus
                    );
            }

            int fallbackDamage =
                fallbackDirectDamage +
                DefinitionDirectDamageBonus +
                individualDirectDamageBonus;

            return Mathf.Max(
                fallbackDamage,
                1
            );
        }
    }

    public float CriticalDamageMultiplierBonus
    {
        get
        {
            float runBonus =
                runtimeStats != null
                    ? runtimeStats
                        .CriticalDamageMultiplierBonus
                    : 0f;

            return Mathf.Max(
                runBonus +
                individualCriticalDamageMultiplierBonus,
                0f
            );
        }
    }

    public int BaseDamage =>
        DirectDamage;

    public BallTraitEffect ActiveTraitEffect =>
        activeTraitEffect;

    public event Action<BallDefinition>
        DefinitionChanged;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
        ConfigureTraitEffect();
    }

    private void OnValidate()
    {
        NormalizeSettings();

        if (!Application.isPlaying)
        {
            return;
        }

        FindReferences();
        ConfigureTraitEffect();
    }

    private void FindReferences()
    {
        if (ball == null)
        {
            ball =
                GetComponent<Ball>();
        }
    }

    private void NormalizeSettings()
    {
        fallbackDirectDamage =
            Mathf.Max(
                fallbackDirectDamage,
                1
            );

        individualCriticalDamageMultiplierBonus =
            Mathf.Max(
                individualCriticalDamageMultiplierBonus,
                0f
            );
    }

    public void ApplyRuntimeStats(
        BallRuntimeStats newRuntimeStats)
    {
        runtimeStats =
            newRuntimeStats;

        if (!showDebugLog)
        {
            return;
        }

        string statsName =
            runtimeStats != null
                ? runtimeStats.name
                : "None";

        Debug.Log(
            "BallCombatController: " +
            $"{name}에 Runtime Stats " +
            $"{statsName} 연결",
            this
        );
    }

    public void ApplyDefinition(
        BallDefinition newDefinition)
    {
        definition =
            newDefinition;

        ConfigureTraitEffect();

        DefinitionChanged?.Invoke(
            definition
        );

        if (!showDebugLog)
        {
            return;
        }

        string definitionName =
            definition != null
                ? definition.DisplayName
                : "Fallback";

        Debug.Log(
            "BallCombatController: " +
            $"{name}에 {definitionName} 적용, " +
            $"Trait={TraitType}, " +
            $"DirectDamage={DirectDamage}",
            this
        );
    }

    public void AddIndividualDirectDamageBonus(
        int amount)
    {
        individualDirectDamageBonus +=
            amount;
    }

    public void SetIndividualDirectDamageBonus(
        int amount)
    {
        individualDirectDamageBonus =
            amount;
    }

    public void AddIndividualCriticalDamageMultiplierBonus(
        float amount)
    {
        individualCriticalDamageMultiplierBonus =
            Mathf.Max(
                individualCriticalDamageMultiplierBonus +
                amount,
                0f
            );
    }

    public void SetIndividualCriticalDamageMultiplierBonus(
        float amount)
    {
        individualCriticalDamageMultiplierBonus =
            Mathf.Max(
                amount,
                0f
            );
    }

    public int ApplyDamage(
        Block target,
        int calculatedDamage,
        Vector2 hitPoint,
        BallDamageTextStyleDefinition
            styleOverride = null)
    {
        if (target == null ||
            !target.IsAlive)
        {
            return 0;
        }

        calculatedDamage =
            Mathf.Max(
                calculatedDamage,
                1
            );

        int healthBeforeDamage =
            Mathf.Max(
                target.CurrentHealth,
                0
            );

        target.TakeDamage(
            calculatedDamage
        );

        int healthAfterDamage =
            target != null
                ? Mathf.Max(
                    target.CurrentHealth,
                    0
                )
                : 0;

        int appliedHealthDamage =
            Mathf.Clamp(
                healthBeforeDamage -
                healthAfterDamage,
                0,
                healthBeforeDamage
            );

        if (appliedHealthDamage <= 0)
        {
            return 0;
        }

        BallDamageTextStyleDefinition
            resolvedStyle =
                styleOverride != null
                    ? styleOverride
                    : definition != null
                        ? definition
                            .DamageTextStyle
                        : null;

        BallDamageEvents.Publish(
            new BallDamageEvent(
                ball,
                definition,
                target,
                calculatedDamage,
                appliedHealthDamage,
                hitPoint,
                resolvedStyle
            )
        );

        return appliedHealthDamage;
    }

    public BallHitResult ResolveBlockHit(
        Block hitBlock,
        Vector2 hitPoint,
        Vector2 incomingVelocity)
    {
        if (hitBlock == null ||
            !hitBlock.IsAlive)
        {
            return BallHitResult.NotHandled();
        }

        if (activeTraitEffect == null)
        {
            ConfigureTraitEffect();
        }

        if (activeTraitEffect == null)
        {
            Debug.LogWarning(
                "BallCombatController: " +
                "활성화된 공 특성 효과가 없어 " +
                "직접 피해만 적용합니다.",
                this
            );

            ApplyDamage(
                hitBlock,
                DirectDamage,
                hitPoint
            );

            return BallHitResult
                .HandledWithBounce();
        }

        BallHitContext context =
            new BallHitContext(
                ball,
                hitBlock,
                definition,
                hitPoint,
                incomingVelocity,
                DirectDamage,
                CriticalDamageMultiplierBonus
            );

        return activeTraitEffect.ResolveHit(
            context
        );
    }

    private void ConfigureTraitEffect()
    {
        FindReferences();

        Type requiredEffectType =
            GetRequiredEffectType(
                TraitType
            );

        BallTraitEffect[] existingEffects =
            GetComponents<BallTraitEffect>();

        activeTraitEffect = null;

        for (int i = 0;
             i < existingEffects.Length;
             i++)
        {
            BallTraitEffect effect =
                existingEffects[i];

            if (effect == null)
            {
                continue;
            }

            bool isRequiredEffect =
                effect.GetType() ==
                requiredEffectType;

            effect.enabled =
                isRequiredEffect;

            if (isRequiredEffect)
            {
                activeTraitEffect =
                    effect;
            }
        }

        if (activeTraitEffect == null)
        {
            activeTraitEffect =
                gameObject.AddComponent(
                    requiredEffectType
                ) as BallTraitEffect;
        }

        if (activeTraitEffect == null)
        {
            Debug.LogError(
                "BallCombatController: " +
                $"{requiredEffectType.Name}을 " +
                "생성하지 못했습니다.",
                this
            );

            return;
        }

        activeTraitEffect.enabled =
            true;

        activeTraitEffect.Initialize(
            ball,
            this,
            TraitDefinition
        );
    }

    private Type GetRequiredEffectType(
        BallTraitType traitType)
    {
        switch (traitType)
        {
            case BallTraitType.Basic:
                return typeof(
                    BasicBallEffect
                );

            case BallTraitType.Critical:
                return typeof(
                    CriticalBallEffect
                );

            case BallTraitType.Explosion:
                return typeof(
                    ExplosionBallEffect
                );

            case BallTraitType.Elemental:
                return typeof(
                    ElementalBallEffect
                );

            case BallTraitType.Piercing:
                return typeof(
                    PiercingBallEffect
                );

            default:
                if (showDebugLog)
                {
                    Debug.LogWarning(
                        "BallCombatController: " +
                        $"{traitType} 효과가 구현되지 않아 " +
                        "BasicBallEffect를 사용합니다.",
                        this
                    );
                }

                return typeof(
                    BasicBallEffect
                );
        }
    }
}