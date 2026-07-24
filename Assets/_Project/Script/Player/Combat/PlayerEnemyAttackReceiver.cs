using UnityEngine;

public sealed class PlayerEnemyAttackReceiver : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BlockGridManager blockGridManager;

    [SerializeField]
    private PlayerHealth playerHealth;

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
    }

    private void FindReferences()
    {
        if (blockGridManager == null)
        {
            blockGridManager =
                FindFirstObjectByType<BlockGridManager>();
        }

        if (playerHealth == null)
        {
            playerHealth =
                GetComponent<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            playerHealth =
                FindFirstObjectByType<PlayerHealth>();
        }
    }

    private void ValidateReferences()
    {
        if (blockGridManager == null)
        {
            Debug.LogError(
                "PlayerEnemyAttackReceiver: " +
                "BlockGridManager를 찾지 못했습니다.",
                this
            );
        }

        if (playerHealth == null)
        {
            Debug.LogError(
                "PlayerEnemyAttackReceiver: " +
                "PlayerHealth를 찾지 못했습니다.",
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

        blockGridManager.EnemyAttackTriggered +=
            HandleEnemyAttackTriggered;

        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed ||
            blockGridManager == null)
        {
            return;
        }

        blockGridManager.EnemyAttackTriggered -=
            HandleEnemyAttackTriggered;

        isSubscribed = false;
    }

    private void HandleEnemyAttackTriggered(
        Block attackingBlock,
        int damage)
    {
        if (playerHealth == null ||
            playerHealth.IsDead ||
            damage <= 0)
        {
            return;
        }

        string attackerName =
            attackingBlock != null
                ? attackingBlock.name
                : "Unknown Block";

        Debug.Log(
            "PlayerEnemyAttackReceiver: " +
            $"{attackerName}에게 " +
            $"{damage} 피해를 받았습니다.",
            this
        );

        playerHealth.TakeDamage(
            damage
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