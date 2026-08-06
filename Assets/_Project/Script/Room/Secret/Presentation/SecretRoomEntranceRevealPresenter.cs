using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SecretRoomEntranceRevealPresenter :
    MonoBehaviour
{
    [Header("Canvas")]

    [SerializeField]
    private Canvas rootCanvas;

    [SerializeField]
    private RectTransform animationLayer;

    [Header("Key Visual")]

    [Tooltip(
        "화면 상단에 표시 중인 비밀문 공명석 아이콘입니다."
    )]
    [SerializeField]
    private RectTransform ownedKeyIcon;

    [Header("Direction Buttons")]

    [SerializeField]
    private Button upButton;

    [SerializeField]
    private Button rightButton;

    [SerializeField]
    private Button downButton;

    [SerializeField]
    private Button leftButton;

    [Header("Animation")]

    [SerializeField, Min(0.01f)]
    private float prepareDuration = 0.15f;

    [SerializeField, Min(0f)]
    private float holdDuration = 0.1f;

    [SerializeField, Min(0.01f)]
    private float travelDuration = 0.65f;

    [SerializeField, Min(0.01f)]
    private float arrivalDuration = 0.25f;

    [SerializeField]
    private float prepareScale = 1.2f;

    [SerializeField]
    private Ease prepareEase = Ease.OutBack;

    [SerializeField]
    private Ease travelEase = Ease.InOutCubic;

    [SerializeField]
    private Ease arrivalEase = Ease.OutBack;

    private Sequence revealSequence;

    private Vector2 originalAnchoredPosition;
    private Vector3 originalLocalScale;
    private Transform originalParent;
    private int originalSiblingIndex;

    public bool IsPlaying =>
        revealSequence != null &&
        revealSequence.IsActive() &&
        revealSequence.IsPlaying();

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
        NormalizeSettings();
        ValidateReferences();
        CacheOriginalState();
    }

    private void OnDisable()
    {
        KillSequence();
        RestoreOwnedIcon();
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
    }

    private void NormalizeSettings()
    {
        prepareDuration =
            Mathf.Max(
                prepareDuration,
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

        prepareScale =
            Mathf.Max(
                prepareScale,
                0.01f
            );
    }

    private void ValidateReferences()
    {
        if (rootCanvas == null)
        {
            Debug.LogError(
                "SecretRoomEntranceRevealPresenter: " +
                "Root Canvas가 연결되지 않았습니다.",
                this
            );
        }

        if (animationLayer == null)
        {
            Debug.LogError(
                "SecretRoomEntranceRevealPresenter: " +
                "Animation Layer가 연결되지 않았습니다.",
                this
            );
        }

        if (ownedKeyIcon == null)
        {
            Debug.LogError(
                "SecretRoomEntranceRevealPresenter: " +
                "Owned Key Icon이 연결되지 않았습니다.",
                this
            );
        }

        if (upButton == null ||
            rightButton == null ||
            downButton == null ||
            leftButton == null)
        {
            Debug.LogError(
                "SecretRoomEntranceRevealPresenter: " +
                "상하좌우 버튼 중 연결되지 않은 버튼이 있습니다.",
                this
            );
        }
    }

    public bool PlayReveal(
        RoomDirection direction,
        Action completed)
    {
        if (IsPlaying ||
            ownedKeyIcon == null ||
            animationLayer == null)
        {
            return false;
        }

        RectTransform target =
            GetDirectionTarget(
                direction
            );

        if (target == null)
        {
            return false;
        }

        CacheOriginalState();
        KillSequence();

        Vector2 currentPosition =
            ConvertToAnimationLayerPosition(
                ownedKeyIcon
            );

        Vector2 targetPosition =
            ConvertToAnimationLayerPosition(
                target
            );

        ownedKeyIcon.SetParent(
            animationLayer,
            true
        );

        ownedKeyIcon.SetAsLastSibling();

        ownedKeyIcon.anchoredPosition =
            currentPosition;

        ownedKeyIcon.localScale =
            Vector3.one;

        ownedKeyIcon.gameObject.SetActive(
            true
        );

        revealSequence =
            DOTween.Sequence();

        revealSequence.SetLink(
            gameObject,
            LinkBehaviour.KillOnDisable
        );

        revealSequence.Append(
            ownedKeyIcon
                .DOScale(
                    prepareScale,
                    prepareDuration
                )
                .SetEase(
                    prepareEase
                )
        );

        if (holdDuration > 0f)
        {
            revealSequence.AppendInterval(
                holdDuration
            );
        }

        revealSequence.Append(
            ownedKeyIcon
                .DOAnchorPos(
                    targetPosition,
                    travelDuration
                )
                .SetEase(
                    travelEase
                )
        );

        revealSequence.Join(
            ownedKeyIcon
                .DOScale(
                    0.65f,
                    travelDuration
                )
                .SetEase(
                    Ease.InCubic
                )
        );

        revealSequence.Append(
            ownedKeyIcon
                .DOPunchScale(
                    Vector3.one * 0.3f,
                    arrivalDuration,
                    6,
                    0.6f
                )
                .SetEase(
                    arrivalEase
                )
        );

        revealSequence.AppendCallback(
            () =>
            {
                ownedKeyIcon.gameObject.SetActive(
                    false
                );

                RestoreOwnedIcon();

                completed?.Invoke();
            }
        );

        revealSequence.OnComplete(
            () =>
            {
                revealSequence =
                    null;
            }
        );

        return true;
    }

    public void CancelReveal()
    {
        KillSequence();
        RestoreOwnedIcon();
    }

    private RectTransform GetDirectionTarget(
        RoomDirection direction)
    {
        Button button;

        switch (direction)
        {
            case RoomDirection.Up:
                button = upButton;
                break;

            case RoomDirection.Right:
                button = rightButton;
                break;

            case RoomDirection.Down:
                button = downButton;
                break;

            case RoomDirection.Left:
                button = leftButton;
                break;

            default:
                return null;
        }

        return button != null
            ? button.transform as RectTransform
            : null;
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
            RectTransformUtility.WorldToScreenPoint(
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

    private void CacheOriginalState()
    {
        if (ownedKeyIcon == null)
        {
            return;
        }

        originalParent =
            ownedKeyIcon.parent;

        originalSiblingIndex =
            ownedKeyIcon.GetSiblingIndex();

        originalAnchoredPosition =
            ownedKeyIcon.anchoredPosition;

        originalLocalScale =
            ownedKeyIcon.localScale;
    }

    private void RestoreOwnedIcon()
    {
        if (ownedKeyIcon == null ||
            originalParent == null)
        {
            return;
        }

        ownedKeyIcon.SetParent(
            originalParent,
            false
        );

        ownedKeyIcon.SetSiblingIndex(
            Mathf.Clamp(
                originalSiblingIndex,
                0,
                originalParent.childCount - 1
            )
        );

        ownedKeyIcon.anchoredPosition =
            originalAnchoredPosition;

        ownedKeyIcon.localScale =
            originalLocalScale;
    }

    private void KillSequence()
    {
        if (revealSequence != null &&
            revealSequence.IsActive())
        {
            revealSequence.Kill();
        }

        revealSequence =
            null;

        if (ownedKeyIcon != null)
        {
            ownedKeyIcon.DOKill();
        }
    }
}