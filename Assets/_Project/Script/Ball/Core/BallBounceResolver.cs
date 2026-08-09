using System;
using UnityEngine;

[Serializable]
public sealed class BallBounceResolver
{
    [Header("Box Collider Bounce")]
    [Tooltip(
        "BoxCollider2D의 평평한 면에서는 충돌 노멀을 " +
        "가로 또는 세로 축으로 보정합니다. " +
        "모서리에서는 실제 물리 노멀을 사용합니다."
    )]
    [SerializeField]
    private bool snapBoxNormalToAxis = true;

    [Tooltip(
        "모서리에서 가로면과 세로면의 거리가 " +
        "비슷하다고 판단하는 범위입니다."
    )]
    [SerializeField, Range(0f, 0.5f)]
    private float cornerTieTolerance = 0.12f;

    [SerializeField, Min(0.0001f)]
    private float minimumNormalMagnitude = 0.001f;

    public void Normalize()
    {
        cornerTieTolerance =
            Mathf.Clamp(
                cornerTieTolerance,
                0f,
                0.5f
            );

        minimumNormalMagnitude =
            Mathf.Max(
                minimumNormalMagnitude,
                0.0001f
            );
    }

    public bool TryResolve(
        Collision2D collision,
        Collider2D ownCollider,
        Vector2 incomingVelocity,
        float targetSpeed,
        out Vector2 outgoingVelocity,
        out Vector2 resolvedNormal,
        out Collider2D hitCollider)
    {
        outgoingVelocity =
            Vector2.zero;

        resolvedNormal =
            Vector2.zero;

        hitCollider =
            null;

        if (collision == null ||
            collision.contactCount == 0 ||
            incomingVelocity.sqrMagnitude <=
            0.0001f)
        {
            return false;
        }

        if (!TryGetBestContact(
                collision,
                ownCollider,
                incomingVelocity,
                out ContactPoint2D bestContact,
                out hitCollider
            ))
        {
            return false;
        }

        return TryResolve(
            hitCollider,
            bestContact.point,
            bestContact.normal,
            incomingVelocity,
            targetSpeed,
            out outgoingVelocity,
            out resolvedNormal
        );
    }

    public bool TryResolve(
        Collider2D hitCollider,
        Vector2 contactPoint,
        Vector2 physicsNormal,
        Vector2 incomingVelocity,
        float targetSpeed,
        out Vector2 outgoingVelocity,
        out Vector2 resolvedNormal)
    {
        outgoingVelocity =
            Vector2.zero;

        resolvedNormal =
            Vector2.zero;

        if (incomingVelocity.sqrMagnitude <=
            0.0001f)
        {
            return false;
        }

        Normalize();

        resolvedNormal =
            ResolveSurfaceNormal(
                hitCollider,
                contactPoint,
                physicsNormal,
                incomingVelocity
            );

        if (resolvedNormal.sqrMagnitude <=
            minimumNormalMagnitude *
            minimumNormalMagnitude)
        {
            return false;
        }

        Vector2 incomingDirection =
            incomingVelocity.normalized;

        Vector2 reflectedDirection =
            Vector2.Reflect(
                incomingDirection,
                resolvedNormal
            );

        if (reflectedDirection.sqrMagnitude <=
            0.0001f)
        {
            reflectedDirection =
                -incomingDirection;
        }

        reflectedDirection.Normalize();

        targetSpeed =
            Mathf.Max(
                targetSpeed,
                0.01f
            );

        outgoingVelocity =
            reflectedDirection *
            targetSpeed;

        return true;
    }

    private bool TryGetBestContact(
        Collision2D collision,
        Collider2D ownCollider,
        Vector2 incomingVelocity,
        out ContactPoint2D bestContact,
        out Collider2D hitCollider)
    {
        bestContact =
            default;

        hitCollider =
            null;

        Vector2 incomingDirection =
            incomingVelocity.normalized;

        float bestScore =
            float.NegativeInfinity;

        bool foundContact =
            false;

        for (int i = 0;
             i < collision.contactCount;
             i++)
        {
            ContactPoint2D contact =
                collision.GetContact(i);

            Vector2 normal =
                contact.normal;

            if (normal.sqrMagnitude <=
                0.0001f)
            {
                continue;
            }

            normal.Normalize();

            if (Vector2.Dot(
                    incomingDirection,
                    normal
                ) > 0f)
            {
                normal =
                    -normal;
            }

            float score =
                -Vector2.Dot(
                    incomingDirection,
                    normal
                );

            if (score <=
                bestScore)
            {
                continue;
            }

            bestScore =
                score;

            bestContact =
                contact;

            hitCollider =
                FindHitCollider(
                    contact,
                    collision,
                    ownCollider
                );

            foundContact =
                true;
        }

        return foundContact;
    }

    private Collider2D FindHitCollider(
        ContactPoint2D contact,
        Collision2D collision,
        Collider2D ownCollider)
    {
        if (contact.collider != null &&
            contact.collider != ownCollider)
        {
            return contact.collider;
        }

        if (contact.otherCollider != null &&
            contact.otherCollider != ownCollider)
        {
            return contact.otherCollider;
        }

        if (collision.collider != null &&
            collision.collider != ownCollider)
        {
            return collision.collider;
        }

        if (collision.otherCollider != null &&
            collision.otherCollider != ownCollider)
        {
            return collision.otherCollider;
        }

        return collision.collider;
    }

    private Vector2 ResolveSurfaceNormal(
        Collider2D hitCollider,
        Vector2 contactPoint,
        Vector2 physicsNormal,
        Vector2 incomingVelocity)
    {
        Vector2 normalizedPhysicsNormal =
            NormalizePhysicsNormal(
                physicsNormal,
                incomingVelocity
            );

        if (!snapBoxNormalToAxis ||
            !(hitCollider is BoxCollider2D boxCollider))
        {
            return normalizedPhysicsNormal;
        }

        return ResolveBoxSurfaceNormal(
            boxCollider,
            contactPoint,
            normalizedPhysicsNormal,
            incomingVelocity
        );
    }

    private Vector2 ResolveBoxSurfaceNormal(
        BoxCollider2D boxCollider,
        Vector2 contactPoint,
        Vector2 physicsNormal,
        Vector2 incomingVelocity)
    {
        Transform boxTransform =
            boxCollider.transform;

        Vector3 localPoint3 =
            boxTransform.InverseTransformPoint(
                contactPoint
            );

        Vector2 localPoint =
            new Vector2(
                localPoint3.x,
                localPoint3.y
            );

        Vector3 localIncoming3 =
            boxTransform.InverseTransformVector(
                incomingVelocity
            );

        Vector2 localIncoming =
            new Vector2(
                localIncoming3.x,
                localIncoming3.y
            );

        Vector3 localPhysicsNormal3 =
            boxTransform.InverseTransformDirection(
                physicsNormal
            );

        Vector2 localPhysicsNormal =
            new Vector2(
                localPhysicsNormal3.x,
                localPhysicsNormal3.y
            );

        Vector2 localCenter =
            boxCollider.offset;

        Vector2 halfSize =
            boxCollider.size *
            0.5f;

        float normalizedX =
            Mathf.Abs(
                (
                    localPoint.x -
                    localCenter.x
                ) /
                Mathf.Max(
                    halfSize.x,
                    0.0001f
                )
            );

        float normalizedY =
            Mathf.Abs(
                (
                    localPoint.y -
                    localCenter.y
                ) /
                Mathf.Max(
                    halfSize.y,
                    0.0001f
                )
            );

        float edgeDifference =
            Mathf.Abs(
                normalizedX -
                normalizedY
            );

        bool isCornerContact =
            edgeDifference <= cornerTieTolerance;

        if (isCornerContact)
        {
            // 진입 방향을 normal로 쓰면 항상 정반대로 되돌아가
            // 인접 Collider 사이에서 같은 접점을 반복할 수 있다.
            return physicsNormal;
        }

        bool useHorizontalNormal;

        useHorizontalNormal = normalizedX > normalizedY;

        Vector2 localAxisNormal;

        if (useHorizontalNormal)
        {
            float normalSign =
                ResolveNormalSign(
                    localPhysicsNormal.x,
                    localPoint.x -
                    localCenter.x,
                    localIncoming.x
                );

            localAxisNormal =
                new Vector2(
                    normalSign,
                    0f
                );
        }
        else
        {
            float normalSign =
                ResolveNormalSign(
                    localPhysicsNormal.y,
                    localPoint.y -
                    localCenter.y,
                    localIncoming.y
                );

            localAxisNormal =
                new Vector2(
                    0f,
                    normalSign
                );
        }

        Vector3 worldNormal3 =
            boxTransform.TransformDirection(
                localAxisNormal
            );

        Vector2 worldNormal =
            new Vector2(
                worldNormal3.x,
                worldNormal3.y
            );

        if (worldNormal.sqrMagnitude <=
            minimumNormalMagnitude *
            minimumNormalMagnitude)
        {
            return physicsNormal;
        }

        worldNormal.Normalize();

        if (Vector2.Dot(
                incomingVelocity,
                worldNormal
            ) > 0f)
        {
            worldNormal =
                -worldNormal;
        }

        return worldNormal;
    }

    private Vector2 NormalizePhysicsNormal(
        Vector2 physicsNormal,
        Vector2 incomingVelocity)
    {
        Vector2 normal =
            physicsNormal;

        if (normal.sqrMagnitude <=
            minimumNormalMagnitude *
            minimumNormalMagnitude)
        {
            normal =
                -incomingVelocity.normalized;
        }
        else
        {
            normal.Normalize();
        }

        if (Vector2.Dot(
                incomingVelocity,
                normal
            ) > 0f)
        {
            normal =
                -normal;
        }

        return normal;
    }

    private float ResolveNormalSign(
        float physicsNormalComponent,
        float contactOffset,
        float incomingComponent)
    {
        if (Mathf.Abs(
                physicsNormalComponent
            ) >
            minimumNormalMagnitude)
        {
            return Mathf.Sign(
                physicsNormalComponent
            );
        }

        if (Mathf.Abs(
                contactOffset
            ) >
            minimumNormalMagnitude)
        {
            return Mathf.Sign(
                contactOffset
            );
        }

        if (Mathf.Abs(
                incomingComponent
            ) >
            minimumNormalMagnitude)
        {
            return -Mathf.Sign(
                incomingComponent
            );
        }

        return 1f;
    }
}
