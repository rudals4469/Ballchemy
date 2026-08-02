using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Reward_BallUpgrade",
    menuName =
        "Ballchemy/Rewards/Ball Upgrade Reward"
)]
public sealed class BallUpgradeRewardDefinition :
    RewardDefinition
{
    [Header("Ball Upgrade Reward")]

    [Tooltip(
        "이 보상을 선택했을 때 무작위로 승급할 " +
        "최대 공 개수입니다."
    )]
    [SerializeField, Min(1)]
    private int upgradeCount = 3;

    private readonly List<Ball>
        upgradeCandidates =
            new List<Ball>();

    public override RewardType RewardType =>
        RewardType.Ball;

    public int UpgradeCount =>
        upgradeCount;

    public override bool CanApply(
        RewardApplyContext context)
    {
        if (context == null ||
            !context.HasBallCollection)
        {
            return false;
        }

        BallCollection ballCollection =
            context.BallCollection;

        if (!ballCollection.IsInitialized)
        {
            return false;
        }

        return HasUpgradeableBall(
            ballCollection
        );
    }

    public override bool Apply(
        RewardApplyContext context)
    {
        if (context == null ||
            !context.HasBallCollection)
        {
            Debug.LogWarning(
                $"BallUpgradeRewardDefinition: {name} 보상에 " +
                "BallCollection이 전달되지 않았습니다.",
                this
            );

            return false;
        }

        BallCollection ballCollection =
            context.BallCollection;

        CollectUpgradeCandidates(
            ballCollection
        );

        if (upgradeCandidates.Count == 0)
        {
            Debug.LogWarning(
                $"BallUpgradeRewardDefinition: {name} 보상을 " +
                "적용할 수 있는 승급 가능 공이 없습니다.",
                this
            );

            return false;
        }

        ShuffleCandidates();

        int targetUpgradeCount =
            Mathf.Min(
                upgradeCount,
                upgradeCandidates.Count
            );

        int upgradedCount = 0;

        for (int i = 0;
             i < targetUpgradeCount;
             i++)
        {
            Ball targetBall =
                upgradeCandidates[i];

            if (TryUpgradeBall(
                    targetBall
                ))
            {
                upgradedCount++;
            }
        }

        upgradeCandidates.Clear();

        if (upgradedCount <= 0)
        {
            Debug.LogWarning(
                $"BallUpgradeRewardDefinition: {name} 보상으로 " +
                "공을 승급하지 못했습니다.",
                this
            );

            return false;
        }

        Debug.Log(
            "BallUpgradeRewardDefinition: " +
            $"무작위 공 {upgradedCount}개 승급 완료",
            this
        );

        return true;
    }

    private bool HasUpgradeableBall(
        BallCollection ballCollection)
    {
        if (ballCollection == null)
        {
            return false;
        }

        IReadOnlyList<Ball> balls =
            ballCollection.Balls;

        if (balls == null)
        {
            return false;
        }

        for (int i = 0;
             i < balls.Count;
             i++)
        {
            if (IsUpgradeableBall(
                    balls[i]
                ))
            {
                return true;
            }
        }

        return false;
    }

    private void CollectUpgradeCandidates(
        BallCollection ballCollection)
    {
        upgradeCandidates.Clear();

        if (ballCollection == null)
        {
            return;
        }

        IReadOnlyList<Ball> balls =
            ballCollection.Balls;

        if (balls == null)
        {
            return;
        }

        for (int i = 0;
             i < balls.Count;
             i++)
        {
            Ball ball =
                balls[i];

            if (!IsUpgradeableBall(
                    ball
                ))
            {
                continue;
            }

            upgradeCandidates.Add(
                ball
            );
        }
    }

    private static bool IsUpgradeableBall(
        Ball ball)
    {
        if (ball == null)
        {
            return false;
        }

        BallDefinition definition =
            ball.Definition;

        if (definition == null)
        {
            return false;
        }

        return
            definition.CanUpgrade &&
            definition.NextStarDefinition != null;
    }

    private static bool TryUpgradeBall(
        Ball ball)
    {
        if (!IsUpgradeableBall(
                ball
            ))
        {
            return false;
        }

        BallDefinition currentDefinition =
            ball.Definition;

        BallDefinition nextDefinition =
            currentDefinition
                .NextStarDefinition;

        BallCombatController combatController =
            ball.CombatController;

        if (combatController == null ||
            nextDefinition == null)
        {
            return false;
        }

        combatController.ApplyDefinition(
            nextDefinition
        );

        bool upgraded =
            ball.Definition ==
            nextDefinition;

        if (!upgraded)
        {
            return false;
        }

        Debug.Log(
            "BallUpgradeRewardDefinition: " +
            $"{ball.name} 승급, " +
            $"{currentDefinition.DisplayName} " +
            $"→ {nextDefinition.DisplayName}",
            ball
        );

        return true;
    }

    private void ShuffleCandidates()
    {
        for (int i =
                 upgradeCandidates.Count - 1;
             i > 0;
             i--)
        {
            int randomIndex =
                Random.Range(
                    0,
                    i + 1
                );

            Ball temporary =
                upgradeCandidates[i];

            upgradeCandidates[i] =
                upgradeCandidates[randomIndex];

            upgradeCandidates[randomIndex] =
                temporary;
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        upgradeCount =
            Mathf.Max(
                upgradeCount,
                1
            );
    }
}