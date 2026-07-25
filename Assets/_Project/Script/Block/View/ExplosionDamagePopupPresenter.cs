using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(
    typeof(ExplosionOnDestroyedEffect)
)]
public sealed class ExplosionDamagePopupPresenter :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ExplosionOnDestroyedEffect
        explosionEffect;

    [SerializeField]
    private WorldFloatingText popupPrefab;

    [Header("Presentation")]
    [Tooltip(
        "각 피해 대상 블록의 위치를 기준으로 " +
        "팝업 위치를 보정합니다."
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
            0.55f,
            0.1f,
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
        if (explosionEffect == null)
        {
            explosionEffect =
                GetComponent<
                    ExplosionOnDestroyedEffect
                >();
        }
    }

    private void ValidateReferences()
    {
        if (explosionEffect == null)
        {
            Debug.LogError(
                "ExplosionDamagePopupPresenter: " +
                "ExplosionOnDestroyedEffect를 " +
                "찾지 못했습니다.",
                this
            );
        }

        if (popupPrefab == null)
        {
            Debug.LogError(
                "ExplosionDamagePopupPresenter: " +
                "Popup Prefab이 연결되지 않았습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (explosionEffect == null)
        {
            return;
        }

        explosionEffect
            .ExplosionDamageApplied -=
            HandleExplosionDamageApplied;

        explosionEffect
            .ExplosionDamageApplied +=
            HandleExplosionDamageApplied;
    }

    private void UnsubscribeEvents()
    {
        if (explosionEffect == null)
        {
            return;
        }

        explosionEffect
            .ExplosionDamageApplied -=
            HandleExplosionDamageApplied;
    }

    private void HandleExplosionDamageApplied(
        Vector3 targetPosition,
        int appliedDamage)
    {
        if (appliedDamage <= 0)
        {
            return;
        }

        string message =
            FormatDamageMessage(
                appliedDamage
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
                "ExplosionDamagePopupPresenter: " +
                "Popup Prefab이 없어 " +
                "팝업을 생성할 수 없습니다.",
                this
            );

            return;
        }

        Vector3 spawnPosition =
            targetPosition +
            spawnOffset;

        /*
         * 각 피해 대상 위치에 별도의 팝업을 생성한다.
         * 대상 블록이 파괴되어도 팝업은 씬 루트에 남아 재생된다.
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
                "ExplosionDamagePopupPresenter: " +
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
                "ExplosionDamagePopupPresenter: " +
                $"폭발 피해 팝업 생성, " +
                $"위치={spawnPosition}, " +
                $"문자={message}",
                popup
            );
        }
    }

    private string FormatDamageMessage(
        int appliedDamage)
    {
        try
        {
            return string.Format(
                textFormat,
                appliedDamage
            );
        }
        catch
        {
            return $"-{appliedDamage}";
        }
    }
}