using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BlockHitFeedback :
    MonoBehaviour
{
    [Header("References")]
    [Tooltip(
        "실제로 움직일 비주얼 부모입니다. " +
        "Collider가 있는 Block 루트는 연결하지 마세요."
    )]
    [SerializeField]
    private Transform visualRoot;

    [Header("Recoil Position")]
    [Tooltip(
        "공에 맞았을 때 뒤로 밀리는 거리입니다."
    )]
    [SerializeField, Min(0f)]
    private float recoilDistance = 0.1f;

    [Tooltip(
        "충돌 직후 뒤로 밀리는 시간입니다."
    )]
    [SerializeField, Min(0.01f)]
    private float pushDuration = 0.04f;

    [Tooltip(
        "뒤로 밀린 위치에서 잠깐 멈추는 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float recoilHoldDuration = 0.015f;

    [Tooltip(
        "밀린 위치에서 원래 자리로 돌아오는 시간입니다."
    )]
    [SerializeField, Min(0.01f)]
    private float returnDuration = 0.11f;

    [Header("Scale Feedback")]
    [SerializeField]
    private bool useScaleFeedback = true;

    [Tooltip(
        "맞는 순간 줄어드는 크기입니다. " +
        "0.04는 약 4%입니다."
    )]
    [SerializeField, Range(0f, 0.2f)]
    private float scaleAmount = 0.04f;

    [Tooltip(
        "크기 반응 전체 시간입니다."
    )]
    [SerializeField, Min(0.01f)]
    private float scaleDuration = 0.12f;

    private Vector3 originalLocalPosition;
    private Vector3 originalLocalScale;

    private Sequence positionSequence;
    private Sequence scaleSequence;

    private bool isInitialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnValidate()
    {
        recoilDistance =
            Mathf.Max(
                recoilDistance,
                0f
            );

        pushDuration =
            Mathf.Max(
                pushDuration,
                0.01f
            );

        recoilHoldDuration =
            Mathf.Max(
                recoilHoldDuration,
                0f
            );

        returnDuration =
            Mathf.Max(
                returnDuration,
                0.01f
            );

        scaleAmount =
            Mathf.Clamp(
                scaleAmount,
                0f,
                0.2f
            );

        scaleDuration =
            Mathf.Max(
                scaleDuration,
                0.01f
            );
    }

    private void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        if (visualRoot == null)
        {
            Transform foundVisualRoot =
                transform.Find(
                    "VisualRoot"
                );

            if (foundVisualRoot != null)
            {
                visualRoot =
                    foundVisualRoot;
            }
        }

        if (visualRoot == null)
        {
            Debug.LogWarning(
                "BlockHitFeedback: " +
                "Visual Root가 연결되지 않았습니다.",
                this
            );

            return;
        }

        if (visualRoot == transform)
        {
            Debug.LogError(
                "BlockHitFeedback: " +
                "Block 루트를 Visual Root로 사용하면 " +
                "Collider까지 움직이게 됩니다.",
                this
            );

            visualRoot = null;

            return;
        }

        originalLocalPosition =
            visualRoot.localPosition;

        originalLocalScale =
            visualRoot.localScale;

        isInitialized = true;
    }

    private void OnCollisionEnter2D(
        Collision2D collision)
    {
        if (!IsBallCollision(
                collision))
        {
            return;
        }

        Vector2 recoilDirection =
            CalculateRecoilDirection(
                collision
            );

        PlayHit(
            recoilDirection
        );
    }

    public void PlayHit(
        Vector2 worldRecoilDirection)
    {
        Initialize();

        if (!isInitialized ||
            visualRoot == null)
        {
            return;
        }

        if (worldRecoilDirection.sqrMagnitude <=
            0.0001f)
        {
            return;
        }

        StopCurrentFeedback();

        visualRoot.localPosition =
            originalLocalPosition;

        visualRoot.localScale =
            originalLocalScale;

        Vector3 localRecoilDirection =
            ConvertWorldDirectionToLocal(
                worldRecoilDirection.normalized
            );

        Vector3 recoilPosition =
            originalLocalPosition +
            localRecoilDirection *
            recoilDistance;

        PlayPositionFeedback(
            recoilPosition
        );

        if (useScaleFeedback &&
            scaleAmount > 0f)
        {
            PlayScaleFeedback();
        }
    }

    private void PlayPositionFeedback(
        Vector3 recoilPosition)
    {
        positionSequence =
            DOTween.Sequence();

        positionSequence
            .Append(
                visualRoot
                    .DOLocalMove(
                        recoilPosition,
                        pushDuration
                    )
                    .SetEase(
                        Ease.OutQuad
                    )
            );

        if (recoilHoldDuration > 0f)
        {
            positionSequence
                .AppendInterval(
                    recoilHoldDuration
                );
        }

        positionSequence
            .Append(
                visualRoot
                    .DOLocalMove(
                        originalLocalPosition,
                        returnDuration
                    )
                    .SetEase(
                        Ease.OutBack,
                        1.15f
                    )
            )
            .SetLink(
                gameObject,
                LinkBehaviour.KillOnDestroy
            )
            .OnComplete(
                RestorePosition
            );
    }

    private bool IsBallCollision(
        Collision2D collision)
    {
        if (collision == null)
        {
            return false;
        }

        if (collision.rigidbody != null &&
            collision.rigidbody
                .GetComponent<Ball>() != null)
        {
            return true;
        }

        return collision.gameObject
                   .GetComponentInParent<Ball>() !=
               null;
    }

    private Vector2 CalculateRecoilDirection(
        Collision2D collision)
    {
        if (collision.contactCount > 0)
        {
            ContactPoint2D contact =
                collision.GetContact(
                    0
                );

            Vector2 blockCenter =
                transform.position;

            Vector2 directionFromImpactToCenter =
                blockCenter -
                contact.point;

            if (directionFromImpactToCenter
                    .sqrMagnitude >
                0.0001f)
            {
                return directionFromImpactToCenter
                    .normalized;
            }
        }

        Vector2 relativeVelocity =
            collision.relativeVelocity;

        if (relativeVelocity.sqrMagnitude >
            0.0001f)
        {
            return relativeVelocity.normalized;
        }

        return Vector2.zero;
    }

    private Vector3 ConvertWorldDirectionToLocal(
        Vector2 worldDirection)
    {
        Vector3 direction =
            new Vector3(
                worldDirection.x,
                worldDirection.y,
                0f
            );

        Transform localReference =
            visualRoot.parent;

        if (localReference != null)
        {
            direction =
                localReference
                    .InverseTransformDirection(
                        direction
                    );
        }

        direction.z = 0f;

        if (direction.sqrMagnitude >
            0.0001f)
        {
            direction.Normalize();
        }

        return direction;
    }

    private void PlayScaleFeedback()
    {
        float shrinkMultiplier =
            1f -
            scaleAmount;

        Vector3 compressedScale =
            originalLocalScale *
            shrinkMultiplier;

        float compressDuration =
            scaleDuration *
            0.35f;

        float restoreDuration =
            scaleDuration *
            0.65f;

        scaleSequence =
            DOTween.Sequence();

        scaleSequence
            .Append(
                visualRoot
                    .DOScale(
                        compressedScale,
                        compressDuration
                    )
                    .SetEase(
                        Ease.OutQuad
                    )
            )
            .Append(
                visualRoot
                    .DOScale(
                        originalLocalScale,
                        restoreDuration
                    )
                    .SetEase(
                        Ease.OutBack,
                        1.1f
                    )
            )
            .SetLink(
                gameObject,
                LinkBehaviour.KillOnDestroy
            )
            .OnComplete(
                RestoreScale
            );
    }

    private void StopCurrentFeedback()
    {
        if (positionSequence != null &&
            positionSequence.IsActive())
        {
            positionSequence.Kill(
                false
            );
        }

        if (scaleSequence != null &&
            scaleSequence.IsActive())
        {
            scaleSequence.Kill(
                false
            );
        }

        positionSequence = null;
        scaleSequence = null;
    }

    private void RestorePosition()
    {
        if (visualRoot == null)
        {
            return;
        }

        visualRoot.localPosition =
            originalLocalPosition;

        positionSequence = null;
    }

    private void RestoreScale()
    {
        if (visualRoot == null)
        {
            return;
        }

        visualRoot.localScale =
            originalLocalScale;

        scaleSequence = null;
    }

    private void RestoreVisual()
    {
        if (visualRoot == null)
        {
            return;
        }

        visualRoot.localPosition =
            originalLocalPosition;

        visualRoot.localScale =
            originalLocalScale;
    }

    private void OnDisable()
    {
        StopCurrentFeedback();

        if (isInitialized)
        {
            RestoreVisual();
        }
    }

    private void OnDestroy()
    {
        StopCurrentFeedback();
    }
}