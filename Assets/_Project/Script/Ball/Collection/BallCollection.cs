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

    public int Count
    {
        get
        {
            RemoveDestroyedBallReferences();

            return balls.Count;
        }
    }

    public IReadOnlyList<Ball> Balls =>
        balls;

    public bool IsInitialized =>
        isInitialized;

    public bool AreBallsVisible =>
        areBallsVisible;

    public Vector2 StandbyPosition =>
        standbyPosition;

    public bool CanModifyBallComposition =>
        isInitialized &&
        Ball.ActiveMovingBallCount <= 0;

    public event Action<Ball>
        BallCreated;

    public event Action<int>
        BallCountChanged;

    public event Action<int>
        BallsAdded;

    public event Action<int>
        BallsRemoved;

    public event Action<int>
        BallDefinitionsReplaced;

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
            MigrateRemovedBallDefinitions();

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

        MigrateRemovedBallDefinitions();

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
            ResolveSupportedDefinition(
                definition != null
                    ? definition
                    : startingBallDefinition
            );

        if (resolvedDefinition == null)
        {
            Debug.LogError(
                "BallCollection: " +
                "추가할 공의 BallDefinition이 없습니다.",
                this
            );

            return 0;
        }

        RemoveDestroyedBallReferences();

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

    /*
     * 조건 없이 보유 공 중 무작위 공을 제거합니다.
     *
     * minimumRemainingCount보다 적은 수의 공이
     * 남도록 제거하지 않습니다.
     */
    public int RemoveRandomBalls(
        int amount,
        int minimumRemainingCount = 1)
    {
        return RemoveRandomBalls(
            amount,
            null,
            minimumRemainingCount
        );
    }

    /*
     * predicate 조건을 만족하는 공 중 무작위로 제거합니다.
     *
     * 예:
     *
     * 1성 공만 제거:
     * ballCollection.RemoveRandomBalls(
     *     5,
     *     ball => ball.StarGrade ==
     *         BallStarGrade.OneStar
     * );
     *
     * 기본 공만 제거:
     * ballCollection.RemoveRandomBalls(
     *     5,
     *     ball => ball.TraitType ==
     *         BallTraitType.Basic
     * );
     */
    public int RemoveRandomBalls(
        int amount,
        Predicate<Ball> predicate,
        int minimumRemainingCount = 1)
    {
        if (!ValidateCompositionModification(
                "공 제거"
            ))
        {
            return 0;
        }

        amount =
            Mathf.Max(
                amount,
                0
            );

        minimumRemainingCount =
            Mathf.Max(
                minimumRemainingCount,
                1
            );

        if (amount <= 0)
        {
            return 0;
        }

        RemoveDestroyedBallReferences();

        int maximumRemovableCount =
            Mathf.Max(
                balls.Count -
                minimumRemainingCount,
                0
            );

        if (maximumRemovableCount <= 0)
        {
            Debug.LogWarning(
                "BallCollection: " +
                "최소 보유 공 개수 때문에 " +
                "공을 제거할 수 없습니다. " +
                $"현재={balls.Count}, " +
                $"최소={minimumRemainingCount}",
                this
            );

            return 0;
        }

        List<Ball> candidates =
            CollectMatchingBalls(
                predicate
            );

        if (candidates.Count == 0)
        {
            return 0;
        }

        ShuffleBalls(
            candidates
        );

        int removeCount =
            Mathf.Min(
                amount,
                maximumRemovableCount,
                candidates.Count
            );

        int removedCount = 0;

        for (int i = 0;
             i < removeCount;
             i++)
        {
            Ball targetBall =
                candidates[i];

            if (!RemoveBallInternal(
                    targetBall
                ))
            {
                continue;
            }

            removedCount++;
        }

        if (removedCount <= 0)
        {
            return 0;
        }

        RemoveDestroyedBallReferences();

        BallCountChanged?.Invoke(
            balls.Count
        );

        BallsRemoved?.Invoke(
            removedCount
        );

        Debug.Log(
            "BallCollection: " +
            $"공 {removedCount}개 제거, " +
            $"현재 총 {balls.Count}개",
            this
        );

        return removedCount;
    }

    /*
     * 조건 없이 무작위 공의 Definition을 교체합니다.
     */
    public int ReplaceRandomBallDefinitions(
        int amount,
        BallDefinition replacementDefinition)
    {
        return ReplaceRandomBallDefinitions(
            amount,
            replacementDefinition,
            null
        );
    }

    /*
     * predicate 조건을 만족하는 공 중 무작위 공의
     * Definition을 영구 교체합니다.
     *
     * 공 개수는 변하지 않습니다.
     */
    public int ReplaceRandomBallDefinitions(
        int amount,
        BallDefinition replacementDefinition,
        Predicate<Ball> predicate)
    {
        if (!ValidateCompositionModification(
                "공 Definition 교체"
            ))
        {
            return 0;
        }

        if (replacementDefinition == null)
        {
            Debug.LogWarning(
                "BallCollection: " +
                "교체할 BallDefinition이 없습니다.",
                this
            );

            return 0;
        }

        amount =
            Mathf.Max(
                amount,
                0
            );

        if (amount <= 0)
        {
            return 0;
        }

        RemoveDestroyedBallReferences();

        List<Ball> candidates =
            CollectMatchingBalls(
                ball =>
                {
                    if (ball == null)
                    {
                        return false;
                    }

                    if (ball.Definition ==
                        replacementDefinition)
                    {
                        return false;
                    }

                    return predicate == null ||
                           predicate(
                               ball
                           );
                }
            );

        if (candidates.Count == 0)
        {
            return 0;
        }

        ShuffleBalls(
            candidates
        );

        int replaceCount =
            Mathf.Min(
                amount,
                candidates.Count
            );

        int replacedCount = 0;

        for (int i = 0;
             i < replaceCount;
             i++)
        {
            Ball targetBall =
                candidates[i];

            if (!ReplaceBallDefinitionInternal(
                    targetBall,
                    replacementDefinition
                ))
            {
                continue;
            }

            replacedCount++;
        }

        if (replacedCount <= 0)
        {
            return 0;
        }

        BallDefinitionsReplaced?.Invoke(
            replacedCount
        );

        Debug.Log(
            "BallCollection: " +
            $"공 {replacedCount}개를 " +
            $"{replacementDefinition.DisplayName}(으)로 변환",
            this
        );

        return replacedCount;
    }

    /*
     * 조건을 만족하는 현재 보유 공의 수를 반환합니다.
     */
    public int CountMatchingBalls(
        Predicate<Ball> predicate)
    {
        RemoveDestroyedBallReferences();

        int count = 0;

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

            if (predicate != null &&
                !predicate(
                    ball
                ))
            {
                continue;
            }

            count++;
        }

        return count;
    }

    public int CountBallsWithDefinition(
        BallDefinition definition)
    {
        if (definition == null)
        {
            return 0;
        }

        return CountMatchingBalls(
            ball =>
                ball != null &&
                ball.Definition ==
                definition
        );
    }

    public int CountBallsWithGrade(
        BallStarGrade grade)
    {
        return CountMatchingBalls(
            ball =>
                ball != null &&
                ball.StarGrade ==
                grade
        );
    }

    public int CountBallsWithTrait(
        BallTraitType traitType)
    {
        return CountMatchingBalls(
            ball =>
                ball != null &&
                ball.TraitType ==
                traitType
        );
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
        RemoveDestroyedBallReferences();

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
        definition =
            ResolveSupportedDefinition(
                definition
            );

        if (definition == null)
        {
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
                definition,
                balls.Count + 1
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

    private bool RemoveBallInternal(
        Ball targetBall)
    {
        if (targetBall == null)
        {
            return false;
        }

        if (targetBall.IsMoving)
        {
            Debug.LogWarning(
                "BallCollection: " +
                "이동 중인 공은 제거할 수 없습니다.",
                targetBall
            );

            return false;
        }

        bool removed =
            balls.Remove(
                targetBall
            );

        if (!removed)
        {
            return false;
        }

        targetBall.ClearTemporaryUpgradeRuntime();

        Destroy(
            targetBall.gameObject
        );

        return true;
    }

    private bool ReplaceBallDefinitionInternal(
        Ball targetBall,
        BallDefinition replacementDefinition)
    {
        replacementDefinition =
            ResolveSupportedDefinition(
                replacementDefinition
            );

        if (targetBall == null ||
            replacementDefinition == null)
        {
            return false;
        }

        if (targetBall.IsMoving)
        {
            Debug.LogWarning(
                "BallCollection: " +
                "이동 중인 공의 Definition은 " +
                "교체할 수 없습니다.",
                targetBall
            );

            return false;
        }

        BallCombatController combatController =
            targetBall.CombatController;

        if (combatController == null)
        {
            Debug.LogWarning(
                "BallCollection: " +
                "공에 BallCombatController가 없어 " +
                "Definition을 교체할 수 없습니다.",
                targetBall
            );

            return false;
        }

        targetBall.ClearTemporaryUpgradeRuntime();

        combatController.ApplyDefinition(
            replacementDefinition
        );

        targetBall.name =
            CreateBallObjectName(
                replacementDefinition,
                ResolveBallDisplayNumber(
                    targetBall
                )
            );

        return
            targetBall.Definition ==
            replacementDefinition;
    }

    public int MigrateRemovedBallDefinitions()
    {
        if (Ball.ActiveMovingBallCount > 0)
        {
            return 0;
        }

        RemoveDestroyedBallReferences();

        int replacedCount = 0;

        for (int i = 0;
             i < balls.Count;
             i++)
        {
            Ball ball = balls[i];

            if (ball == null ||
                !BallPoolPolicy
                    .IsRemovedFromPlayerPool(
                        ball.Definition
                    ))
            {
                continue;
            }

            BallDefinition replacement =
                ResolveBasicDefinitionForGrade(
                    ball.StarGrade
                );

            if (ReplaceBallDefinitionInternal(
                    ball,
                    replacement
                ))
            {
                replacedCount++;
            }
        }

        if (replacedCount > 0)
        {
            BallDefinitionsReplaced?.Invoke(
                replacedCount
            );

            Debug.Log(
                "BallCollection: 제거 대상 공 " +
                $"{replacedCount}개를 같은 등급 " +
                "Basic Ball로 변환했습니다.",
                this
            );
        }

        return replacedCount;
    }

    private BallDefinition ResolveSupportedDefinition(
        BallDefinition definition)
    {
        if (definition == null ||
            !BallPoolPolicy
                .IsRemovedFromPlayerPool(
                    definition
                ))
        {
            return definition;
        }

        return ResolveBasicDefinitionForGrade(
            definition.StarGrade
        );
    }

    private BallDefinition ResolveBasicDefinitionForGrade(
        BallStarGrade targetGrade)
    {
        BallDefinition current =
            startingBallDefinition;

        if (current == null ||
            current.TraitType != BallTraitType.Basic)
        {
            return startingBallDefinition;
        }

        if (targetGrade == BallStarGrade.None)
        {
            targetGrade = BallStarGrade.OneStar;
        }

        while (current != null &&
               (int)current.StarGrade <
               (int)targetGrade)
        {
            BallDefinition next =
                current.NextStarDefinition;

            if (next == null ||
                next.TraitType != BallTraitType.Basic)
            {
                break;
            }

            current = next;
        }

        return current;
    }

    private bool ValidateCompositionModification(
        string operationName)
    {
        if (!isInitialized)
        {
            Debug.LogWarning(
                "BallCollection: " +
                $"초기화 전에 {operationName}을 시도했습니다.",
                this
            );

            return false;
        }

        if (Ball.ActiveMovingBallCount > 0)
        {
            Debug.LogWarning(
                "BallCollection: " +
                $"공이 이동 중이므로 {operationName}을 " +
                "실행할 수 없습니다. " +
                $"이동 중 공={Ball.ActiveMovingBallCount}",
                this
            );

            return false;
        }

        return true;
    }

    private List<Ball> CollectMatchingBalls(
        Predicate<Ball> predicate)
    {
        List<Ball> matches =
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

            if (predicate != null &&
                !predicate(
                    ball
                ))
            {
                continue;
            }

            matches.Add(
                ball
            );
        }

        return matches;
    }

    private static void ShuffleBalls(
        List<Ball> targetBalls)
    {
        if (targetBalls == null)
        {
            return;
        }

        for (int i = targetBalls.Count - 1;
             i > 0;
             i--)
        {
            int randomIndex =
                UnityEngine.Random.Range(
                    0,
                    i + 1
                );

            Ball temporary =
                targetBalls[i];

            targetBalls[i] =
                targetBalls[randomIndex];

            targetBalls[randomIndex] =
                temporary;
        }
    }

    private void RemoveDestroyedBallReferences()
    {
        for (int i = balls.Count - 1;
             i >= 0;
             i--)
        {
            if (balls[i] != null)
            {
                continue;
            }

            balls.RemoveAt(
                i
            );
        }
    }

    private string CreateBallObjectName(
        BallDefinition definition,
        int ballNumber)
    {
        ballNumber =
            Mathf.Max(
                ballNumber,
                1
            );

        if (definition == null ||
            string.IsNullOrWhiteSpace(
                definition.BallId
            ))
        {
            return
                $"Ball_{ballNumber}";
        }

        return
            $"Ball_{ballNumber}_" +
            $"{definition.BallId}";
    }

    private int ResolveBallDisplayNumber(
        Ball targetBall)
    {
        int index =
            balls.IndexOf(
                targetBall
            );

        return index >= 0
            ? index + 1
            : 1;
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
        RemoveDestroyedBallReferences();

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

        RemoveDestroyedBallReferences();

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
