using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class TeleportPortalController : MonoBehaviour
{
    [Header("Exit Safety")]
    [SerializeField, Min(0f)] private float exitPadding = 0.025f;
    [SerializeField, Min(0f)] private float maximumExitCorrection = 0.08f;
    [SerializeField, Min(0.001f)] private float exitCorrectionStep = 0.01f;
    [SerializeField] private LayerMask blockingLayers = ~0;
    [SerializeField] private bool warnOnceWhenExitBlocked = true;

    private readonly Collider2D[] overlapResults = new Collider2D[32];
    private BoxCollider2D triggerCollider;
    private TeleportPortalController partner;
    private bool blockedWarningIssued;

    public TeleportPortalController Partner => partner;
    public bool IsLinked => partner != null && partner != this;

    public event Action<Ball> BallEntered;
    public event Action<Ball> BallExited;

    private void Awake()
    {
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        NormalizeSettings();
    }

    private void OnValidate()
    {
        NormalizeSettings();

        BoxCollider2D portalCollider = GetComponent<BoxCollider2D>();
        if (portalCollider != null)
        {
            portalCollider.isTrigger = true;
        }
    }

    public void Link(TeleportPortalController other)
    {
        partner = other != this ? other : null;
        blockedWarningIssued = false;
    }

    public void Unlink()
    {
        partner = null;
        blockedWarningIssued = false;
    }

    public bool Overlaps(Collider2D other)
    {
        if (triggerCollider == null || other == null ||
            !triggerCollider.enabled || !other.enabled)
        {
            return false;
        }

        return triggerCollider.Distance(other).isOverlapped;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsLinked || other == null)
        {
            return;
        }

        Ball ball = other.GetComponentInParent<Ball>();
        if (ball == null ||
            !ball.IsMoving ||
            other != ball.PhysicsCollider)
        {
            return;
        }

        BallTeleportState state = ball.GetComponent<BallTeleportState>();
        if (state == null)
        {
            state = ball.gameObject.AddComponent<BallTeleportState>();
        }

        if (!state.CanEnter(this))
        {
            return;
        }

        Vector2 preservedVelocity = ball.Velocity;
        if (preservedVelocity.sqrMagnitude <= 0.0001f ||
            !partner.TryResolveExit(ball, preservedVelocity.normalized, out Vector2 exitPosition))
        {
            WarnBlockedExit(ball);
            return;
        }

        if (!ball.TryApplyTeleport(exitPosition, preservedVelocity))
        {
            return;
        }

        state.LockUntilExited(partner);
        BallEntered?.Invoke(ball);
        partner.BallExited?.Invoke(ball);
    }

    private bool TryResolveExit(
        Ball ball,
        Vector2 direction,
        out Vector2 exitPosition)
    {
        exitPosition = transform.position;

        if (triggerCollider == null || ball == null || direction.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        CircleCollider2D ballCollider = ball.PhysicsCollider;
        if (ballCollider == null)
        {
            return false;
        }

        float ballRadius = ResolveWorldRadius(ballCollider);
        Vector2 center = triggerCollider.bounds.center;
        Vector2 surface = triggerCollider.ClosestPoint(center + direction * 1000f);
        Vector2 baseExit = surface + direction * (ballRadius + exitPadding);
        int correctionCount = Mathf.CeilToInt(maximumExitCorrection / exitCorrectionStep);

        for (int i = 0; i <= correctionCount; i++)
        {
            float correction = Mathf.Min(i * exitCorrectionStep, maximumExitCorrection);
            Vector2 candidate = baseExit + direction * correction;

            if (CanPlaceBall(ball, candidate, ballRadius))
            {
                exitPosition = candidate;
                return true;
            }
        }

        return false;
    }

    private bool CanPlaceBall(Ball ball, Vector2 position, float radius)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(blockingLayers);
        filter.useTriggers = false;

        int count = Physics2D.OverlapCircle(position, radius, filter, overlapResults);
        Transform ballRoot = ball.transform;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = overlapResults[i];
            overlapResults[i] = null;

            if (hit == null || hit.transform == ballRoot || hit.transform.IsChildOf(ballRoot))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private float ResolveWorldRadius(CircleCollider2D collider)
    {
        Vector3 scale = collider.transform.lossyScale;
        return collider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
    }

    private void WarnBlockedExit(Ball ball)
    {
        if (warnOnceWhenExitBlocked && blockedWarningIssued)
        {
            return;
        }

        blockedWarningIssued = true;
        Debug.LogWarning($"TeleportPortalController: {name} 출구가 막혀 {ball.name} 텔레포트를 취소했습니다.", this);
    }

    private void NormalizeSettings()
    {
        exitPadding = Mathf.Max(exitPadding, 0f);
        maximumExitCorrection = Mathf.Max(maximumExitCorrection, 0f);
        exitCorrectionStep = Mathf.Max(exitCorrectionStep, 0.001f);
    }

    private void OnDisable()
    {
        Unlink();
    }
}
