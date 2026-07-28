using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class BallLastStandBounce :
    MonoBehaviour
{
    [Header("Activation")]
    [Tooltip(
        "공이 ReturnZone에 처음 도착했을 때 " +
        "마지막 저지선 튀기 연출을 사용할지 결정합니다."
    )]
    [SerializeField]
    private bool enableLastStandBounce = true;

    [Header("Floor Bounce Count")]
    [Tooltip(
        "바닥에서 튀어 오르는 총 횟수입니다. " +
        "최초 바닥 접촉의 튀기도 횟수에 포함됩니다."
    )]
    [SerializeField, Min(1)]
    private int maximumFloorBounceCount = 3;

    [Tooltip(
        "바닥에 다시 닿을 때마다 튀어 오르는 힘이 " +
        "감소하는 비율입니다."
    )]
    [SerializeField, Range(0.1f, 1f)]
    private float floorBounceStrengthDecay = 0.72f;

    [Tooltip(
        "두 번째 바닥 튀기부터 적용되는 " +
        "최소 수직 속도입니다."
    )]
    [SerializeField, Min(0.1f)]
    private float minimumRepeatedVerticalSpeed = 4.5f;

    [Header("Initial Floor Bounce")]
    [Tooltip(
        "바닥에서 튀어 오를 때 적용되는 " +
        "최소 좌우 속도입니다."
    )]
    [SerializeField, Min(0f)]
    private float minimumHorizontalSpeed = 2.5f;

    [Tooltip(
        "바닥에서 튀어 오를 때 적용되는 " +
        "최대 좌우 속도입니다."
    )]
    [SerializeField, Min(0f)]
    private float maximumHorizontalSpeed = 5f;

    [Tooltip(
        "첫 번째 바닥 튀기의 최소 상승 속도입니다."
    )]
    [SerializeField, Min(0.1f)]
    private float minimumVerticalSpeed = 7.5f;

    [Tooltip(
        "첫 번째 바닥 튀기의 최대 상승 속도입니다."
    )]
    [SerializeField, Min(0.1f)]
    private float maximumVerticalSpeed = 9.5f;

    [Tooltip(
        "기존 진행 방향과 반대쪽으로 튀어 나갈 확률입니다."
    )]
    [SerializeField, Range(0f, 1f)]
    private float horizontalFlipChance = 0.3f;

    [Header("Last Stand Gravity")]
    [Tooltip(
        "마지막 저지선 상태에서 공에 적용되는 중력입니다."
    )]
    [SerializeField, Min(0.1f)]
    private float gravity = 22f;

    [Tooltip(
        "마지막 저지선 상태에서 허용되는 최대 낙하 속도입니다."
    )]
    [SerializeField, Min(0.1f)]
    private float maximumFallSpeed = 14f;

    [Header("Collision Rebound")]
    [Tooltip(
        "블록이나 벽에 충돌한 뒤 유지할 최소 속도입니다."
    )]
    [SerializeField, Min(0.1f)]
    private float minimumReboundSpeed = 5.5f;

    [Tooltip(
        "블록이나 벽에 충돌한 뒤 허용할 최대 속도입니다."
    )]
    [SerializeField, Min(0.1f)]
    private float maximumReboundSpeed = 11f;

    [Tooltip(
        "블록에 충돌할 때 반사 방향에 추가되는 " +
        "최대 무작위 각도입니다."
    )]
    [SerializeField, Range(0f, 30f)]
    private float blockDirectionJitter = 10f;

    [Tooltip(
        "벽에 충돌할 때 반사 방향에 추가되는 " +
        "최대 무작위 각도입니다."
    )]
    [SerializeField, Range(0f, 15f)]
    private float wallDirectionJitter = 3f;

    [Tooltip(
        "충돌 후 좌우 움직임이 완전히 사라지는 것을 " +
        "막기 위한 최소 좌우 속도입니다."
    )]
    [SerializeField, Min(0f)]
    private float minimumHorizontalReboundSpeed =
        1.25f;

    [Tooltip(
        "충돌면에 계속 붙어 있는 현상을 막기 위해 " +
        "충돌면 바깥으로 이동시키는 거리입니다."
    )]
    [SerializeField, Min(0f)]
    private float surfaceSeparationDistance = 0.025f;

    [Header("Return Safety")]
    [Tooltip(
        "첫 바닥 접촉 직후 다시 회수되는 것을 막는 " +
        "최소 체공 시간입니다."
    )]
    [SerializeField, Min(0f)]
    private float minimumAirTime = 0.15f;

    [Tooltip(
        "마지막 저지선 상태가 유지될 수 있는 최대 시간입니다. " +
        "시간을 넘기면 마지막 바닥 접촉 위치로 회수됩니다."
    )]
    [SerializeField, Min(0.1f)]
    private float maximumDuration = 2.4f;

    private Rigidbody2D body;

    private bool isActive;
    private bool hasExitedReturnZone;

    private int currentFloorBounceCount;

    private float elapsedTime;

    private Vector2 lastFloorContactPosition;

    public bool IsActive =>
        isActive;

    public bool CanBegin =>
        enableLastStandBounce &&
        !isActive;

    public bool CanProcessReturnZoneEnter =>
        isActive &&
        hasExitedReturnZone &&
        elapsedTime >= minimumAirTime;

    public int CurrentFloorBounceCount =>
        currentFloorBounceCount;

    public Vector2 LastFloorContactPosition =>
        lastFloorContactPosition;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
    }

    private void OnValidate()
    {
        FindReferences();
        NormalizeSettings();
    }

    private void FindReferences()
    {
        if (body == null)
        {
            body =
                GetComponent<Rigidbody2D>();
        }
    }

    public void NormalizeSettings()
    {
        maximumFloorBounceCount =
            Mathf.Max(
                maximumFloorBounceCount,
                1
            );

        floorBounceStrengthDecay =
            Mathf.Clamp(
                floorBounceStrengthDecay,
                0.1f,
                1f
            );

        minimumRepeatedVerticalSpeed =
            Mathf.Max(
                minimumRepeatedVerticalSpeed,
                0.1f
            );

        minimumHorizontalSpeed =
            Mathf.Max(
                minimumHorizontalSpeed,
                0f
            );

        maximumHorizontalSpeed =
            Mathf.Max(
                maximumHorizontalSpeed,
                minimumHorizontalSpeed
            );

        minimumVerticalSpeed =
            Mathf.Max(
                minimumVerticalSpeed,
                0.1f
            );

        maximumVerticalSpeed =
            Mathf.Max(
                maximumVerticalSpeed,
                minimumVerticalSpeed
            );

        horizontalFlipChance =
            Mathf.Clamp01(
                horizontalFlipChance
            );

        gravity =
            Mathf.Max(
                gravity,
                0.1f
            );

        maximumFallSpeed =
            Mathf.Max(
                maximumFallSpeed,
                0.1f
            );

        minimumReboundSpeed =
            Mathf.Max(
                minimumReboundSpeed,
                0.1f
            );

        maximumReboundSpeed =
            Mathf.Max(
                maximumReboundSpeed,
                minimumReboundSpeed
            );

        blockDirectionJitter =
            Mathf.Clamp(
                blockDirectionJitter,
                0f,
                30f
            );

        wallDirectionJitter =
            Mathf.Clamp(
                wallDirectionJitter,
                0f,
                15f
            );

        minimumHorizontalReboundSpeed =
            Mathf.Max(
                minimumHorizontalReboundSpeed,
                0f
            );

        surfaceSeparationDistance =
            Mathf.Max(
                surfaceSeparationDistance,
                0f
            );

        minimumAirTime =
            Mathf.Max(
                minimumAirTime,
                0f
            );

        maximumDuration =
            Mathf.Max(
                maximumDuration,
                0.1f
            );
    }

    public bool TryBegin(
        Vector2 floorContactPosition,
        Vector2 incomingVelocity,
        float speedMultiplier)
    {
        if (!CanBegin ||
            body == null)
        {
            return false;
        }

        speedMultiplier =
            Mathf.Max(
                speedMultiplier,
                0.1f
            );

        lastFloorContactPosition =
            floorContactPosition;

        elapsedTime = 0f;

        currentFloorBounceCount = 1;

        hasExitedReturnZone = false;
        isActive = true;

        body.simulated = true;
        body.gravityScale = 0f;
        body.angularVelocity = 0f;

        body.linearVelocity =
            CreateFloorBounceVelocity(
                incomingVelocity,
                speedMultiplier,
                1f
            );

        return true;
    }

    public bool TryContinueFloorBounce(
        Vector2 floorContactPosition,
        float speedMultiplier)
    {
        if (!isActive ||
            body == null)
        {
            return false;
        }

        /*
         * 다음 턴 시작 위치로 사용할 바닥 접촉 위치를
         * 매번 최신 위치로 갱신합니다.
         */
        lastFloorContactPosition =
            floorContactPosition;

        if (currentFloorBounceCount >=
            maximumFloorBounceCount)
        {
            return false;
        }

        speedMultiplier =
            Mathf.Max(
                speedMultiplier,
                0.1f
            );

        currentFloorBounceCount++;

        hasExitedReturnZone = false;

        float strengthMultiplier =
            Mathf.Pow(
                floorBounceStrengthDecay,
                currentFloorBounceCount - 1
            );

        body.linearVelocity =
            CreateFloorBounceVelocity(
                body.linearVelocity,
                speedMultiplier,
                strengthMultiplier
            );

        return true;
    }

    private Vector2 CreateFloorBounceVelocity(
        Vector2 incomingVelocity,
        float speedMultiplier,
        float strengthMultiplier)
    {
        float horizontalDirection;

        if (Mathf.Abs(incomingVelocity.x) >
            0.05f)
        {
            horizontalDirection =
                Mathf.Sign(
                    incomingVelocity.x
                );

            if (Random.value <
                horizontalFlipChance)
            {
                horizontalDirection *= -1f;
            }
        }
        else
        {
            horizontalDirection =
                Random.value < 0.5f
                    ? -1f
                    : 1f;
        }

        float horizontalSpeed =
            Random.Range(
                minimumHorizontalSpeed,
                maximumHorizontalSpeed
            ) *
            speedMultiplier *
            Mathf.Lerp(
                0.8f,
                1f,
                strengthMultiplier
            );

        float verticalSpeed =
            Random.Range(
                minimumVerticalSpeed,
                maximumVerticalSpeed
            ) *
            speedMultiplier *
            strengthMultiplier;

        float scaledMinimumRepeatedSpeed =
            minimumRepeatedVerticalSpeed *
            speedMultiplier;

        if (currentFloorBounceCount > 1)
        {
            verticalSpeed =
                Mathf.Max(
                    verticalSpeed,
                    scaledMinimumRepeatedSpeed
                );
        }

        return new Vector2(
            horizontalDirection *
            horizontalSpeed,
            verticalSpeed
        );
    }

    public bool TickFixed(
        float fixedDeltaTime,
        float speedMultiplier)
    {
        if (!isActive ||
            body == null)
        {
            return false;
        }

        fixedDeltaTime =
            Mathf.Max(
                fixedDeltaTime,
                0f
            );

        speedMultiplier =
            Mathf.Max(
                speedMultiplier,
                0.1f
            );

        elapsedTime +=
            fixedDeltaTime *
            speedMultiplier;

        float scaledGravity =
            gravity *
            speedMultiplier *
            speedMultiplier;

        float scaledMaximumFallSpeed =
            maximumFallSpeed *
            speedMultiplier;

        Vector2 velocity =
            body.linearVelocity;

        velocity.y -=
            scaledGravity *
            fixedDeltaTime;

        velocity.y =
            Mathf.Max(
                velocity.y,
                -scaledMaximumFallSpeed
            );

        body.linearVelocity =
            velocity;

        return elapsedTime >=
               maximumDuration;
    }

    public void RescaleVelocity(
        float scaleRatio)
    {
        if (!isActive ||
            body == null)
        {
            return;
        }

        scaleRatio =
            Mathf.Max(
                scaleRatio,
                0.01f
            );

        body.linearVelocity *=
            scaleRatio;
    }

    public void NotifyReturnZoneExited()
    {
        if (!isActive)
        {
            return;
        }

        hasExitedReturnZone = true;
    }

    public Vector2 ResolveCollision(
        Collision2D collision,
        bool isBlockCollision,
        float speedMultiplier)
    {
        if (!isActive ||
            body == null)
        {
            return body != null
                ? body.linearVelocity
                : Vector2.zero;
        }

        speedMultiplier =
            Mathf.Max(
                speedMultiplier,
                0.1f
            );

        Vector2 incomingVelocity =
            body.linearVelocity;

        Vector2 collisionNormal =
            ResolveCollisionNormal(
                collision,
                incomingVelocity
            );

        Vector2 reflectedVelocity =
            Vector2.Reflect(
                incomingVelocity,
                collisionNormal
            );

        float directionJitter =
            isBlockCollision
                ? blockDirectionJitter
                : wallDirectionJitter;

        if (directionJitter > 0f)
        {
            float randomAngle =
                Random.Range(
                    -directionJitter,
                    directionJitter
                );

            reflectedVelocity =
                Quaternion.Euler(
                    0f,
                    0f,
                    randomAngle
                ) *
                reflectedVelocity;
        }

        float scaledMinimumSpeed =
            minimumReboundSpeed *
            speedMultiplier;

        float scaledMaximumSpeed =
            maximumReboundSpeed *
            speedMultiplier;

        float resolvedSpeed =
            Mathf.Clamp(
                reflectedVelocity.magnitude,
                scaledMinimumSpeed,
                scaledMaximumSpeed
            );

        if (isBlockCollision)
        {
            resolvedSpeed *=
                Random.Range(
                    0.95f,
                    1.06f
                );

            resolvedSpeed =
                Mathf.Clamp(
                    resolvedSpeed,
                    scaledMinimumSpeed,
                    scaledMaximumSpeed
                );
        }

        if (reflectedVelocity.sqrMagnitude <=
            0.0001f)
        {
            reflectedVelocity =
                collisionNormal;
        }

        reflectedVelocity =
            reflectedVelocity.normalized *
            resolvedSpeed;

        float scaledMinimumHorizontalSpeed =
            minimumHorizontalReboundSpeed *
            speedMultiplier;

        if (Mathf.Abs(reflectedVelocity.x) <
            scaledMinimumHorizontalSpeed)
        {
            float horizontalDirection =
                ResolveHorizontalDirection(
                    reflectedVelocity,
                    collisionNormal
                );

            reflectedVelocity.x =
                horizontalDirection *
                scaledMinimumHorizontalSpeed;

            reflectedVelocity =
                reflectedVelocity.normalized *
                resolvedSpeed;
        }

        SeparateFromSurface(
            collisionNormal
        );

        body.linearVelocity =
            reflectedVelocity;

        return reflectedVelocity;
    }

    private Vector2 ResolveCollisionNormal(
        Collision2D collision,
        Vector2 incomingVelocity)
    {
        Vector2 collisionNormal;

        if (collision != null &&
            collision.contactCount > 0)
        {
            collisionNormal =
                collision
                    .GetContact(0)
                    .normal;
        }
        else if (incomingVelocity.sqrMagnitude >
                 0.0001f)
        {
            collisionNormal =
                -incomingVelocity.normalized;
        }
        else
        {
            collisionNormal =
                Vector2.up;
        }

        if (collisionNormal.sqrMagnitude <=
            0.0001f)
        {
            collisionNormal =
                Vector2.up;
        }

        collisionNormal.Normalize();

        if (Vector2.Dot(
                incomingVelocity,
                collisionNormal
            ) > 0f)
        {
            collisionNormal *= -1f;
        }

        return collisionNormal;
    }

    private float ResolveHorizontalDirection(
        Vector2 reflectedVelocity,
        Vector2 collisionNormal)
    {
        if (Mathf.Abs(reflectedVelocity.x) >
            0.01f)
        {
            return Mathf.Sign(
                reflectedVelocity.x
            );
        }

        if (Mathf.Abs(collisionNormal.x) >
            0.01f)
        {
            return Mathf.Sign(
                collisionNormal.x
            );
        }

        return Random.value < 0.5f
            ? -1f
            : 1f;
    }

    private void SeparateFromSurface(
        Vector2 surfaceNormal)
    {
        if (body == null ||
            surfaceSeparationDistance <= 0f ||
            surfaceNormal.sqrMagnitude <=
            0.0001f)
        {
            return;
        }

        body.position +=
            surfaceNormal.normalized *
            surfaceSeparationDistance;
    }

    public void Cancel()
    {
        isActive = false;
        hasExitedReturnZone = false;

        currentFloorBounceCount = 0;
        elapsedTime = 0f;
    }
}