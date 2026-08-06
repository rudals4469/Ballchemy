using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RetreatCostDiscountState :
    MonoBehaviour
{
    [Header("Runtime State")]

    [Tooltip(
        "현재 런에 적용 중인 후퇴 비용 할인율입니다.\n" +
        "0.5는 후퇴 비용 50% 할인을 의미합니다."
    )]
    [SerializeField, Range(0f, 1f)]
    private float discountRatio;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog = true;

    public float DiscountRatio =>
        discountRatio;

    public bool HasDiscount =>
        discountRatio > 0f;

    public event Action<float>
        DiscountRatioChanged;

    private void Awake()
    {
        /*
         * 플레이 시작 시 이전 Inspector 런타임 값이
         * 남아 있지 않도록 초기화합니다.
         *
         * 현재 구조에서는 이 컴포넌트의 생명주기를
         * 한 번의 런과 동일하게 사용합니다.
         */
        ResetForNewRun();
    }

    private void OnValidate()
    {
        discountRatio =
            Mathf.Clamp01(
                discountRatio
            );
    }

    public bool TryApplyDiscount(
        float newDiscountRatio)
    {
        newDiscountRatio =
            Mathf.Clamp01(
                newDiscountRatio
            );

        if (newDiscountRatio <= 0f)
        {
            return false;
        }

        /*
         * 후퇴 허가증은 현재 한 런에서
         * 한 번만 적용되는 상품으로 처리합니다.
         */
        if (HasDiscount)
        {
            return false;
        }

        discountRatio =
            newDiscountRatio;

        DiscountRatioChanged?.Invoke(
            discountRatio
        );

        if (showDebugLog)
        {
            Debug.Log(
                "RetreatCostDiscountState: " +
                "후퇴 비용 할인 적용. " +
                $"할인율={discountRatio:P0}",
                this
            );
        }

        return true;
    }

    public bool TryRemoveDiscount(
        float expectedDiscountRatio)
    {
        if (!HasDiscount)
        {
            return true;
        }

        expectedDiscountRatio =
            Mathf.Clamp01(
                expectedDiscountRatio
            );

        if (!Mathf.Approximately(
                discountRatio,
                expectedDiscountRatio
            ))
        {
            return false;
        }

        discountRatio =
            0f;

        DiscountRatioChanged?.Invoke(
            discountRatio
        );

        if (showDebugLog)
        {
            Debug.Log(
                "RetreatCostDiscountState: " +
                "후퇴 비용 할인 롤백.",
                this
            );
        }

        return true;
    }

    public void ResetForNewRun()
    {
        bool hadDiscount =
            HasDiscount;

        discountRatio =
            0f;

        if (hadDiscount)
        {
            DiscountRatioChanged?.Invoke(
                discountRatio
            );
        }

        if (showDebugLog)
        {
            Debug.Log(
                "RetreatCostDiscountState: " +
                "새 런 상태로 초기화.",
                this
            );
        }
    }
}