using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public sealed class NextBallQueueItemView : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Image ballIcon;

    [SerializeField]
    private TMP_Text starText;

    [SerializeField]
    private CanvasGroup canvasGroup;

    [Header("Fallback Visual")]
    [Tooltip(
        "BallDefinition과 실제 공에서 스프라이트를 찾지 못했을 때 " +
        "사용할 기본 공 스프라이트입니다."
    )]
    [SerializeField]
    private Sprite fallbackSprite;

    [Header("Scale")]
    [SerializeField, Min(0.1f)]
    private float normalScale = 1f;

    [Tooltip(
        "맨 아래 NEXT 슬롯에 있는 공의 크기입니다. " +
        "고정 배경으로 강조한다면 1을 권장합니다."
    )]
    [SerializeField, Min(0.1f)]
    private float nextBallScale = 1f;

    private RectTransform rectTransform;
    private Sequence activeSequence;

    private Vector3 targetLocalPosition;
    private Vector3 targetLocalScale;

    private Vector3 positionVelocity;
    private Vector3 scaleVelocity;

    private float followSmoothTime = 0.1f;
    private float followMaxSpeed = 2500f;

    private bool isFollowingTarget;
    private bool hasStar;
    private bool isStarVisible = true;

    public Ball BoundBall
    {
        get;
        private set;
    }

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void LateUpdate()
    {
        UpdateFlowMovement();
    }

    private void OnValidate()
    {
        normalScale =
            Mathf.Max(
                normalScale,
                0.1f
            );

        nextBallScale =
            Mathf.Max(
                nextBallScale,
                0.1f
            );

        FindReferences();
    }

    private void FindReferences()
    {
        if (rectTransform == null)
        {
            rectTransform =
                GetComponent<RectTransform>();
        }

        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }
    }

    private void ValidateReferences()
    {
        if (ballIcon == null)
        {
            Debug.LogError(
                "NextBallQueueItemView: " +
                "Ball Icon이 연결되지 않았습니다.",
                this
            );
        }

        if (starText == null)
        {
            Debug.LogError(
                "NextBallQueueItemView: " +
                "Star Text가 연결되지 않았습니다.",
                this
            );
        }

        if (canvasGroup == null)
        {
            Debug.LogError(
                "NextBallQueueItemView: " +
                "Canvas Group을 찾지 못했습니다.",
                this
            );
        }
    }

    public void ConfigureFlow(
        float smoothTime,
        float maxSpeed)
    {
        followSmoothTime =
            Mathf.Max(
                smoothTime,
                0.01f
            );

        followMaxSpeed =
            Mathf.Max(
                maxSpeed,
                1f
            );
    }

    public void Bind(Ball ball)
    {
        BoundBall = ball;

        RefreshBallIcon(ball);
        RefreshStarText(ball);
    }

    private void RefreshBallIcon(Ball ball)
    {
        if (ballIcon == null)
        {
            return;
        }

        BallDefinition definition =
            ball != null
                ? ball.Definition
                : null;

        SpriteRenderer ballRenderer =
            ball != null
                ? ball.GetComponentInChildren<
                    SpriteRenderer
                >(true)
                : null;

        Sprite resolvedSprite = null;
        Color resolvedColor = Color.white;

        if (definition != null &&
            definition.Sprite != null)
        {
            resolvedSprite =
                definition.Sprite;
        }
        else if (ballRenderer != null &&
                 ballRenderer.sprite != null)
        {
            resolvedSprite =
                ballRenderer.sprite;
        }
        else
        {
            resolvedSprite =
                fallbackSprite;
        }

        if (definition != null)
        {
            resolvedColor =
                definition.Color;
        }
        else if (ballRenderer != null)
        {
            resolvedColor =
                ballRenderer.color;
        }

        ballIcon.sprite =
            resolvedSprite;

        ballIcon.color =
            resolvedColor;

        ballIcon.enabled =
            resolvedSprite != null;
    }

    private void RefreshStarText(Ball ball)
    {
        if (starText == null)
        {
            return;
        }

        int starCount =
            ball != null
                ? Mathf.Clamp(
                    (int)ball.StarGrade,
                    0,
                    3
                )
                : 0;

        hasStar =
            starCount > 0;

        starText.text =
            hasStar
                ? new string(
                    '★',
                    starCount
                )
                : string.Empty;

        ApplyStarVisibility();
    }

    public void SetStarVisible(bool visible)
    {
        isStarVisible = visible;

        ApplyStarVisibility();
    }

    private void ApplyStarVisibility()
    {
        if (starText == null)
        {
            return;
        }

        starText.gameObject.SetActive(
            hasStar &&
            isStarVisible
        );
    }

    public void SetImmediateState(
        Vector3 localPosition,
        bool isNextBall)
    {
        KillActiveTween();

        isFollowingTarget = true;

        targetLocalPosition =
            localPosition;

        targetLocalScale =
            Vector3.one *
            ResolveScale(isNextBall);

        rectTransform.localPosition =
            targetLocalPosition;

        rectTransform.localScale =
            targetLocalScale;

        positionVelocity =
            Vector3.zero;

        scaleVelocity =
            Vector3.zero;

        canvasGroup.alpha = 1f;
    }

    public void SetTargetState(
        Vector3 localPosition,
        bool isNextBall)
    {
        if (rectTransform == null)
        {
            return;
        }

        targetLocalPosition =
            localPosition;

        targetLocalScale =
            Vector3.one *
            ResolveScale(isNextBall);

        isFollowingTarget = true;

        canvasGroup.alpha = 1f;
    }

    public void PlayEnter(
        Vector3 startLocalPosition,
        Vector3 targetPosition,
        bool isNextBall)
    {
        KillActiveTween();

        rectTransform.localPosition =
            startLocalPosition;

        rectTransform.localScale =
            Vector3.one *
            normalScale;

        targetLocalPosition =
            targetPosition;

        targetLocalScale =
            Vector3.one *
            ResolveScale(isNextBall);

        positionVelocity =
            Vector3.zero;

        scaleVelocity =
            Vector3.zero;

        canvasGroup.alpha = 1f;

        isFollowingTarget = true;
    }

    public void PlayExit(
        Vector3 exitLocalPosition,
        float duration,
        Ease ease)
    {
        KillActiveTween();

        isFollowingTarget = false;

        positionVelocity =
            Vector3.zero;

        scaleVelocity =
            Vector3.zero;

        SetStarVisible(false);

        canvasGroup.alpha = 1f;

        duration =
            Mathf.Max(
                duration,
                0f
            );

        if (duration <= 0f)
        {
            rectTransform.localPosition =
                exitLocalPosition;

            Destroy(gameObject);
            return;
        }

        Sequence sequence =
            DOTween.Sequence();

        activeSequence =
            sequence;

        sequence.Append(
            rectTransform
                .DOLocalMove(
                    exitLocalPosition,
                    duration
                )
                .SetEase(ease)
        );

        sequence
            .SetLink(
                gameObject,
                LinkBehaviour.KillOnDestroy
            )
            .OnComplete(
                () =>
                {
                    activeSequence = null;

                    Destroy(gameObject);
                }
            );
    }

    public void DisposeImmediate()
    {
        KillActiveTween();

        Destroy(gameObject);
    }

    private void UpdateFlowMovement()
    {
        if (!isFollowingTarget ||
            rectTransform == null)
        {
            return;
        }

        float deltaTime =
            Time.deltaTime;

        if (deltaTime <= 0f)
        {
            return;
        }

        rectTransform.localPosition =
            Vector3.SmoothDamp(
                rectTransform.localPosition,
                targetLocalPosition,
                ref positionVelocity,
                followSmoothTime,
                followMaxSpeed,
                deltaTime
            );

        float scaleSmoothTime =
            Mathf.Max(
                followSmoothTime * 0.7f,
                0.01f
            );

        rectTransform.localScale =
            Vector3.SmoothDamp(
                rectTransform.localScale,
                targetLocalScale,
                ref scaleVelocity,
                scaleSmoothTime,
                followMaxSpeed,
                deltaTime
            );

        if (
            (
                rectTransform.localPosition -
                targetLocalPosition
            ).sqrMagnitude <= 0.01f
        )
        {
            rectTransform.localPosition =
                targetLocalPosition;

            positionVelocity =
                Vector3.zero;
        }

        if (
            (
                rectTransform.localScale -
                targetLocalScale
            ).sqrMagnitude <= 0.0001f
        )
        {
            rectTransform.localScale =
                targetLocalScale;

            scaleVelocity =
                Vector3.zero;
        }
    }

    private float ResolveScale(bool isNextBall)
    {
        return isNextBall
            ? nextBallScale
            : normalScale;
    }

    private void KillActiveTween()
    {
        if (activeSequence != null)
        {
            activeSequence.Kill(false);
            activeSequence = null;
        }

        if (rectTransform != null)
        {
            rectTransform.DOKill(false);
        }

        if (canvasGroup != null)
        {
            canvasGroup.DOKill(false);
        }
    }

    private void OnDestroy()
    {
        KillActiveTween();
    }
}