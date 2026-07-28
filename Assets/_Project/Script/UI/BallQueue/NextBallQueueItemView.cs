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
        "맨 아래의 다음 발사 공에 적용할 확대 배율입니다."
    )]
    [SerializeField, Min(0.1f)]
    private float nextBallScale = 1.05f;

    private RectTransform rectTransform;
    private Sequence activeSequence;

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
                normalScale
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
        Vector3 targetLocalPosition,
        bool isNextBall)
    {
        KillActiveTween();

        rectTransform.localPosition =
            targetLocalPosition;

        rectTransform.localScale =
            Vector3.one *
            ResolveScale(isNextBall);

        canvasGroup.alpha = 1f;
    }

    public void PlayMove(
        Vector3 targetLocalPosition,
        float duration,
        Ease ease,
        bool isNextBall)
    {
        KillActiveTween();

        duration =
            Mathf.Max(
                duration,
                0f
            );

        if (duration <= 0f)
        {
            SetImmediateState(
                targetLocalPosition,
                isNextBall
            );

            return;
        }

        Sequence sequence =
            DOTween.Sequence();

        activeSequence =
            sequence;

        sequence.Join(
            rectTransform
                .DOLocalMove(
                    targetLocalPosition,
                    duration
                )
                .SetEase(ease)
        );

        sequence.Join(
            rectTransform
                .DOScale(
                    ResolveScale(isNextBall),
                    duration
                )
                .SetEase(ease)
        );

        sequence.Join(
            DOTween.To(
                () => canvasGroup.alpha,
                value =>
                    canvasGroup.alpha =
                        value,
                1f,
                duration
            )
        );

        sequence.OnComplete(
            () =>
            {
                if (activeSequence ==
                    sequence)
                {
                    activeSequence = null;
                }
            }
        );
    }

    public void PlayEnter(
        Vector3 startLocalPosition,
        Vector3 targetLocalPosition,
        float duration,
        float delay,
        Ease ease,
        bool isNextBall)
    {
        KillActiveTween();

        rectTransform.localPosition =
            startLocalPosition;

        rectTransform.localScale =
            Vector3.one * 0.85f;

        canvasGroup.alpha = 0f;

        Sequence sequence =
            DOTween.Sequence();

        activeSequence =
            sequence;

        if (delay > 0f)
        {
            sequence.AppendInterval(delay);
        }

        sequence.Join(
            rectTransform
                .DOLocalMove(
                    targetLocalPosition,
                    duration
                )
                .SetEase(ease)
        );

        sequence.Join(
            rectTransform
                .DOScale(
                    ResolveScale(isNextBall),
                    duration
                )
                .SetEase(ease)
        );

        sequence.Join(
            DOTween.To(
                () => canvasGroup.alpha,
                value =>
                    canvasGroup.alpha =
                        value,
                1f,
                duration
            )
        );

        sequence.OnComplete(
            () =>
            {
                if (activeSequence ==
                    sequence)
                {
                    activeSequence = null;
                }
            }
        );
    }

    public void PlayExit(
        Vector3 exitLocalPosition,
        float duration,
        float exitScale,
        Ease ease)
    {
        KillActiveTween();
        SetStarVisible(false);

        Sequence sequence =
            DOTween.Sequence();

        activeSequence =
            sequence;

        sequence.Join(
            rectTransform
                .DOLocalMove(
                    exitLocalPosition,
                    duration
                )
                .SetEase(ease)
        );

        sequence.Join(
            rectTransform
                .DOScale(
                    Mathf.Max(
                        exitScale,
                        0f
                    ),
                    duration
                )
                .SetEase(ease)
        );

        sequence.Join(
            DOTween.To(
                () => canvasGroup.alpha,
                value =>
                    canvasGroup.alpha =
                        value,
                0f,
                duration
            )
        );

        sequence.OnComplete(
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