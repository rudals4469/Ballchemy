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

    [Header("Combat")]
    [SerializeField, Min(1)]
    private int damage = 1;

    private static int activeMovingBallCount;

    private Rigidbody2D body;

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

        StopMovement();
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

    public void Launch(Vector2 direction)
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

        isMoving = true;
        body.simulated = true;

        body.linearVelocity =
            direction.normalized *
            moveSpeed;
    }

    public void ResetTo(Vector2 position)
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
                    activeMovingBallCount - 1,
                    0
                );

            MovingBallCountChanged?.Invoke(
                activeMovingBallCount
            );
        }

        isMoving = false;

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
        if (!isMoving)
        {
            return;
        }

        if (!collision.gameObject.TryGetComponent(
                out Block block
            ))
        {
            return;
        }

        if (!block.IsAlive)
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
                activeMovingBallCount - 1,
                0
            );

        MovingBallCountChanged?.Invoke(
            activeMovingBallCount
        );

        isMoving = false;
    }
}