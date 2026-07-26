using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class BallCollection :
    MonoBehaviour
{
    [Header("Starting Ball Settings")]
    [SerializeField]
    private Ball ballPrefab;

    [Tooltip(
        "게임을 시작할 때 보유하는 전체 공 개수입니다. " +
        "추가로 생성되는 개수가 아닙니다."
    )]
    [FormerlySerializedAs("initialBallCount")]
    [SerializeField, Min(1)]
    private int startingBallCount = 20;

    [Tooltip(
        "게임 시작 시 생성되는 공의 Definition입니다. " +
        "반드시 연결되어 있어야 합니다."
    )]
    [FormerlySerializedAs("defaultBallDefinition")]
    [SerializeField]
    private BallDefinition startingBallDefinition;

    private readonly List<Ball> balls =
        new List<Ball>();

    private Vector2 standbyPosition;

    private bool isInitialized;

    public Ball BallPrefab =>
        ballPrefab;

    public BallDefinition StartingBallDefinition =>
        startingBallDefinition;

    /*
     * 기존 코드에서 이 프로퍼티를 참조하고 있을 가능성을
     * 고려해 이름을 유지한다.
     */
    public BallDefinition DefaultBallDefinition =>
        startingBallDefinition;

    public int StartingBallCount =>
        startingBallCount;

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
        startingBallCount =
            Mathf.Max(
                startingBallCount,
                1
            );
    }

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (ballPrefab == null)
        {
            Debug.LogError(
                "BallCollection: " +
                "Ball Prefab이 연결되지 않았습니다.",
                this
            );

            isValid = false;
        }

        if (startingBallDefinition == null)
        {
            Debug.LogError(
                "BallCollection: " +
                "Starting Ball Definition이 연결되지 않았습니다. " +
                "Definition이 없으면 공을 생성하지 않습니다.",
                this
            );

            isValid = false;
        }

        return isValid;
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

        if (!ValidateReferences())
        {
            return;
        }

        CreateBalls(
            startingBallCount,
            startingBallDefinition
        );

        isInitialized = true;

        Debug.Log(
            "BallCollection: " +
            $"초기 공 {balls.Count}개 생성 완료, " +
            $"공 종류 = {startingBallDefinition.DisplayName}, " +
            $"기본 피해 = {startingBallDefinition.BaseDamage}",
            this
        );
    }

    public int AddBalls(
        int amount)
    {
        return AddBalls(
            amount,
            startingBallDefinition
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
                : startingBallDefinition;

        if (resolvedDefinition == null)
        {
            Debug.LogError(
                "BallCollection: " +
                "추가할 공의 BallDefinition이 없습니다.",
                this
            );

            return 0;
        }

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

        Debug.Log(
            "BallCollection: " +
            $"{resolvedDefinition.DisplayName} " +
            $"{addedCount}개 추가, " +
            $"현재 총 {balls.Count}개",
            this
        );

        return addedCount;
    }

    private void CreateBalls(
        int amount,
        BallDefinition definition)
    {
        if (ballPrefab == null ||
            definition == null)
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
        if (definition == null)
        {
            Debug.LogError(
                "BallCollection: " +
                "BallDefinition이 없는 공은 생성할 수 없습니다.",
                this
            );

            return null;
        }

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

        BallCombatController combatController =
            newBall.GetComponent<
                BallCombatController
            >();

        if (combatController == null)
        {
            Debug.LogError(
                "BallCollection: " +
                "생성된 공에 BallCombatController가 없습니다.",
                newBall
            );

            Destroy(
                newBall.gameObject
            );

            return null;
        }

        combatController.ApplyDefinition(
            definition
        );

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
                definition.BallId
            ))
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
                existingBall.GetComponent<
                    Collider2D
                >();

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