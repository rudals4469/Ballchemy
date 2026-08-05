using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RunCurrencyState :
    MonoBehaviour
{
    [Header("Currency")]

    [Tooltip(
        "현재 런에서 플레이어가 보유한 골드입니다."
    )]
    [SerializeField, Min(0)]
    private int currentGold;

    [Header("Run Start")]

    [Tooltip(
        "새 런 시작 시 보유할 골드입니다."
    )]
    [SerializeField, Min(0)]
    private int startingGold;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog = true;

    public int CurrentGold =>
        currentGold;

    public int StartingGold =>
        startingGold;

    public event Action<int>
        GoldChanged;

    public event Action<int>
        GoldAdded;

    public event Action<int>
        GoldSpent;

    public event Action<int>
        GoldRolledBack;

    private void Awake()
    {
        InitializeGold();
    }

    private void OnValidate()
    {
        currentGold =
            Mathf.Max(
                currentGold,
                0
            );

        startingGold =
            Mathf.Max(
                startingGold,
                0
            );
    }

    private void InitializeGold()
    {
        currentGold =
            Mathf.Max(
                startingGold,
                0
            );

        if (showDebugLog)
        {
            Debug.Log(
                "RunCurrencyState: " +
                $"골드 초기화 {currentGold}G",
                this
            );
        }
    }

    public bool CanAfford(
        int amount)
    {
        if (amount < 0)
        {
            return false;
        }

        return currentGold >=
               amount;
    }

    public bool TryAddGold(
        int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        currentGold +=
            amount;

        if (showDebugLog)
        {
            Debug.Log(
                "RunCurrencyState: " +
                $"골드 획득 +{amount}G, " +
                $"현재 {currentGold}G",
                this
            );
        }

        GoldAdded?.Invoke(
            amount
        );

        GoldChanged?.Invoke(
            currentGold
        );

        return true;
    }

    public bool TrySpendGold(
        int amount)
    {
        if (amount <= 0 ||
            !CanAfford(
                amount
            ))
        {
            return false;
        }

        currentGold -=
            amount;

        if (showDebugLog)
        {
            Debug.Log(
                "RunCurrencyState: " +
                $"골드 사용 -{amount}G, " +
                $"현재 {currentGold}G",
                this
            );
        }

        GoldSpent?.Invoke(
            amount
        );

        GoldChanged?.Invoke(
            currentGold
        );

        return true;
    }

    /*
     * 구매가 아니라 미확정 전투 수입을 되돌릴 때 사용합니다.
     *
     * GoldSpent는 발생시키지 않고,
     * GoldRolledBack과 GoldChanged만 발생시킵니다.
     */
    public bool TryRollbackGold(
        int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        if (currentGold < amount)
        {
            Debug.LogError(
                "RunCurrencyState: " +
                "회수할 골드가 현재 보유 골드보다 많습니다. " +
                $"현재={currentGold}G, " +
                $"회수 요청={amount}G",
                this
            );

            return false;
        }

        currentGold -=
            amount;

        if (showDebugLog)
        {
            Debug.Log(
                "RunCurrencyState: " +
                $"미확정 골드 회수 -{amount}G, " +
                $"현재 {currentGold}G",
                this
            );
        }

        GoldRolledBack?.Invoke(
            amount
        );

        GoldChanged?.Invoke(
            currentGold
        );

        return true;
    }

    public bool TrySetGold(
        int amount)
    {
        amount =
            Mathf.Max(
                amount,
                0
            );

        if (currentGold ==
            amount)
        {
            return false;
        }

        currentGold =
            amount;

        if (showDebugLog)
        {
            Debug.Log(
                "RunCurrencyState: " +
                $"골드 설정 {currentGold}G",
                this
            );
        }

        GoldChanged?.Invoke(
            currentGold
        );

        return true;
    }

    public void ResetRunCurrency()
    {
        currentGold =
            Mathf.Max(
                startingGold,
                0
            );

        if (showDebugLog)
        {
            Debug.Log(
                "RunCurrencyState: " +
                $"런 골드 재설정 {currentGold}G",
                this
            );
        }

        GoldChanged?.Invoke(
            currentGold
        );
    }
}