using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StageKeyPickupPresenter :
    MonoBehaviour
{
    [Header("State")]

    [SerializeField]
    private StageKeyState stageKeyState;

    [Header("Canvas")]

    [Tooltip(
        "열쇠 비행 아이콘과 시작·도착 지점을 포함하는 Canvas입니다."
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
        "인게임 화면 중앙에 배치하는 열쇠 등장 위치입니다."
    )]
    [SerializeField]
    private RectTransform pickupStartPoint;

    [Tooltip(
        "후퇴 버튼 왼쪽에 배치하는 열쇠 도착 위치입니다."
    )]
    [SerializeField]
    private RectTransform pickupDestinationPoint;

    [Header("Visuals")]

    [Tooltip(
        "이동 애니메이션에 사용하는 임시 열쇠 아이콘입니다."
    )]
    [SerializeField]
    private RectTransform flyingKeyIcon;

    [Tooltip(
        "선택 사항입니다. 연결하면 등장과 이동 중 알파값도 연출합니다."
    )]
    [SerializeField]
    private CanvasGroup flyingKeyCanvasGroup;

    [Tooltip(
        "열쇠 보유 중 후퇴 버튼 왼쪽에 고정 표시할 아이콘입니다."
    )]
    [SerializeField]
    private RectTransform ownedKeyIcon;

    [Header("Timing")]

    [SerializeField, Min(0.01f)]
    private float appearDuration = 0.2f;

    [SerializeField, Min(0.01f)]
    private float riseDuration = 0.25f;

    [SerializeField, Min(0f)]
    private float holdDuration = 0.1f;

    [SerializeField, Min(0.01f)]
    private float travelDuration = 0.65f;

    [SerializeField, Min(0.01f)]
    private float arrivalDuration = 0.25f;

    [Header("Motion")]

    [Tooltip(
        "중앙 등장 후 이동을 시작하기 전에 위로 떠오르는 거리입니다."
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

        if (stageKeyState == null &&
            Application.isPlaying)
        {
            stageKeyState =
                FindFirstObjectByType<
                    StageKeyState
                >();
        }
    }

    private void ValidateReferences()
    {
        if (stageKeyState == null)
        {
            Debug.LogError(
                "StageKeyPickupPresenter: " +
                "StageKeyState가 연결되지 않았습니다.",
                this
            );
        }

        if (rootCanvas == null)
        {
            Debug.LogError(
                "StageKeyPickupPresenter: " +
                "Canvas가 연결되지 않았습니다.",
                this
            );
        }

        if (animationLayer == null)
        {
            Debug.LogError(
                "StageKeyPickupPresenter: " +
                "Animation Layer가 연결되지 않았습니다.",
                this
            );
        }

        if (pickupStartPoint == null)
        {
            Debug.LogError(
                "StageKeyPickupPresenter: " +
                "Pickup Start Point가 연결되지 않았습니다.",
                this
            );
        }

        if (pickupDestinationPoint == null)
        {
            Debug.LogError(
                "StageKeyPickupPresenter: " +
                "Pickup Destination Point가 연결되지 않았습니다.",
                this
            );
        }

        if (flyingKeyIcon == null)
        {
            Debug.LogError(
                "StageKeyPickupPresenter: " +
                "Flying Key Icon이 연결되지 않았습니다.",
                this
            );
        }

        if (ownedKeyIcon == null)
        {
            Debug.LogError(
                "StageKeyPickupPresenter: " +
                "Owned Key Icon이 연결되지 않았습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        if (stageKeyState == null)
        {
            return;
        }

        stageKeyState.StageStateReset -=
            HandleStageStateReset;

        stageKeyState.StageStateReset +=
            HandleStageStateReset;

        stageKeyState.KeyConsumed -=
            HandleKeyConsumed;

        stageKeyState.KeyConsumed +=
            HandleKeyConsumed;
    }

    private void UnsubscribeEvents()
    {
        if (stageKeyState == null)
        {
            return;
        }

        stageKeyState.StageStateReset -=
            HandleStageStateReset;

        stageKeyState.KeyConsumed -=
            HandleKeyConsumed;
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

        pickupSequence
            .SetLink(
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
    }

    private bool CanPlay()
    {
        return animationLayer != null &&
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

        ownedKeyIcon.gameObject.SetActive(
            true
        );

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

        if (ownedKeyIcon != null)
        {
            bool shouldShow =
                stageKeyState != null &&
                stageKeyState.HasKey;

            ownedKeyIcon.gameObject.SetActive(
                shouldShow
            );

            ownedKeyIcon.localScale =
                Vector3.one;
        }
    }

    private void HandleStageStateReset()
    {
        ApplyImmediateState();
    }

    private void HandleKeyConsumed()
    {
        ApplyImmediateState();
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