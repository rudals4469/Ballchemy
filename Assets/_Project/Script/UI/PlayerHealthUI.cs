using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerHealth playerHealth;

    [SerializeField]
    private Image healthFillImage;

    [SerializeField]
    private TMP_Text healthText;

    [SerializeField]
    private Image damageFlashImage;

    [SerializeField]
    private RectTransform shakeTarget;

    [Header("Health Bar")]
    [SerializeField, Min(0.1f)]
    private float fillChangeSpeed = 3f;

    [Header("Damage Flash")]
    [SerializeField, Min(0f)]
    private float flashDuration = 0.1f;

    [SerializeField, Range(0f, 1f)]
    private float flashAlpha = 0.5f;

    [Header("Damage Shake")]
    [SerializeField, Min(0f)]
    private float shakeDuration = 0.1f;

    [SerializeField, Min(0f)]
    private float shakeStrength = 4f;

    private Coroutine damageFeedbackCoroutine;

    private Vector2 originalShakePosition;

    private float displayedFillAmount;
    private float targetFillAmount;

    private bool isSubscribed;
    private bool hasStarted;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();

        if (shakeTarget != null)
        {
            originalShakePosition =
                shakeTarget.anchoredPosition;
        }

        SetFlashAlpha(0f);
    }

    private void OnEnable()
    {
        SubscribeEvents();

        // 게임 시작 이후 UI가 다시 활성화되는 경우에만
        // 현재 체력으로 즉시 갱신한다.
        if (hasStarted)
        {
            RefreshImmediately();
        }
    }

    private void Start()
    {
        hasStarted = true;

        FindReferences();
        SubscribeEvents();

        // 모든 오브젝트의 Awake가 끝난 뒤
        // PlayerHealth의 초기 체력을 읽는다.
        RefreshImmediately();
    }

    private void Update()
    {
        UpdateHealthBar();
    }

    private void FindReferences()
    {
        if (playerHealth == null)
        {
            playerHealth =
                FindFirstObjectByType<PlayerHealth>();
        }

        if (shakeTarget == null)
        {
            shakeTarget =
                GetComponent<RectTransform>();
        }
    }

    private void ValidateReferences()
    {
        if (playerHealth == null)
        {
            Debug.LogError(
                "PlayerHealthUI: PlayerHealth가 연결되지 않았습니다.",
                this
            );
        }

        if (healthFillImage == null)
        {
            Debug.LogError(
                "PlayerHealthUI: HealthBarFill Image가 연결되지 않았습니다.",
                this
            );
        }

        if (healthText == null)
        {
            Debug.LogError(
                "PlayerHealthUI: HealthText가 연결되지 않았습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (isSubscribed ||
            playerHealth == null)
        {
            return;
        }

        playerHealth.HealthChanged +=
            HandleHealthChanged;

        playerHealth.Damaged +=
            HandleDamaged;

        isSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!isSubscribed ||
            playerHealth == null)
        {
            return;
        }

        playerHealth.HealthChanged -=
            HandleHealthChanged;

        playerHealth.Damaged -=
            HandleDamaged;

        isSubscribed = false;
    }

    private void RefreshImmediately()
    {
        if (playerHealth == null)
        {
            return;
        }

        int currentHealth =
            playerHealth.CurrentHealth;

        int maxHealth =
            playerHealth.MaxHealth;

        targetFillAmount =
            CalculateHealthRatio(
                currentHealth,
                maxHealth
            );

        displayedFillAmount =
            targetFillAmount;

        ApplyHealthFill(
            displayedFillAmount
        );

        ApplyHealthText(
            currentHealth,
            maxHealth
        );
    }

    private void HandleHealthChanged(
        int currentHealth,
        int maxHealth)
    {
        targetFillAmount =
            CalculateHealthRatio(
                currentHealth,
                maxHealth
            );

        ApplyHealthText(
            currentHealth,
            maxHealth
        );
    }

    private void HandleDamaged(
        int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        PlayDamageFeedback();
    }

    private void UpdateHealthBar()
    {
        if (healthFillImage == null)
        {
            return;
        }

        displayedFillAmount =
            Mathf.MoveTowards(
                displayedFillAmount,
                targetFillAmount,
                fillChangeSpeed *
                Time.deltaTime
            );

        ApplyHealthFill(
            displayedFillAmount
        );
    }

    private void ApplyHealthFill(
        float fillAmount)
    {
        if (healthFillImage == null)
        {
            return;
        }

        healthFillImage.fillAmount =
            Mathf.Clamp01(
                fillAmount
            );
    }

    private void ApplyHealthText(
        int currentHealth,
        int maxHealth)
    {
        if (healthText == null)
        {
            return;
        }

        healthText.text =
            $"{currentHealth} / {maxHealth}";
    }

    private static float CalculateHealthRatio(
        int currentHealth,
        int maxHealth)
    {
        if (maxHealth <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01(
            (float)currentHealth /
            maxHealth
        );
    }

    private void PlayDamageFeedback()
    {
        if (damageFeedbackCoroutine != null)
        {
            StopCoroutine(
                damageFeedbackCoroutine
            );
        }

        ResetDamageFeedback();

        damageFeedbackCoroutine =
            StartCoroutine(
                DamageFeedbackRoutine()
            );
    }

    private IEnumerator DamageFeedbackRoutine()
    {
        float totalDuration =
            Mathf.Max(
                flashDuration,
                shakeDuration
            );

        if (totalDuration <= 0f)
        {
            ResetDamageFeedback();

            damageFeedbackCoroutine = null;
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime <
               totalDuration)
        {
            elapsedTime +=
                Time.deltaTime;

            UpdateFlash(
                elapsedTime
            );

            UpdateShake(
                elapsedTime
            );

            yield return null;
        }

        ResetDamageFeedback();

        damageFeedbackCoroutine = null;
    }

    private void UpdateFlash(
        float elapsedTime)
    {
        if (damageFlashImage == null ||
            flashDuration <= 0f)
        {
            return;
        }

        float progress =
            Mathf.Clamp01(
                elapsedTime /
                flashDuration
            );

        float currentAlpha =
            Mathf.Lerp(
                flashAlpha,
                0f,
                progress
            );

        SetFlashAlpha(
            currentAlpha
        );
    }

    private void UpdateShake(
        float elapsedTime)
    {
        if (shakeTarget == null ||
            shakeDuration <= 0f)
        {
            return;
        }

        float progress =
            Mathf.Clamp01(
                elapsedTime /
                shakeDuration
            );

        float currentStrength =
            shakeStrength *
            (1f - progress);

        Vector2 randomOffset =
            Random.insideUnitCircle *
            currentStrength;

        shakeTarget.anchoredPosition =
            originalShakePosition +
            randomOffset;
    }

    private void SetFlashAlpha(
        float alpha)
    {
        if (damageFlashImage == null)
        {
            return;
        }

        Color currentColor =
            damageFlashImage.color;

        currentColor.a =
            Mathf.Clamp01(
                alpha
            );

        damageFlashImage.color =
            currentColor;
    }

    private void ResetDamageFeedback()
    {
        SetFlashAlpha(0f);

        if (shakeTarget != null)
        {
            shakeTarget.anchoredPosition =
                originalShakePosition;
        }
    }

    private void OnDisable()
    {
        UnsubscribeEvents();

        if (damageFeedbackCoroutine != null)
        {
            StopCoroutine(
                damageFeedbackCoroutine
            );

            damageFeedbackCoroutine = null;
        }

        ResetDamageFeedback();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }
}