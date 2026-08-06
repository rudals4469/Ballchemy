using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SecretRoomKeyPickupPresenter :
    MonoBehaviour
{
    [Header("State")]

    [SerializeField]
    private SecretRoomKeyState secretRoomKeyState;

    [Header("Canvas")]

    [Tooltip(
        "공명석 비행 아이콘과 시작·도착 지점을 포함하는 Canvas입니다."
    )]
    [SerializeField]
    private Canvas rootCanvas;

    [Tooltip(
        "비행 아이콘의 좌표 기준이 되는 전체 화면 RectTransform입니다."
    )]
    [SerializeField]
    private RectTransform animationLayer;

    [Header("Animation Points")]

    [Tooltip(
        "공명석을 처음 표시할 화면 중앙 위치입니다."
    )]
    [SerializeField]
    private RectTransform pickupStartPoint;

    [Tooltip(
        "화면 상단의 공명석 보유 아이콘 위치입니다."
    )]
    [SerializeField]
    private RectTransform pickupDestinationPoint;

    [Header("Visuals")]

    [Tooltip(
        "획득 애니메이션 중 이동하는 임시 공명석 아이콘입니다."
    )]
    [SerializeField]
    private RectTransform flyingKeyIcon;

    [Tooltip(
        "선택 사항입니다. 비행 아이콘의 알파 연출에 사용합니다."
    )]
    [SerializeField]
    private CanvasGroup flyingKeyCanvasGroup;

    [Tooltip(
        "공명석을 보유하는 동안 화면 상단에 표시할 아이콘입니다."
    )]
    [SerializeField]
    private RectTransform ownedKeyIcon;

    [Header("Timing")]

    [SerializeField, Min(0.01f)]
    private float appearDuration = 0.2f;

    [SerializeField, Min(0.01f)]
    private float riseDuration = 0.25f;

    [SerializeField, Min(0f)]
    private float holdDuration = 0.15f;

    [SerializeField, Min(0.01f)]
    private float travelDuration = 0.65f;

    [SerializeField, Min(0.01f)]
    private float arrivalDuration = 0.25f;

    [Header("Motion")]

    [Tooltip(
        "중앙에 등장한 뒤 위로 떠오르는 거리입니다."
    )]
    [SerializeField]
    private Vector2 riseOffset =
        new Vector2(
            0f,
            80f
        );

    [SerializeField]
    private Ease appearEase =
        Ease.OutBack;

    [SerializeField]
    private Ease riseEase =
        Ease.OutCubic;

    [SerializeField]
    private Ease travelEase =
        Ease.InOutCubic;

    [SerializeField]
    private Ease arrivalEase =
        Ease.OutBack;

    private Sequence pickupSequence;

    public bool IsPlaying =>
        pickupSequence != null &&
        pickupSequence.IsActive() &&
        pickupSequence.IsPlaying();

    private Camera CanvasCamera
    {
        get
        {
            if (rootCanvas == null ||
                rootCanvas.renderMode ==
                RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return rootCanvas.worldCamera;
        }
    }

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
        ApplyImmediateState();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();
        ApplyImmediateState();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        KillSequence();
    }

    private void OnDestroy()
    {
        KillSequence();
    }

    private void OnValidate()
    {
        FindReferences();
        NormalizeSettings();
    }

    private void FindReferences()
    {
        if (rootCanvas == null)
        {
            rootCanvas =
                GetComponentInParent<Canvas>();
        }

        if (animationLayer == null)
        {
            animationLayer =
                transform as RectTransform;
        }

        if (flyingKeyCanvasGroup == null &&
            flyingKeyIcon != null)
        {
            flyingKeyCanvasGroup =
                flyingKeyIcon.GetComponent<
                    CanvasGroup
                >();
        }

        if (secretRoomKeyState == null &&
            Application.isPlaying)
        {
            secretRoomKeyState =
                FindFirstObjectByType<
                    SecretRoomKeyState
                >();
        }
    }

    private void NormalizeSettings()
    {
        appearDuration =
            Mathf.Max(
                appearDuration,
                0.01f
            );

        riseDuration =
            Mathf.Max(
                riseDuration,
                0.01f
            );

        holdDuration =
            Mathf.Max(
                holdDuration,
                0f
            );

        travelDuration =
            Mathf.Max(
                travelDuration,
                0.01f
            );

        arrivalDuration =
            Mathf.Max(
                arrivalDuration,
                0.01f
            );
    }

    private void ValidateReferences()
    {
        if (secretRoomKeyState == null)
        {
            Debug.LogError(
                "SecretRoomKeyPickupPresenter: " +
                "SecretRoomKeyState가 연결되지 않았습니다.",
                this
            );
        }

        if (rootCanvas == null)
        {
            Debug.LogError(
                "SecretRoomKeyPickupPresenter: " +
                "Root Canvas가 연결되지 않았습니다.",
                this
            );
        }

        if (animationLayer == null)
        {
            Debug.LogError(
                "SecretRoomKeyPickupPresenter: " +
                "Animation Layer가 연결되지 않았습니다.",
                this
            );
        }

        if (pickupStartPoint == null)
        {
            Debug.LogError(
                "SecretRoomKeyPickupPresenter: " +
                "Pickup Start Point가 연결되지 않았습니다.",
                this
            );
        }

        if (pickupDestinationPoint == null)
        {
            Debug.LogError(
                "SecretRoomKeyPickupPresenter: " +
                "Pickup Destination Point가 연결되지 않았습니다.",
                this
            );
        }

        if (flyingKeyIcon == null)
        {
            Debug.LogError(
                "SecretRoomKeyPickupPresenter: " +
                "Flying Key Icon이 연결되지 않았습니다.",
                this
            );
        }

        if (ownedKeyIcon == null)
        {
            Debug.LogError(
                "SecretRoomKeyPickupPresenter: " +
                "Owned Key Icon이 연결되지 않았습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (secretRoomKeyState == null)
        {
            return;
        }

        secretRoomKeyState.KeyStateChanged -=
            HandleKeyStateChanged;

        secretRoomKeyState.KeyStateChanged +=
            HandleKeyStateChanged;
    }

    private void UnsubscribeEvents()
    {
        if (secretRoomKeyState == null)
        {
            return;
        }

        secretRoomKeyState.KeyStateChanged -=
            HandleKeyStateChanged;
    }

    public void PlayPickup()
    {
        if (!CanPlay())
        {
            ApplyImmediateState();

            return;
        }

        KillSequence();

        Vector2 startPosition =
            ConvertToAnimationLayerPosition(
                pickupStartPoint
            );

        Vector2 destinationPosition =
            ConvertToAnimationLayerPosition(
                pickupDestinationPoint
            );

        Vector2 risenPosition =
            startPosition +
            riseOffset;

        ownedKeyIcon.gameObject.SetActive(
            false
        );

        flyingKeyIcon.gameObject.SetActive(
            true
        );

        flyingKeyIcon.SetAsLastSibling();

        flyingKeyIcon.anchoredPosition =
            startPosition;

        flyingKeyIcon.localScale =
            Vector3.zero;

        if (flyingKeyCanvasGroup != null)
        {
            flyingKeyCanvasGroup.alpha =
                0f;
        }

        pickupSequence =
            DOTween.Sequence();

        pickupSequence.SetLink(
            gameObject,
            LinkBehaviour.KillOnDisable
        );

        pickupSequence.Append(
            flyingKeyIcon
                .DOScale(
                    1f,
                    appearDuration
                )
                .SetEase(
                    appearEase
                )
        );

        if (flyingKeyCanvasGroup != null)
        {
            pickupSequence.Join(
                flyingKeyCanvasGroup
                    .DOFade(
                        1f,
                        appearDuration
                    )
            );
        }

        pickupSequence.Append(
            flyingKeyIcon
                .DOAnchorPos(
                    risenPosition,
                    riseDuration
                )
                .SetEase(
                    riseEase
                )
        );

        if (holdDuration > 0f)
        {
            pickupSequence.AppendInterval(
                holdDuration
            );
        }

        pickupSequence.Append(
            flyingKeyIcon
                .DOAnchorPos(
                    destinationPosition,
                    travelDuration
                )
                .SetEase(
                    travelEase
                )
        );

        pickupSequence.Join(
            flyingKeyIcon
                .DOScale(
                    0.65f,
                    travelDuration
                )
                .SetEase(
                    Ease.InCubic
                )
        );

        if (flyingKeyCanvasGroup != null)
        {
            pickupSequence.Join(
                flyingKeyCanvasGroup
                    .DOFade(
                        0.8f,
                        travelDuration
                    )
            );
        }

        pickupSequence.AppendCallback(
            CompletePickupVisual
        );

        pickupSequence.OnComplete(
            HandleSequenceCompleted
        );
    }

    public void RefreshImmediate()
    {
        ApplyImmediateState();
    }

    private bool CanPlay()
    {
        return
            animationLayer != null &&
            pickupStartPoint != null &&
            pickupDestinationPoint != null &&
            flyingKeyIcon != null &&
            ownedKeyIcon != null;
    }

    private void CompletePickupVisual()
    {
        if (flyingKeyIcon != null)
        {
            flyingKeyIcon.gameObject.SetActive(
                false
            );
        }

        if (ownedKeyIcon == null)
        {
            return;
        }

        bool shouldShowOwnedIcon =
            secretRoomKeyState != null &&
            secretRoomKeyState.HasKey;

        ownedKeyIcon.gameObject.SetActive(
            shouldShowOwnedIcon
        );

        if (!shouldShowOwnedIcon)
        {
            return;
        }

        ownedKeyIcon.localScale =
            Vector3.one;

        ownedKeyIcon
            .DOPunchScale(
                Vector3.one * 0.25f,
                arrivalDuration,
                6,
                0.6f
            )
            .SetEase(
                arrivalEase
            )
            .SetLink(
                ownedKeyIcon.gameObject,
                LinkBehaviour.KillOnDisable
            );
    }

    private void HandleSequenceCompleted()
    {
        pickupSequence =
            null;
    }

    private Vector2 ConvertToAnimationLayerPosition(
        RectTransform target)
    {
        if (target == null ||
            animationLayer == null)
        {
            return Vector2.zero;
        }

        Vector2 screenPosition =
            RectTransformUtility
                .WorldToScreenPoint(
                    CanvasCamera,
                    target.position
                );

        bool converted =
            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    animationLayer,
                    screenPosition,
                    CanvasCamera,
                    out Vector2 localPosition
                );

        return converted
            ? localPosition
            : Vector2.zero;
    }

    private void ApplyImmediateState()
    {
        KillSequence();

        if (flyingKeyIcon != null)
        {
            flyingKeyIcon.gameObject.SetActive(
                false
            );
        }

        if (ownedKeyIcon == null)
        {
            return;
        }

        bool shouldShow =
            secretRoomKeyState != null &&
            secretRoomKeyState.HasKey;

        ownedKeyIcon.gameObject.SetActive(
            shouldShow
        );

        ownedKeyIcon.localScale =
            Vector3.one;
    }

    private void HandleKeyStateChanged(
        bool hasKey)
    {
        if (!hasKey)
        {
            ApplyImmediateState();

            return;
        }

        /*
         * 구매 성공 이벤트에서 PlayPickup을 따로 호출합니다.
         * 다른 시스템이 공명석을 직접 지급한 경우에는
         * 즉시 보유 아이콘만 표시합니다.
         */
        if (!IsPlaying &&
            ownedKeyIcon != null)
        {
            ownedKeyIcon.gameObject.SetActive(
                true
            );

            ownedKeyIcon.localScale =
                Vector3.one;
        }
    }

    private void KillSequence()
    {
        if (pickupSequence != null &&
            pickupSequence.IsActive())
        {
            pickupSequence.Kill();
        }

        pickupSequence =
            null;

        if (flyingKeyIcon != null)
        {
            flyingKeyIcon.DOKill();
        }

        if (flyingKeyCanvasGroup != null)
        {
            flyingKeyCanvasGroup.DOKill();
        }

        if (ownedKeyIcon != null)
        {
            ownedKeyIcon.DOKill();
        }
    }
}