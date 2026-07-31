using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(BallCombatController))]
[RequireComponent(typeof(BallLastStandBounce))]
public sealed class Ball :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BallCombatController combatController;

    [SerializeField]
    private BallLastStandBounce lastStandBounce;

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

    [Header("Last Stand Floor")]
    [Tooltip(
        "마지막 저지선 튀기 직전에 공을 ReturnZone 윗면보다 " +
        "조금 위에 배치하는 여유 거리입니다."
    )]
    [SerializeField, Min(0f)]
    private float returnZoneSurfacePadding = 0.02f;

    private static int activeMovingBallCount;

    private Rigidbody2D body;
    private CircleCollider2D circleCollider;

    private Vector2 lastPhysicsVelocity;

    private float runtimeSpeedMultiplier = 1f;

    private bool isMoving;
    private bool hasMovedUpward;
    private bool hasStartedDescending;

    private bool isPresentationVisible = true;

    private Renderer[]
        cachedRenderers =
            Array.Empty<Renderer>();

    private Collider2D[]
        cachedColliders =
            Array.Empty<Collider2D>();

    private readonly Dictionary<Renderer, bool>
        rendererEnabledStates =
            new Dictionary<Renderer, bool>();

    private readonly Dictionary<Collider2D, bool>
        colliderEnabledStates =
            new Dictionary<Collider2D, bool>();

    public static int ActiveMovingBallCount =>
        activeMovingBallCount;

    public BallCombatController CombatController =>
        combatController;

    public BallDefinition Definition =>
        combatController != null
            ? combatController.Definition
            : null;

    public BallTraitType TraitType =>
        combatController != null
            ? combatController.TraitType
            : BallTraitType.Basic;

    public BallStarGrade StarGrade =>
        combatController != null
            ? combatController.StarGrade
            : BallStarGrade.None;

    public int CurrentDamage =>
        combatController != null
            ? combatController.BaseDamage
            : 1;

    public bool IsMoving =>
        isMoving;

    public bool IsLastStandBouncing =>
        lastStandBounce != null &&
        lastStandBounce.IsActive;

    public bool HasStartedDescending =>
        hasStartedDescending;

    public bool IsPresentationVisible =>
        isPresentationVisible;

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
        CachePresentationComponents();

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

        bounceSeparationDistance =
            Mathf.Max(
                bounceSeparationDistance,
                0f
            );

        returnZoneSurfacePadding =
            Mathf.Max(
                returnZoneSurfacePadding,
                0f
            );

        FindReferences();
        EnsureHelpers();

        bounceResolver.Normalize();
        loopEscape.Normalize();

        lastStandBounce
            ?.NormalizeSettings();
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

        if (combatController == null)
        {
            combatController =
                GetComponent<
                    BallCombatController
                >();
        }

        if (lastStandBounce == null)
        {
            lastStandBounce =
                GetComponent<
                    BallLastStandBounce
                >();
        }

        if (combatController == null &&
            Application.isPlaying)
        {
            combatController =
                gameObject.AddComponent<
                    BallCombatController
                >();
        }

        if (lastStandBounce == null &&
            Application.isPlaying)
        {
            lastStandBounce =
                gameObject.AddComponent<
                    BallLastStandBounce
                >();
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

    private void CachePresentationComponents()
    {
        cachedRenderers =
            GetComponentsInChildren<
                Renderer
            >(
                true
            );

        cachedColliders =
            GetComponentsInChildren<
                Collider2D
            >(
                true
            );

        rendererEnabledStates.Clear();
        colliderEnabledStates.Clear();

        for (int i = 0;
             i < cachedRenderers.Length;
             i++)
        {
            Renderer targetRenderer =
                cachedRenderers[i];

            if (targetRenderer == null)
            {
                continue;
            }

            rendererEnabledStates[
                targetRenderer
            ] = targetRenderer.enabled;
        }

        for (int i = 0;
             i < cachedColliders.Length;
             i++)
        {
            Collider2D targetCollider =
                cachedColliders[i];

            if (targetCollider == null)
            {
                continue;
            }

            colliderEnabledStates[
                targetCollider
            ] = targetCollider.enabled;
        }
    }

    public void SetPresentationVisible(
        bool shouldShow)
    {
        if (isPresentationVisible ==
            shouldShow)
        {
            return;
        }

        if (!shouldShow)
        {
            StopMovement();
        }

        isPresentationVisible =
            shouldShow;

        EnsurePresentationCache();

        for (int i = 0;
             i < cachedRenderers.Length;
             i++)
        {
            Renderer targetRenderer =
                cachedRenderers[i];

            if (targetRenderer == null)
            {
                continue;
            }

            if (!shouldShow)
            {
                targetRenderer.enabled =
                    false;

                if (targetRenderer is
                    TrailRenderer trailRenderer)
                {
                    trailRenderer.Clear();
                }

                continue;
            }

            bool originalState =
                rendererEnabledStates
                    .TryGetValue(
                        targetRenderer,
                        out bool storedState
                    )
                    ? storedState
                    : true;

            targetRenderer.enabled =
                originalState;
        }

        for (int i = 0;
             i < cachedColliders.Length;
             i++)
        {
            Collider2D targetCollider =
                cachedColliders[i];

            if (targetCollider == null)
            {
                continue;
            }

            if (!shouldShow)
            {
                targetCollider.enabled =
                    false;

                continue;
            }

            bool originalState =
                colliderEnabledStates
                    .TryGetValue(
                        targetCollider,
                        out bool storedState
                    )
                    ? storedState
                    : true;

            targetCollider.enabled =
                originalState;
        }
    }

    private void EnsurePresentationCache()
    {
        if (cachedRenderers == null ||
            cachedRenderers.Length == 0 ||
            cachedColliders == null ||
            cachedColliders.Length == 0)
        {
            CachePresentationComponents();
        }
    }

    private void FixedUpdate()
    {
        if (!isMoving ||
            body == null)
        {
            return;
        }

        if (IsLastStandBouncing)
        {
            bool reachedMaximumDuration =
                lastStandBounce.TickFixed(
                    Time.fixedDeltaTime,
                    runtimeSpeedMultiplier
                );

            lastPhysicsVelocity =
                body.linearVelocity;

            UpdateVerticalMovementState(
                body.linearVelocity.y
            );

            if (reachedMaximumDuration)
            {
                CompleteLastStandReturn();
            }

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
        if (!isPresentationVisible)
        {
            return;
        }

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

        lastStandBounce?.Cancel();

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

        if (body != null)
        {
            body.position =
                position;
        }

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

        float previousMultiplier =
            runtimeSpeedMultiplier;

        runtimeSpeedMultiplier =
            multiplier;

        if (!isMoving ||
            body == null)
        {
            return;
        }

        if (IsLastStandBouncing)
        {
            float scaleRatio =
                runtimeSpeedMultiplier /
                Mathf.Max(
                    previousMultiplier,
                    0.1f
                );

            lastStandBounce.RescaleVelocity(
                scaleRatio
            );

            lastPhysicsVelocity =
                body.linearVelocity;

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

        if (IsLastStandBouncing)
        {
            CompleteLastStandReturn();

            return true;
        }

        StopMovement();

        Returned?.Invoke(
            this
        );

        return true;
    }

    public void NotifyBlockHitHandled()
    {
        BlockHitOccurred?.Invoke(
            this
        );
    }

    private void ClearPiercingSensorRuntime()
    {
        PiercingBallSensor piercingSensor =
            GetComponent<PiercingBallSensor>();

        piercingSensor
            ?.ClearRuntimeContacts();
    }

    private void StopMovement()
    {
        ClearPiercingSensorRuntime();

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
        lastStandBounce?.Cancel();

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

        BallHitResult hitResult =
            ResolveCombatHit(
                hitBlock,
                collision,
                incomingVelocity
            );

        if (IsLastStandBouncing)
        {
            ResolveLastStandCollision(
                collision,
                hitBlock
            );

            if (hitBlock != null &&
                hitResult.WasHandled)
            {
                NotifyBlockHitHandled();
            }

            return;
        }

        bool shouldResolveBounce =
            hitBlock == null ||
            hitResult.ShouldBounce;

        if (shouldResolveBounce)
        {
            ResolveBounce(
                collision,
                hitBlock,
                incomingVelocity
            );
        }

        if (hitBlock != null &&
            hitResult.WasHandled)
        {
            NotifyBlockHitHandled();
        }
    }

    private void ResolveLastStandCollision(
        Collision2D collision,
        Block hitBlock)
    {
        if (lastStandBounce == null)
        {
            return;
        }

        Vector2 reboundVelocity =
            lastStandBounce.ResolveCollision(
                collision,
                hitBlock != null,
                runtimeSpeedMultiplier
            );

        lastPhysicsVelocity =
            reboundVelocity;
    }

    private BallHitResult ResolveCombatHit(
        Block hitBlock,
        Collision2D collision,
        Vector2 incomingVelocity)
    {
        if (hitBlock == null ||
            !hitBlock.IsAlive)
        {
            return BallHitResult.NotHandled();
        }

        Vector2 hitPoint =
            body != null
                ? body.position
                : (Vector2)transform.position;

        if (collision != null &&
            collision.contactCount > 0)
        {
            hitPoint =
                collision
                    .GetContact(0)
                    .point;
        }

        if (combatController == null)
        {
            FindReferences();
        }

        if (combatController == null)
        {
            hitBlock.TakeDamage(
                1
            );

            return BallHitResult
                .HandledWithBounce();
        }

        return combatController.ResolveBlockHit(
            hitBlock,
            hitPoint,
            incomingVelocity
        );
    }

    private void ResolveBounce(
        Collision2D collision,
        Block hitBlock,
        Vector2 incomingVelocity)
    {
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

        if (!resolvedBounce)
        {
            return;
        }

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

        if (IsLastStandBouncing)
        {
            if (!lastStandBounce
                    .CanProcessReturnZoneEnter)
            {
                return;
            }

            Vector2 floorContactPosition =
                ResolveReturnZoneSurfacePosition(
                    other
                );

            ApplyPhysicsPosition(
                floorContactPosition
            );

            bool bouncedAgain =
                lastStandBounce
                    .TryContinueFloorBounce(
                        floorContactPosition,
                        runtimeSpeedMultiplier
                    );

            if (bouncedAgain)
            {
                lastPhysicsVelocity =
                    body.linearVelocity;

                return;
            }

            CompleteLastStandReturn();

            return;
        }

        BeginLastStandBounce(
            other
        );
    }

    private void OnTriggerExit2D(
        Collider2D other)
    {
        if (!IsLastStandBouncing)
        {
            return;
        }

        if (!other.CompareTag(
            "ReturnZone"
        ))
        {
            return;
        }

        lastStandBounce
            .NotifyReturnZoneExited();
    }

    private void BeginLastStandBounce(
        Collider2D returnZone)
    {
        FindReferences();

        if (lastStandBounce == null ||
            body == null)
        {
            CompleteImmediateReturn();

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

        Vector2 floorContactPosition =
            ResolveReturnZoneSurfacePosition(
                returnZone
            );

        ApplyPhysicsPosition(
            floorContactPosition
        );

        bool started =
            lastStandBounce.TryBegin(
                floorContactPosition,
                incomingVelocity,
                runtimeSpeedMultiplier
            );

        if (!started)
        {
            CompleteImmediateReturn();

            return;
        }

        lastPhysicsVelocity =
            body.linearVelocity;
    }

    private Vector2 ResolveReturnZoneSurfacePosition(
        Collider2D returnZone)
    {
        Vector2 resolvedPosition =
            body != null
                ? body.position
                : (Vector2)transform.position;

        if (returnZone == null)
        {
            return resolvedPosition;
        }

        float ballWorldRadius =
            ResolveBallWorldRadius();

        resolvedPosition.y =
            returnZone.bounds.max.y +
            ballWorldRadius +
            returnZoneSurfacePadding;

        return resolvedPosition;
    }

    private float ResolveBallWorldRadius()
    {
        if (circleCollider == null)
        {
            return 0f;
        }

        Vector3 colliderScale =
            circleCollider.transform.lossyScale;

        float largestScale =
            Mathf.Max(
                Mathf.Abs(
                    colliderScale.x
                ),
                Mathf.Abs(
                    colliderScale.y
                )
            );

        return circleCollider.radius *
               largestScale;
    }

    private void ApplyPhysicsPosition(
        Vector2 position)
    {
        if (body != null)
        {
            body.position =
                position;
        }

        transform.position =
            new Vector3(
                position.x,
                position.y,
                transform.position.z
            );
    }

    private void CompleteLastStandReturn()
    {
        if (!isMoving)
        {
            return;
        }

        Vector2 returnPosition =
            lastStandBounce != null
                ? lastStandBounce
                    .LastFloorContactPosition
                : (Vector2)transform.position;

        ApplyPhysicsPosition(
            returnPosition
        );

        StopMovement();

        Returned?.Invoke(
            this
        );
    }

    private void CompleteImmediateReturn()
    {
        StopMovement();

        Returned?.Invoke(
            this
        );
    }

    private void OnDisable()
    {
        ClearPiercingSensorRuntime();
    }

    private void OnDestroy()
    {
        ClearPiercingSensorRuntime();

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