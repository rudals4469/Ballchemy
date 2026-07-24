using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BallCollection))]
public sealed class BallGatherAnimator :
    MonoBehaviour
{
    private sealed class GatherBallState
    {
        public Ball Ball;
        public Rigidbody2D Rigidbody;
        public bool WasSimulated;
    }

    [Header("References")]
    [SerializeField]
    private BallCollection ballCollection;

    [Header("Gather Animation")]
    [SerializeField, Min(0f)]
    private float gatherDuration = 0.35f;

    [Tooltip(
        "공마다 모이기 시작하는 시간에 " +
        "조금씩 차이를 줍니다."
    )]
    [SerializeField, Min(0f)]
    private float maximumRandomStartDelay = 0.06f;

    [SerializeField]
    private Ease gatherEase =
        Ease.InOutSine;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog = true;

    private readonly List<GatherBallState>
        activeBallStates =
            new List<GatherBallState>();

    private Sequence currentSequence;

    public bool IsGathering =>
        currentSequence != null &&
        currentSequence.IsActive();

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
        ValidateReferences();
    }

    private void OnValidate()
    {
        FindReferences();
        NormalizeSettings();
    }

    private void FindReferences()
    {
        if (ballCollection == null)
        {
            ballCollection =
                GetComponent<BallCollection>();
        }
    }

    private void NormalizeSettings()
    {
        gatherDuration =
            Mathf.Max(
                gatherDuration,
                0f
            );

        maximumRandomStartDelay =
            Mathf.Max(
                maximumRandomStartDelay,
                0f
            );
    }

    private void ValidateReferences()
    {
        if (ballCollection == null)
        {
            Debug.LogError(
                "BallGatherAnimator: " +
                "BallCollection을 찾지 못했습니다.",
                this
            );
        }
    }

    public IEnumerator PlayGatherRoutine(
        Vector2 targetPosition)
    {
        if (ballCollection == null)
        {
            yield break;
        }

        StopCurrentAnimation(
            false
        );

        List<Ball> balls =
            ballCollection.CreateSnapshot();

        if (balls.Count == 0)
        {
            yield break;
        }

        if (gatherDuration <= 0f)
        {
            ballCollection.AlignAll(
                targetPosition
            );

            yield break;
        }

        PrepareBallsForGather(
            balls
        );

        if (activeBallStates.Count == 0)
        {
            yield break;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "BallGatherAnimator: " +
                $"공 {activeBallStates.Count}개를 " +
                $"{targetPosition}으로 모으기 시작",
                this
            );
        }

        Sequence sequence =
            DOTween.Sequence();

        currentSequence =
            sequence;

        int insertedTweenCount = 0;

        for (int i = 0;
             i < activeBallStates.Count;
             i++)
        {
            GatherBallState state =
                activeBallStates[i];

            if (state == null ||
                state.Ball == null)
            {
                continue;
            }

            state.Ball.transform.DOKill(
                false
            );

            if (state.Rigidbody != null)
            {
                DOTween.Kill(
                    state.Rigidbody,
                    false
                );
            }

            float startDelay =
                maximumRandomStartDelay > 0f
                    ? Random.Range(
                        0f,
                        maximumRandomStartDelay
                    )
                    : 0f;

            Tween moveTween =
                CreateMoveTween(
                    state,
                    targetPosition
                );

            if (moveTween == null)
            {
                continue;
            }

            sequence.Insert(
                startDelay,
                moveTween
            );

            insertedTweenCount++;
        }

        if (insertedTweenCount <= 0)
        {
            RestoreBallPhysics();

            ballCollection.AlignAll(
                targetPosition
            );

            sequence.Kill(
                false
            );

            currentSequence = null;

            yield break;
        }

        sequence
            .SetLink(
                gameObject,
                LinkBehaviour.KillOnDestroy
            )
            .Play();

        yield return sequence
            .WaitForCompletion();

        /*
         * Tween이 끝난 뒤 마지막 미세 오차를 제거하고
         * Ball의 정지 상태와 대기 위치를 확정한다.
         */
        ballCollection.AlignAll(
            targetPosition
        );

        RestoreBallPhysics();

        if (currentSequence ==
            sequence)
        {
            currentSequence = null;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "BallGatherAnimator: " +
                "공 모으기 애니메이션 완료",
                this
            );
        }
    }

    private Tween CreateMoveTween(
        GatherBallState state,
        Vector2 targetPosition)
    {
        if (state == null ||
            state.Ball == null)
        {
            return null;
        }

        Vector2 currentPosition =
            GetCurrentPosition(
                state
            );

        /*
         * Transform.DOMove에만 맡기지 않고,
         * DOTween의 매 프레임 값으로
         * Rigidbody2D.position과 Transform.position을
         * 직접 함께 갱신한다.
         */
        Tween tween =
            DOTween.To(
                    () => currentPosition,
                    updatedPosition =>
                    {
                        currentPosition =
                            updatedPosition;

                        ApplyPosition(
                            state,
                            updatedPosition
                        );
                    },
                    targetPosition,
                    gatherDuration
                )
                .SetEase(
                    gatherEase
                )
                .SetTarget(
                    state.Ball
                );

        return tween;
    }

    private Vector2 GetCurrentPosition(
        GatherBallState state)
    {
        if (state.Rigidbody != null)
        {
            return state.Rigidbody.position;
        }

        return state.Ball.transform.position;
    }

    private void ApplyPosition(
        GatherBallState state,
        Vector2 position)
    {
        if (state == null ||
            state.Ball == null)
        {
            return;
        }

        if (state.Rigidbody != null)
        {
            state.Rigidbody.position =
                position;
        }

        Transform ballTransform =
            state.Ball.transform;

        Vector3 currentTransformPosition =
            ballTransform.position;

        ballTransform.position =
            new Vector3(
                position.x,
                position.y,
                currentTransformPosition.z
            );
    }

    private void PrepareBallsForGather(
        IReadOnlyList<Ball> balls)
    {
        activeBallStates.Clear();

        for (int i = 0;
             i < balls.Count;
             i++)
        {
            Ball ball =
                balls[i];

            if (ball == null)
            {
                continue;
            }

            Rigidbody2D ballRigidbody =
                ball.GetComponent<Rigidbody2D>();

            GatherBallState state =
                new GatherBallState
                {
                    Ball = ball,
                    Rigidbody = ballRigidbody,
                    WasSimulated =
                        ballRigidbody != null &&
                        ballRigidbody.simulated
                };

            if (ballRigidbody != null)
            {
                ballRigidbody.linearVelocity =
                    Vector2.zero;

                ballRigidbody.angularVelocity =
                    0f;

                /*
                 * 모으는 동안 충돌 및 물리 이동을 막는다.
                 * 위치는 DOTween setter에서 직접 갱신한다.
                 */
                ballRigidbody.simulated =
                    false;
            }

            activeBallStates.Add(
                state
            );
        }
    }

    private void RestoreBallPhysics()
    {
        for (int i = 0;
             i < activeBallStates.Count;
             i++)
        {
            GatherBallState state =
                activeBallStates[i];

            if (state == null ||
                state.Rigidbody == null)
            {
                continue;
            }

            state.Rigidbody.linearVelocity =
                Vector2.zero;

            state.Rigidbody.angularVelocity =
                0f;

            state.Rigidbody.simulated =
                state.WasSimulated;
        }

        activeBallStates.Clear();
    }

    public void StopCurrentAnimation(
        bool complete)
    {
        if (currentSequence != null &&
            currentSequence.IsActive())
        {
            currentSequence.Kill(
                complete
            );
        }

        currentSequence = null;

        RestoreBallPhysics();
    }

    private void OnDisable()
    {
        StopCurrentAnimation(
            false
        );
    }

    private void OnDestroy()
    {
        StopCurrentAnimation(
            false
        );
    }
}