using UnityEngine;

[DisallowMultipleComponent]
public sealed class ElementalBallEffect :
    BallTraitEffect
{
    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog;

    private static ElementReactionVfxSpawner
        cachedReactionVfxSpawner;

    private static bool
        hasWarnedMissingVfxSpawner;

    private ElementalBallTraitDefinition
        elementalDefinition;

    public override BallTraitType TraitType =>
        BallTraitType.Elemental;

    protected override void OnInitialized()
    {
        elementalDefinition =
            TraitDefinition as
                ElementalBallTraitDefinition;

        if (elementalDefinition == null)
        {
            Debug.LogWarning(
                "ElementalBallEffect: " +
                "ElementalBallTraitDefinition이 " +
                "연결되지 않았습니다. " +
                "직접 피해만 적용됩니다.",
                this
            );
        }
    }

    public override BallHitResult ResolveHit(
        BallHitContext context)
    {
        if (context == null ||
            context.Block == null ||
            !context.Block.IsAlive ||
            CombatController == null)
        {
            return BallHitResult.NotHandled();
        }

        Block targetBlock =
            context.Block;

        /*
         * 1. 직접 피해를 적용합니다.
         *
         * ApplyDamage의 반환값은 실제로 감소한 체력입니다.
         * 쉴드에 막히면 0이 반환됩니다.
         */
        int appliedDirectDamage =
            CombatController.ApplyDamage(
                targetBlock,
                context.DirectDamage,
                context.HitPoint
            );

        /*
         * 쉴드나 무적 효과로 체력이 감소하지 않았다면
         * 신규 속성 스택과 감전을 처리하지 않습니다.
         *
         * 기존에 있던 스택은 그대로 유지합니다.
         */
        if (appliedDirectDamage <= 0)
        {
            if (showDebugLog)
            {
                Debug.Log(
                    "ElementalBallEffect: " +
                    $"{targetBlock.name}의 직접 피해가 " +
                    "막혀 속성 스택을 적용하지 않습니다.",
                    this
                );
            }

            return BallHitResult
                .HandledWithBounce();
        }

        /*
         * 직접 피해로 블록이 파괴됐다면
         * 신규 스택을 적용하지 않습니다.
         */
        if (targetBlock == null ||
            !targetBlock.IsAlive)
        {
            return BallHitResult
                .HandledWithBounce();
        }

        if (elementalDefinition == null)
        {
            return BallHitResult
                .HandledWithBounce();
        }

        BlockElementStatus elementStatus =
            GetOrAddElementStatus(
                targetBlock
            );

        if (elementStatus == null)
        {
            return BallHitResult
                .HandledWithBounce();
        }

        BallStarGrade starGrade =
            context.Definition != null
                ? context.Definition.StarGrade
                : BallStarGrade.OneStar;

        int requestedStackAmount =
            elementalDefinition.GetStackAmount(
                starGrade
            );

        /*
         * 2. 속성 스택을 추가합니다.
         * 3. 젖음과 전하를 반응시킵니다.
         * 4. 반응 후 남은 최종 스택을 반영합니다.
         */
        ElementReactionResult reactionResult =
            ElementReactionResolver
                .ApplyElementAndResolve(
                    elementStatus,
                    elementalDefinition.ElementType,
                    requestedStackAmount,
                    context.DirectDamage,
                    elementalDefinition
                        .ElectrocutionDamageMultiplier
                );

        if (showDebugLog)
        {
            Debug.Log(
                "ElementalBallEffect: " +
                $"{targetBlock.name}, " +
                $"속성={elementalDefinition.ElementType}, " +
                $"적용 스택={reactionResult.AppliedElementStack}, " +
                $"감전 횟수={reactionResult.ReactionCount}, " +
                $"남은 젖음={reactionResult.WetStackAfter}, " +
                $"남은 전하={reactionResult.ChargeStackAfter}",
                this
            );
        }

        if (!reactionResult.HasReaction)
        {
            return BallHitResult
                .HandledWithBounce();
        }

        if (targetBlock == null ||
            !targetBlock.IsAlive)
        {
            return BallHitResult
                .HandledWithBounce();
        }

        /*
         * 감전 피해를 적용하기 전에 시각 정보를 복사합니다.
         *
         * 감전 피해로 블록이 파괴되더라도
         * VFX는 Spawner 아래에서 독립적으로 끝까지 재생됩니다.
         */
        PlayElectrocutionVfx(
            targetBlock,
            reactionResult.ReactionCount
        );

        /*
         * 감전 추가 피해는 직접 피해와 구분하여
         * Style_Damage_Electrocution으로 표시합니다.
         */
        CombatController.ApplyDamage(
            targetBlock,
            reactionResult.TotalDamage,
            targetBlock.transform.position,
            elementalDefinition
                .ElectrocutionDamageTextStyle
        );

        return BallHitResult
            .HandledWithBounce();
    }

    private void PlayElectrocutionVfx(
        Block targetBlock,
        int reactionCount)
    {
        if (targetBlock == null ||
            reactionCount <= 0)
        {
            return;
        }

        if (cachedReactionVfxSpawner == null)
        {
            cachedReactionVfxSpawner =
                Object.FindFirstObjectByType<
                    ElementReactionVfxSpawner
                >();
        }

        if (cachedReactionVfxSpawner != null)
        {
            cachedReactionVfxSpawner
                .PlayElectrocution(
                    targetBlock,
                    reactionCount
                );

            return;
        }

        if (hasWarnedMissingVfxSpawner)
        {
            return;
        }

        hasWarnedMissingVfxSpawner =
            true;

        if (showDebugLog)
        {
            Debug.LogWarning(
                "ElementalBallEffect: " +
                "Scene에서 ElementReactionVfxSpawner를 " +
                "찾지 못했습니다. 감전 계산과 피해는 " +
                "정상 처리되지만 VFX는 재생되지 않습니다.",
                this
            );
        }
    }

    private static BlockElementStatus
        GetOrAddElementStatus(
            Block targetBlock)
    {
        if (targetBlock == null)
        {
            return null;
        }

        BlockElementStatus status =
            targetBlock.GetComponent<
                BlockElementStatus
            >();

        if (status != null)
        {
            return status;
        }

        return targetBlock.gameObject
            .AddComponent<
                BlockElementStatus
            >();
    }
}