using TMPro;
using UnityEngine;

public sealed class EnemyAttackTurnUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BlockGridManager blockGridManager;

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
        Subscribe();
    }

    private void Start()
    {
        if (!isSubscribed)
        {
            FindReferences();
            Subscribe();
        }

        RefreshImmediately();
    }

    private void FindReferences()
    {
        if (blockGridManager == null)
        {
            blockGridManager =
                FindFirstObjectByType<BlockGridManager>();
        }

        if (turnText == null)
        {
            turnText =
                GetComponent<TMP_Text>();
        }
    }

    private void ValidateReferences()
    {
        if (blockGridManager == null)
        {
            Debug.LogError(
                "EnemyAttackTurnUI: " +
                "BlockGridManager가 연결되지 않았습니다.",
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

    private void Subscribe()
    {
        if (isSubscribed ||
            blockGridManager == null)
        {
            return;
        }

        blockGridManager
            .TurnsUntilAttackChanged +=
            HandleTurnsUntilAttackChanged;

        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed ||
            blockGridManager == null)
        {
            return;
        }

        blockGridManager
            .TurnsUntilAttackChanged -=
            HandleTurnsUntilAttackChanged;

        isSubscribed = false;
    }

    private void RefreshImmediately()
    {
        if (blockGridManager == null)
        {
            return;
        }

        UpdateTurnText(
            blockGridManager
                .TurnsUntilAttack
        );
    }

    private void HandleTurnsUntilAttackChanged(
        int remainingTurns)
    {
        UpdateTurnText(
            remainingTurns
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
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }
}