using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RoomClearHealingState :
    MonoBehaviour
{
    [Header("Runtime State")]

    [Tooltip(
        "일반 전투방 또는 네임드방을 클리어했을 때 " +
        "회복할 체력입니다.\n" +
        "0이면 효과가 비활성 상태입니다."
    )]
    [SerializeField, Min(0)]
    private int healingAmountPerCombatRoomClear;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog =
        true;

    public int HealingAmountPerCombatRoomClear =>
        healingAmountPerCombatRoomClear;

    public bool IsActive =>
        healingAmountPerCombatRoomClear > 0;

    public event Action<int>
        HealingAmountChanged;

    private void Awake()
    {
        ResetForNewRun();
    }

    private void OnValidate()
    {
        healingAmountPerCombatRoomClear =
            Mathf.Max(
                healingAmountPerCombatRoomClear,
                0
            );
    }

    public bool TryActivate(
        int healingAmount)
    {
        healingAmount =
            Mathf.Max(
                healingAmount,
                0
            );

        if (healingAmount <= 0)
        {
            return false;
        }

        /*
         * 생존자의 문장은 현재 한 런에
         * 한 번만 적용되는 효과입니다.
         */
        if (IsActive)
        {
            return false;
        }

        healingAmountPerCombatRoomClear =
            healingAmount;

        HealingAmountChanged?.Invoke(
            healingAmountPerCombatRoomClear
        );

        if (showDebugLog)
        {
            Debug.Log(
                "RoomClearHealingState: " +
                "전투방 클리어 회복 효과 활성화. " +
                $"회복량={healingAmountPerCombatRoomClear}",
                this
            );
        }

        return true;
    }

    public bool TryDeactivate(
        int expectedHealingAmount)
    {
        if (!IsActive)
        {
            return true;
        }

        expectedHealingAmount =
            Mathf.Max(
                expectedHealingAmount,
                0
            );

        if (healingAmountPerCombatRoomClear !=
            expectedHealingAmount)
        {
            return false;
        }

        healingAmountPerCombatRoomClear =
            0;

        HealingAmountChanged?.Invoke(
            healingAmountPerCombatRoomClear
        );

        if (showDebugLog)
        {
            Debug.Log(
                "RoomClearHealingState: " +
                "전투방 클리어 회복 효과 롤백.",
                this
            );
        }

        return true;
    }

    public void ResetForNewRun()
    {
        bool hadEffect =
            IsActive;

        healingAmountPerCombatRoomClear =
            0;

        if (hadEffect)
        {
            HealingAmountChanged?.Invoke(
                healingAmountPerCombatRoomClear
            );
        }

        if (showDebugLog)
        {
            Debug.Log(
                "RoomClearHealingState: " +
                "새 런 상태로 초기화.",
                this
            );
        }
    }
}