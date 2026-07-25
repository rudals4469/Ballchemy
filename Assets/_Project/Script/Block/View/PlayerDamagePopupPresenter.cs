using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(
    typeof(PlayerDamageOnDestroyedEffect)
)]
public sealed class PlayerDamagePopupPresenter :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerDamageOnDestroyedEffect
        damageEffect;

    [SerializeField]
    private WorldFloatingText popupPrefab;

    [Header("Presentation")]
    [Tooltip(
        "저주 블록의 위치를 기준으로 " +
        "팝업이 생성되는 위치 보정값입니다."
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
            1f,
            0.2f,
            0.2f,
            1f
        );

    [SerializeField]
    private string textFormat = "-{0}";

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
            textFormat = "-{0}";
        }
    }

    private void FindReferences()
    {
        if (damageEffect == null)
        {
            damageEffect =
                GetComponent<
                    PlayerDamageOnDestroyedEffect
                >();
        }
    }

    private void ValidateReferences()
    {
        if (damageEffect == null)
        {
            Debug.LogError(
                "PlayerDamagePopupPresenter: " +
                "PlayerDamageOnDestroyedEffect를 " +
                "찾지 못했습니다.",
                this
            );
        }

        if (popupPrefab == null)
        {
            Debug.LogError(
                "PlayerDamagePopupPresenter: " +
                "Popup Prefab이 연결되지 않았습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (damageEffect == null)
        {
            return;
        }

        damageEffect.DamageApplied -=
            HandleDamageApplied;

        damageEffect.DamageApplied +=
            HandleDamageApplied;
    }

    private void UnsubscribeEvents()
    {
        if (damageEffect == null)
        {
            return;
        }

        damageEffect.DamageApplied -=
            HandleDamageApplied;
    }

    private void HandleDamageApplied(
        int damagedAmount)
    {
        if (damagedAmount <= 0)
        {
            return;
        }

        string message =
            FormatDamageMessage(
                damagedAmount
            );

        CreatePopup(
            message
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
                "PlayerDamagePopupPresenter: " +
                "Popup Prefab이 없어 " +
                "팝업을 생성할 수 없습니다.",
                this
            );

            return;
        }

        /*
         * 저주 블록 자체의 위치에서
         * 피해 숫자를 표시한다.
         *
         * Block.Destroyed 이벤트 시점에는
         * 아직 블록 오브젝트가 Destroy되기 전이므로
         * 현재 위치를 정상적으로 가져올 수 있다.
         */
        Vector3 spawnPosition =
            transform.position +
            spawnOffset;

        /*
         * 블록이 바로 파괴되더라도 팝업이 유지되도록
         * 부모 없이 씬 루트에 생성한다.
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
                "PlayerDamagePopupPresenter: " +
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
                "PlayerDamagePopupPresenter: " +
                $"저주 피해 팝업 생성, " +
                $"위치={spawnPosition}, " +
                $"문자={message}",
                popup
            );
        }
    }

    private string FormatDamageMessage(
        int damagedAmount)
    {
        try
        {
            return string.Format(
                textFormat,
                damagedAmount
            );
        }
        catch
        {
            return $"-{damagedAmount}";
        }
    }
}