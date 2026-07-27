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

        int appliedDirectDamage =
            CombatController.ApplyDamage(
                targetBlock,
                context.DirectDamage,
                context.HitPoint
            );

        /*
         * 쉴드나 무적 효과로 체력이 감소하지 않으면
         * 신규 속성 스택과 반응은 발생하지 않습니다.
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

        ElementReactionResult reactionResult =
            ElementReactionResolver
                .ApplyElementAndResolve(
                    elementStatus,
                    elementalDefinition.ElementType,
                    requestedStackAmount,
                    context.DirectDamage,
                    elementalDefinition
                        .ElectrocutionDamageMultiplier,
                    elementalDefinition
                        .ThermalShockDamageMultiplier
                );

        if (showDebugLog)
        {
            Debug.Log(
                "ElementalBallEffect: " +
                $"{targetBlock.name}, " +
                $"속성={elementalDefinition.ElementType}, " +
                $"적용 스택={reactionResult.AppliedElementStack}, " +
                $"반응={reactionResult.ReactionKind}, " +
                $"반응 횟수={reactionResult.ReactionCount}, " +
                $"젖음={reactionResult.WetStackAfter}, " +
                $"전하={reactionResult.ChargeStackAfter}, " +
                $"화상={reactionResult.BurnStackAfter}, " +
                $"냉기={reactionResult.FrostStackAfter}",
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

        PlayReactionVfx(
            targetBlock,
            reactionResult
        );

        BallDamageTextStyleDefinition
            reactionDamageStyle =
                ResolveReactionDamageStyle(
                    reactionResult
                );

        CombatController.ApplyDamage(
            targetBlock,
            reactionResult.TotalDamage,
            targetBlock.transform.position,
            reactionDamageStyle
        );

        return BallHitResult
            .HandledWithBounce();
    }

    private BallDamageTextStyleDefinition
        ResolveReactionDamageStyle(
            ElementReactionResult reactionResult)
    {
        if (elementalDefinition == null)
        {
            return null;
        }

        switch (reactionResult.ReactionKind)
        {
            case ElementReactionKind
                .Electrocution:
            {
                return elementalDefinition
                    .ElectrocutionDamageTextStyle;
            }

            case ElementReactionKind
                .ThermalShock:
            {
                return elementalDefinition
                    .ThermalShockDamageTextStyle;
            }

            default:
                return null;
        }
    }

    private void PlayReactionVfx(
        Block targetBlock,
        ElementReactionResult reactionResult)
    {
        switch (reactionResult.ReactionKind)
        {
            case ElementReactionKind
                .Electrocution:
            {
                PlayElectrocutionVfx(
                    targetBlock,
                    reactionResult.ReactionCount
                );

                break;
            }

            case ElementReactionKind
                .ThermalShock:
            {
                /*
                 * 열충격 계산과 피해는 적용됩니다.
                 *
                 * 순간 VFX는 현재
                 * ElementReactionVfxSpawner의 전체 API를
                 * 확인한 뒤 다음 단계에서 연결합니다.
                 */
                break;
            }
        }
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

        if (status == null)
        {
            status =
                targetBlock.gameObject
                    .AddComponent<
                        BlockElementStatus
                    >();
        }

        EnsureStatusViews(
            targetBlock
        );

        return status;
    }

    private static void EnsureStatusViews(
        Block targetBlock)
    {
        if (targetBlock == null)
        {
            return;
        }

        BlockElementSurfaceStatusView
            surfaceStatusView =
                targetBlock.GetComponent<
                    BlockElementSurfaceStatusView
                >();

        if (surfaceStatusView == null)
        {
            targetBlock.gameObject
                .AddComponent<
                    BlockElementSurfaceStatusView
                >();
        }
    }
}