using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AddBallRewardEffect))]
public sealed class AddBallRewardPopupPresenter :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private AddBallRewardEffect rewardEffect;

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
            0.9f,
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
        if (rewardEffect == null)
        {
            rewardEffect =
                GetComponent<
                    AddBallRewardEffect
                >();
        }
    }

    private void ValidateReferences()
    {
        if (rewardEffect == null)
        {
            Debug.LogError(
                "AddBallRewardPopupPresenter: " +
                "AddBallRewardEffect를 찾지 못했습니다.",
                this
            );
        }

        if (popupPrefab == null)
        {
            Debug.LogError(
                "AddBallRewardPopupPresenter: " +
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

        rewardEffect.RewardGranted -=
            HandleRewardGranted;

        rewardEffect.RewardGranted +=
            HandleRewardGranted;
    }

    private void UnsubscribeEvents()
    {
        if (rewardEffect == null)
        {
            return;
        }

        rewardEffect.RewardGranted -=
            HandleRewardGranted;
    }

    private void HandleRewardGranted(
        int addedBallCount)
    {
        if (addedBallCount <= 0)
        {
            return;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "AddBallRewardPopupPresenter: " +
                $"+{addedBallCount} 팝업 생성 요청",
                this
            );
        }

        if (popupPrefab == null)
        {
            Debug.LogError(
                "AddBallRewardPopupPresenter: " +
                "Popup Prefab이 없어 팝업을 생성할 수 없습니다.",
                this
            );

            return;
        }

        Vector3 spawnPosition =
            transform.position +
            spawnOffset;

        /*
         * 부모를 지정하지 않고 씬 루트에 생성한다.
         * 블록이 파괴돼도 팝업은 함께 파괴되지 않는다.
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
                "AddBallRewardPopupPresenter: " +
                "팝업 생성에 실패했습니다.",
                this
            );

            return;
        }

        string message =
            FormatMessage(
                addedBallCount
            );

        popup.Show(
            message,
            popupColor
        );

        if (showDebugLog)
        {
            Debug.Log(
                "AddBallRewardPopupPresenter: " +
                $"팝업 생성 완료, 위치={spawnPosition}, " +
                $"문자={message}",
                popup
            );
        }
    }

    private string FormatMessage(
        int addedBallCount)
    {
        try
        {
            return string.Format(
                textFormat,
                addedBallCount
            );
        }
        catch
        {
            return $"+{addedBallCount}";
        }
    }
}