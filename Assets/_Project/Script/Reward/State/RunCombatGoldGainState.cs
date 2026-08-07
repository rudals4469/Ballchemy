using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RunCombatGoldGainState :
    MonoBehaviour
{
    [Header("Runtime State")]

    [Tooltip(
        "구매 이후 런이 끝날 때까지 " +
        "전투방 블록 골드에 적용되는 증가율입니다.\n" +
        "0.25는 25% 증가를 의미합니다.\n" +
        "0이면 효과가 비활성 상태입니다."
    )]
    [SerializeField, Min(0f)]
    private float goldGainIncreaseRatio;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog =
        true;

    public float GoldGainIncreaseRatio =>
        goldGainIncreaseRatio;

    public bool IsActive =>
        goldGainIncreaseRatio > 0f;

    public event Action<float>
        GoldGainIncreaseRatioChanged;

    private void Awake()
    {
        ResetForNewRun();
    }

    private void OnValidate()
    {
        goldGainIncreaseRatio =
            Mathf.Max(
                goldGainIncreaseRatio,
                0f
            );
    }

    public bool TryActivate(
        float increaseRatio)
    {
        increaseRatio =
            Mathf.Max(
                increaseRatio,
                0f
            );

        if (increaseRatio <= 0f)
        {
            return false;
        }

        /*
         * 현재 특수 상품은 한 런에
         * 한 번만 적용되는 구조입니다.
         */
        if (IsActive)
        {
            return false;
        }

        goldGainIncreaseRatio =
            increaseRatio;

        GoldGainIncreaseRatioChanged
            ?.Invoke(
                goldGainIncreaseRatio
            );

        if (showDebugLog)
        {
            Debug.Log(
                "RunCombatGoldGainState: " +
                "런 전체 전투방 골드 증가 효과 활성화. " +
                $"증가율={goldGainIncreaseRatio:P0}",
                this
            );
        }

        return true;
    }

    public bool TryDeactivate(
        float expectedIncreaseRatio)
    {
        if (!IsActive)
        {
            return true;
        }

        expectedIncreaseRatio =
            Mathf.Max(
                expectedIncreaseRatio,
                0f
            );

        if (!Mathf.Approximately(
                goldGainIncreaseRatio,
                expectedIncreaseRatio
            ))
        {
            return false;
        }

        goldGainIncreaseRatio =
            0f;

        GoldGainIncreaseRatioChanged
            ?.Invoke(
                goldGainIncreaseRatio
            );

        if (showDebugLog)
        {
            Debug.Log(
                "RunCombatGoldGainState: " +
                "런 전체 전투방 골드 증가 효과 롤백.",
                this
            );
        }

        return true;
    }

    public void ResetForNewRun()
    {
        bool hadEffect =
            IsActive;

        goldGainIncreaseRatio =
            0f;

        if (hadEffect)
        {
            GoldGainIncreaseRatioChanged
                ?.Invoke(
                    goldGainIncreaseRatio
                );
        }

        if (showDebugLog)
        {
            Debug.Log(
                "RunCombatGoldGainState: " +
                "새 런 상태로 초기화.",
                this
            );
        }
    }
}