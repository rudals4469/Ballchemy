using System;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public sealed class Ball :
    MonoBehaviour
{
    [Header("Data")]
    [Tooltip(
        "현재 공에 적용된 공 데이터입니다. " +
        "BallCollection에서 생성 시 설정합니다."
    )]
    [SerializeField]
    private BallDefinition definition;

    [Header("Visual")]
    [Tooltip(
        "공의 스프라이트와 색상을 표시하는 " +
        "SpriteRenderer입니다."
    )]
    [SerializeField]
    private SpriteRenderer visualRenderer;

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

    [Tooltip(
        "충돌 직후 공을 충돌면 바깥으로 밀어내는 거리입니다. " +
        "모서리에서 같은 충돌이 반복되는 현상을 방지합니다."
    )]
    [SerializeField, Min(0f)]
    private float bounceSeparationDistance = 0.02f;

    [Header("Combat Fallback")]
    [Tooltip(
        "BallDefinition이 연결되지 않았을 때만 " +
        "사용하는 임시 피해량입니다."
    )]
    [FormerlySerializedAs("damage")]
    [SerializeField, Min(1)]
    private int fallbackDamage = 1;

    private static int activeMovingBallCount;

    private Rigidbody2D body;
    private CircleCollider2D circleCollider;

    private Vector2 lastPhysicsVelocity;

    private float runtimeSpeedMultiplier = 1f;

    private bool isMoving;
    private bool hasMovedUpward;
    private bool hasStartedDescending;

    public static int ActiveMovingBallCount =>
        activeMovingBallCount;

    public BallDefinition Definition =>
        definition;

    public BallTraitType TraitType =>
        definition != null
            ? definition.TraitType
            : BallTraitType.Basic;

    public BallStarGrade StarGrade =>
        definition != null
            ? definition.StarGrade
            : BallStarGrade.None;

    public int CurrentDamage =>
        definition != null
            ? Mathf.Max(
                definition.BaseDamage,
                1
            )
            : fallbackDamage;

    public bool IsMoving =>
        isMoving;

    public bool HasStartedDescending =>
        hasStartedDescending;

    public float RuntimeSpeedMultiplier =>
        runtimeSpeedMultiplier;

    public float CurrentMoveSpeed =>
        moveSpeed *
        runtimeSpeedMultiplier;

    public Vector2 Velocity =>
        body != null
            ? body.linearVelocity
            : Vector2.zero;

    public float VerticalVelocity =>
        body != null
            ? body.linearVelocity.y
            : 0f;

    public event Action<Ball>
        Returned;

    public event Action<Ball, BallDefinition>
        DefinitionChanged;

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
        FindReferences();

        EnsureHelpers();
        ConfigureRigidbody();

        loopEscape.Initialize(
            GetInstanceID()
        );

        RefreshDefinitionVisual();

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

        bounceSeparationDistance =
            Mathf.Max(
                bounceSeparationDistance,
                0f
            );

        fallbackDamage =
            Mathf.Max(
                fallbackDamage,
                1
            );

        FindReferences();
        EnsureHelpers();

        bounceResolver.Normalize();
        loopEscape.Normalize();

        RefreshDefinitionVisual();
    }

    private void FindReferences()
    {
        if (body == null)
        {
            body =
                GetComponent<Rigidbody2D>();
        }

        if (circleCollider == null)
        {
            circleCollider =
                GetComponent<CircleCollider2D>();
        }

        if (visualRenderer == null)
        {
            visualRenderer =
                GetComponentInChildren<
                    SpriteRenderer
                >(
                    true
                );
        }
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

    public void ApplyDefinition(
        BallDefinition newDefinition)
    {
        definition =
            newDefinition;

        RefreshDefinitionVisual();

        DefinitionChanged?.Invoke(
            this,
            definition
        );
    }

    private void RefreshDefinitionVisual()
    {
        if (definition == null)
        {
            return;
        }

        if (visualRenderer == null)
        {
            visualRenderer =
                GetComponentInChildren<
                    SpriteRenderer
                >(
                    true
                );
        }

        if (visualRenderer == null)
        {
            return;
        }

        if (definition.Sprite != null)
        {
            visualRenderer.sprite =
                definition.Sprite;
        }

        visualRenderer.color =
            definition.Color;

        /*
         * SpriteRenderer가 공 루트의 자식일 때만
         * 비주얼 크기를 적용한다.
         *
         * 루트 오브젝트의 크기를 바꾸면
         * CircleCollider2D 크기까지 변할 수 있으므로
         * 루트 Renderer에는 자동 스케일을 적용하지 않는다.
         */
        if (visualRenderer.transform !=
            transform)
        {
            visualRenderer.transform
                .localScale =
                definition.VisualScale;
        }
    }

    private void FixedUpdate()
    {
        if (!isMoving ||
            body == null)
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
                CurrentMoveSpeed;

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
            CurrentMoveSpeed;

        body.linearVelocity =
            launchVelocity;

        lastPhysicsVelocity =
            launchVelocity;
    }

    public void ResetTo(
        Vector2 position)
    {
        StopMovement();

        runtimeSpeedMultiplier = 1f;

        body.position =
            position;

        transform.position =
            position;
    }

    public void SetRuntimeSpeedMultiplier(
        float multiplier)
    {
        multiplier =
            Mathf.Max(
                multiplier,
                0.1f
            );

        if (Mathf.Approximately(
                runtimeSpeedMultiplier,
                multiplier
            ))
        {
            return;
        }

        runtimeSpeedMultiplier =
            multiplier;

        if (!isMoving ||
            body == null)
        {
            return;
        }

        Vector2 currentVelocity =
            body.linearVelocity;

        if (currentVelocity.sqrMagnitude <=
            0.0001f)
        {
            return;
        }

        Vector2 adjustedVelocity =
            currentVelocity.normalized *
            CurrentMoveSpeed;

        body.linearVelocity =
            adjustedVelocity;

        lastPhysicsVelocity =
            adjustedVelocity;
    }

    public bool ForceReturn()
    {
        if (!isMoving)
        {
            return false;
        }

        StopMovement();

        Returned?.Invoke(
            this
        );

        return true;
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
                CurrentMoveSpeed,
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
                    CurrentMoveSpeed,
                    out Vector2 escapedVelocity
                );

            Vector2 finalVelocity =
                escapedLoop
                    ? escapedVelocity
                    : outgoingVelocity;

            SeparateFromSurface(
                resolvedNormal
            );

            body.linearVelocity =
                finalVelocity;

            lastPhysicsVelocity =
                finalVelocity;
        }

        HandleBlockHit(
            hitBlock
        );
    }

    private void SeparateFromSurface(
        Vector2 surfaceNormal)
    {
        if (body == null ||
            bounceSeparationDistance <= 0f ||
            surfaceNormal.sqrMagnitude <=
            0.0001f)
        {
            return;
        }

        Vector2 separationDirection =
            surfaceNormal.normalized;

        body.position +=
            separationDirection *
            bounceSeparationDistance;
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
            CurrentDamage
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