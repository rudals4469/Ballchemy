using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BallCollection))]
public sealed class BallTurnQueueController :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BallCollection ballCollection;

    [Header("Shuffle")]
    [Tooltip(
        "매 턴 시작 전에 보유 공의 " +
        "발사 순서를 무작위로 섞습니다."
    )]
    [SerializeField]
    private bool shuffleEveryTurn = true;

    [Tooltip(
        "셔플 결과가 직전 턴과 완전히 같으면 " +
        "순서를 한 칸 회전시킵니다."
    )]
    [SerializeField]
    private bool avoidIdenticalTurnOrder = true;

    [Header("Debug")]
    [SerializeField]
    private bool showDebugLog = true;

    private readonly List<Ball>
        preparedQueue =
            new List<Ball>();

    private readonly List<Ball>
        previousTurnQueue =
            new List<Ball>();

    private readonly List<Ball>
        debugUpcomingBuffer =
            new List<Ball>();

    private int nextLaunchIndex;
    private int activeLaunchCount;

    private bool isTurnActive;
    private bool isQueuePrepared;
    private bool isQueueDirty;

    public bool IsTurnActive =>
        isTurnActive;

    public bool IsQueuePrepared =>
        isQueuePrepared;

    public int PreparedBallCount =>
        preparedQueue.Count;

    public int ActiveLaunchCount =>
        isTurnActive
            ? activeLaunchCount
            : preparedQueue.Count;

    public int NextLaunchIndex =>
        nextLaunchIndex;

    public int RemainingBallCount
    {
        get
        {
            int queueEndIndex =
                isTurnActive
                    ? activeLaunchCount
                    : preparedQueue.Count;

            int queueStartIndex =
                isTurnActive
                    ? nextLaunchIndex
                    : 0;

            return Mathf.Max(
                queueEndIndex -
                queueStartIndex,
                0
            );
        }
    }

    public Ball NextBall =>
        GetUpcomingBall(
            0
        );

    public IReadOnlyList<Ball>
        PreparedQueue =>
            preparedQueue;

    public event Action
        QueueChanged;

    public event Action
        TurnQueuePrepared;

    public event Action<Ball, int>
        BallLaunchedFromQueue;

    private void Awake()
    {
        FindReferences();
        ValidateReferences();
    }

    private void OnEnable()
    {
        FindReferences();
        SubscribeEvents();

        if (ballCollection != null &&
            ballCollection.IsInitialized)
        {
            EnsureQueuePrepared();
        }
    }

    private void Start()
    {
        EnsureQueuePrepared();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void OnValidate()
    {
        FindReferences();
    }

    private void FindReferences()
    {
        if (ballCollection == null)
        {
            ballCollection =
                GetComponent<
                    BallCollection
                >();
        }
    }

    private void ValidateReferences()
    {
        if (ballCollection == null)
        {
            Debug.LogError(
                "BallTurnQueueController: " +
                "BallCollection을 찾지 못했습니다.",
                this
            );
        }
    }

    private void SubscribeEvents()
    {
        UnsubscribeEvents();

        if (ballCollection == null)
        {
            return;
        }

        ballCollection.BallCountChanged +=
            HandleBallCountChanged;
    }

    private void UnsubscribeEvents()
    {
        if (ballCollection == null)
        {
            return;
        }

        ballCollection.BallCountChanged -=
            HandleBallCountChanged;
    }

    private void HandleBallCountChanged(
        int currentBallCount)
    {
        isQueueDirty = true;

        /*
         * 공격 중에는 현재 턴 큐를 유지합니다.
         * 공격이 끝난 뒤 다음 턴 큐에 새 공을 반영합니다.
         */
        if (isTurnActive)
        {
            return;
        }

        PrepareTurnQueue();
    }

    public void EnsureQueuePrepared()
    {
        if (isTurnActive)
        {
            return;
        }

        if (isQueuePrepared &&
            !isQueueDirty)
        {
            return;
        }

        PrepareTurnQueue();
    }

    public bool PrepareTurnQueue()
    {
        if (isTurnActive ||
            ballCollection == null ||
            !ballCollection.IsInitialized)
        {
            return false;
        }

        SavePreviousTurnQueue();

        preparedQueue.Clear();

        List<Ball> collectionSnapshot =
            ballCollection.CreateSnapshot();

        for (int i = 0;
             i < collectionSnapshot.Count;
             i++)
        {
            Ball ball =
                collectionSnapshot[i];

            if (ball == null)
            {
                continue;
            }

            preparedQueue.Add(
                ball
            );
        }

        if (shuffleEveryTurn)
        {
            ShuffleQueue(
                preparedQueue
            );
        }

        if (avoidIdenticalTurnOrder &&
            IsIdenticalToPreviousQueue())
        {
            RotateQueueLeftByOne();
        }

        nextLaunchIndex = 0;
        activeLaunchCount =
            preparedQueue.Count;

        isTurnActive = false;
        isQueuePrepared = true;
        isQueueDirty = false;

        TurnQueuePrepared?.Invoke();
        QueueChanged?.Invoke();

        if (showDebugLog)
        {
            Debug.Log(
                "BallTurnQueueController: " +
                $"다음 턴 공 {preparedQueue.Count}개 " +
                $"발사 순서 준비 완료\n" +
                CreateDebugQueueText(
                    preparedQueue,
                    10
                ),
                this
            );
        }

        return preparedQueue.Count > 0;
    }

    private void SavePreviousTurnQueue()
    {
        previousTurnQueue.Clear();

        if (!isQueuePrepared)
        {
            return;
        }

        for (int i = 0;
             i < preparedQueue.Count;
             i++)
        {
            Ball ball =
                preparedQueue[i];

            if (ball == null)
            {
                continue;
            }

            previousTurnQueue.Add(
                ball
            );
        }
    }

    private static void ShuffleQueue(
        List<Ball> queue)
    {
        if (queue == null ||
            queue.Count <= 1)
        {
            return;
        }

        for (int i = queue.Count - 1;
             i > 0;
             i--)
        {
            int randomIndex =
                UnityEngine.Random.Range(
                    0,
                    i + 1
                );

            Ball temporaryBall =
                queue[i];

            queue[i] =
                queue[randomIndex];

            queue[randomIndex] =
                temporaryBall;
        }
    }

    private bool IsIdenticalToPreviousQueue()
    {
        if (preparedQueue.Count <= 1 ||
            preparedQueue.Count !=
            previousTurnQueue.Count)
        {
            return false;
        }

        for (int i = 0;
             i < preparedQueue.Count;
             i++)
        {
            if (preparedQueue[i] !=
                previousTurnQueue[i])
            {
                return false;
            }
        }

        return true;
    }

    private void RotateQueueLeftByOne()
    {
        if (preparedQueue.Count <= 1)
        {
            return;
        }

        Ball firstBall =
            preparedQueue[0];

        preparedQueue.RemoveAt(
            0
        );

        preparedQueue.Add(
            firstBall
        );
    }

    public List<Ball> CreateLaunchSnapshot(
        int maximumBallCount)
    {
        EnsureQueuePrepared();

        List<Ball> launchSnapshot =
            new List<Ball>();

        int resolvedCount =
            Mathf.Clamp(
                maximumBallCount,
                0,
                preparedQueue.Count
            );

        for (int i = 0;
             i < resolvedCount;
             i++)
        {
            Ball ball =
                preparedQueue[i];

            if (ball == null)
            {
                continue;
            }

            launchSnapshot.Add(
                ball
            );
        }

        return launchSnapshot;
    }

    public bool BeginLaunch(
        int plannedLaunchCount)
    {
        AugmentCombatModifiers.BeginTurn();
        NamedCoreBehavior.BeginPlayerTurn();

        EnsureQueuePrepared();

        if (!isQueuePrepared ||
            preparedQueue.Count <= 0)
        {
            return false;
        }

        activeLaunchCount =
            Mathf.Clamp(
                plannedLaunchCount,
                0,
                preparedQueue.Count
            );

        nextLaunchIndex = 0;

        isTurnActive =
            activeLaunchCount > 0;

        QueueChanged?.Invoke();

        if (showDebugLog)
        {
            Debug.Log(
                "BallTurnQueueController: " +
                $"현재 턴 발사 큐 시작, " +
                $"발사 예정={activeLaunchCount}\n" +
                CreateDebugQueueText(
                    preparedQueue,
                    Mathf.Min(
                        activeLaunchCount,
                        10
                    )
                ),
                this
            );
        }

        return isTurnActive;
    }

    public void NotifyBallLaunched(
        Ball launchedBall)
    {
        if (!isTurnActive ||
            launchedBall == null ||
            nextLaunchIndex >=
            activeLaunchCount)
        {
            return;
        }

        int launchedQueueIndex =
            FindBallIndexFromCurrentPosition(
                launchedBall
            );

        if (launchedQueueIndex < 0)
        {
            if (showDebugLog)
            {
                Debug.LogWarning(
                    "BallTurnQueueController: " +
                    $"{launchedBall.name}을 현재 발사 큐에서 " +
                    "찾지 못했습니다.",
                    launchedBall
                );
            }

            return;
        }

        nextLaunchIndex =
            launchedQueueIndex + 1;

        AugmentCombatModifiers.NotifyBallLaunched(
            launchedBall);

        BallLaunchedFromQueue?.Invoke(
            launchedBall,
            launchedQueueIndex
        );

        QueueChanged?.Invoke();

        if (showDebugLog)
        {
            CopyUpcomingBalls(
                debugUpcomingBuffer,
                5
            );

            Debug.Log(
                "BallTurnQueueController: " +
                $"{launchedBall.name} 발사 완료, " +
                $"남은 공={RemainingBallCount}\n" +
                CreateDebugQueueText(
                    debugUpcomingBuffer,
                    debugUpcomingBuffer.Count
                ),
                launchedBall
            );
        }
    }

    private int FindBallIndexFromCurrentPosition(
        Ball targetBall)
    {
        int maximumIndex =
            Mathf.Min(
                activeLaunchCount,
                preparedQueue.Count
            );

        for (int i = nextLaunchIndex;
             i < maximumIndex;
             i++)
        {
            if (preparedQueue[i] ==
                targetBall)
            {
                return i;
            }
        }

        return -1;
    }

    public Ball GetUpcomingBall(
        int offset)
    {
        if (offset < 0 ||
            !isQueuePrepared)
        {
            return null;
        }

        int startingIndex =
            isTurnActive
                ? nextLaunchIndex
                : 0;

        int endingIndex =
            isTurnActive
                ? activeLaunchCount
                : preparedQueue.Count;

        int targetIndex =
            startingIndex +
            offset;

        if (targetIndex < 0 ||
            targetIndex >= endingIndex ||
            targetIndex >=
            preparedQueue.Count)
        {
            return null;
        }

        return preparedQueue[
            targetIndex
        ];
    }

    public void CopyUpcomingBalls(
        List<Ball> destination,
        int maximumCount)
    {
        if (destination == null)
        {
            return;
        }

        destination.Clear();

        if (!isQueuePrepared ||
            maximumCount <= 0)
        {
            return;
        }

        int startingIndex =
            isTurnActive
                ? nextLaunchIndex
                : 0;

        int endingIndex =
            isTurnActive
                ? activeLaunchCount
                : preparedQueue.Count;

        for (int i = startingIndex;
             i < endingIndex;
             i++)
        {
            if (destination.Count >=
                maximumCount)
            {
                break;
            }

            if (i < 0 ||
                i >= preparedQueue.Count)
            {
                continue;
            }

            Ball ball =
                preparedQueue[i];

            if (ball == null)
            {
                continue;
            }

            destination.Add(
                ball
            );
        }
    }

    public void CompleteTurnAndPrepareNextQueue()
    {
        isTurnActive = false;
        isQueuePrepared = false;

        nextLaunchIndex = 0;
        activeLaunchCount = 0;

        PrepareTurnQueue();
    }

    public void InvalidateQueue()
    {
        if (isTurnActive)
        {
            isQueueDirty = true;
            return;
        }

        isQueuePrepared = false;
        isQueueDirty = true;

        EnsureQueuePrepared();
    }

    private string CreateDebugQueueText(
        IReadOnlyList<Ball> queue,
        int maximumCount)
    {
        if (queue == null ||
            queue.Count <= 0 ||
            maximumCount <= 0)
        {
            return "예정 공 없음";
        }

        int resolvedCount =
            Mathf.Min(
                queue.Count,
                maximumCount
            );

        string result =
            "발사 순서: ";

        for (int i = 0;
             i < resolvedCount;
             i++)
        {
            Ball ball =
                queue[i];

            if (i > 0)
            {
                result += " → ";
            }

            result +=
                ball != null
                    ? ball.name
                    : "Null";
        }

        if (queue.Count >
            resolvedCount)
        {
            result +=
                $" → ... (+{queue.Count - resolvedCount})";
        }

        return result;
    }
}
