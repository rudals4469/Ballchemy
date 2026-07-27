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

    private static BlockElementSystem
        cachedBlockElementSystem;

    private static bool
        hasWarnedMissingBlockElementSystem;

    private Ball ball;

    private BallTraitEffect
        activeTraitEffect;

    /*
     * 현재 ResolveBlockHit에서 처리 중인
     * 직접 충돌을 추적합니다.
     *
     * 첫 번째로 충돌 대상 블록에 적용된 피해만
     * 직접 피해로 인정합니다.
     *
     * 이후 같은 블록에 적용되는 감전, 열충격 등의
     * 추가 피해는 동결 파쇄를 다시 일으키지 않습니다.
     */
    private bool isResolvingDirectBlockHit;

    private Block resolvingDirectHitBlock;

    private bool resolvingBlockWasFrozen;

    private bool hasCapturedDirectDamage;

    public Ball SourceBall =>
        ball;

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

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType
            .SubsystemRegistration
    )]
    private static void ResetStaticCache()
    {
        cachedBlockElementSystem =
            null;

        hasWarnedMissingBlockElementSystem =
            false;
    }

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

        /*
         * 첫 직접 피해가 쉴드에 막혀 0이더라도
         * 해당 충돌의 직접 피해 판정은 이미 끝난 것입니다.
         *
         * 같은 충돌에서 나중에 발생하는 추가 피해를
         * 직접 피해로 오인하지 않도록 먼저 기록합니다.
         */
        bool shouldResolveFrozenShatter =
            TryCaptureDirectHitDamage(
                target,
                calculatedDamage,
                appliedHealthDamage
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

        /*
         * 직접 피해 숫자가 먼저 발행된 뒤
         * 파쇄 추가 피해를 별도로 적용합니다.
         */
        if (shouldResolveFrozenShatter)
        {
            ResolveFrozenShatter(
                target,
                calculatedDamage
            );
        }

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

        BeginDirectHitTracking(
            hitBlock
        );

        try
        {
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
        finally
        {
            EndDirectHitTracking();
        }
    }

    private void BeginDirectHitTracking(
        Block hitBlock)
    {
        isResolvingDirectBlockHit =
            true;

        resolvingDirectHitBlock =
            hitBlock;

        hasCapturedDirectDamage =
            false;

        BlockElementStatus status =
            hitBlock != null
                ? hitBlock.GetComponent<
                    BlockElementStatus
                >()
                : null;

        resolvingBlockWasFrozen =
            status != null &&
            status.IsFrozen;
    }

    private void EndDirectHitTracking()
    {
        isResolvingDirectBlockHit =
            false;

        resolvingDirectHitBlock =
            null;

        resolvingBlockWasFrozen =
            false;

        hasCapturedDirectDamage =
            false;
    }

    private bool TryCaptureDirectHitDamage(
        Block target,
        int calculatedDamage,
        int appliedHealthDamage)
    {
        if (!isResolvingDirectBlockHit ||
            hasCapturedDirectDamage ||
            target == null ||
            target != resolvingDirectHitBlock)
        {
            return false;
        }

        hasCapturedDirectDamage =
            true;

        /*
         * 충돌 시작 시점에 동결 상태가 아니었다면
         * 이번 직접 피해로 파쇄할 대상이 아닙니다.
         */
        if (!resolvingBlockWasFrozen)
        {
            return false;
        }

        /*
         * 쉴드, 무적, 파괴 불가 등의 이유로
         * 체력이 감소하지 않았다면 동결도 유지됩니다.
         */
        if (appliedHealthDamage <= 0)
        {
            if (showDebugLog)
            {
                Debug.Log(
                    "BallCombatController: " +
                    $"{target.name}의 직접 피해가 막혀 " +
                    "동결 파쇄를 실행하지 않습니다.",
                    target
                );
            }

            return false;
        }

        /*
         * 직접 피해 자체로 파괴된 경우에는
         * 추가 피해와 주변 전파를 실행하지 않습니다.
         */
        if (!target.IsAlive)
        {
            return false;
        }

        return calculatedDamage > 0;
    }

    private void ResolveFrozenShatter(
        Block targetBlock,
        int calculatedDirectDamage)
    {
        BlockElementSystem elementSystem =
            FindBlockElementSystem();

        if (elementSystem != null)
        {
            elementSystem.ResolveFrozenShatter(
                this,
                targetBlock,
                calculatedDirectDamage
            );

            return;
        }

        if (hasWarnedMissingBlockElementSystem)
        {
            return;
        }

        hasWarnedMissingBlockElementSystem =
            true;

        Debug.LogWarning(
            "BallCombatController: " +
            "BlockElementSystem을 찾지 못해 " +
            "동결 파쇄를 실행하지 않습니다.",
            this
        );
    }

    private BlockElementSystem
        FindBlockElementSystem()
    {
        if (cachedBlockElementSystem != null)
        {
            return cachedBlockElementSystem;
        }

        cachedBlockElementSystem =
            UnityEngine.Object
                .FindFirstObjectByType<
                    BlockElementSystem
                >();

        return cachedBlockElementSystem;
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