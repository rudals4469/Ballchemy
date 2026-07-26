using System;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(Ball))]
[RequireComponent(typeof(BallVisualView))]
public sealed class BallCombatController :
    MonoBehaviour
{
    [Header("Ball Data")]
    [Tooltip(
        "현재 공에 적용된 공 Definition입니다."
    )]
    [SerializeField]
    private BallDefinition definition;

    [Header("Fallback")]
    [Tooltip(
        "Definition이 연결되지 않았을 때만 " +
        "사용되는 임시 피해량입니다."
    )]
    [FormerlySerializedAs("damage")]
    [SerializeField, Min(1)]
    private int fallbackDamage = 1;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog;

    private Ball ball;

    private BallTraitEffect
        activeTraitEffect;

    public BallDefinition Definition =>
        definition;

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

    public int BaseDamage =>
        definition != null
            ? Mathf.Max(
                definition.BaseDamage,
                1
            )
            : fallbackDamage;

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
        fallbackDamage =
            Mathf.Max(
                fallbackDamage,
                1
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
            $"Damage={BaseDamage}",
            this
        );
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
                "기본 피해를 직접 적용합니다.",
                this
            );

            hitBlock.TakeDamage(
                BaseDamage
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
                BaseDamage
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

            /*
             * 폭발, 속성, 관통 공은
             * 해당 기능을 구현할 때 추가한다.
             */

            default:
                if (showDebugLog)
                {
                    Debug.LogWarning(
                        "BallCombatController: " +
                        $"{traitType} 효과가 아직 구현되지 않아 " +
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