using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(
    typeof(RepairOnDestroyedEffect)
)]
public sealed class RepairHealingPopupPresenter :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private RepairOnDestroyedEffect
        repairEffect;

    [SerializeField]
    private WorldFloatingText popupPrefab;

    [Header("Presentation")]
    [Tooltip(
        "회복 대상 블록 위치를 기준으로 " +
        "팝업 생성 위치를 보정합니다."
    )]
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
    }

    private void FindReferences()
    {
        if (repairEffect == null)
        {
            repairEffect =
                GetComponent<
                    RepairOnDestroyedEffect
                >();
        }
    }

    private void ValidateReferences()
    {
        if (repairEffect == null)
        {
            Debug.LogError(
                "RepairHealingPopupPresenter: " +
                "RepairOnDestroyedEffect를 " +
                "찾지 못했습니다.",
                this
            );
        }

        if (popupPrefab == null)
        {
            Debug.LogError(
                "RepairHealingPopupPresenter: " +
                "Popup Prefab이 연결되지 않았습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (repairEffect == null)
        {
            return;
        }

        repairEffect.HealingApplied -=
            HandleHealingApplied;

        repairEffect.HealingApplied +=
            HandleHealingApplied;
    }

    private void UnsubscribeEvents()
    {
        if (repairEffect == null)
        {
            return;
        }

        repairEffect.HealingApplied -=
            HandleHealingApplied;
    }

    private void HandleHealingApplied(
        Vector3 targetPosition,
        int appliedHealing)
    {
        if (appliedHealing <= 0)
        {
            return;
        }

        string message =
            FormatHealingMessage(
                appliedHealing
            );

        CreatePopup(
            targetPosition,
            message
        );
    }

    private void CreatePopup(
        Vector3 targetPosition,
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
                "RepairHealingPopupPresenter: " +
                "Popup Prefab이 없어 " +
                "팝업을 생성할 수 없습니다.",
                this
            );

            return;
        }

        Vector3 spawnPosition =
            targetPosition +
            spawnOffset;

        WorldFloatingText popup =
            Instantiate(
                popupPrefab,
                spawnPosition,
                Quaternion.identity
            );

        if (popup == null)
        {
            Debug.LogError(
                "RepairHealingPopupPresenter: " +
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
                "RepairHealingPopupPresenter: " +
                $"수리 팝업 생성, " +
                $"위치={spawnPosition}, " +
                $"문자={message}",
                popup
            );
        }
    }

    private string FormatHealingMessage(
        int appliedHealing)
    {
        try
        {
            return string.Format(
                textFormat,
                appliedHealing
            );
        }
        catch
        {
            return $"+{appliedHealing}";
        }
    }
}