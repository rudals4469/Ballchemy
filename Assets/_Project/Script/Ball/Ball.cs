using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public sealed class Ball : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0.1f)]
    private float moveSpeed = 12f;

    [Tooltip(
        "상승과 하강을 구분할 때 사용하는 " +
        "최소 수직 속도입니다."
    )]
    [SerializeField, Min(0f)]
    private float verticalDirectionThreshold = 0.05f;

    [Header("Bounce")]
    [SerializeField]
    private BallBounceResolver bounceResolver =
        new BallBounceResolver();

    [SerializeField]
    private BallLoopEscape loopEscape =
        new BallLoopEscape();

    [Header("Combat")]
    [SerializeField, Min(1)]
    private int damage = 1;

    private static int activeMovingBallCount;

    private Rigidbody2D body;
    private CircleCollider2D circleCollider;

    private Vector2 lastPhysicsVelocity;

    private bool isMoving;
    private bool hasMovedUpward;
    private bool hasStartedDescending;

    public static int ActiveMovingBallCount =>
        activeMovingBallCount;

    public bool IsMoving =>
        isMoving;

    public bool HasStartedDescending =>
        hasStartedDescending;

    public Vector2 Velocity =>
        body != null
            ? body.linearVelocity
            : Vector2.zero;

    public float VerticalVelocity =>
        body != null
            ? body.linearVelocity.y
            : 0f;

    public event Action<Ball> Returned;

    public static event Action<Ball>
        BlockHitOccurred;

    public static event Action<int>
        MovingBallCountChanged;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration
    )]
    private static void ResetStaticState()
    {
        activeMovingBallCount = 0;
        BlockHitOccurred = null;
        MovingBallCountChanged = null;
    }

    private void Awake()
    {
        body =
            GetComponent<Rigidbody2D>();

        circleCollider =
            GetComponent<CircleCollider2D>();

        EnsureHelpers();
        ConfigureRigidbody();

        loopEscape.Initialize(
            GetInstanceID()
        );

        StopMovement();
    }

    private void OnValidate()
    {
        moveSpeed =
            Mathf.Max(
                moveSpeed,
                0.1f
            );

        verticalDirectionThreshold =
            Mathf.Max(
                verticalDirectionThreshold,
                0f
            );

        damage =
            Mathf.Max(
                damage,
                1
            );

        EnsureHelpers();

        bounceResolver.Normalize();
        loopEscape.Normalize();
    }

    private void EnsureHelpers()
    {
        if (bounceResolver == null)
        {
            bounceResolver =
                new BallBounceResolver();
        }

        if (loopEscape == null)
        {
            loopEscape =
                new BallLoopEscape();
        }
    }

    private void ConfigureRigidbody()
    {
        if (body == null)
        {
            return;
        }

        body.gravityScale = 0f;

        body.freezeRotation = true;

        body.collisionDetectionMode =
            CollisionDetectionMode2D.Continuous;

        body.interpolation =
            RigidbodyInterpolation2D.Interpolate;
    }

    private void FixedUpdate()
    {
        if (!isMoving)
        {
            return;
        }

        Vector2 currentVelocity =
            body.linearVelocity;

        if (currentVelocity.sqrMagnitude >
            0.01f)
        {
            currentVelocity =
                currentVelocity.normalized *
                moveSpeed;

            body.linearVelocity =
                currentVelocity;

            lastPhysicsVelocity =
                currentVelocity;
        }

        UpdateVerticalMovementState(
            currentVelocity.y
        );
    }

    private void UpdateVerticalMovementState(
        float verticalVelocity)
    {
        if (verticalVelocity >
            verticalDirectionThreshold)
        {
            hasMovedUpward = true;

            return;
        }

        if (!hasMovedUpward)
        {
            return;
        }

        if (verticalVelocity >=
            -verticalDirectionThreshold)
        {
            return;
        }

        hasStartedDescending = true;
    }

    public void Launch(
        Vector2 direction)
    {
        if (direction.sqrMagnitude <=
            0.001f)
        {
            return;
        }

        if (!isMoving)
        {
            activeMovingBallCount++;

            MovingBallCountChanged?.Invoke(
                activeMovingBallCount
            );
        }

        hasMovedUpward = false;
        hasStartedDescending = false;

        loopEscape.ResetRuntime();

        isMoving = true;
        body.simulated = true;

        Vector2 launchVelocity =
            direction.normalized *
            moveSpeed;

        body.linearVelocity =
            launchVelocity;

        lastPhysicsVelocity =
            launchVelocity;
    }

    public void ResetTo(
        Vector2 position)
    {
        StopMovement();

        body.position =
            position;

        transform.position =
            position;
    }

    private void StopMovement()
    {
        if (isMoving)
        {
            activeMovingBallCount =
                Mathf.Max(
                    activeMovingBallCount -
                    1,
                    0
                );

            MovingBallCountChanged?.Invoke(
                activeMovingBallCount
            );
        }

        isMoving = false;

        hasMovedUpward = false;
        hasStartedDescending = false;

        lastPhysicsVelocity =
            Vector2.zero;

        loopEscape?.ResetRuntime();

        if (body == null)
        {
            return;
        }

        body.linearVelocity =
            Vector2.zero;

        body.angularVelocity =
            0f;

        body.simulated =
            false;
    }

    private void OnCollisionEnter2D(
        Collision2D collision)
    {
        if (!isMoving ||
            body == null)
        {
            return;
        }

        Vector2 incomingVelocity =
            lastPhysicsVelocity;

        if (incomingVelocity.sqrMagnitude <=
            0.0001f)
        {
            incomingVelocity =
                body.linearVelocity;
        }

        Block hitBlock =
            FindHitBlock(
                collision
            );

        bool resolvedBounce =
            bounceResolver.TryResolve(
                collision,
                circleCollider,
                incomingVelocity,
                moveSpeed,
                out Vector2 outgoingVelocity,
                out Vector2 resolvedNormal,
                out Collider2D hitCollider
            );

        if (resolvedBounce)
        {
            if (hitBlock == null &&
                hitCollider != null)
            {
                hitBlock =
                    hitCollider
                        .GetComponentInParent<Block>();
            }

            bool escapedLoop =
                loopEscape.TryEscape(
                    hitBlock,
                    resolvedNormal,
                    outgoingVelocity,
                    body.position,
                    moveSpeed,
                    out Vector2 escapedVelocity
                );

            body.linearVelocity =
                escapedLoop
                    ? escapedVelocity
                    : outgoingVelocity;

            lastPhysicsVelocity =
                body.linearVelocity;
        }

        HandleBlockHit(
            hitBlock
        );
    }

    private Block FindHitBlock(
        Collision2D collision)
    {
        if (collision == null)
        {
            return null;
        }

        Block hitBlock =
            collision.gameObject
                .GetComponent<Block>();

        if (hitBlock != null)
        {
            return hitBlock;
        }

        return collision.gameObject
            .GetComponentInParent<Block>();
    }

    private void HandleBlockHit(
        Block block)
    {
        if (block == null ||
            !block.IsAlive)
        {
            return;
        }

        block.TakeDamage(
            damage
        );

        BlockHitOccurred?.Invoke(
            this
        );
    }

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        if (!isMoving)
        {
            return;
        }

        if (!other.CompareTag(
                "ReturnZone"
            ))
        {
            return;
        }

        StopMovement();

        Returned?.Invoke(
            this
        );
    }

    private void OnDestroy()
    {
        if (!isMoving)
        {
            return;
        }

        activeMovingBallCount =
            Mathf.Max(
                activeMovingBallCount -
                1,
                0
            );

        MovingBallCountChanged?.Invoke(
            activeMovingBallCount
        );

        isMoving = false;
    }
}