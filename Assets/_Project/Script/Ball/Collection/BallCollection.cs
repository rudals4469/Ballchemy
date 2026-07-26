using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BallCollection :
    MonoBehaviour
{
    [Header("Ball Settings")]
    [SerializeField]
    private Ball ballPrefab;

    [SerializeField, Min(1)]
    private int initialBallCount = 5;

    [Header("Default Ball Data")]
    [Tooltip(
        "게임 시작 시 생성되는 공과, " +
        "종류를 별도로 지정하지 않고 추가되는 공에 " +
        "사용할 기본 BallDefinition입니다."
    )]
    [SerializeField]
    private BallDefinition defaultBallDefinition;

    private readonly List<Ball> balls =
        new List<Ball>();

    private Vector2 standbyPosition;

    private bool isInitialized;

    public Ball BallPrefab =>
        ballPrefab;

    public BallDefinition DefaultBallDefinition =>
        defaultBallDefinition;

    public int Count =>
        balls.Count;

    public IReadOnlyList<Ball> Balls =>
        balls;

    public bool IsInitialized =>
        isInitialized;

    public Vector2 StandbyPosition =>
        standbyPosition;

    public event Action<Ball>
        BallCreated;

    public event Action<int>
        BallCountChanged;

    public event Action<int>
        BallsAdded;

    private void Awake()
    {
        NormalizeSettings();
        ValidateReferences();
    }

    private void OnValidate()
    {
        NormalizeSettings();
    }

    private void NormalizeSettings()
    {
        initialBallCount =
            Mathf.Max(
                initialBallCount,
                1
            );
    }

    private void ValidateReferences()
    {
        if (ballPrefab == null)
        {
            Debug.LogError(
                "BallCollection: " +
                "Ball Prefab이 연결되지 않았습니다.",
                this
            );

            return;
        }

        if (defaultBallDefinition == null &&
            ballPrefab.Definition == null)
        {
            Debug.LogWarning(
                "BallCollection: " +
                "Default Ball Definition과 " +
                "Ball Prefab의 Definition이 모두 비어 있습니다. " +
                "Fallback Damage가 사용됩니다.",
                this
            );
        }
    }

    public void Initialize(
        Vector2 initialStandbyPosition)
    {
        standbyPosition =
            initialStandbyPosition;

        if (isInitialized)
        {
            AlignAll(
                standbyPosition
            );

            return;
        }

        if (ballPrefab == null)
        {
            Debug.LogError(
                "BallCollection: " +
                "Ball Prefab이 없어 초기화할 수 없습니다.",
                this
            );

            return;
        }

        isInitialized = true;

        CreateBalls(
            initialBallCount,
            defaultBallDefinition
        );

        Debug.Log(
            "BallCollection: " +
            $"초기 공 {balls.Count}개 생성 완료",
            this
        );
    }

    public int AddBalls(
        int amount)
    {
        return AddBalls(
            amount,
            defaultBallDefinition
        );
    }

    public int AddBalls(
        int amount,
        BallDefinition definition)
    {
        if (!isInitialized)
        {
            Debug.LogWarning(
                "BallCollection: " +
                "초기화 전에 공을 추가하려 했습니다.",
                this
            );

            return 0;
        }

        amount =
            Mathf.Max(
                amount,
                0
            );

        if (amount == 0)
        {
            return 0;
        }

        BallDefinition resolvedDefinition =
            definition != null
                ? definition
                : defaultBallDefinition;

        int previousCount =
            balls.Count;

        CreateBalls(
            amount,
            resolvedDefinition
        );

        int addedCount =
            balls.Count -
            previousCount;

        if (addedCount <= 0)
        {
            return 0;
        }

        BallsAdded?.Invoke(
            addedCount
        );

        string definitionName =
            resolvedDefinition != null
                ? resolvedDefinition.DisplayName
                : "Fallback Ball";

        Debug.Log(
            "BallCollection: " +
            $"{definitionName} {addedCount}개 추가, " +
            $"현재 총 {balls.Count}개",
            this
        );

        return addedCount;
    }

    private void CreateBalls(
        int amount,
        BallDefinition definition)
    {
        if (ballPrefab == null)
        {
            return;
        }

        for (int i = 0;
             i < amount;
             i++)
        {
            CreateBall(
                definition
            );
        }

        BallCountChanged?.Invoke(
            balls.Count
        );
    }

    private Ball CreateBall(
        BallDefinition definition)
    {
        Ball newBall =
            Instantiate(
                ballPrefab,
                standbyPosition,
                Quaternion.identity
            );

        newBall.name =
            CreateBallObjectName(
                definition
            );

        if (definition != null)
        {
            newBall.ApplyDefinition(
                definition
            );
        }

        newBall.ResetTo(
            standbyPosition
        );

        IgnoreCollisionWithExistingBalls(
            newBall
        );

        balls.Add(
            newBall
        );

        BallCreated?.Invoke(
            newBall
        );

        return newBall;
    }

    private string CreateBallObjectName(
        BallDefinition definition)
    {
        int ballNumber =
            balls.Count + 1;

        if (definition == null ||
            string.IsNullOrWhiteSpace(
                definition.BallId))
        {
            return $"Ball_{ballNumber}";
        }

        return
            $"Ball_{ballNumber}_" +
            $"{definition.BallId}";
    }

    private void IgnoreCollisionWithExistingBalls(
        Ball newBall)
    {
        if (newBall == null)
        {
            return;
        }

        Collider2D newCollider =
            newBall.GetComponent<Collider2D>();

        if (newCollider == null)
        {
            return;
        }

        for (int i = 0;
             i < balls.Count;
             i++)
        {
            Ball existingBall =
                balls[i];

            if (existingBall == null)
            {
                continue;
            }

            Collider2D existingCollider =
                existingBall
                    .GetComponent<Collider2D>();

            if (existingCollider == null)
            {
                continue;
            }

            Physics2D.IgnoreCollision(
                newCollider,
                existingCollider,
                true
            );
        }
    }

    public List<Ball> CreateSnapshot()
    {
        List<Ball> snapshot =
            new List<Ball>();

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

            snapshot.Add(
                ball
            );
        }

        return snapshot;
    }

    public void SetStandbyPosition(
        Vector2 position)
    {
        standbyPosition =
            position;
    }

    public void AlignAll(
        Vector2 position)
    {
        standbyPosition =
            position;

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

            ball.ResetTo(
                standbyPosition
            );
        }
    }
}