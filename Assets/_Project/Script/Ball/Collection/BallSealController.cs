using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BallSealController :
    MonoBehaviour
{
    /*
     * 봉인은 특수 블록이 만료되는 적 공격 단계에서 적용된다.
     *
     * 적용 직후에도 새 웨이브가 한 번 생성되기 때문에,
     * 첫 번째 웨이브 생성 알림에서는 봉인을 유지하고
     * 두 번째 웨이브 생성 알림에서 봉인을 해제한다.
     */
    private const int
        InitialWaveGenerationStepsUntilClear = 2;

    [Header("Seal Limit")]
    [SerializeField, Range(0f, 0.95f)]
    private float maximumSealRatio = 0.8f;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog = true;

    private int pendingSealedBallCount;

    private int
        remainingWaveGenerationSteps;

    public bool HasPendingSeal =>
        pendingSealedBallCount > 0;

    public bool HasActiveSeal =>
        pendingSealedBallCount > 0;

    public int PendingSealedBallCount =>
        pendingSealedBallCount;

    public int ActiveSealedBallCount =>
        pendingSealedBallCount;

    public int
        RemainingWaveGenerationSteps =>
            remainingWaveGenerationSteps;

    public event Action<int, int>
        SealApplied;

    /*
     * 기존 BallCountView와의 호환을 위해
     * 이벤트 이름은 SealConsumed를 유지한다.
     *
     * 이제는 발사 순간이 아니라
     * 봉인 지속 시간이 끝나거나
     * 보스전에 진입할 때 발생한다.
     */
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
         * 여러 봉인 블록이 동시에 만료되면
         * 수치를 합산하지 않고 가장 강한 봉인만 적용한다.
         */
        pendingSealedBallCount =
            Mathf.Max(
                pendingSealedBallCount,
                requestedSealedCount
            );

        /*
         * 새 봉인이 적용되면 지속 시간을 갱신한다.
         *
         * 첫 번째 생성:
         * 봉인 적용 직후 생성되는 웨이브
         *
         * 두 번째 생성:
         * 다음 적 공격 주기에 생성되는 웨이브
         * 이 시점에 봉인 해제
         */
        remainingWaveGenerationSteps =
            InitialWaveGenerationStepsUntilClear;

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
                $"사용 가능 " +
                $"{launchableBallCount}/{totalBallCount}, " +
                "다음 블록 생성 주기까지 유지",
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

    /*
     * 기존 코드에서 이 함수를 호출하는 곳이 남아 있어도
     * 컴파일 오류가 발생하지 않도록 유지한다.
     *
     * 이제 이 함수는 봉인을 소비하지 않고
     * 현재 발사 가능한 개수만 반환한다.
     */
    public int
        ConsumePendingSealAndGetLaunchableCount(
        int totalBallCount)
    {
        return GetLaunchableBallCount(
            totalBallCount
        );
    }

    public void ClearEncounterSeal()
    {
        ClearSeal("보스방 종료");
    }

    /*
     * 실제 새 웨이브 생성이 완료됐을 때 호출한다.
     */
    public void NotifyWaveGenerated()
    {
        if (!HasPendingSeal)
        {
            return;
        }

        remainingWaveGenerationSteps =
            Mathf.Max(
                remainingWaveGenerationSteps - 1,
                0
            );

        if (remainingWaveGenerationSteps > 0)
        {
            if (showDebugLog)
            {
                Debug.Log(
                    "BallSealController: " +
                    "봉인 적용 직후 웨이브가 생성됐습니다. " +
                    "봉인을 다음 블록 생성 주기까지 유지합니다.",
                    this
                );
            }

            return;
        }

        ClearSeal(
            "다음 블록 생성 주기 도달"
        );
    }

    /*
     * 보스전 진입이나 외부 시스템에서
     * 봉인을 즉시 초기화할 때 사용한다.
     */
    public void ClearPendingSeal()
    {
        ClearSeal(
            "외부 초기화 요청"
        );
    }

    private void ClearSeal(
        string reason)
    {
        if (!HasPendingSeal)
        {
            remainingWaveGenerationSteps = 0;

            return;
        }

        int clearedSealedBallCount =
            pendingSealedBallCount;

        pendingSealedBallCount = 0;
        remainingWaveGenerationSteps = 0;

        /*
         * BallCountView가 이 이벤트를 받아
         * 전체 공 개수로 다시 표시한다.
         */
        SealConsumed?.Invoke(
            clearedSealedBallCount
        );

        if (showDebugLog)
        {
            Debug.Log(
                "BallSealController: " +
                $"봉인 {clearedSealedBallCount}개 해제, " +
                $"사유={reason}",
                this
            );
        }
    }
}
