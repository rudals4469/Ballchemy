using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StageBuffAmplificationState :
    MonoBehaviour
{
    [Header("Runtime State")]

    [Tooltip(
        "상점에서 이후 구매하는 비율형 스테이지 버프의 " +
        "추가 증폭 비율입니다.\n" +
        "0.5는 기존 효과를 50% 추가 증폭합니다.\n" +
        "0이면 효과가 비활성 상태입니다."
    )]
    [SerializeField, Min(0f)]
    private float amplificationRatio;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog =
        true;

    public float AmplificationRatio =>
        amplificationRatio;

    public bool IsActive =>
        amplificationRatio > 0f;

    public event Action<float>
        AmplificationRatioChanged;

    private void Awake()
    {
        ResetForNewRun();
    }

    private void OnValidate()
    {
        amplificationRatio =
            Mathf.Max(
                amplificationRatio,
                0f
            );
    }

    public float ApplyAmplification(
        float baseRatio)
    {
        baseRatio =
            Mathf.Max(
                baseRatio,
                0f
            );

        if (baseRatio <= 0f ||
            !IsActive)
        {
            return baseRatio;
        }

        return baseRatio *
               (
                   1f +
                   amplificationRatio
               );
    }

    public bool TryActivate(
        float ratio)
    {
        ratio =
            Mathf.Max(
                ratio,
                0f
            );

        if (ratio <= 0f)
        {
            return false;
        }

        /*
         * 연금 촉매는 현재 한 런에
         * 한 번만 활성화되는 특수 효과입니다.
         */
        if (IsActive)
        {
            return false;
        }

        amplificationRatio =
            ratio;

        AmplificationRatioChanged?.Invoke(
            amplificationRatio
        );

        if (showDebugLog)
        {
            Debug.Log(
                "StageBuffAmplificationState: " +
                "스테이지 버프 증폭 효과 활성화. " +
                $"증폭률={amplificationRatio:P0}",
                this
            );
        }

        return true;
    }

    public bool TryDeactivate(
        float expectedRatio)
    {
        if (!IsActive)
        {
            return true;
        }

        expectedRatio =
            Mathf.Max(
                expectedRatio,
                0f
            );

        if (!Mathf.Approximately(
                amplificationRatio,
                expectedRatio
            ))
        {
            return false;
        }

        amplificationRatio =
            0f;

        AmplificationRatioChanged?.Invoke(
            amplificationRatio
        );

        if (showDebugLog)
        {
            Debug.Log(
                "StageBuffAmplificationState: " +
                "스테이지 버프 증폭 효과 롤백.",
                this
            );
        }

        return true;
    }

    public void ResetForNewRun()
    {
        bool hadEffect =
            IsActive;

        amplificationRatio =
            0f;

        if (hadEffect)
        {
            AmplificationRatioChanged?.Invoke(
                amplificationRatio
            );
        }

        if (showDebugLog)
        {
            Debug.Log(
                "StageBuffAmplificationState: " +
                "새 런 상태로 초기화.",
                this
            );
        }
    }
}