using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class TeleportPortalController : MonoBehaviour
{
    private const float ExitDirectionSearchStep = 15f;

    [Header("Portal Size")]
    [SerializeField, Range(0.1f, 1f)] private float portalSizeRatio = 0.5f;

    [Header("Exit Safety")]
    [SerializeField, Min(0f)] private float exitPadding = 0.025f;
    [SerializeField, Min(0f)] private float maximumExitCorrection = 0.08f;
    [SerializeField, Min(0.001f)] private float exitCorrectionStep = 0.01f;
    [SerializeField] private LayerMask blockingLayers = ~0;
    [SerializeField] private bool warnOnceWhenExitBlocked = true;

    private readonly Collider2D[] overlapResults = new Collider2D[32];
    private Block block;
    private BoxCollider2D triggerCollider;
    private TeleportPortalController partner;
    private bool blockedWarningIssued;

    public TeleportPortalController Partner => partner;
    public bool IsLinked => partner != null && partner != this;

    public event Action<Ball> BallEntered;
    public event Action<Ball> BallExited;

    private void Awake()
    {
        block = GetComponent<Block>();
        triggerCollider = GetComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        NormalizeSettings();
        RefreshTriggerSize();
    }

    private void OnEnable()
    {
        if (block == null)
        {
            block = GetComponent<Block>();
        }

        block.LayoutChanged += HandleBlockLayoutChanged;
        RefreshTriggerSize();
    }

    private void OnValidate()
    {
        NormalizeSettings();

        BoxCollider2D portalCollider = GetComponent<BoxCollider2D>();
        if (portalCollider != null)
        {
            portalCollider.isTrigger = true;
            portalCollider.size = Vector2.one * portalSizeRatio;
            portalCollider.offset = Vector2.zero;
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
        if (preservedVelocity.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float speed = preservedVelocity.magnitude;
        partner.TryResolveExit(
            preservedVelocity.normalized,
            ResolveWorldRadius(ball.PhysicsCollider),
            ball.transform,
            out Vector2 exitPosition,
            out Vector2 exitDirection);

        if (!ball.TryApplyTeleport(exitPosition, exitDirection * speed))
        {
            return;
        }

        state.RecordSuccessfulTeleport();
        state.LockUntilExited(partner);
        BallEntered?.Invoke(ball);
        partner.BallExited?.Invoke(ball);
    }

    public bool TryResolvePredictedExit(
        Vector2 direction,
        float ballRadius,
        out Vector2 exitPosition,
        out Vector2 exitDirection)
    {
        return TryResolveExit(
            direction,
            ballRadius,
            null,
            out exitPosition,
            out exitDirection);
    }

    private bool TryResolveExit(
        Vector2 direction,
        float ballRadius,
        Transform ignoredRoot,
        out Vector2 exitPosition,
        out Vector2 exitDirection)
    {
        exitPosition = transform.position;
        exitDirection = direction.normalized;

        if (triggerCollider == null ||
            direction.sqrMagnitude <= 0.0001f ||
            ballRadius <= 0f)
        {
            return false;
        }

        direction.Normalize();

        if (TryResolveExitInDirection(
                direction,
                ballRadius,
                ignoredRoot,
                out exitPosition))
        {
            exitDirection = direction;
            return true;
        }

        int searchStepCount = Mathf.CeilToInt(180f / ExitDirectionSearchStep);
        for (int i = 1; i <= searchStepCount; i++)
        {
            float angle = ExitDirectionSearchStep * i;

            Vector2 counterClockwise = Rotate(direction, angle);
            if (TryResolveExitInDirection(
                    counterClockwise,
                    ballRadius,
                    ignoredRoot,
                    out exitPosition))
            {
                exitDirection = counterClockwise;
                return true;
            }

            Vector2 clockwise = Rotate(direction, -angle);
            if (TryResolveExitInDirection(
                    clockwise,
                    ballRadius,
                    ignoredRoot,
                    out exitPosition))
            {
                exitDirection = clockwise;
                return true;
            }
        }

        // The portal occupies an otherwise empty grid cell. Falling back to its
        // center guarantees that a linked portal never cancels solely because
        // every outward candidate is temporarily blocked.
        exitPosition = triggerCollider.bounds.center;
        exitDirection = direction;
        return true;
    }

    private bool TryResolveExitInDirection(
        Vector2 direction,
        float ballRadius,
        Transform ignoredRoot,
        out Vector2 exitPosition)
    {
        exitPosition = triggerCollider.bounds.center;
        Vector2 center = triggerCollider.bounds.center;
        Vector2 surface = triggerCollider.ClosestPoint(center + direction * 1000f);
        Vector2 baseExit = surface + direction * (ballRadius + exitPadding);
        int correctionCount = Mathf.CeilToInt(maximumExitCorrection / exitCorrectionStep);

        for (int i = 0; i <= correctionCount; i++)
        {
            float correction = Mathf.Min(i * exitCorrectionStep, maximumExitCorrection);
            Vector2 candidate = baseExit + direction * correction;

            if (CanPlaceBall(ignoredRoot, candidate, ballRadius))
            {
                exitPosition = candidate;
                return true;
            }
        }

        return false;
    }

    private bool CanPlaceBall(Transform ignoredRoot, Vector2 position, float radius)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(blockingLayers);
        filter.useTriggers = false;

        int count = Physics2D.OverlapCircle(position, radius, filter, overlapResults);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = overlapResults[i];
            overlapResults[i] = null;

            if (hit == null ||
                (ignoredRoot != null &&
                 (hit.transform == ignoredRoot || hit.transform.IsChildOf(ignoredRoot))))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private static Vector2 Rotate(Vector2 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cosine = Mathf.Cos(radians);
        float sine = Mathf.Sin(radians);

        return new Vector2(
            direction.x * cosine - direction.y * sine,
            direction.x * sine + direction.y * cosine).normalized;
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
        portalSizeRatio = Mathf.Clamp(portalSizeRatio, 0.1f, 1f);
        exitPadding = Mathf.Max(exitPadding, 0f);
        maximumExitCorrection = Mathf.Max(maximumExitCorrection, 0f);
        exitCorrectionStep = Mathf.Max(exitCorrectionStep, 0.001f);
    }

    private void OnDisable()
    {
        if (block != null)
        {
            block.LayoutChanged -= HandleBlockLayoutChanged;
        }

        Unlink();
    }

    private void HandleBlockLayoutChanged(Vector2Int _, float __)
    {
        RefreshTriggerSize();
    }

    private void RefreshTriggerSize()
    {
        if (block == null || triggerCollider == null)
        {
            return;
        }

        Vector2 desiredWorldSize = block.WorldSize * portalSizeRatio;
        Vector3 scale = triggerCollider.transform.lossyScale;

        triggerCollider.size = new Vector2(
            desiredWorldSize.x / Mathf.Max(Mathf.Abs(scale.x), 0.0001f),
            desiredWorldSize.y / Mathf.Max(Mathf.Abs(scale.y), 0.0001f));
        triggerCollider.offset = Vector2.zero;
    }
}
