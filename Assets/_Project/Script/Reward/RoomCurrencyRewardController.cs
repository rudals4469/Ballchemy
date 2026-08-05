using UnityEngine;

[DisallowMultipleComponent]
public sealed class RoomCurrencyRewardController :
    MonoBehaviour
{
    [Header("References")]

    [Tooltip(
        "전투방 최초 클리어 이벤트를 제공하는 " +
        "방 이동 상태 관리자입니다."
    )]
    [SerializeField]
    private StageRoomNavigator
        roomNavigator;

    [Tooltip(
        "현재 런의 골드를 보관하는 상태입니다."
    )]
    [SerializeField]
    private RunCurrencyState
        runCurrencyState;

    [Header("Gold Rewards")]

    [Tooltip(
        "일반 전투방 최초 클리어 시 지급할 골드입니다."
    )]
    [SerializeField, Min(0)]
    private int normalCombatGold =
        15;

    [Tooltip(
        "네임드 전투방 최초 클리어 시 지급할 골드입니다."
    )]
    [SerializeField, Min(0)]
    private int namedCombatGold =
        30;

    [Tooltip(
        "보스방 최초 클리어 시 지급할 골드입니다."
    )]
    [SerializeField, Min(0)]
    private int bossGold =
        50;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLog =
        true;

    private bool isSubscribed;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        normalCombatGold =
            Mathf.Max(
                normalCombatGold,
                0
            );

        namedCombatGold =
            Mathf.Max(
                namedCombatGold,
                0
            );

        bossGold =
            Mathf.Max(
                bossGold,
                0
            );
    }

    private void FindReferences()
    {
        if (roomNavigator == null)
        {
            roomNavigator =
                FindFirstObjectByType<
                    StageRoomNavigator
                >();
        }

        if (runCurrencyState == null)
        {
            runCurrencyState =
                FindFirstObjectByType<
                    RunCurrencyState
                >();
        }
    }

    private void ValidateReferences()
    {
        if (roomNavigator == null)
        {
            Debug.LogError(
                "RoomCurrencyRewardController: " +
                "StageRoomNavigator가 연결되지 않았습니다.",
                this
            );
        }

        if (runCurrencyState == null)
        {
            Debug.LogError(
                "RoomCurrencyRewardController: " +
                "RunCurrencyState가 연결되지 않았습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (isSubscribed ||
            roomNavigator == null)
        {
            return;
        }

        roomNavigator.CombatRoomCleared +=
            HandleCombatRoomCleared;

        isSubscribed =
            true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (roomNavigator != null)
        {
            roomNavigator.CombatRoomCleared -=
                HandleCombatRoomCleared;
        }

        isSubscribed =
            false;
    }

    private void HandleCombatRoomCleared(
        RoomNode clearedRoom)
    {
        if (clearedRoom == null ||
            runCurrencyState == null)
        {
            return;
        }

        int rewardAmount =
            ResolveGoldReward(
                clearedRoom.RoomType
            );

        if (rewardAmount <= 0)
        {
            return;
        }

        bool wasAdded =
            runCurrencyState.TryAddGold(
                rewardAmount
            );

        if (!wasAdded)
        {
            Debug.LogWarning(
                "RoomCurrencyRewardController: " +
                $"Room {clearedRoom.RoomId}의 " +
                "골드 지급에 실패했습니다.",
                this
            );

            return;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "RoomCurrencyRewardController: " +
                $"Room {clearedRoom.RoomId}, " +
                $"{clearedRoom.RoomType} 클리어, " +
                $"+{rewardAmount}G",
                this
            );
        }
    }

    private int ResolveGoldReward(
        RoomType roomType)
    {
        switch (roomType)
        {
            case RoomType.NormalCombat:
                return normalCombatGold;

            case RoomType.NamedCombat:
                return namedCombatGold;

            case RoomType.Boss:
                return bossGold;

            default:
                return 0;
        }
    }
}