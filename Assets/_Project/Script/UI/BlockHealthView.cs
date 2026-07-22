using TMPro;
using UnityEngine;

public sealed class BlockHealthView : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Block block;

    [SerializeField]
    private TMP_Text healthText;

    [Header("Display")]
    [Tooltip(
        "체력을 현재 체력만 표시할지, " +
        "현재/최대 체력으로 표시할지 결정합니다."
    )]
    [SerializeField]
    private bool showMaximumHealth;

    private bool isSubscribed;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void Start()
    {
        Refresh();
    }

    private void FindReferences()
    {
        if (block == null)
        {
            block = GetComponent<Block>();
        }

        if (block == null)
        {
            block = GetComponentInParent<Block>();
        }

        if (healthText == null)
        {
            healthText =
                GetComponentInChildren<TMP_Text>(
                    true
                );
        }
    }

    private void ValidateReferences()
    {
        if (block == null)
        {
            Debug.LogError(
                "BlockHealthView: Block이 연결되지 않았습니다.",
                this
            );
        }

        if (healthText == null)
        {
            Debug.LogError(
                "BlockHealthView: HealthText가 연결되지 않았습니다.",
                this
            );
        }
    }

    private void Subscribe()
    {
        if (isSubscribed || block == null)
        {
            return;
        }

        block.HealthChanged +=
            HandleHealthChanged;

        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || block == null)
        {
            return;
        }

        block.HealthChanged -=
            HandleHealthChanged;

        isSubscribed = false;
    }

    private void Refresh()
    {
        if (block == null)
        {
            return;
        }

        UpdateText(
            block.CurrentHealth,
            block.MaxHealth
        );
    }

    private void HandleHealthChanged(
        int currentHealth,
        int maxHealth)
    {
        UpdateText(
            currentHealth,
            maxHealth
        );
    }

    private void UpdateText(
        int currentHealth,
        int maxHealth)
    {
        if (healthText == null)
        {
            return;
        }

        if (showMaximumHealth)
        {
            healthText.text =
                $"{currentHealth}/{maxHealth}";

            return;
        }

        healthText.text =
            currentHealth.ToString();
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