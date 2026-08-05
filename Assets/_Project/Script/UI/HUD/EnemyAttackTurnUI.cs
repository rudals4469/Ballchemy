using TMPro;
using UnityEngine;

public sealed class EnemyAttackTurnUI :
    MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private StageRoomNavigator roomNavigator;

    [SerializeField]
    private BlockGridManager blockGridManager;

    [Tooltip(
        "적 공격 턴 UI 전체를 켜고 끌 표시용 루트입니다.\n" +
        "이 스크립트가 붙은 GameObject 자체가 아니라 " +
        "하위 표시 오브젝트를 연결하는 것을 권장합니다."
    )]
    [SerializeField]
    private GameObject uiRoot;

    [SerializeField]
    private TMP_Text turnText;

    [Header("Text")]

    [SerializeField]
    private string waitingTextFormat =
        "적 공격까지 {0}턴";

    [SerializeField]
    private string attackingText =
        "적 공격!";

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

    private void Start()
    {
        FindReferences();
        SubscribeEvents();

        RefreshVisibility();
        RefreshTurnText();
    }

    private void OnValidate()
    {
        FindReferences();
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

        if (blockGridManager == null)
        {
            blockGridManager =
                FindFirstObjectByType<
                    BlockGridManager
                >();
        }

        if (turnText == null)
        {
            turnText =
                GetComponentInChildren<
                    TMP_Text
                >(
                    true
                );
        }

        /*
         * 별도의 UI Root가 연결되지 않았다면
         * 텍스트 GameObject만 표시 대상으로 사용합니다.
         *
         * 스크립트가 붙은 자기 자신을 끄지 않기 위한 처리입니다.
         */
        if (uiRoot == null &&
            turnText != null &&
            turnText.gameObject != gameObject)
        {
            uiRoot =
                turnText.gameObject;
        }
    }

    private void ValidateReferences()
    {
        if (roomNavigator == null)
        {
            Debug.LogError(
                "EnemyAttackTurnUI: " +
                "StageRoomNavigator가 연결되지 않았습니다.",
                this
            );
        }

        if (blockGridManager == null)
        {
            Debug.LogError(
                "EnemyAttackTurnUI: " +
                "BlockGridManager가 연결되지 않았습니다.",
                this
            );
        }

        if (uiRoot == null)
        {
            Debug.LogError(
                "EnemyAttackTurnUI: " +
                "UI Root가 연결되지 않았습니다. " +
                "스크립트가 붙은 오브젝트와 별도의 " +
                "하위 표시 루트를 연결해 주세요.",
                this
            );
        }

        if (turnText == null)
        {
            Debug.LogError(
                "EnemyAttackTurnUI: " +
                "TMP Text가 연결되지 않았습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (isSubscribed)
        {
            return;
        }

        if (roomNavigator != null)
        {
            roomNavigator.RoomChanged -=
                HandleRoomChanged;

            roomNavigator.RoomChanged +=
                HandleRoomChanged;
        }

        if (blockGridManager != null)
        {
            blockGridManager
                .TurnsUntilAttackChanged -=
                HandleTurnsUntilAttackChanged;

            blockGridManager
                .TurnsUntilAttackChanged +=
                HandleTurnsUntilAttackChanged;

            blockGridManager
                .RoomStateChanged -=
                HandleRoomStateChanged;

            blockGridManager
                .RoomStateChanged +=
                HandleRoomStateChanged;
        }

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
            roomNavigator.RoomChanged -=
                HandleRoomChanged;
        }

        if (blockGridManager != null)
        {
            blockGridManager
                .TurnsUntilAttackChanged -=
                HandleTurnsUntilAttackChanged;

            blockGridManager
                .RoomStateChanged -=
                HandleRoomStateChanged;
        }

        isSubscribed =
            false;
    }

    private void HandleRoomChanged(
        RoomNode previousRoom,
        RoomNode currentRoom)
    {
        RefreshVisibility();
        RefreshTurnText();
    }

    private void HandleRoomStateChanged(
        RoomCombatState roomState)
    {
        RefreshVisibility();

        if (roomState ==
            RoomCombatState.InCombat)
        {
            RefreshTurnText();
        }
    }

    private void HandleTurnsUntilAttackChanged(
        int remainingTurns)
    {
        if (!ShouldShow())
        {
            return;
        }

        UpdateTurnText(
            remainingTurns
        );
    }

    private void RefreshVisibility()
    {
        if (uiRoot == null)
        {
            return;
        }

        uiRoot.SetActive(
            ShouldShow()
        );
    }

    private bool ShouldShow()
    {
        if (roomNavigator == null ||
            blockGridManager == null)
        {
            return false;
        }

        RoomNode currentRoom =
            roomNavigator.CurrentRoom;

        if (currentRoom == null)
        {
            return false;
        }

        bool isSupportedCombatRoom =
            currentRoom.RoomType ==
                RoomType.NormalCombat ||
            currentRoom.RoomType ==
                RoomType.NamedCombat;

        if (!isSupportedCombatRoom)
        {
            return false;
        }

        /*
         * 클리어한 일반·네임드방에 다시 들어왔을 때는
         * 공격 턴 UI를 표시하지 않습니다.
         */
        return blockGridManager.CurrentRoomState ==
               RoomCombatState.InCombat;
    }

    private void RefreshTurnText()
    {
        if (!ShouldShow() ||
            blockGridManager == null)
        {
            return;
        }

        UpdateTurnText(
            blockGridManager.TurnsUntilAttack
        );
    }

    private void UpdateTurnText(
        int remainingTurns)
    {
        if (turnText == null)
        {
            return;
        }

        if (remainingTurns <= 0)
        {
            turnText.text =
                attackingText;

            return;
        }

        turnText.text =
            string.Format(
                waitingTextFormat,
                remainingTurns
            );
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }
}