using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ExplosionBallEffect :
    BallTraitEffect
{
    private static BlockGridManager
        cachedBlockGridManager;

    private static ExplosionPatternVfxSpawner
        cachedPatternVfxSpawner;

    private ExplosionBallTraitDefinition
        explosionDefinition;

    public override BallTraitType TraitType =>
        BallTraitType.Explosion;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType
            .SubsystemRegistration
    )]
    private static void ResetStaticCache()
    {
        cachedBlockGridManager =
            null;

        cachedPatternVfxSpawner =
            null;
    }

    protected override void OnInitialized()
    {
        explosionDefinition =
            TraitDefinition as
                ExplosionBallTraitDefinition;

        if (explosionDefinition == null)
        {
            Debug.LogWarning(
                "ExplosionBallEffect: " +
                "ExplosionBallTraitDefinition이 " +
                "연결되지 않았습니다. " +
                "직접 피해만 적용됩니다.",
                this
            );
        }

        FindBlockGridManager();
        FindPatternVfxSpawner();
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

        int explosionRange =
            GetExplosionRange(
                context
            );

        List<Block> explosionTargets =
            FindExplosionTargets(
                context,
                explosionRange
            );

        /*
         * 데미지가 적용되는 순간 패턴 선을 함께 표시합니다.
         *
         * Cross         → + 모양
         * Diagonal      → X 모양
         * AllDirections → 8방향
         */
        PlayPatternVfx(
            context.Block,
            explosionRange
        );

        /*
         * 충돌한 중심 블록에는 직접 피해만 적용합니다.
         */
        CombatController.ApplyDamage(
            context.Block,
            context.DirectDamage,
            context.HitPoint
        );

        if (explosionDefinition == null ||
            explosionTargets == null ||
            explosionTargets.Count <= 0)
        {
            return BallHitResult
                .HandledWithBounce();
        }

        int explosionDamage =
            explosionDefinition
                .CalculateExplosionDamage(
                    context.DirectDamage
                );

        BallDamageTextStyleDefinition
            explosionTextStyle =
                explosionDefinition
                    .ExplosionDamageTextStyle;

        for (int i = 0;
             i < explosionTargets.Count;
             i++)
        {
            Block targetBlock =
                explosionTargets[i];

            if (!CanDamageExplosionTarget(
                    targetBlock))
            {
                continue;
            }

            Vector2 targetHitPoint =
                targetBlock.transform.position;

            CombatController.ApplyDamage(
                targetBlock,
                explosionDamage,
                targetHitPoint,
                explosionTextStyle
            );
        }

        return BallHitResult
            .HandledWithBounce();
    }

    private int GetExplosionRange(
        BallHitContext context)
    {
        if (explosionDefinition == null)
        {
            return 1;
        }

        BallStarGrade starGrade =
            context != null &&
            context.Definition != null
                ? context.Definition.StarGrade
                : BallStarGrade.OneStar;

        return explosionDefinition.GetRange(
            starGrade
        );
    }

    private List<Block> FindExplosionTargets(
        BallHitContext context,
        int explosionRange)
    {
        List<Block> emptyResult =
            new List<Block>();

        if (context == null ||
            context.Block == null ||
            explosionDefinition == null)
        {
            return emptyResult;
        }

        BlockGridManager blockGridManager =
            FindBlockGridManager();

        if (blockGridManager == null)
        {
            Debug.LogWarning(
                "ExplosionBallEffect: " +
                "BlockGridManager를 찾지 못해 " +
                "주변 폭발 피해를 적용하지 않습니다.",
                this
            );

            return emptyResult;
        }

        return BlockNeighborhoodResolver
            .FindPatternBlocks(
                context.Block,
                blockGridManager.ActiveBlocks,
                explosionDefinition.PatternType,
                explosionRange
            );
    }

    private void PlayPatternVfx(
        Block sourceBlock,
        int explosionRange)
    {
        if (sourceBlock == null ||
            explosionDefinition == null)
        {
            return;
        }

        ExplosionPatternVfxSpawner
            vfxSpawner =
                FindPatternVfxSpawner();

        if (vfxSpawner == null)
        {
            return;
        }

        vfxSpawner.Play(
            sourceBlock,
            explosionDefinition.PatternType,
            explosionRange
        );
    }

    private BlockGridManager
        FindBlockGridManager()
    {
        if (cachedBlockGridManager != null)
        {
            return cachedBlockGridManager;
        }

        cachedBlockGridManager =
            UnityEngine.Object
                .FindFirstObjectByType<
                    BlockGridManager
                >();

        return cachedBlockGridManager;
    }

    private ExplosionPatternVfxSpawner
        FindPatternVfxSpawner()
    {
        if (cachedPatternVfxSpawner != null)
        {
            return cachedPatternVfxSpawner;
        }

        cachedPatternVfxSpawner =
            UnityEngine.Object
                .FindFirstObjectByType<
                    ExplosionPatternVfxSpawner
                >();

        return cachedPatternVfxSpawner;
    }

    private static bool
        CanDamageExplosionTarget(
            Block targetBlock)
    {
        if (targetBlock == null ||
            !targetBlock.IsAlive)
        {
            return false;
        }

        if (!targetBlock.IsBreakable)
        {
            return false;
        }

        return true;
    }
}