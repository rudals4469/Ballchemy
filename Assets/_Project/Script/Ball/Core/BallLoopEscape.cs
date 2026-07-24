using System;
using UnityEngine;

[Serializable]
public sealed class BallLoopEscape
{
    [Header("Loop Detection")]
    [Tooltip(
        "파괴 불가 블록의 위아래 면을 " +
        "번갈아 충돌한 횟수입니다."
    )]
    [SerializeField, Min(2)]
    private int requiredAlternatingHits = 4;

    [Tooltip(
        "이 시간 안에 다시 충돌해야 " +
        "연속 충돌로 판단합니다."
    )]
    [SerializeField, Min(0.05f)]
    private float maximumHitInterval = 1.25f;

    [Tooltip(
        "상하 면 충돌로 판단할 " +
        "충돌 노멀의 최소 Y 비율입니다."
    )]
    [SerializeField, Range(0.5f, 1f)]
    private float verticalNormalThreshold = 0.8f;

    [Header("Escape Direction")]
    [Tooltip(
        "탈출 시 보장할 최소 수평 속도 비율입니다."
    )]
    [SerializeField, Range(0.05f, 0.75f)]
    private float minimumHorizontalRatio = 0.22f;

    [Tooltip(
        "공이 좌우 어느 쪽으로 탈출할지 결정할 때 " +
        "사용하는 보드 중앙 X 좌표입니다."
    )]
    [SerializeField]
    private float boardCenterX;

    [SerializeField, Min(0f)]
    private float escapeCooldown = 0.25f;

    private int alternatingHitCount;

    private float lastVerticalNormalSign;
    private float lastHitTime =
        float.NegativeInfinity;

    private float nextEscapeAllowedTime;

    private float fallbackHorizontalSign = 1f;

    public void Normalize()
    {
        requiredAlternatingHits =
            Mathf.Max(
                requiredAlternatingHits,
                2
            );

        maximumHitInterval =
            Mathf.Max(
                maximumHitInterval,
                0.05f
            );

        verticalNormalThreshold =
            Mathf.Clamp(
                verticalNormalThreshold,
                0.5f,
                1f
            );

        minimumHorizontalRatio =
            Mathf.Clamp(
                minimumHorizontalRatio,
                0.05f,
                0.75f
            );

        escapeCooldown =
            Mathf.Max(
                escapeCooldown,
                0f
            );
    }

    public void Initialize(
        int seed)
    {
        Normalize();

        fallbackHorizontalSign =
            seed % 2 == 0
                ? 1f
                : -1f;

        ResetRuntime();
    }

    public void ResetRuntime()
    {
        alternatingHitCount = 0;

        lastVerticalNormalSign = 0f;

        lastHitTime =
            float.NegativeInfinity;

        nextEscapeAllowedTime = 0f;
    }

    public bool TryEscape(
        Block hitBlock,
        Vector2 collisionNormal,
        Vector2 currentVelocity,
        Vector2 ballPosition,
        float targetSpeed,
        out Vector2 escapedVelocity)
    {
        escapedVelocity =
            currentVelocity;

        Normalize();

        if (hitBlock == null ||
            !hitBlock.IsIndestructible)
        {
            ResetCollisionSequence();

            return false;
        }

        if (Mathf.Abs(
                collisionNormal.y) <
            verticalNormalThreshold)
        {
            ResetCollisionSequence();

            return false;
        }

        if (currentVelocity.sqrMagnitude <=
            0.0001f)
        {
            ResetCollisionSequence();

            return false;
        }

        float currentTime =
            Time.time;

        float currentNormalSign =
            Mathf.Sign(
                collisionNormal.y
            );

        bool exceededHitInterval =
            currentTime -
            lastHitTime >
            maximumHitInterval;

        if (exceededHitInterval)
        {
            alternatingHitCount = 1;
        }
        else if (
            lastVerticalNormalSign != 0f &&
            currentNormalSign !=
            lastVerticalNormalSign)
        {
            alternatingHitCount++;
        }
        else
        {
            alternatingHitCount = 1;
        }

        lastVerticalNormalSign =
            currentNormalSign;

        lastHitTime =
            currentTime;

        Vector2 currentDirection =
            currentVelocity.normalized;

        if (Mathf.Abs(
                currentDirection.x) >=
            minimumHorizontalRatio)
        {
            ResetCollisionSequence();

            return false;
        }

        if (alternatingHitCount <
            requiredAlternatingHits)
        {
            return false;
        }

        if (currentTime <
            nextEscapeAllowedTime)
        {
            return false;
        }

        float horizontalSign =
            ResolveHorizontalSign(
                currentDirection,
                ballPosition
            );

        float horizontalComponent =
            horizontalSign *
            minimumHorizontalRatio;

        float verticalComponentMagnitude =
            Mathf.Sqrt(
                Mathf.Max(
                    0f,
                    1f -
                    horizontalComponent *
                    horizontalComponent
                )
            );

        float verticalSign;

        if (Mathf.Abs(
                currentDirection.y) >
            0.001f)
        {
            verticalSign =
                Mathf.Sign(
                    currentDirection.y
                );
        }
        else
        {
            verticalSign =
                -currentNormalSign;
        }

        Vector2 escapedDirection =
            new Vector2(
                horizontalComponent,
                verticalComponentMagnitude *
                verticalSign
            ).normalized;

        targetSpeed =
            Mathf.Max(
                targetSpeed,
                0.01f
            );

        escapedVelocity =
            escapedDirection *
            targetSpeed;

        fallbackHorizontalSign =
            -fallbackHorizontalSign;

        nextEscapeAllowedTime =
            currentTime +
            escapeCooldown;

        ResetCollisionSequence();

        return true;
    }

    private float ResolveHorizontalSign(
        Vector2 currentDirection,
        Vector2 ballPosition)
    {
        if (Mathf.Abs(
                currentDirection.x) >
            0.001f)
        {
            return Mathf.Sign(
                currentDirection.x
            );
        }

        if (ballPosition.x <
            boardCenterX -
            0.01f)
        {
            return 1f;
        }

        if (ballPosition.x >
            boardCenterX +
            0.01f)
        {
            return -1f;
        }

        return fallbackHorizontalSign;
    }

    private void ResetCollisionSequence()
    {
        alternatingHitCount = 0;

        lastVerticalNormalSign = 0f;

        lastHitTime =
            float.NegativeInfinity;
    }
}