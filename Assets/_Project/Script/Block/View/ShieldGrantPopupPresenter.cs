using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(
    typeof(ShieldOnDestroyedEffect)
)]
public sealed class ShieldGrantPopupPresenter :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ShieldOnDestroyedEffect
        shieldEffect;

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
            0.15f,
            0.8f,
            1f,
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
        if (shieldEffect == null)
        {
            shieldEffect =
                GetComponent<
                    ShieldOnDestroyedEffect
                >();
        }
    }

    private void ValidateReferences()
    {
        if (shieldEffect == null)
        {
            Debug.LogError(
                "ShieldGrantPopupPresenter: " +
                "ShieldOnDestroyedEffect를 " +
                "찾지 못했습니다.",
                this
            );
        }

        if (popupPrefab == null)
        {
            Debug.LogError(
                "ShieldGrantPopupPresenter: " +
                "Popup Prefab이 연결되지 않았습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (shieldEffect == null)
        {
            return;
        }

        shieldEffect.ShieldApplied -=
            HandleShieldApplied;

        shieldEffect.ShieldApplied +=
            HandleShieldApplied;
    }

    private void UnsubscribeEvents()
    {
        if (shieldEffect == null)
        {
            return;
        }

        shieldEffect.ShieldApplied -=
            HandleShieldApplied;
    }

    private void HandleShieldApplied(
        Vector3 targetPosition,
        int appliedShield)
    {
        if (appliedShield <= 0)
        {
            return;
        }

        string message =
            FormatMessage(
                appliedShield
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
        if (popupPrefab == null ||
            string.IsNullOrWhiteSpace(
                message))
        {
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
            return;
        }

        popup.Show(
            message,
            popupColor
        );

        if (showDebugLog)
        {
            Debug.Log(
                "ShieldGrantPopupPresenter: " +
                $"쉴드 팝업 생성, " +
                $"위치={spawnPosition}, " +
                $"문자={message}",
                popup
            );
        }
    }

    private string FormatMessage(
        int appliedShield)
    {
        try
        {
            return string.Format(
                textFormat,
                appliedShield
            );
        }
        catch
        {
            return $"+{appliedShield}";
        }
    }
}