using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ExplosionBallEffect :
    BallTraitEffect
{
    private static BlockGridManager
        cachedBlockGridManager;

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

        /*
         * 직접 피해로 중심 블록이 파괴되기 전에
         * 주변 대상을 먼저 찾아둡니다.
         *
         * 이후 Block의 파괴 처리 방식이 바뀌어
         * Grid Position을 바로 지우더라도
         * 폭발 범위 계산이 깨지지 않습니다.
         */
        List<Block> explosionTargets =
            FindExplosionTargets(
                context
            );

        /*
         * 충돌한 중심 블록에는 공의 직접 피해가 들어갑니다.
         * BallDefinition에 연결된 기본 데미지 텍스트 스타일을
         * 사용합니다.
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

            /*
             * 주변 폭발 텍스트는 해당 대상 블록의 중심을
             * 타격 지점으로 사용합니다.
             *
             * 이후 DamageTextSpawner가 블록 중심 주변 슬롯과
             * 랜덤 위치를 추가로 적용합니다.
             */
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

    private List<Block> FindExplosionTargets(
        BallHitContext context)
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

        BallStarGrade starGrade =
            context.Definition != null
                ? context.Definition.StarGrade
                : BallStarGrade.OneStar;

        int explosionRange =
            explosionDefinition.GetRange(
                starGrade
            );

        return BlockNeighborhoodResolver
            .FindPatternBlocks(
                context.Block,
                blockGridManager.ActiveBlocks,
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

    private static bool
        CanDamageExplosionTarget(
            Block targetBlock)
    {
        if (targetBlock == null ||
            !targetBlock.IsAlive)
        {
            return false;
        }

        /*
         * 무적 블록과 TriggerOnly 블록은
         * 일반 폭발 피해 대상에서 제외합니다.
         */
        if (!targetBlock.IsBreakable)
        {
            return false;
        }

        return true;
    }
}