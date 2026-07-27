using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
public sealed class BlockElementStatus : MonoBehaviour
{
    [Header("Stack Settings")]
    [SerializeField, Min(1)]
    private int maximumStack = 6;

    [Header("Border Channel")]
    [SerializeField, Min(0)]
    private int wetStack;

    [SerializeField, Min(0)]
    private int chargeStack;

    [Header("Surface Channel")]
    [SerializeField, Min(0)]
    private int burnStack;

    [SerializeField, Min(0)]
    private int frostStack;

    private Block block;

    public int MaximumStack =>
        maximumStack;

    public int WetStack =>
        wetStack;

    public int ChargeStack =>
        chargeStack;

    public int BurnStack =>
        burnStack;

    public int FrostStack =>
        frostStack;

    public bool HasWet =>
        wetStack > 0;

    public bool HasCharge =>
        chargeStack > 0;

    public bool HasBurn =>
        burnStack > 0;

    public bool HasFrost =>
        frostStack > 0;

    public bool HasBorderStack =>
        wetStack > 0 ||
        chargeStack > 0;

    public bool HasSurfaceStack =>
        burnStack > 0 ||
        frostStack > 0;

    public bool HasAnyStack =>
        HasBorderStack ||
        HasSurfaceStack;

    public ElementType? BorderElement
    {
        get
        {
            if (!HasBorderStack)
            {
                return null;
            }

            return wetStack >= chargeStack
                ? ElementType.Water
                : ElementType.Electric;
        }
    }

    public int BorderStack =>
        Mathf.Max(
            wetStack,
            chargeStack
        );

    public ElementType? SurfaceElement
    {
        get
        {
            if (!HasSurfaceStack)
            {
                return null;
            }

            return burnStack >= frostStack
                ? ElementType.Fire
                : ElementType.Ice;
        }
    }

    public int SurfaceStack =>
        Mathf.Max(
            burnStack,
            frostStack
        );

    /*
     * 기존 코드와의 호환성을 위해 유지합니다.
     * 기존 Dominant 값은 물·전기 외곽 채널을 뜻합니다.
     */
    public ElementType? DominantElement =>
        BorderElement;

    public int DominantStack =>
        BorderStack;

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

        burnStack =
            Mathf.Clamp(
                burnStack,
                0,
                maximumStack
            );

        frostStack =
            Mathf.Clamp(
                frostStack,
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
        return AddStack(
            ref wetStack,
            amount
        );
    }

    public int AddCharge(
        int amount)
    {
        return AddStack(
            ref chargeStack,
            amount
        );
    }

    public int AddBurn(
        int amount)
    {
        return AddStack(
            ref burnStack,
            amount
        );
    }

    public int AddFrost(
        int amount)
    {
        return AddStack(
            ref frostStack,
            amount
        );
    }

    public int ConsumeWet(
        int amount)
    {
        return ConsumeStack(
            ref wetStack,
            amount
        );
    }

    public int ConsumeCharge(
        int amount)
    {
        return ConsumeStack(
            ref chargeStack,
            amount
        );
    }

    public int ConsumeBurn(
        int amount)
    {
        return ConsumeStack(
            ref burnStack,
            amount
        );
    }

    public int ConsumeFrost(
        int amount)
    {
        return ConsumeStack(
            ref frostStack,
            amount
        );
    }

    private int AddStack(
        ref int currentStack,
        int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        int previousStack =
            currentStack;

        currentStack =
            Mathf.Clamp(
                currentStack + amount,
                0,
                maximumStack
            );

        int appliedAmount =
            currentStack -
            previousStack;

        if (appliedAmount > 0)
        {
            NotifyStatusChanged();
        }

        return appliedAmount;
    }

    private int ConsumeStack(
        ref int currentStack,
        int amount)
    {
        if (amount <= 0 ||
            currentStack <= 0)
        {
            return 0;
        }

        int previousStack =
            currentStack;

        currentStack =
            Mathf.Max(
                currentStack - amount,
                0
            );

        int consumedAmount =
            previousStack -
            currentStack;

        if (consumedAmount > 0)
        {
            NotifyStatusChanged();
        }

        return consumedAmount;
    }

    /*
     * 모든 속성 추가와 반응 계산이 끝난 뒤
     * 최종 상태를 한 번만 시각 시스템에 전달합니다.
     */
    public void ApplyResolvedStacks(
        int resolvedWetStack,
        int resolvedChargeStack,
        int resolvedBurnStack,
        int resolvedFrostStack)
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

        resolvedBurnStack =
            Mathf.Clamp(
                resolvedBurnStack,
                0,
                maximumStack
            );

        resolvedFrostStack =
            Mathf.Clamp(
                resolvedFrostStack,
                0,
                maximumStack
            );

        if (wetStack ==
                resolvedWetStack &&
            chargeStack ==
                resolvedChargeStack &&
            burnStack ==
                resolvedBurnStack &&
            frostStack ==
                resolvedFrostStack)
        {
            return;
        }

        wetStack =
            resolvedWetStack;

        chargeStack =
            resolvedChargeStack;

        burnStack =
            resolvedBurnStack;

        frostStack =
            resolvedFrostStack;

        NotifyStatusChanged();
    }

    /*
     * 기존 물·전기 코드와의 호환용 오버로드입니다.
     */
    public void ApplyResolvedStacks(
        int resolvedWetStack,
        int resolvedChargeStack)
    {
        ApplyResolvedStacks(
            resolvedWetStack,
            resolvedChargeStack,
            burnStack,
            frostStack
        );
    }

    public void Clear()
    {
        if (!HasAnyStack)
        {
            return;
        }

        wetStack = 0;
        chargeStack = 0;
        burnStack = 0;
        frostStack = 0;

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