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

    [Header("Frozen")]
    [SerializeField]
    private bool isFrozen;

    [Header("Burn Runtime Data")]
    [Tooltip(
        "현재 화상 피해 계산에 사용하는 " +
        "가장 강한 불 공의 직접 피해입니다."
    )]
    [SerializeField, Min(0)]
    private int burnSourceDirectDamage;

    private Block block;

    public int MaximumStack =>
        maximumStack;

    public int WetStack =>
        wetStack;

    public int ChargeStack =>
        chargeStack;

    public int BurnStack =>
        burnStack;

    /*
     * 동결 상태에서는 기존 Surface View가
     * 강한 얼음 효과를 표시할 수 있도록
     * FrostStack을 최대치로 반환합니다.
     *
     * 실제 반응 계산에서는 StoredFrostStack을
     * 사용해야 합니다.
     */
    public int FrostStack =>
        isFrozen
            ? maximumStack
            : frostStack;

    public int StoredFrostStack =>
        frostStack;

    public int BurnSourceDirectDamage =>
        burnSourceDirectDamage;

    public bool IsFrozen =>
        isFrozen;

    public bool HasWet =>
        wetStack > 0;

    public bool HasCharge =>
        chargeStack > 0;

    public bool HasBurn =>
        burnStack > 0;

    public bool HasFrost =>
        !isFrozen &&
        frostStack > 0;

    public bool HasBorderStack =>
        wetStack > 0 ||
        chargeStack > 0;

    public bool HasSurfaceStack =>
        burnStack > 0 ||
        frostStack > 0 ||
        isFrozen;

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
            if (isFrozen)
            {
                return ElementType.Ice;
            }

            if (!HasSurfaceStack)
            {
                return null;
            }

            return burnStack >= frostStack
                ? ElementType.Fire
                : ElementType.Ice;
        }
    }

    public int SurfaceStack
    {
        get
        {
            if (isFrozen)
            {
                return maximumStack;
            }

            return Mathf.Max(
                burnStack,
                frostStack
            );
        }
    }

    /*
     * 기존 물·번개 View와의 호환용 프로퍼티입니다.
     */
    public ElementType? DominantElement =>
        BorderElement;

    public int DominantStack =>
        BorderStack;

    public event Action<BlockElementStatus>
        StatusChanged;

    public event Action<BlockElementStatus>
        StatusCleared;

    public event Action<BlockElementStatus>
        FrozenApplied;

    public event Action<BlockElementStatus>
        FrozenConsumed;

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

        burnSourceDirectDamage =
            Mathf.Max(
                burnSourceDirectDamage,
                0
            );

        if (isFrozen)
        {
            frostStack = 0;
        }

        if (burnStack <= 0)
        {
            burnSourceDirectDamage = 0;
        }
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
        return AddBurn(
            amount,
            0
        );
    }

    public int AddBurn(
        int amount,
        int sourceDirectDamage)
    {
        int appliedAmount =
            AddStack(
                ref burnStack,
                amount
            );

        if (appliedAmount > 0)
        {
            RegisterBurnSourceDamage(
                sourceDirectDamage
            );
        }

        return appliedAmount;
    }

    public int AddFrost(
        int amount)
    {
        if (amount <= 0 ||
            isFrozen)
        {
            return 0;
        }

        int previousStack =
            frostStack;

        frostStack =
            Mathf.Clamp(
                frostStack + amount,
                0,
                maximumStack
            );

        int appliedAmount =
            frostStack -
            previousStack;

        if (appliedAmount <= 0)
        {
            return 0;
        }

        bool becameFrozen =
            TryConvertFrostToFrozen();

        NotifyStatusChanged();

        if (becameFrozen)
        {
            FrozenApplied?.Invoke(
                this
            );
        }

        return appliedAmount;
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
        int consumedAmount =
            ConsumeStack(
                ref burnStack,
                amount
            );

        if (burnStack <= 0)
        {
            burnSourceDirectDamage = 0;
        }

        return consumedAmount;
    }

    public int ConsumeFrost(
        int amount)
    {
        if (isFrozen)
        {
            return 0;
        }

        return ConsumeStack(
            ref frostStack,
            amount
        );
    }

    public void RegisterBurnSourceDamage(
        int sourceDirectDamage)
    {
        if (burnStack <= 0 ||
            sourceDirectDamage <= 0)
        {
            return;
        }

        burnSourceDirectDamage =
            Mathf.Max(
                burnSourceDirectDamage,
                sourceDirectDamage
            );
    }

    public bool ConsumeFrozen()
    {
        if (!isFrozen)
        {
            return false;
        }

        isFrozen = false;
        frostStack = 0;

        FrozenConsumed?.Invoke(
            this
        );

        NotifyStatusChanged();

        return true;
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

        /*
         * 이미 동결된 동안 추가 냉기는 쌓이지 않습니다.
         * 직접 타격 파쇄 기능이 붙으면 동결이 먼저 깨지고
         * 이후 냉기 적용 여부를 다시 처리합니다.
         */
        if (isFrozen)
        {
            resolvedFrostStack = 0;
        }

        bool shouldBecomeFrozen =
            !isFrozen &&
            resolvedFrostStack >=
            maximumStack;

        if (shouldBecomeFrozen)
        {
            resolvedFrostStack = 0;
        }

        bool hasChanged =
            wetStack !=
                resolvedWetStack ||
            chargeStack !=
                resolvedChargeStack ||
            burnStack !=
                resolvedBurnStack ||
            frostStack !=
                resolvedFrostStack ||
            shouldBecomeFrozen;

        if (!hasChanged)
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

        if (shouldBecomeFrozen)
        {
            isFrozen = true;
        }

        if (burnStack <= 0)
        {
            burnSourceDirectDamage = 0;
        }

        NotifyStatusChanged();

        if (shouldBecomeFrozen)
        {
            FrozenApplied?.Invoke(
                this
            );
        }
    }

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

    private bool TryConvertFrostToFrozen()
    {
        if (isFrozen ||
            frostStack <
            maximumStack)
        {
            return false;
        }

        frostStack = 0;
        isFrozen = true;

        return true;
    }

    public void Clear()
    {
        if (!HasAnyStack &&
            burnSourceDirectDamage <= 0)
        {
            return;
        }

        wetStack = 0;
        chargeStack = 0;
        burnStack = 0;
        frostStack = 0;

        isFrozen = false;

        burnSourceDirectDamage = 0;

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