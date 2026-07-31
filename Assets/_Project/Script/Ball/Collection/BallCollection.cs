using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(BallRuntimeStats))]
public sealed class BallCollection :
    MonoBehaviour
{
    [Header("Starting Ball Settings")]
    [SerializeField]
    private Ball ballPrefab;

    [Tooltip(
        "게임을 시작할 때 보유하는 전체 공 개수입니다."
    )]
    [FormerlySerializedAs("initialBallCount")]
    [SerializeField, Min(1)]
    private int startingBallCount = 20;

    [Tooltip(
        "게임 시작 시 생성되는 공의 Definition입니다."
    )]
    [FormerlySerializedAs("defaultBallDefinition")]
    [SerializeField]
    private BallDefinition startingBallDefinition;

    [Header("Runtime Stats")]

    [Tooltip(
        "생성된 모든 공이 공유할 " +
        "현재 런의 공 전투 스탯입니다."
    )]
    [SerializeField]
    private BallRuntimeStats runtimeStats;

    private readonly List<Ball> balls =
        new List<Ball>();

    private Vector2 standbyPosition;

    private bool isInitialized;

    /*
     * 시작방과 클리어한 방에서도
     * 보유 공 목록은 유지하되 화면에는 숨기기 위한 상태입니다.
     *
     * 숨김 중 보상으로 새 공이 추가되면
     * 새 공에도 동일한 숨김 상태를 적용합니다.
     */
    private bool areBallsVisible = true;

    public Ball BallPrefab =>
        ballPrefab;

    public BallDefinition StartingBallDefinition =>
        startingBallDefinition;

    public BallDefinition DefaultBallDefinition =>
        startingBallDefinition;

    public BallRuntimeStats RuntimeStats =>
        runtimeStats;

    public int StartingBallCount =>
        startingBallCount;

    public int Count =>
        balls.Count;

    public IReadOnlyList<Ball> Balls =>
        balls;

    public bool IsInitialized =>
        isInitialized;

    public bool AreBallsVisible =>
        areBallsVisible;

    public Vector2 StandbyPosition =>
        standbyPosition;

    public event Action<Ball>
        BallCreated;

    public event Action<int>
        BallCountChanged;

    public event Action<int>
        BallsAdded;

    public event Action<bool>
        BallsVisibilityChanged;

    private void Awake()
    {
        FindReferences();
        NormalizeSettings();
        ValidateReferences();
    }

    private void OnValidate()
    {
        FindReferences();
        NormalizeSettings();
    }

    private void FindReferences()
    {
        if (runtimeStats == null)
        {
            runtimeStats =
                GetComponent<
                    BallRuntimeStats
                >();
        }
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
                "Starting Ball Definition이 " +
                "연결되지 않았습니다.",
                this
            );

            isValid = false;
        }

        if (runtimeStats == null)
        {
            Debug.LogError(
                "BallCollection: " +
                "BallRuntimeStats가 연결되지 않았습니다.",
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

            ApplyCurrentVisibilityToAll();

            return;
        }

        FindReferences();

        if (!ValidateReferences())
        {
            return;
        }

        CreateBalls(
            startingBallCount,
            startingBallDefinition
        );

        isInitialized = true;

        ApplyCurrentVisibilityToAll();

        Debug.Log(
            "BallCollection: " +
            $"초기 공 {balls.Count}개 생성 완료, " +
            $"공 종류 = " +
            $"{startingBallDefinition.DisplayName}, " +
            $"공통 기본 피해 = " +
            $"{runtimeStats.BaseDirectDamage}",
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

    public void SetBallsVisible(
        bool shouldShow)
    {
        bool visibilityChanged =
            areBallsVisible !=
            shouldShow;

        areBallsVisible =
            shouldShow;

        /*
         * 같은 값이 다시 들어오더라도
         * 런타임 중 새로 생성된 공이나 개별 상태가
         * 달라졌을 수 있으므로 전체에 다시 적용합니다.
         */
        ApplyCurrentVisibilityToAll();

        if (!visibilityChanged)
        {
            return;
        }

        BallsVisibilityChanged?.Invoke(
            areBallsVisible
        );

        Debug.Log(
            "BallCollection: 공 표시 상태 " +
            (
                areBallsVisible
                    ? "활성화"
                    : "비활성화"
            ),
            this
        );
    }

    private void ApplyCurrentVisibilityToAll()
    {
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

            ball.SetPresentationVisible(
                areBallsVisible
            );
        }
    }

    private void CreateBalls(
        int amount,
        BallDefinition definition)
    {
        if (ballPrefab == null ||
            definition == null ||
            runtimeStats == null)
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

        combatController.ApplyRuntimeStats(
            runtimeStats
        );

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

        /*
         * 보상 선택 중처럼 기존 공이 숨겨져 있다면
         * 새로 지급된 공도 즉시 같은 상태로 맞춥니다.
         */
        newBall.SetPresentationVisible(
            areBallsVisible
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