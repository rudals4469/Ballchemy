using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
public sealed class BlockElementStatus : MonoBehaviour
{
    [Header("Stack Settings")]
    [SerializeField, Min(1)]
    private int maximumStack = 6;

    [Header("Runtime Status")]
    [SerializeField, Min(0)]
    private int wetStack;

    [SerializeField, Min(0)]
    private int chargeStack;

    private Block block;

    public int MaximumStack =>
        maximumStack;

    public int WetStack =>
        wetStack;

    public int ChargeStack =>
        chargeStack;

    public bool HasWet =>
        wetStack > 0;

    public bool HasCharge =>
        chargeStack > 0;

    public bool HasAnyStack =>
        wetStack > 0 ||
        chargeStack > 0;

    /*
     * 반응 계산이 끝난 뒤에는 젖음과 전하 중
     * 하나만 남거나 둘 다 0이 됩니다.
     */
    public ElementType? DominantElement
    {
        get
        {
            if (wetStack <= 0 &&
                chargeStack <= 0)
            {
                return null;
            }

            return wetStack >= chargeStack
                ? ElementType.Water
                : ElementType.Electric;
        }
    }

    public int DominantStack
    {
        get
        {
            if (wetStack <= 0 &&
                chargeStack <= 0)
            {
                return 0;
            }

            return Mathf.Max(
                wetStack,
                chargeStack
            );
        }
    }

    public event Action<BlockElementStatus>
        StatusChanged;

    public event Action<BlockElementStatus>
        StatusCleared;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeBlockEvents();
    }

    private void OnDisable()
    {
        /*
         * 오브젝트가 비활성화되거나 풀로 돌아가는 경우에도
         * 이전 블록의 속성 상태가 남지 않게 초기화합니다.
         */
        Clear();
        UnsubscribeBlockEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeBlockEvents();
    }

    private void OnValidate()
    {
        NormalizeSettings();
    }

    private void FindReferences()
    {
        if (block == null)
        {
            block =
                GetComponent<Block>();
        }
    }

    private void NormalizeSettings()
    {
        maximumStack =
            Mathf.Max(
                maximumStack,
                1
            );

        wetStack =
            Mathf.Clamp(
                wetStack,
                0,
                maximumStack
            );

        chargeStack =
            Mathf.Clamp(
                chargeStack,
                0,
                maximumStack
            );
    }

    private void SubscribeBlockEvents()
    {
        if (block == null)
        {
            return;
        }

        block.Destroyed -=
            HandleBlockDestroyed;

        block.Destroyed +=
            HandleBlockDestroyed;

        block.ExpiredWithoutReward -=
            HandleBlockExpired;

        block.ExpiredWithoutReward +=
            HandleBlockExpired;
    }

    private void UnsubscribeBlockEvents()
    {
        if (block == null)
        {
            return;
        }

        block.Destroyed -=
            HandleBlockDestroyed;

        block.ExpiredWithoutReward -=
            HandleBlockExpired;
    }

    private void HandleBlockDestroyed(
        Block destroyedBlock)
    {
        Clear();
    }

    private void HandleBlockExpired(
        Block expiredBlock)
    {
        Clear();
    }

    public int AddWet(
        int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        int previousStack =
            wetStack;

        wetStack =
            Mathf.Clamp(
                wetStack + amount,
                0,
                maximumStack
            );

        int appliedAmount =
            wetStack -
            previousStack;

        if (appliedAmount > 0)
        {
            NotifyStatusChanged();
        }

        return appliedAmount;
    }

    public int AddCharge(
        int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        int previousStack =
            chargeStack;

        chargeStack =
            Mathf.Clamp(
                chargeStack + amount,
                0,
                maximumStack
            );

        int appliedAmount =
            chargeStack -
            previousStack;

        if (appliedAmount > 0)
        {
            NotifyStatusChanged();
        }

        return appliedAmount;
    }

    public int ConsumeWet(
        int amount)
    {
        if (amount <= 0 ||
            wetStack <= 0)
        {
            return 0;
        }

        int previousStack =
            wetStack;

        wetStack =
            Mathf.Max(
                wetStack - amount,
                0
            );

        int consumedAmount =
            previousStack -
            wetStack;

        if (consumedAmount > 0)
        {
            NotifyStatusChanged();
        }

        return consumedAmount;
    }

    public int ConsumeCharge(
        int amount)
    {
        if (amount <= 0 ||
            chargeStack <= 0)
        {
            return 0;
        }

        int previousStack =
            chargeStack;

        chargeStack =
            Mathf.Max(
                chargeStack - amount,
                0
            );

        int consumedAmount =
            previousStack -
            chargeStack;

        if (consumedAmount > 0)
        {
            NotifyStatusChanged();
        }

        return consumedAmount;
    }

    /*
     * 속성 추가와 감전 소비를 계산한 뒤
     * 최종 결과를 한 번에 반영합니다.
     *
     * 나중에 테두리 연출을 추가했을 때
     * 파랑 → 노랑 → 파랑처럼 중간 상태가
     * 순간적으로 표시되는 것을 막기 위한 구조입니다.
     */
    public void ApplyResolvedStacks(
        int resolvedWetStack,
        int resolvedChargeStack)
    {
        resolvedWetStack =
            Mathf.Clamp(
                resolvedWetStack,
                0,
                maximumStack
            );

        resolvedChargeStack =
            Mathf.Clamp(
                resolvedChargeStack,
                0,
                maximumStack
            );

        if (wetStack ==
                resolvedWetStack &&
            chargeStack ==
                resolvedChargeStack)
        {
            return;
        }

        wetStack =
            resolvedWetStack;

        chargeStack =
            resolvedChargeStack;

        NotifyStatusChanged();
    }

    public void Clear()
    {
        if (wetStack <= 0 &&
            chargeStack <= 0)
        {
            return;
        }

        wetStack = 0;
        chargeStack = 0;

        StatusCleared?.Invoke(
            this
        );

        NotifyStatusChanged();
    }

    private void NotifyStatusChanged()
    {
        StatusChanged?.Invoke(
            this
        );
    }
}