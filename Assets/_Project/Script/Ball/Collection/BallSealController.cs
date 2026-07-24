using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BallSealController :
    MonoBehaviour
{
    [Header("Seal Limit")]
    [SerializeField, Range(0f, 0.95f)]
    private float maximumSealRatio = 0.8f;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog = true;

    private int pendingSealedBallCount;

    public bool HasPendingSeal =>
        pendingSealedBallCount > 0;

    public int PendingSealedBallCount =>
        pendingSealedBallCount;

    public event Action<int, int>
        SealApplied;

    public event Action<int>
        SealConsumed;

    private void OnValidate()
    {
        maximumSealRatio =
            Mathf.Clamp(
                maximumSealRatio,
                0f,
                0.95f
            );
    }

    public int ApplySealRatio(
        float sealRatio,
        int totalBallCount)
    {
        totalBallCount =
            Mathf.Max(
                totalBallCount,
                0
            );

        if (totalBallCount <= 1)
        {
            return 0;
        }

        sealRatio =
            Mathf.Clamp(
                sealRatio,
                0f,
                maximumSealRatio
            );

        if (sealRatio <= 0f)
        {
            return 0;
        }

        int requestedSealedCount =
            Mathf.CeilToInt(
                totalBallCount *
                sealRatio
            );

        requestedSealedCount =
            Mathf.Clamp(
                requestedSealedCount,
                1,
                totalBallCount - 1
            );

        /*
         * 여러 봉인이 동시에 적용됐을 경우
         * 합산하지 않고 가장 강한 봉인만 유지한다.
         */
        pendingSealedBallCount =
            Mathf.Max(
                pendingSealedBallCount,
                requestedSealedCount
            );

        int launchableBallCount =
            GetLaunchableBallCount(
                totalBallCount
            );

        SealApplied?.Invoke(
            pendingSealedBallCount,
            launchableBallCount
        );

        if (showDebugLog)
        {
            Debug.Log(
                "BallSealController: " +
                $"공 {pendingSealedBallCount}개 봉인, " +
                $"다음 공격 발사 가능 " +
                $"{launchableBallCount}/{totalBallCount}",
                this
            );
        }

        return pendingSealedBallCount;
    }

    public int GetLaunchableBallCount(
        int totalBallCount)
    {
        totalBallCount =
            Mathf.Max(
                totalBallCount,
                0
            );

        if (totalBallCount == 0)
        {
            return 0;
        }

        int sealedBallCount =
            Mathf.Clamp(
                pendingSealedBallCount,
                0,
                Mathf.Max(
                    totalBallCount - 1,
                    0
                )
            );

        return Mathf.Max(
            totalBallCount -
            sealedBallCount,
            1
        );
    }

    public int
        ConsumePendingSealAndGetLaunchableCount(
        int totalBallCount)
    {
        totalBallCount =
            Mathf.Max(
                totalBallCount,
                0
            );

        int launchableBallCount =
            GetLaunchableBallCount(
                totalBallCount
            );

        int consumedSealedBallCount =
            Mathf.Clamp(
                totalBallCount -
                launchableBallCount,
                0,
                totalBallCount
            );

        pendingSealedBallCount = 0;

        if (consumedSealedBallCount > 0)
        {
            SealConsumed?.Invoke(
                consumedSealedBallCount
            );

            if (showDebugLog)
            {
                Debug.Log(
                    "BallSealController: " +
                    $"봉인 {consumedSealedBallCount}개를 " +
                    "이번 공격에 적용하고 해제합니다.",
                    this
                );
            }
        }

        return launchableBallCount;
    }

    public void ClearPendingSeal()
    {
        if (pendingSealedBallCount <= 0)
        {
            return;
        }

        pendingSealedBallCount = 0;

        if (showDebugLog)
        {
            Debug.Log(
                "BallSealController: " +
                "대기 중인 봉인을 해제했습니다.",
                this
            );
        }
    }
}