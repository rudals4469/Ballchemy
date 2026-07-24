using UnityEngine;

[DisallowMultipleComponent]
public sealed class HealRewardPopupPresenter :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private HealRewardEffect rewardEffect;

    [SerializeField]
    private WorldFloatingText popupPrefab;

    [Header("Presentation")]
    [SerializeField]
    private Vector3 spawnOffset =
        new Vector3(
            0f,
            0.15f,
            0f
        );

    [SerializeField]
    private Color popupColor =
        new Color(
            0.35f,
            1f,
            0.45f,
            1f
        );

    [SerializeField]
    private string textFormat = "+{0}";

    [SerializeField]
    private string fullHealthText = "FULL";

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog = true;

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

    private void OnValidate()
    {
        FindReferences();

        if (string.IsNullOrWhiteSpace(
                textFormat))
        {
            textFormat = "+{0}";
        }

        if (string.IsNullOrWhiteSpace(
                fullHealthText))
        {
            fullHealthText = "FULL";
        }
    }

    private void FindReferences()
    {
        if (rewardEffect == null)
        {
            rewardEffect =
                GetComponent<
                    HealRewardEffect
                >();
        }
    }

    private void ValidateReferences()
    {
        if (rewardEffect == null)
        {
            Debug.LogError(
                "HealRewardPopupPresenter: " +
                "HealRewardEffect를 찾지 못했습니다.",
                this
            );
        }

        if (popupPrefab == null)
        {
            Debug.LogError(
                "HealRewardPopupPresenter: " +
                "Popup Prefab이 연결되지 않았습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (rewardEffect == null)
        {
            return;
        }

        rewardEffect.HealingGranted -=
            HandleHealingGranted;

        rewardEffect.HealingGranted +=
            HandleHealingGranted;

        rewardEffect
            .HealingBlockedByFullHealth -=
            HandleHealingBlockedByFullHealth;

        rewardEffect
            .HealingBlockedByFullHealth +=
            HandleHealingBlockedByFullHealth;
    }

    private void UnsubscribeEvents()
    {
        if (rewardEffect == null)
        {
            return;
        }

        rewardEffect.HealingGranted -=
            HandleHealingGranted;

        rewardEffect
            .HealingBlockedByFullHealth -=
            HandleHealingBlockedByFullHealth;
    }

    private void HandleHealingGranted(
        int healedAmount)
    {
        if (healedAmount <= 0)
        {
            return;
        }

        string message =
            FormatHealingMessage(
                healedAmount
            );

        CreatePopup(
            message
        );
    }

    private void
        HandleHealingBlockedByFullHealth()
    {
        CreatePopup(
            fullHealthText
        );
    }

    private void CreatePopup(
        string message)
    {
        if (string.IsNullOrWhiteSpace(
                message))
        {
            return;
        }

        if (popupPrefab == null)
        {
            Debug.LogError(
                "HealRewardPopupPresenter: " +
                "Popup Prefab이 없어 팝업을 생성할 수 없습니다.",
                this
            );

            return;
        }

        Vector3 spawnPosition =
            transform.position +
            spawnOffset;

        /*
         * 부모 없이 씬 루트에 생성한다.
         * 회복 블록이 파괴되어도
         * 팝업 애니메이션은 계속 유지된다.
         */
        WorldFloatingText popup =
            Instantiate(
                popupPrefab,
                spawnPosition,
                Quaternion.identity
            );

        if (popup == null)
        {
            Debug.LogError(
                "HealRewardPopupPresenter: " +
                "팝업 생성에 실패했습니다.",
                this
            );

            return;
        }

        popup.Show(
            message,
            popupColor
        );

        if (showDebugLog)
        {
            Debug.Log(
                "HealRewardPopupPresenter: " +
                $"팝업 생성 완료, " +
                $"위치={spawnPosition}, " +
                $"문자={message}",
                popup
            );
        }
    }

    private string FormatHealingMessage(
        int healedAmount)
    {
        try
        {
            return string.Format(
                textFormat,
                healedAmount
            );
        }
        catch
        {
            return $"+{healedAmount}";
        }
    }
}